# Production reverse proxy / HTTPS — runbook (Bespoke Sewing Studio)

Production domain: `https://oksanalogosha.com`

Этот документ описывает, как безопасно развернуть backend за reverse proxy и
Cloudflare с HTTPS, и как это проверить. Все значения ниже — только
placeholder-примеры. Реальные IP, пути, TLS private keys, Cloudflare Origin
Certificate private keys и API tokens в Git не добавляются. Snippets нельзя
копировать без адаптации под конкретный сервер.

## A. Краткое решение по архитектуре

- Production domain: `https://oksanalogosha.com`.
- Canonical origin: apex без `www`.
- `www.oksanalogosha.com` редиректит на `https://oksanalogosha.com`.
- Cloudflare управляет DNS / proxy / TLS на edge.
- На сервере перед Kestrel стоит reverse proxy.
- Kestrel не принимает публичный интернет-трафик напрямую.
- Backend уже обрабатывает `X-Forwarded-For`, `X-Forwarded-Proto`,
  `X-Forwarded-Host`, но только от trusted proxy.
- Точные `ForwardedHeaders__KnownProxies` / `KnownNetworks` задаются на deployment,
  не в Git.

## B. Что НЕ хранить в Git

- TLS private keys;
- Cloudflare Origin Certificate private key;
- Cloudflare API tokens;
- реальные public/private IP сервера, если они раскрывают инфраструктуру;
- `.env`, `.env.local`, `.env.production`;
- `appsettings.Production.json`, `appsettings.Staging.json`;
- скриншоты с секретами;
- полный production Nginx/IIS/Caddy config с приватными путями/секретами;
- backup-архивы с сертификатами/секретами без защиты.

## C. Recommended deployment topology

Логическая схема:

```
Internet → Cloudflare (DNS/proxy/TLS edge) → reverse proxy on server → Kestrel backend
```

- frontend SPA assets отдаются static host / reverse proxy;
- API-запросы (`/api/*`, `/health/*`, `/healthz`, `/readyz`, `/hubs/*`) проксируются
  в Kestrel;
- frontend SPA и backend API могут быть на одном origin `https://oksanalogosha.com`;
- если API вынести на отдельный subdomain, нужно отдельно настроить CORS и
  `VITE_API_BASE_URL`;
- пока предпочтителен one-origin deployment (проще: меньше проблем с CORS/cookie).

## D. Cloudflare checklist

Без реальных account secrets.

- DNS `A`/`AAAA` или `CNAME` для `oksanalogosha.com` на сервер/hosting;
- `www` → apex redirect;
- SSL/TLS mode должен быть full end-to-end (Full / Full (strict)), **НЕ Flexible**
  (Flexible вызывает redirect loops и небезопасен: edge→origin по HTTP);
- origin должен иметь валидный сертификат: Let's Encrypt или Cloudflare Origin
  Certificate;
- Always Use HTTPS / HTTP→HTTPS redirect можно включить на Cloudflare или reverse
  proxy, но избегать двойного redirect loop;
- HSTS включать аккуратно, только после проверки стабильного HTTPS;
- при включённом Cloudflare proxy (orange cloud) backend всё равно должен доверять
  только ближайшему reverse proxy, который подключается к Kestrel;
- не вписывать Cloudflare IP ranges вручную как статичный список в docs; если
  reverse proxy подключается к Kestrel с localhost, доверять можно только
  localhost/адресу reverse proxy;
- Cloudflare Email Routing НЕ использовать как SMTP sender (см.
  [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md)).

## E. Reverse proxy requirements (Nginx/IIS/Caddy)

- TLS termination или pass-through по выбранной схеме;
- HTTP → HTTPS redirect;
- проксировать в Kestrel internal URL, placeholder `http://127.0.0.1:<kestrel-port>`;
- передавать заголовки: `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`,
  `Host`, а также upgrade headers для SignalR/WebSockets;
