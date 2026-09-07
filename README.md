# kinopoisk

Домашний кинотеатр в докере. Каталог тянет с TMDB, видео отдаёт Jellyfin,
морда — Blazor Server.

## Запуск

Нужны Docker, ключ TMDB (https://www.themoviedb.com/settings/api) и место на диске.
Без ключа каталог будет пустой, всё остальное заведётся.

```bash
git clone <repo> && cd kinopoisk
```

Дальше руками:

1. `sing-box-config.json` в корне — его нет в репозитории (там были секреты),
   соберите по примеру ниже. Это прокси для запросов к TMDB.
2. В `docker-compose.yml` вбить своё: `TMDB_API_KEY`, `Jellyfin__ApiKey`,
   пароль постгреса, `Jwt__Key` подлиннее, пути вместо `D:/...`.
3. Если есть свой `.ovpn` — кинуть в `vpn/` и проверить путь в volume сервиса `vpn`.
   Нету — не страшно, заведётся на одном VLESS (пока он жив).

```bash
docker compose up -d --build
```

UI — http://localhost:5044, API — http://localhost:5000.
Первый пользователь — через `/register`. Админа себе выдать можно только
напрямую в базе: таблица `Users`, поле `Role`, поставить `0`, перелогиниться.

## Что внутри

- `blazor-web` (5044) — фронт. Заодно проксирует картинки и видео, чтобы браузер
  ходил в один origin и не знал про внутреннюю сеть докера.
- `api-gateway` (5000) — YARP, вход в API. Проверяет JWT, дальше прокидывает
  `X-User-Id`. Без токена пускает только логин/регистрацию, постеры и стрим.
- `identity-service` (5001) — регистрация, логин, JWT на 15 минут + refresh на 7 дней.
- `catalog-service` (5003) — всё остальное: каталог, поиск, рекомендации на pgvector,
  отзывы, плейлисты, друзья и т.д.
- `jellyfin-service` (5002) — прокси HLS/стрима к Jellyfin.
- `admin-service` (5009) — торренты через Transmission, скан `/media`.
- `cache-image-service` (5007) — кэш постеров на диске.
- `embedding-service` (5004) — вектора: текст через Ollama, картинки через oCLIP.
- Плюс `postgres` (pgvector), сам `jellyfin`, `transmission`, `ollama`, `oclip`.

Про `DataParserToDB` — это просто создание схемы БД (`EnsureCreated`), не парсер,
название историческое. Миграций в проекте нет.

## Наружу

TMDB и картинки ходят через `socks5://vless-proxy:1080`, больше наружу никто не лазит
(внутри сервисы общаются напрямую, в коде `UseProxy=false`).

`vless-proxy` — sing-box. Внутри `urltest`: раз в минуту проверяет TMDB
и сам выбирает через что идти — `vless-out` или запасной `http://vpn:8888`.
Пример `sing-box-config.json`:

```json
{
  "inbounds": [{ "type": "socks", "tag": "socks-in", "listen": "0.0.0.0", "listen_port": 1080 }],
  "outbounds": [
    { "type": "vless", "tag": "vless-out" },
    { "type": "http", "tag": "gluetun-out", "server": "vpn", "server_port": 8888 },
    {
      "type": "urltest", "tag": "auto",
      "outbounds": ["vless-out", "gluetun-out"],
      "url": "https://api.themoviedb.org/3/configuration?api_key=ВАШ_КЛЮЧ",
      "interval": "1m", "tolerance": 50
    }
  ],
  "route": { "final": "auto" }
}
```

Запасной egress — связка `vpn` + `vpn-proxy`:

- `vpn` — свой образ `vpn-client/` (alpine + openvpn). Конфиг монтируется
  из `vpn/`, логин/пароль — инлайн-блоком прямо в `.ovpn`, в compose секретов нет.
- `vpn-proxy` — `gost` с `network_mode: service:vpn`, слушает 8888.
  Его исходящий трафик идёт через туннель.

## Косяки

- Пересоздали `vpn` — пересоздайте и `vpn-proxy` (`--force-recreate`).
  Он живёт в чужом netns и после пересоздания смотрит в пустоту, молча.
