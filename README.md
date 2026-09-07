# Kinopoisk — микросервисный клон Кинопоиска

Самописный кино-каталог с метаданными TMDB, семантическим поиском (pgvector + embeddings),
социальными функциями, стримингом через Jellyfin и Blazor Server фронтендом.

## Архитектура

```
                          ┌─────────────┐
                          │   Browser   │
                          └──────┬──────┘
                    ┌────────────┴────────────┐
                    │                         │
            localhost:5044              localhost:5000
              blazor-web               api-gateway (YARP)
           (SSR + прокси                    │
            картинок/видео)     ┌────────────┼────────────┬──────────────┐
                    │           │            │            │              │
              identity:5001 jellyfin:5002 catalog:5003 admin:5009   internal
              (auth/JWT)  (прокси HLS) (каталог)   (торренты)
                    │            │            │              │
                    └────────────┴───── postgres:5432 ──────┴──────────────┘
                                     (pgvector/pg16, mydb)

TMDB + image.tmdb.org ←── socks5://vless-proxy:1080 (sing-box, см. «Egress»)
```

Единственные публичные входы: `blazor-web :5044` (UI) и `api-gateway :5000` (API).
Внутренний трафик между сервисами идёт напрямую по именам Docker-сети, мимо прокси
(в коде `UseProxy=false` + `NO_PROXY`).

## Сервисы

| Сервис | Порт | Стек | Назначение |
|---|---|---|---|
| `blazor-web` | 5044 | .NET 9, Blazor Server, MudBlazor | SSR-фронт; проксирует картинки (`/api/catalog/poster`, `/avatar`) и видео (`/api/jellyfin/Media/...`) same-origin, чтобы браузеру не нужен был доступ во внутреннюю сеть |
| `api-gateway` | 5000 | .NET, YARP | Единая точка входа API; JWT-валидация + сверка `tokenVersion` с Identity; прокидывает `X-User-Id`/`X-User-Role` downstream |
| `identity-service` | 5001 | .NET 10, EF + Postgres | Регистрация/логин (BCrypt), JWT 15 мин + refresh 7 суток, профили, аватары, роли (`0=admin`), `TokenVersion` для инвалидации |
| `catalog-service` | 5003 | .NET 10, EF + pgvector | Ядро: read-through каталог TMDB→Postgres, pgvector-поиск/рекомендации/настроение/поиск по картинке, плейлисты, дневник, отзывы, друзья, лента, уведомления, файлы фильмов |
| `jellyfin-service` | 5002 | .NET 10 | Тонкий прокси HLS (`hls/{id}/{**}`) и прямого стрима (`stream/{id}`) к Jellyfin, проброс `Range` |
| `admin-service` | 5009 | .NET 9 | Торренты через Transmission, сверка файлов с Jellyfin-библиотекой (`/media`), скан `POST /api/admin/movies/scan` |
| `cache-image-service` | 5007 | .NET 9 | Файловый кэш постеров/аватаров (`D:/KinopoiskCacheImages`), отдача `File(bytes)` |
| `embedding-service` | 5004 | .NET 10 | Текст→вектор (Ollama `nomic-embed-text`, 768d), картинка/текст→вектор (oCLIP `CLIP-ViT-B-32`, 512d) |
| `DataParserToDB` | — | .NET 9, console | DDL-утилита: `EnsureCreated` схемы БД. Импорта данных сейчас не делает |

Инфраструктура: `postgres` (pgvector/pg16, `:54320` наружу), `jellyfin :8096`,
`transmission :9091`, `ollama` + `oclip :11435` (GPU), `vless-proxy :1080`, `vpn` + `vpn-proxy`.

## Egress: VLESS + OpenVPN failover

Внешний трафик (TMDB API, `image.tmdb.org`) нужен только `catalog-service` и
`cache-image-service` — оба смотрят в `socks5://vless-proxy:1080`.

- `vless-proxy` (sing-box): SOCKS `:1080`. Селектор `auto` (`urltest`, healthcheck
  раз в минуту на `api.themoviedb.org/3/configuration`) выбирает живой egress:
  `vless-out` (основной) → `gluetun-out` = `http://vpn:8888` (запасной).
- `vpn` (свой образ `vpn-client/`: alpine + OpenVPN 2.7): поднимает туннель из
  `vpn/VPNTYPE-AMS5.ovpn` (credentials inline в `<auth-user-pass>`, в compose
  секретов нет). Healthcheck — наличие `tun0`.
- `vpn-proxy` (`gost -L http://:8888`, `network_mode: service:vpn`): HTTP-прокси,
  чей исходящий трафик идёт через туннель.

Правила эксплуатации:

- Положили `.ovpn` в `vpn/` (имя файла = `VPNTYPE-AMS5.ovpn`, см. volume в compose).
  Каталог `vpn/` в `.gitignore` — секреты не коммитить.
- Пересоздали `vpn` → **обязательно пересоздать и `vpn-proxy`**
  (`up -d --force-recreate vpn-proxy`): он привязан к netns контейнера `vpn`.
- Проверка: `docker logs vpn` → `Initialization Sequence Completed`;
  `docker logs vless-proxy` → задержки обоих egress; TMDB через цепочку:
  `curl -x socks5://localhost:1080 'https://api.themoviedb.org/3/configuration?api_key=...'`.