- поддержать WebSockets для `/hubs/admin-notifications`;
- не кэшировать admin/API private responses;
- не публиковать upload storage directory как static files (см.
  [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md));
- отдавать SPA fallback для frontend routes (см. `DEPLOYMENT_NOTES_RU.md`);
- не проксировать secrets/config files;
- ограничить body size согласованно с backend upload limit;
- access/error logs без токенов/cookies/request bodies.

## F. Backend environment variables / config examples

Только placeholder-значения:

```
ForwardedHeaders__ForwardLimit=1
ForwardedHeaders__KnownProxies__0=<reverse-proxy-ip-connected-to-kestrel>
# или вместо KnownProxies:
ForwardedHeaders__KnownNetworks__0=<trusted-proxy-cidr>
Cors__AllowedOrigins__0=https://oksanalogosha.com
```

Важно:

- `KnownProxies`/`KnownNetworks` должны описывать только proxy, который напрямую
  подключается к Kestrel; нельзя доверять всем forwarded headers из интернета;
- слишком широкий доверенный диапазон = security risk (spoofing client IP);
- если frontend и API на одном origin, CORS проще, но политика должна включать
  production origin, если браузер делает cross-origin вызовы;
- `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com`;
- `VITE_API_BASE_URL` должен соответствовать фактическому API origin/path;
- если API same-origin, убедиться, что frontend build НЕ указывает на `localhost`
  (иначе mixed content / broken API в production).

## G. Cookies / auth / HTTPS

- admin refresh cookie — `HttpOnly`;
- production должен быть HTTPS-only;
- проверить `Secure` / `SameSite` behavior;
- не смешивать `localhost`, `127.0.0.1` и production domain;
- за reverse proxy `X-Forwarded-Proto=https` должен восстанавливать scheme, иначе
  cookie/redirect/HSTS/links могут работать неправильно;
- проверить login, 2FA, refresh after page reload, logout, revoke session.

## H. Security headers / CSP / HSTS

- backend отдаёт baseline API security headers (`X-Content-Type-Options`,
  `Referrer-Policy`, `X-Frame-Options`, `Permissions-Policy`,
  `Content-Security-Policy`);
- HSTS (`Strict-Transport-Security`) включён outside Development, вместе с HTTPS
  redirection;
- document CSP для SPA HTML задаёт frontend host/reverse proxy, потому что backend
  не отдаёт SPA HTML;
- проверять `Strict-Transport-Security` только после стабильной HTTPS настройки;
- проверить `Content-Security-Policy`, `X-Frame-Options`, `Referrer-Policy`,
  `X-Content-Type-Options`, `Permissions-Policy`;
- если SignalR идёт через WebSocket, frontend CSP должен разрешать `wss:` / нужный
  origin в `connect-src`.

## I. Health checks behind proxy

- liveness: `/health/live`, `/healthz` (также `/health` и `/api/health`);
- readiness: `/health/ready`, `/readyz` (зависит от PostgreSQL);
- `/api/version`;
- reverse proxy / load balancer должны использовать liveness/readiness по
  назначению;
- health responses не раскрывают secrets;
- проверить эти endpoints и через public HTTPS, и напрямую с сервера.

## J. Example snippets (ТОЛЬКО placeholders — адаптировать под сервер)

Nginx (иллюстративно, заменить домены/пути/порт):

```nginx
# HTTP → HTTPS
server {
    listen 80;
    server_name oksanalogosha.com www.oksanalogosha.com;
    return 301 https://oksanalogosha.com$request_uri;
}

server {
    listen 443 ssl;
    server_name oksanalogosha.com;

    # Заменить на реальные пути (в Git не хранить):
    ssl_certificate     <path-to-fullchain-cert>;
    ssl_certificate_key <path-to-cert-key>;

    client_max_body_size 6m;  # согласовать с UploadStorage MaxFileSizeBytes

    # API / health / SignalR → Kestrel
    location ~ ^/(api|health|healthz|readyz|hubs)/ {
        proxy_pass http://127.0.0.1:<kestrel-port>;
        proxy_set_header Host              $host;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host  $host;
        proxy_http_version 1.1;
        proxy_set_header Upgrade    $http_upgrade;   # WebSocket для SignalR
        proxy_set_header Connection "upgrade";
    }

    # SPA fallback для frontend routes
    location / {
        root <path-to-spa-dist>;
        try_files $uri $uri/ /index.html;
    }
}
```

