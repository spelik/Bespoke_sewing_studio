# DEPLOYMENT NOTES - Bespoke Sewing Studio

## React SPA и fallback

Проект использует `React Router` c history-based routing.

На production-хостинге прямые заходы на маршруты вроде `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/admin` и `404`-маршруты должны отдавать `index.html`, чтобы роутинг обрабатывался на стороне frontend.

Если сервер не настроен на SPA fallback, прямой переход или перезагрузка страницы по вложенному URL может вернуть серверный `404`, даже если frontend-маршрут существует.

## Reverse proxy / HTTPS

Полный production runbook по reverse proxy и HTTPS (Cloudflare, forwarded headers,
HSTS, WebSockets, health checks, smoke test, troubleshooting) —
[`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md).

За reverse proxy важно:

- SPA fallback нужен и за reverse proxy (см. секции ниже);
- `/api`, `/health`, `/healthz`, `/readyz`, `/hubs` должны проксироваться в backend,
  а не отдавать `index.html`;
- `/admin` — frontend route (отдаётся SPA), но backend `/api/admin...` остаётся API;
- WebSocket upgrade нужен для SignalR (`/hubs/admin-notifications`).

## Nginx

Для SPA fallback нужен `try_files`:

```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

## IIS

Для IIS нужен `URL Rewrite` rule, который перенаправляет неизвестные frontend-маршруты на `index.html`.

Смысл правила:

- если запрошенный путь не является реальным файлом
- и не является реальной директорией
- отдавать `/index.html`

## Static hosting

Для статического хостинга нужна поддержка `SPA fallback` / `history fallback`.

Хостинг должен:

- отдавать реальные ассеты как обычно
- отдавать `index.html` для неизвестных frontend routes

## Что не делает этот файл

- не настраивает Nginx или IIS автоматически
- не добавляет backend
- не меняет mock/prototype mode
- не заменяет серверную конфигурацию production-окружения
