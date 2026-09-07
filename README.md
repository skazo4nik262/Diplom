# kinopoisk

Домашний клон кинопоиска. Каталог на TMDB, стриминг через Jellyfin, фронт — Blazor Server.

## Запуск

Нужны: Docker Desktop (WSL2), папки `D:/Jellyfin`, `D:/Postgres`, `D:/KinopoiskCacheImages`,
файл `vpn/VPNTYPE-AMS5.ovpn` (иначе `vpn` не поднимется).

```powershell
docker compose up -d --build
```

- UI: http://localhost:5044
- API: http://localhost:5000
- Postgres: localhost:54320 (mydb / skazo4nik / qaz123wsx)

## Что где

| Сервис | Порт | За что отвечает |
|---|---|---|
| blazor-web | 5044 | Фронт. Заодно проксирует картинки и видео, чтобы браузер ходил в один origin |
| api-gateway | 5000 | YARP, единственный вход в API. Проверяет JWT, прокидывает `X-User-Id` дальше |
| identity-service | 5001 | Логин/регистрация, JWT (15 мин) + refresh (7 дней), роли (`0` — админ) |
| catalog-service | 5003 | Весь каталог: TMDB→Postgres, поиск, рекомендации на pgvector, отзывы, плейлисты и т.д. |
| jellyfin-service | 5002 | Прокси HLS/стрима к Jellyfin |
| admin-service | 5009 | Торренты через Transmission, скан `/media` |
| cache-image-service | 5007 | Кэш постеров на диске |
| embedding-service | 5004 | Эмбеддинги: текст — Ollama, картинки — oCLIP |
| DataParserToDB | — | Консольник, создаёт схему БД (`EnsureCreated`). Больше ничего не делает |

Инфра: `postgres` (pgvector), `jellyfin` (8096), `transmission` (9091), `ollama`, `oclip` (11435, нужен GPU).

## Наружу (важно)

TMDB и картинки тянут только `catalog-service` и `cache-image-service` через
`socks5://vless-proxy:1080`. Остальное наружу не ходит.

- `vless-proxy` (sing-box) — SOCKS на 1080. Внутри `urltest`: healthcheck TMDB раз в минуту,
  egress выбирается сам — `vless-out`, если мёртв — `gluetun-out` (`http://vpn:8888`).
- `vpn` — свой образ `vpn-client/` (alpine + openvpn 2.7). Конфиг берётся из
  `vpn/VPNTYPE-AMS5.ovpn` как есть, логин/пароль — инлайн-блок в нём же.
  В compose секретов нет, каталог `vpn/` в `.gitignore`.
- `vpn-proxy` — `gost` с `network_mode: service:vpn`, слушает 8888 внутри сети vpn.

Запомнить:

- VLESS (`tor4.vpntype.dev`) сейчас дохлый, всё идёт через OpenVPN. Это нормально.
- Пересоздал `vpn` — пересоздай и `vpn-proxy` (`--force-recreate`), иначе он висит
  на старом netns и прокси молча не работает.
- Проверка: `docker logs vpn` → `Initialization Sequence Completed`;
  `docker logs vless-proxy` → задержки обоих egress.

## Неочевидное

- Данные фильма в БД заморожены с момента первого импорта. Лечится само:
  stale-TTL (нет `RefreshedAt` / старше 30 дней / обновлено до релиза) дёргает refresh
  из TMDB при открытии страницы. Руками — `POST /movies/{id}/refresh` или кнопка
  «Обновить из TMDB» на карточке в `/admin/movies`. `RefreshedAt` добавляется сам
  при старте catalog-service, миграции не нужны.
- Админ не может открыть `/movie/{id}` — так задумано, `MainLayout` шлёт всех админов
  в `/admin/movies`. Метаданные правятся оттуда же.
- Картинки/видео фронт отдаёт относительными URL и проксирует сам (`Program.cs`,
  `MapGet`), потому что браузер docker-DNS не резолвит.
- JWT: issuer `IdentityService`, audience `JellyfinService`. После смены роли/бана
  старые токены дохнут по `tokenVersion` (сверка раз в 5 минут).
- Blazor иногда виснет на JS-исключении, контейнер при этом жив — рестарт не лечит,
  смотреть консоль браузера.

## Раскладка

```
docker-compose.yml   # всё
sing-box-config.json # socks-in, vless-out, gluetun-out, urltest
vpn/                 # VPNTYPE-AMS5.ovpn (не коммитить)
vpn-client/          # Dockerfile + entrypoint для туннеля
Services/...         # по папке на сервис, солюшены внутри
```
