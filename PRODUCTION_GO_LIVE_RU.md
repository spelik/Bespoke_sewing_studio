# Production go-live runbook (Bespoke Sewing Studio)

Production domain: `https://oksanalogosha.com`

Короткий пошаговый документ «что делать в день запуска». Он НЕ заменяет подробные
runbooks, а ссылается на них. Секреты в Git не хранятся; все реальные значения
задаются в secret store / env на deployment.

## A. Назначение

- Production domain: `https://oksanalogosha.com`.
- Документ используется перед первым публичным запуском и перед крупным production
  redeploy.
- Подробные runbooks:
  - [`PRODUCTION_LAUNCH_CHECKLIST_RU.md`](PRODUCTION_LAUNCH_CHECKLIST_RU.md) — полный checklist;
  - [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md) — backup/restore, rehearsal, rollback;
  - [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md) — email delivery;
  - [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md) — uploads / ClamAV;
  - [`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md) — Data Protection keys;
  - [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md) — reverse proxy / HTTPS / Cloudflare;
  - [`DEPLOYMENT_NOTES_RU.md`](DEPLOYMENT_NOTES_RU.md) — SPA fallback / proxy routing.

## B. Go / No-Go критерии

Запуск разрешён только если выполнено всё:

- Git working tree clean;
- release commit известен и записан;
- GitHub Actions CI green;
- `npm.cmd run typecheck` green;
- `npm.cmd run build` green;
- `dotnet test backend\BespokeStudio.sln` green;
- `dotnet build backend\BespokeStudio.sln` green;
- production backup сделан и проверен (`pg_restore --list`);
- restore rehearsal выполнен хотя бы один раз;
- production env/secrets заданы вне Git;
- Data Protection keys persistent;
- ClamAV/scanner включён или принято осознанное решение не принимать real uploads до включения;
- SMTP strategy выбрана и test email проходит;
- HTTPS / reverse proxy работает;
- admin login / 2FA / session проверены;
- public Order / Contact / upload smoke tests проходят;
- нет production blockers.

Если хоть один пункт No-Go — запуск откладывается.

## C. Freeze перед запуском

- не принимать новые незапланированные code changes;
- не смешивать launch docs changes и runtime code changes в одном деплое;
- записать текущий commit SHA;
- проверить `git status` (clean);
- проверить, что в Git нет `.env*`, production appsettings, backup files, keys/certs;
- подготовить emergency rollback note (см. J).

## D. Pre-launch backup

- DB dump (`pg_dump --format=custom`);
- uploads storage root;
- Data Protection keys folder;
- secret store / env snapshot вне Git;
- reverse proxy / TLS config вне Git;
- проверить dump через `pg_restore --list`;
- записать backup timestamp/source;
- НЕ хранить backup files в repository.

Подробно: [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md).

## E. Production configuration checklist

- `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com`;
- `VITE_API_BASE_URL` не указывает на `localhost`;
- `Cors__AllowedOrigins__0=https://oksanalogosha.com`, если нужен cross-origin;
- `DataProtection__ApplicationName=BespokeSewingStudio` (стабильный, не менять);
- `DataProtection__KeysPath` — persistent absolute path вне repo/publish;
- `UploadStorage__RootPath` — production path (writable, не public static);
- `UploadSecurity__MalwareScanner__Provider` не `Disabled`, если принимаются реальные uploads;
- `UploadSecurity__MalwareScanner__TreatScannerErrorAsRejection=true` (fail-closed);
- SMTP provider выбран: developer-managed `Smtp` или owner-managed Gmail SMTP;
- `Notifications__Email__Provider=Logging` — не production email sender;
- `ForwardedHeaders__KnownProxies` / `KnownNetworks` — точные (только proxy к Kestrel);
- secrets не в Git.

## F. Deployment order

1. backup (см. D);
2. deploy backend artifact/config;
3. apply migrations (после backup);
4. start backend;
5. check backend health direct/internal (`/health/live`, `/health/ready`);
6. deploy frontend build с production env;
7. configure reverse proxy / HTTPS;
8. configure Cloudflare / DNS / www→apex redirect;
9. run public HTTPS smoke test (см. G);
10. enable real customer traffic.

## G. Final smoke test через public HTTPS

- `https://oksanalogosha.com` открывается;
- direct reload `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/terms`, `/admin` отдаёт SPA (не 404);
- `/robots.txt`;
- `/sitemap.xml`;
- `/health/live`;
- `/health/ready`;
- `/healthz`;
- `/readyz`;
- `/api/version`;
- security headers присутствуют;
- HSTS присутствует outside Development;
- admin login;
- 2FA, если включена;
- refresh session after reload;
- Active sessions;
- SignalR admin notifications realtime;
- public Contact form;
- public Order form;
- clean upload проходит;
- too-large upload отклоняется;
- EICAR controlled rejection только в controlled environment (не на реальных клиентах);
- owner notification email;
- customer confirmation email;
- Email Log / outbox statuses;
- Storage Maintenance scan;
- Audit Log пишется;
- в logs нет secrets/tokens/cookies/request bodies.

## H. SEO / legal smoke test

- canonical URLs используют `https://oksanalogosha.com`;
- sitemap содержит только публичные routes;
- admin routes отсутствуют в sitemap;
- `/admin` и `/admin/login` — `noindex, nofollow`;
- Open Graph / Twitter cards корректны;
- Privacy / Terms проверены владельцем;
- нет фиктивных address/hours/WhatsApp/geography.

## I. Go / No-Go decision log

```
Date/time:
Release commit:
Backup timestamp:
Restore rehearsal date:
Operator:
Go/No-Go:
Known risks:
Rollback point:
Notes:
```

## J. Rollback quick plan

- при необходимости прекратить приём новых writes (maintenance window);
- сохранить emergency backup текущего состояния перед destructive restore;
- восстановить предыдущий artifact/commit;
- восстановить DB / uploads / Data Protection keys только если нужно;
- проверить health / admin / public forms;
- записать incident notes.

Подробно: [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md).

## K. After launch monitoring

- проверять logs;
- Email Log / outbox: failed / retrying;
- disk space;
- рост upload storage;
- статус backup job / manual backup;
- health / readiness;
- rate limit / login failures;
- contact / order submissions;
- Cloudflare / reverse proxy errors.

## L. Known future improvements (не blockers)

- automated scheduled backups;
- backup encryption / offsite upload automation;
- PostgreSQL-backed integration tests;
- email manual retry button;
- email monitoring / alerting;
- object storage adapter;
- external Data Protection key store для multi-instance;
- CDN / cache strategy.