Caddy (иллюстративно):

```caddy
oksanalogosha.com {
    @api path /api/* /health/* /healthz /readyz /hubs/*
    reverse_proxy @api 127.0.0.1:<kestrel-port>

    root * <path-to-spa-dist>
    try_files {path} /index.html
    file_server
}

www.oksanalogosha.com {
    redir https://oksanalogosha.com{uri} permanent
}
```

IIS (checklist, без большого XML):

- reverse proxy через ARR + URL Rewrite на `http://127.0.0.1:<kestrel-port>`;
- включить forwarding заголовков `X-Forwarded-For`/`Proto`/`Host`;
- включить WebSocket protocol для SignalR;
- URL Rewrite для SPA fallback на `index.html`;
- `maxAllowedContentLength`/request limits согласовать с upload limit;
- HTTP→HTTPS redirect.

Не вставлять реальные private paths/certs. Snippets требуют адаптации.

## K. Production smoke test

- DNS резолвится;
- `https://oksanalogosha.com` открывается;
- `http://oksanalogosha.com` редиректит на HTTPS;
- `https://www.oksanalogosha.com` редиректит на apex;
- прямой reload `/services`, `/portfolio`, `/order`, `/admin` отдаёт SPA, не 404;
- `/robots.txt` и `/sitemap.xml` открываются;
- `/health/live`, `/health/ready`, `/api/version` работают через HTTPS;
- response headers содержат security headers;
- `Strict-Transport-Security` присутствует outside Development;
- admin login работает;
- 2FA flow работает, если включена;
- refresh after reload работает;
- SignalR admin realtime работает;
- public order/contact формы работают;
- clean upload работает;
- upload выше лимита отклоняется;
- logs не содержат cookies/tokens/request bodies/secrets;
- rate limit видит реальный client IP, а не только reverse proxy IP.

## L. Troubleshooting

- redirect loop из-за Cloudflare Flexible SSL → переключить на Full/Full (strict);
- `X-Forwarded-Proto` не передан → backend считает запрос HTTP (ломаются
  cookie/redirect/HSTS/links);
- `KnownProxies`/`KnownNetworks` не настроены → forwarded headers игнорируются;
- слишком широкий `KnownProxies`/`KnownNetworks` → security risk (spoofed IP);
- CORS error из-за неверного origin;
- refresh cookie не сохраняется/не отправляется (scheme/SameSite/Secure);
- SignalR/WebSocket не подключается (нет upgrade headers);
- direct route reload отдаёт 404 → нет SPA fallback;
- body size limit на proxy меньше backend upload limit;
- health ready `503` из-за PostgreSQL;
- HSTS включён слишком рано (до стабильного HTTPS);
- Cloudflare кэширует private API/admin responses;
- mixed content из-за `localhost` API URL в production build.

## M. Operational checklist

- DNS ready;
- HTTPS ready;
- Cloudflare SSL mode не Flexible;
- www → apex redirect;
- reverse proxy передаёт нужные заголовки;
- WebSocket upgrade включён;
- `ForwardedHeaders__KnownProxies`/`KnownNetworks` настроены точно;
- `Cors__AllowedOrigins__0=https://oksanalogosha.com`, если нужен cross-origin;
- `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com`;
- `VITE_API_BASE_URL` не `localhost`;
- HSTS проверен;
- security headers проверены;
- health endpoints проверены;
- admin auth/session/2FA проверены;
- order/contact/upload smoke tested;
- logs проверены (без секретов);
- в Git нет secrets/certs/private keys.
