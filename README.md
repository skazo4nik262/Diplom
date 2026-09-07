# kinopoisk

Домашний кинотеатр с каталогом: метаданные TMDB, семантический поиск,
стриминг через Jellyfin, фронт на Blazor Server. Всё крутится в Docker Compose.

## Возможности

- Каталог фильмов/сериалов с TMDB (постеры, актёры, трейлеры, коллекции)
- Поиск: текстовый, по фильтрам, по настроению, по картинке (CLIP), рекомендации (pgvector)
- Социальное: рейтинги, статусы, избранное, плейлисты, дневник просмотров, друзья, лента, отзывы
- Видео: HLS-стриминг из Jellyfin, автозагрузка торрентов через Transmission
- Автообновление данных фильма из TMDB (stale-TTL + кнопка в админке)

## Требования

- Docker Desktop (для `vpn` нужен `/dev/net/tun`, на Linux работает из коробки)
- TMDB API key — https://www.themoviedb.com/settings/api
- ~10 ГБ под образы + место под медиа и кэш
- Опционально: NVIDIA GPU (ускорение `oclip`), свой `.ovpn` для запасного egress

## Быстрый старт

```bash
git clone <repo> && cd kinopoisk
```

1. Создайте `sing-box-config.json` в корне (пример ниже) — egress для запросов к TMDB.
2. Положите свой OpenVPN-конфиг в `vpn/` как `vpn/<имя>.ovpn` и поправьте путь
   в volume сервиса `vpn` в `docker-compose.yml` (без VPN заведётся и так,
   внешний трафик пойдёт только через VLESS).
3. Отредактируйте `docker-compose.yml`: ключ `TMDB_API_KEY`, `Jellyfin__ApiKey`,
   пароль Postgres, `Jwt__Key`, пути volume (`D:/...` → свои).
4. Запуск:

```bash
docker compose up -d --build
```

- UI: http://localhost:5044 (первая страница — `/login`, создайте пользователя через `/register`;
  админа выдайте через `/admin/users` — см. ниже)
- API: http://localhost:5000
- Postgres: localhost:54320

Как сделать себя админом: в таблице Identity `Users` поставьте `Role = 0`
(роль хранится числом: `0` — админ, `1` — пользователь), перелогиньтесь.
Админка: `/admin/movies` (торренты, обновление метаданных), `/admin/users`.

## Конфигурация

### `sing-box-config.json` (не коммитится, см. `.gitignore`)

SOCKS `:1080` для всего внешнего трафика + `urltest`-failover между egress:

```json
{
  "log": { "level": "info" },
  "inbounds": [{ "type": "socks", "tag": "socks-in", "listen": "0.0.0.0", "listen_port": 1080 }],
  "outbounds": [
    { "type": "vless", "tag": "vless-out", "...": "ваши параметры VLESS" },
    { "type": "http", "tag": "gluetun-out", "server": "vpn", "server_port": 8888 },
    {
      "type": "urltest", "tag": "auto",
      "outbounds": ["vless-out", "gluetun-out"],
      "url": "https://api.themoviedb.org/3/configuration?api_key=ВАШ_TMDB_КЛЮЧ",
      "interval": "1m", "tolerance": 50
    }
  ],
  "route": { "final": "auto" }
}
```

### Переменные окружения (все — в `docker-compose.yml`)

| Переменная | Где | Зачем |
|---|---|---|
| `TMDB_API_KEY` | catalog-service | Ключ TMDB, без него каталог пустой |
| `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience` | gateway, identity, catalog, jellyfin | Должны совпадать везде; ключ — от 32 символов |
| `ConnectionStrings__*` | identity, catalog, jellyfin, admin | Строка Postgres (`Host=postgres;...` внутри сети) |
| `Jellyfin__Url` / `Jellyfin__ApiKey` | jellyfin-service, admin-service | URL и API-ключ Jellyfin (ключи — в панели Jellyfin) |
| `Transmission__*` | admin-service | URL/логин/пароль Transmission |
| `EmbeddingService__Url`, `CacheImageService__Url` | catalog-service | Внутренние URL, обычно менять не надо |
| `ApiClient__ApiUrl` | blazor-web | URL gateway, видимый **серверу** Blazor (в compose — `http://api-gateway:5000`) |
| `VPN_AUTH` / `.ovpn` | vpn | Файл — в `vpn/`, секреты — только в нём, каталог в `.gitignore` |

Volume из compose (`D:/Jellyfin`, `D:/Postgres`, `D:/KinopoiskCacheImages`) замените
на свои пути. Это единственное, что обязательно править под свою машину, кроме ключей.

## Как устроено

```
browser → blazor-web:5044 (UI, SSR)
browser → api-gateway:5000 → identity / catalog / jellyfin / admin
catalog → postgres (pgvector), TMDB (через vless-proxy), embedding-service, cache-image-service
admin → transmission, jellyfin, postgres
```

- **Каталог — read-through**: miss в Postgres → запрос в TMDB → запись в БД.
  Дальше запись не трогается, пока не протухнет: `RefreshedAt` пуст / старше 30 дней /
  обновление было до даты релиза → тихий refresh при открытии страницы.
  Руками: `POST /api/catalog/movies/{id}/refresh` или кнопка в `/admin/movies`.
- **Картинки и видео** фронт проксирует сам (`blazor-web`, `MapGet` в `Program.cs`),
  поэтому браузеру достаточно одного origin, docker-DNS ему не нужен.
- **Админ ≠ пользователь**: `MainLayout` шлёт всех админов в `/admin/movies`.
  Обычные страницы (`/movie/{id}` и др.) — только для пользователей.
- **БД без миграций**: схема создаётся через `EnsureCreated` (`DataParserToDB`,
  `RefreshedAt` дотягивается SQL при старте catalog-service).

## Раскладка

```
docker-compose.yml    # все сервисы и их env
sing-box-config.json  # egress (создать самому, не коммитить)
vpn/                  # ваш .ovpn (не коммитить)
vpn-client/           # Dockerfile туннеля (alpine + openvpn)
Services/
  ApiGateway/ IdentityService/ CatalogService/ JellyfinService/
  AdminService/ CacheImageService/ EmbeddingService/
  BlazorServerRenderKinopoisk/ DataParserToDB/
```

## Известные грабли

- Пересоздали `vpn` — пересоздайте и `vpn-proxy` (`--force-recreate`): он сидит
  в netns контейнера `vpn` и после пересоздания смотрит в пустоту.