- Известно: VLESS-сервер `tor4.vpntype.dev` сейчас мёртв (таймауты) — весь внешний
  трафик идёт через OpenVPN. Это штатный режим failover, не авария.

## Ключевые механики

### Обновление данных фильма (TMDB → Postgres)
- Первое открытие фильма: miss в БД → `GetMovieFullDetailsAsync` → `AddMovie` → возврат.
- Дальше запись **заморожена**, автообновлений не было — добавлены:
  - `MovieEntity.RefreshedAt` (колонка создаётся идемпотентно при старте CatalogService,
    миграций нет — схема живёт через `EnsureCreated`);
  - `POST /api/catalog/movies/{id}/refresh` — полный upsert: скаляры, коллекция,
    M2M (жанры/компании/страны/языки/keywords с ru-названиями), замена 1-N
    (cast/crew/videos/alt-titles/release-dates/images), докэширование постеров,
    пересчёт эмбеддинга, проверка `new_in_collection`. Локальные отзывы/статусы не трогает.
    404 — нет нигде, 503 — TMDB недоступен;
  - stale-TTL в `GET /movies/{id}`: `RefreshedAt` пуст / старше 30 дней / обновление
    было до даты релиза → молча refresh с fallback на кэш;
  - кнопка «Обновить из TMDB» на карточке фильма в `/admin/movies` (только админ).
- Пользовательская `MoviePage` кнопки не имеет сознательно (см. «Доступ»).

### Картинки через Blazor
`<img>` исполняет браузер, docker-DNS (`api-gateway:5000`) ему недоступен — поэтому
DTO отдают относительные `/api/catalog/poster/...`, а `blazor-web` проксирует их
(и `/api/catalog/avatar/{guid}`) внутрь сети через `HttpClient("catalog")`.
Аватары резолвятся через `Services/AvatarHelper.Resolve` (`/api/...` как есть,
чужие TMDB-аватары → инициалы).

### Видео через Blazor
Плеер (`hls.js`) ходит относительным `/api/jellyfin/Media/hls/{id}/master.m3u8` —
`blazor-web` стрим-проксирует его и `/api/jellyfin/Media/stream/{id}` на gateway
(`HttpClient("media")`, без таймаута, с пробросом query/`Range`/статуса/headers,
без буферизации). Один origin сохраняется для будущего nginx+ddns.

### Доступ: админ ≠ пользователь
- `MainLayout` принудительно редиректит любой `IsAdmin` вне `/admin/` → `/admin/movies`.
  Это by design, не баг: у админа своя админка (`AdminLayout`: `/admin/movies`, `/admin/users`).
- JWT: `Issuer=IdentityService`, `Audience=JellyfinService`, роль в клейме (`0=admin`).
  Gateway сверяет `tokenVersion` с Identity (кэш 5 мин) — после `deactivate/setRole`
  старые токены умирают.

## Запуск

Требования: Docker Desktop (WSL2-бэкенд, `/dev/net/tun` для `vpn`), `D:/Jellyfin`,
`D:/Postgres`, `D:/KinopoiskCacheImages`, файл `vpn/VPNTYPE-AMS5.ovpn`, GPU для `oclip`
(опционально).

```powershell
docker compose up -d --build        # всё
docker compose up -d --build catalog-service blazor-web   # точечно
docker compose logs -f vpn vless-proxy                    # egress
```

Blazor: `http://localhost:5044`, API: `http://localhost:5000`, Postgres: `localhost:54320`.

## Структура репозитория

```
docker-compose.yml          # все сервисы
sing-box-config.json        # socks-in + vless-out/gluetun-out + urltest
vpn/                        # (gitignored) VPNTYPE-AMS5.ovpn
vpn-client/                 # Dockerfile + entrypoint для OpenVPN-туннеля
Services/
  ApiGateway/  IdentityService/  CatalogService/  JellyfinService/
  AdminService/  CacheImageService/  EmbeddingService/
  BlazorServerRenderKinopoisk/  DataParserToDB/
```

## Troubleshooting

| Симптом | Причина / лечение |
|---|---|
| `AUTH_FAILED` в `docker logs vpn` | Неверные credentials в инлайн-блоке `.ovpn`; заменить файл |
| gluetun-стиль `host is not an IP address` | Не используется; свой `vpn-client` резолвит hostname штатно. Не возвращать gluetun без `vpn-dns`-костыля |
| `vless-proxy` после рестарта `vpn` ходит в мёртвый egress | Пересоздать `vpn-proxy` (netns), подождать ~1 мин `urltest` |
| `404 /api/jellyfin/Media/hls/...` из Blazor | Упал прокси-маппинг в `Blazor/Program.cs`; проверить `MapGet` |
| Фильм с устаревшими данными (импорт до премьеры) | Открыть страницу (сработает stale-refresh) или кнопка в `/admin/movies` |
| Админ «не может» открыть `/movie/{id}` | Так задумано (редирект в `MainLayout`); метаданные правятся из `/admin/movies` |
| Blazor «виснет» на JS-исключении без падения | Circuit жив, рестарт контейнера не поможет — смотреть DevTools/browser-консоль, чинить JS-интероп |
