# Bespoke Sewing Studio — production deployment на netcup

Актуальная production-схема для проекта:

```text
User
  -> Cloudflare HTTPS, proxied / orange cloud, Full strict
  -> Caddy on netcup
  -> Docker network web
  -> bespoke-studio-app ASP.NET Core / Kestrel
  -> bespoke-studio-postgres PostgreSQL 18
  -> bespoke-studio-clamav
```

Старый deployment через home-server, `192.168.2.202`, Nginx на домашнем сервере, Cloudflare Tunnel, `cloudflared`, systemd service `bespoke-studio`, `/var/www/bespoke-studio`, `/var/lib/bespoke-studio` и `/etc/bespoke-studio/bespoke-studio.env` считается deprecated. Его можно использовать только как источник данных для миграции базы, uploads и Data Protection keys.

Подробный netcup runbook находится в [`DEPLOY_NETCUP_RU.md`](DEPLOY_NETCUP_RU.md). Этот файл является коротким production hub и фиксирует только поддержанную целевую схему.

## Целевой сервер

- Provider: netcup
- Server: RS 2000 G12
- Hostname: `prod01`
- OS: Ubuntu 24.04.4 LTS
- IPv4: `159.195.196.104`
- Domains: `oksanalogosha.com`, `www.oksanalogosha.com`
- App port binding: `127.0.0.1:5030 -> bespoke-studio-app:8080`
- Caddy upstream: `bespoke-studio-app:8080` через external Docker network `web`

Cloudflare DNS:

```text
oksanalogosha.com   A      159.195.196.104   Proxied / orange cloud
www                 CNAME  oksanalogosha.com Proxied / orange cloud
```

Cloudflare SSL/TLS mode: `Full (strict)`.

## Репозиторийные артефакты

- [`docker-compose.production.yml`](docker-compose.production.yml) — production compose без секретов.
- [`scripts/production/netcup-build-release.ps1`](scripts/production/netcup-build-release.ps1) — локальная сборка release artifact на Windows.
- [`scripts/production/netcup-deploy-release.ps1`](scripts/production/netcup-deploy-release.ps1) — загрузка release на netcup и запуск compose.
- [`scripts/production/netcup-backup.sh`](scripts/production/netcup-backup.sh) — server-side backup текущего netcup состояния.
- [`scripts/production/netcup-check.sh`](scripts/production/netcup-check.sh) — server-side/public smoke checks.
- [`scripts/production/netcup-caddy.example.caddy`](scripts/production/netcup-caddy.example.caddy) — Caddy snippet без фактического secret health token.
- [`scripts/production/netcup-restore-from-home-server.md`](scripts/production/netcup-restore-from-home-server.md) — перенос production DB/uploads/keys со старого home-server.

## Секреты

Не хранить в Git, документации, deploy scripts или чате:

- `BESPOKE_STUDIO_DB_PASSWORD`;
- `BESPOKE_STUDIO_JWT_SIGNING_KEY`;
- Gmail App Password;
- Cloudflare API tokens;
- TLS private keys / Cloudflare Origin Certificate private key;
- фактический secret health URL.

На сервере production `.env` создаётся вручную в `/opt/apps/projects/bespoke-studio/.env`. Документация должна показывать только имена переменных, но не реальные значения.

SMTP/Gmail App Password настраивается только через Admin -> Settings. При переносе production DB SMTP settings должны приехать вместе с таблицами Site Settings / Email delivery settings; пароль приложения не запрашивается в чате и не записывается в файлы проекта.

## Перед deploy

1. Проверить `git status`.
2. Собрать release через `scripts/production/netcup-build-release.ps1`.
3. Проверить, что `publish/netcup/app/wwwroot` содержит frontend `dist`.
4. Подготовить `/opt/apps/projects/bespoke-studio/.env` на сервере без вывода секретов в консоль.
5. Убедиться, что Caddy container подключён к Docker network `web`.
6. Сделать backup текущих netcup configs/db/uploads/keys/current release.
7. Только после backup выполнять deploy.

## После deploy

Минимальные проверки:

```bash
curl -I https://oksanalogosha.com/
curl -I https://www.oksanalogosha.com/
curl -I https://oksanalogosha.com/health
curl -i "$(cat /opt/apps/caddy/oksanalogosha-health-url.txt)"
curl -i -H "Host: oksanalogosha.com" -H "X-Forwarded-Proto: https" -H "X-Forwarded-Host: oksanalogosha.com" http://127.0.0.1:5030/api/version
```

Ожидаемо:

- `https://oksanalogosha.com/` -> `200`;
- `https://www.oksanalogosha.com/` -> `200` или `301`, в зависимости от Caddy policy;
- public `/health` -> `404`;
- secret health URL -> `200 Healthy`/healthy JSON;
- `server: cloudflare`;
- app container доступен только через `127.0.0.1:5030` и Docker network.
