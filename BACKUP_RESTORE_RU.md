# Backup and restore — Bespoke Sewing Studio

Этот документ описывает ручной backup/restore для PostgreSQL и локального storage сайта **Bespoke Sewing Studio**.

Цель: перед production-обновлениями и аварийными работами иметь понятную процедуру, чтобы не потерять:

- orders/enquiries;
- contact messages;
- admin users and roles;
- site/settings/brand/SEO/CMS content;
- customer confirmation templates and email delivery settings;
- human-readable request references;
- admin audit log;
- metadata по загруженным файлам;
- сами загруженные файлы из `backend/storage`.

> Важно: PostgreSQL backup не содержит физические файлы из `backend/storage`. Для полноценного восстановления всегда нужен backup **и базы**, и storage.

## Что входит в backup

Минимальный полный backup должен включать:

1. PostgreSQL dump в custom format (`pg_dump --format=custom`).
2. Архив `backend/storage` или production uploads/object-storage snapshot.
3. Production Data Protection keys — обязательная часть полного backup/restore, если используются protected values (owner-managed Gmail SMTP App Password, 2FA challenge cookie). Без этих keys DB restore приведёт к нечитаемому protected Google App Password (owner-managed Gmail SMTP упадёт в logging fallback), а 2FA challenge cookie перестанет приниматься. При потере keys нужно re-enter/rotate protected secrets (например, заново сохранить Gmail App Password в Admin). Keys backup не хранить без защиты и не коммитить. Подробности и smoke test — в [`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md) и [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md).
4. Информацию о версии приложения: Git commit/tag, дата, применённые migrations.
5. Production конфигурацию окружения без публикации секретов в Git.

SMTP/Gmail secrets (Gmail App Password, SMTP password/username) не должны попадать в Git или в backups без защиты; храните их в secret store/зашифрованном хранилище. После restore обязательно отправьте test email из Admin → Settings → Email delivery и проверьте Email Log.

Не хранить backups внутри репозитория и не коммитить:

- `*.dump`, `*.sql`, `*.backup`, `*.bak`;
- архивы storage/uploads;
- копии `.env`, `appsettings.Production.json`, secrets, Google App Password, SMTP password;
- screenshots с credentials.

Backup содержит персональные данные клиентов и администраторов. Храни его в защищённом месте, лучше с шифрованием и ограниченным доступом.

## Production final pass

Этот раздел — итоговый production-обзор перед публичным запуском: что именно
бэкапить, как классифицировать, как проверять backup, как проводить restore
rehearsal и rollback. Практические PowerShell/bash команды остаются в разделах
ниже.

### A. Production backup inventory

Полный production backup состоит из нескольких независимых частей. Одного DB dump
недостаточно.

| # | Что бэкапить | Где хранится | В Git? |
| - | ------------ | ------------ | ------ |
| 1 | PostgreSQL database dump (`pg_dump --format=custom`) | защищённое backup-хранилище вне Git | нет |
| 2 | Uploads storage root целиком (`backend/storage` / production uploads) | защищённое backup-хранилище вне Git | нет |
| 3 | ASP.NET Core Data Protection keys folder | защищённое backup-хранилище вне Git | нет |
| 4 | Production environment variables / secret store metadata (какие ключи существуют, где хранятся) | secret store / защищённая заметка | нет |
| 5 | SMTP / Gmail provider settings (без raw secrets) | secret store | нет |
| 6 | Reverse proxy / TLS operational config (Nginx/IIS/Caddy) | защищённое хранилище конфигов | нет |
| 7 | TLS certificates / private keys, Cloudflare Origin Cert private key | только защищённый backup | нет |
| 8 | Git commit SHA / release version | текст рядом с backup | commit — да, backup metadata — нет |
| 9 | Applied migrations list | текст рядом с backup | нет |
| 10 | Runbooks / checklists version (эти `.md` файлы) | Git repository | да |

Ключевые взаимозависимости:

- DB dump сам по себе недостаточен — без uploads пропадут физические файлы вложений;
- uploads без DB тоже недостаточны — пропадут metadata, orders, settings, users;
- DB restore без Data Protection keys может сделать protected Gmail App Password
  нечитаемым (owner-managed Gmail SMTP уйдёт в logging fallback), а 2FA challenge
  cookie перестанет приниматься;
- reverse proxy / TLS config нужен для полного service restore, но хранится
  отдельно и защищённо, не в Git.

### B. Backup classification

Разделяй, что и как хранится:

- **repository state** — Git commit/tag (код, миграции как файлы, runbooks). Git
  сам по себе **НЕ** является backup runtime state;
- **runtime state** — PostgreSQL database, uploads storage, Data Protection keys.
  Это то, что нельзя восстановить из Git;
- **secrets / config** — environment variables, SMTP/Gmail credentials, TLS
  private keys, Cloudflare tokens. Хранятся в secret store, не в Git;
- **operational docs / runbooks** — эти `.md` файлы, версионируются в Git.

### C. Production backup cadence / retention

Рекомендации (без автоматизации, выбор владельца):

- daily backup DB + uploads для маленького production сайта;
- хранить минимум 7–14 daily copies;
- weekly/monthly copy, если позволяет место;
- перед каждым migration/deploy — всегда manual backup;
- держать хотя бы одну offsite/encrypted copy;
- retention policy выбирает владелец;
- backups содержат персональные данные — защищать и удалять по policy (GDPR-style).

### D. Consistent (согласованный) backup

Чтобы DB и uploads не рассинхронизировались:

- на маленьком сайте самый безопасный вариант — кратко остановить backend или
  включить maintenance window на время согласованного backup DB + uploads;
- если backend не останавливается, возможна рассинхронизация DB metadata и
  physical files (запись между dump БД и архивом storage);
- перед deploy/migration лучше остановить write operations;
- для production restore использовать **matching набор**: DB dump + uploads archive
  + Data Protection keys одного момента времени.

### E. Backup verification

После каждого важного backup:

- `pg_restore --list` по dump (структура читается, dump не пустой/не битый);
- проверить размер dump и uploads archive (не подозрительно малый);
- проверить, что archive открывается (`tar -tzf` / `Expand-Archive` в temp);
- проверить наличие Data Protection key files в защищённом backup;
- проверить, что рядом с backup записан Git commit SHA;
- проверить, что secrets/backup files не попали в Git;
- периодически выполнять restore rehearsal (см. F) на отдельном test/staging.

### F. Restore rehearsal / disaster recovery drill

Хотя бы один раз перед launch и затем периодически провести полную репетицию
восстановления **не на production**:

1. подготовить временное/staging окружение (не production);
2. восстановить DB dump;
3. восстановить uploads storage;
4. восстановить Data Protection keys;
5. восстановить env vars / secrets из secret store;
6. применить/проверить migrations;
7. запустить backend и frontend;
8. выполнить post-restore smoke test (см. G);
9. записать результат: дата, backup source, Git commit, длительность, проблемы.

Важно во время rehearsal:

- restore rehearsal не должен отправлять реальные письма клиентам без контроля;
- test email только на controlled address;
- не использовать production DNS / Cloudflare без понимания последствий
  (можно перехватить трафик или сломать production).

### G. Post-restore smoke test

Consolidated checklist после restore:

- backend health: `/health/live`, `/health/ready`, `/healthz`, `/readyz`, `/api/version`;
- Admin login;
- 2FA flow, если включена;
- refresh session after page reload;
- active sessions list;
- orders list;
- contact messages;
- existing attachment download;
- Storage Maintenance scan (Admin → Storage);
- clean upload test;
- delete attachment test на тестовом файле;
- owner-managed Gmail SMTP test email, если настроен (controlled address);
- Email Log / outbox statuses;
- public Contact form;
- public Order form;
- frontend public pages (Home/Services/Portfolio/About/Contact/Privacy);
- `/robots.txt` и `/sitemap.xml`;
- HTTPS / reverse proxy health, если restore делает полный service restore
  (см. [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md)).

### H. Rollback plan before migration/deploy

Перед каждым deploy/migration:

- сделать backup DB + uploads + Data Protection keys + config snapshot;
- записать текущий Git commit;
- знать предыдущий release artifact или commit для отката;
- знать точные restore-команды заранее;
- определить decision point: при каком симптоме откатываемся;
- не запускать destructive restore без last-minute emergency backup текущего
  состояния.

### I. Security / privacy notes

- backups содержат персональные данные (orders, contacts, users);
- шифровать backups at rest;
- ограничить доступ к backup-хранилищу;
- не заливать backups в случайные cloud-папки без защиты;
- не прикреплять backups к чату/email;
- не коммитить backups в Git;
- не хранить EICAR test files в backup/repo;
- защищать TLS private keys и Data Protection keys.

Future automation (encryption/offsite upload, automated restore test, backup
monitoring, object storage / CDN backup flow) — см. раздел
[«Что пока не автоматизировано»](#что-пока-не-автоматизировано) в конце документа.

## Draft backup automation script

Reference/draft PowerShell script:

- [`scripts/production/Backup-Production.ps1`](../scripts/production/Backup-Production.ps1)
- docs: [`scripts/production/README_RU.md`](../scripts/production/README_RU.md)

Script автоматизирует часть production backup steps:

- PostgreSQL custom dump (`postgresql.dump`);
- optional uploads archive (`uploads.zip`);
- optional Data Protection keys archive (`data-protection-keys.zip`);
- `backup-metadata.json` с timestamp / Git commit / script version;
- verification через `pg_restore --list` → `postgresql.dump.list.txt`.

Важно:

- `-BackupRoot` должен быть **вне Git repository** (script проверяет это);
- password/secrets **не передавать в Git** и не передавать password parameter в script;
- PostgreSQL credentials — через `PGPASSWORD`, `.pgpass` или secret store;
- сначала всегда запускать `-DryRun`;
- перед production run определить maintenance window / write strategy;
- после script выполнить verification checklist и restore rehearsal из этого runbook;
- backup encryption / offsite upload остаются operator responsibility / future task.

## Когда делать backup

Обязательно делать backup:

- перед production deploy;
- перед `dotnet ef database update` на production;
- перед массовым импортом/изменением CMS-контента;
- перед изменениями upload/storage конфигурации;
- перед переносом сайта на другой сервер;
- перед ручными SQL-операциями;
- регулярно по расписанию после запуска production.

Рекомендация для маленького сайта: ежедневный backup базы + storage, хранение минимум 7–14 последних копий и отдельная еженедельная копия.

## Windows development backup через Docker Compose

Команды ниже рассчитаны на локальный dev setup из `docker-compose.postgres.yml`:

- database: `bespoke_studio_dev`;
- user: `bespoke_user`;
- PostgreSQL container service: `postgres`;
- host port: `5433`.

Из корня проекта:

```powershell
cd C:\Projects\Bespoke_sewing_studio
```

Останови backend на время backup, чтобы получить согласованную пару database + storage:

```powershell
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
```

Убедись, что PostgreSQL запущен:

```powershell
docker compose -f docker-compose.postgres.yml up -d
docker compose -f docker-compose.postgres.yml ps
```

Создай папку backup вне репозитория:

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = "C:\Backups\BespokeStudio\$stamp"
New-Item -ItemType Directory -Force $backupRoot | Out-Null
```

Создай PostgreSQL dump внутри container и скопируй его на host. Такой вариант не использует PowerShell binary redirection и безопаснее для custom dump:

```powershell
docker compose -f docker-compose.postgres.yml exec -T postgres pg_dump -U bespoke_user -d bespoke_studio_dev --format=custom --file=/tmp/bespoke_studio_dev.dump
docker compose -f docker-compose.postgres.yml cp postgres:/tmp/bespoke_studio_dev.dump "$backupRoot\bespoke_studio_dev.dump"
docker compose -f docker-compose.postgres.yml exec -T postgres rm -f /tmp/bespoke_studio_dev.dump
```

Заархивируй local storage, если он есть:

```powershell
if (Test-Path .\backend\storage) {
    Compress-Archive -Path .\backend\storage -DestinationPath "$backupRoot\backend-storage.zip" -Force
} else {
    "backend/storage was not present at backup time." | Set-Content "$backupRoot\NO_BACKEND_STORAGE.txt"
}
```

Сохрани список applied migrations:

```powershell
docker compose -f docker-compose.postgres.yml exec -T postgres psql -U bespoke_user -d bespoke_studio_dev -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";' | Out-File "$backupRoot\applied-migrations.txt" -Encoding utf8
```

Сохрани Git commit:

```powershell
git rev-parse HEAD | Out-File "$backupRoot\git-commit.txt" -Encoding utf8
```

Проверь, что backup-файлы созданы:

```powershell
Get-ChildItem $backupRoot
```

## Проверка dev backup

Быстрая проверка структуры dump:

```powershell
docker compose -f docker-compose.postgres.yml cp "$backupRoot\bespoke_studio_dev.dump" postgres:/tmp/verify-bespoke.dump
docker compose -f docker-compose.postgres.yml exec -T postgres pg_restore --list /tmp/verify-bespoke.dump
docker compose -f docker-compose.postgres.yml exec -T postgres rm -f /tmp/verify-bespoke.dump
```

Проверка `pg_restore --list` не заменяет test restore, но помогает быстро поймать повреждённый или пустой dump.

## Windows development restore через Docker Compose

Restore удаляет текущую dev database. Перед выполнением убедись, что выбран правильный backup.

Останови backend:

```powershell
cd C:\Projects\Bespoke_sewing_studio
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
```

Укажи папку backup:

```powershell
$backupRoot = "C:\Backups\BespokeStudio\YYYYMMDD-HHMMSS"
```

Запусти PostgreSQL:

```powershell
docker compose -f docker-compose.postgres.yml up -d
```

Пересоздай database и восстанови dump:

```powershell
docker compose -f docker-compose.postgres.yml exec -T postgres dropdb -U bespoke_user --if-exists bespoke_studio_dev
docker compose -f docker-compose.postgres.yml exec -T postgres createdb -U bespoke_user bespoke_studio_dev
docker compose -f docker-compose.postgres.yml cp "$backupRoot\bespoke_studio_dev.dump" postgres:/tmp/bespoke_studio_dev.dump
docker compose -f docker-compose.postgres.yml exec -T postgres pg_restore -U bespoke_user -d bespoke_studio_dev --clean --if-exists --no-owner /tmp/bespoke_studio_dev.dump
docker compose -f docker-compose.postgres.yml exec -T postgres rm -f /tmp/bespoke_studio_dev.dump
```

Восстанови storage. Сначала сохрани текущую папку, если она есть:

```powershell
$restoreStamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (Test-Path .\backend\storage) {
    Rename-Item .\backend\storage "storage-before-restore-$restoreStamp"
}

if (Test-Path "$backupRoot\backend-storage.zip") {
    Expand-Archive -Path "$backupRoot\backend-storage.zip" -DestinationPath .\backend -Force
}
```

Проверь database:

```powershell
docker compose -f docker-compose.postgres.yml exec -T postgres psql -U bespoke_user -d bespoke_studio_dev -c 'SELECT COUNT(*) AS orders_count FROM "Orders";'
docker compose -f docker-compose.postgres.yml exec -T postgres psql -U bespoke_user -d bespoke_studio_dev -c 'SELECT COUNT(*) AS contact_messages_count FROM "ContactMessages";'
docker compose -f docker-compose.postgres.yml exec -T postgres psql -U bespoke_user -d bespoke_studio_dev -c 'SELECT COUNT(*) AS audit_log_count FROM "AdminAuditLogEntries";'
```

После restore запусти backend и frontend, затем проверь Admin login, Orders, Contact Messages, Settings, Users, My account и Audit Log.

## Production backup — Docker Compose PostgreSQL на Linux

Если production PostgreSQL тоже работает через Docker Compose, принцип такой же. Пути и compose file могут отличаться.

Пример с placeholders:

```bash
cd /opt/bespoke-studio
STAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_ROOT=/var/backups/bespoke-studio/$STAMP
sudo install -d -m 700 "$BACKUP_ROOT"
```

Желательно временно остановить приложение или включить maintenance mode, чтобы database и uploads были согласованы:

```bash
sudo systemctl stop bespoke-studio-api
```

Database dump:

```bash
docker compose -f docker-compose.postgres.yml exec -T postgres pg_dump -U bespoke_user -d bespoke_studio_dev --format=custom --file=/tmp/bespoke_studio.dump
docker compose -f docker-compose.postgres.yml cp postgres:/tmp/bespoke_studio.dump "$BACKUP_ROOT/bespoke_studio.dump"
docker compose -f docker-compose.postgres.yml exec -T postgres rm -f /tmp/bespoke_studio.dump
```

Storage archive, если production использует local storage:

```bash
tar -czf "$BACKUP_ROOT/backend-storage.tar.gz" -C /opt/bespoke-studio/backend storage
```

Git/app version:

```bash
git rev-parse HEAD > "$BACKUP_ROOT/git-commit.txt"
```

Перезапусти приложение:

```bash
sudo systemctl start bespoke-studio-api
```

## Production backup — PostgreSQL без Docker

Если PostgreSQL установлен как обычный service, используй `pg_dump` с production connection parameters. Не вписывай пароль в команду и не сохраняй его в shell history; используй `.pgpass`, environment variables из secret store или интерактивный ввод.

```bash
STAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_ROOT=/var/backups/bespoke-studio/$STAMP
sudo install -d -m 700 "$BACKUP_ROOT"

pg_dump \
  --host 127.0.0.1 \
  --port 5432 \
  --username bespoke_user \
  --dbname bespoke_studio \
  --format custom \
  --file "$BACKUP_ROOT/bespoke_studio.dump"
```

Storage path depends on production deployment:

```bash
tar -czf "$BACKUP_ROOT/backend-storage.tar.gz" -C /var/www/bespoke-studio/backend storage
```

## Production restore checklist

1. Confirm the target server and database name.
2. Stop the API or enable maintenance mode.
3. Save a last-minute emergency backup of the current database/storage, if possible.
4. Restore database dump with `pg_restore`.
5. Restore uploads/storage from the matching archive.
6. Restore/preserve ASP.NET Core Data Protection keys if protected settings are used.
7. Restore environment variables/secrets from the secret store, not from Git.
8. Start PostgreSQL and the API.
9. Apply newer migrations only if restoring an older dump into newer application code.
10. Check Admin login, Orders, Contact Messages, uploaded attachments, Settings, Users, My account, Audit Log and public forms.
11. Uploads storage is mandatory for a complete restore: run Admin → Storage → Maintenance scan, confirm existing attachments download, do one clean test upload and verify the delete-attachment flow. If the ClamAV/scanner config is restored incorrectly, uploads may be rejected or recorded as `ScanFailed`. See [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md); do not store EICAR test files in the backup or repository.
12. Verify Data Protection: confirm the restored keys folder is in place, send a test email from Admin Settings if owner-managed Gmail SMTP is configured (the protected App Password must stay decryptable), and run the 2FA challenge flow if 2FA is enabled. If keys were lost, re-enter/rotate the Gmail App Password in Admin. See [`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md).
13. Record the restore date, backup source and Git commit used.

Example Docker Compose restore on Linux:

```bash
cd /opt/bespoke-studio
BACKUP_ROOT=/var/backups/bespoke-studio/YYYYMMDD-HHMMSS

sudo systemctl stop bespoke-studio-api

docker compose -f docker-compose.postgres.yml exec -T postgres dropdb -U bespoke_user --if-exists bespoke_studio_dev
docker compose -f docker-compose.postgres.yml exec -T postgres createdb -U bespoke_user bespoke_studio_dev
docker compose -f docker-compose.postgres.yml cp "$BACKUP_ROOT/bespoke_studio.dump" postgres:/tmp/bespoke_studio.dump
docker compose -f docker-compose.postgres.yml exec -T postgres pg_restore -U bespoke_user -d bespoke_studio_dev --clean --if-exists --no-owner /tmp/bespoke_studio.dump
docker compose -f docker-compose.postgres.yml exec -T postgres rm -f /tmp/bespoke_studio.dump

sudo rm -rf /opt/bespoke-studio/backend/storage
sudo tar -xzf "$BACKUP_ROOT/backend-storage.tar.gz" -C /opt/bespoke-studio/backend

sudo systemctl start bespoke-studio-api
```

## Перед production deploy

Перед каждым production deploy:

- убедиться, что Git working tree чистый;
- записать текущий Git commit production;
- сделать PostgreSQL dump;
- сделать backup uploads/storage;
- сохранить applied migrations list;
- проверить, что dump открывается через `pg_restore --list`;
- убедиться, что Data Protection keys не потеряются при redeploy;
- убедиться, что SMTP/App Password secrets не лежат в Git;
- иметь rollback plan: предыдущий build + database/storage backup;
- reverse proxy / TLS config и сертификаты (Nginx/IIS/Caddy config, Let's Encrypt/Cloudflare Origin Certificate) — это operational secrets/config: их нужно бэкапить защищённо вне Git; TLS private keys и Cloudflare origin cert private keys не хранить без защиты (см. [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md)).

После deploy:

- применить EF migrations только после backup;
- проверить `/api/health`, `/api/version`, Swagger или API availability;
- проверить public Home/Services/Portfolio/Order/Contact;
- проверить Admin login;
- проверить Orders, Contact Messages, Users, My account, Audit Log;
- проверить загрузку/скачивание attachment;
- проверить test email и одну реальную Contact/Order отправку, если SMTP включён;
- после restore/redeploy проверить HTTPS, health endpoints, auth/session, SignalR realtime и uploads.

## Что пока не автоматизировано

Draft/reference backup script добавлен (Task 79):

- [`scripts/production/Backup-Production.ps1`](../scripts/production/Backup-Production.ps1)
- [`scripts/production/README_RU.md`](../scripts/production/README_RU.md)

Script помогает с PostgreSQL dump, uploads/Data Protection keys archives, metadata,
`pg_restore --list` verification, dry-run и retention prune — но **не заменяет**
operator decisions и full runbook.

На текущем этапе по-прежнему вручную / future tasks:

- backup encryption script;
- remote/offsite backup upload automation;
- automated restore test;
- backup monitoring/alerting;
- production object storage/CDN backup flow;
- hardened Linux systemd timer / cron packaging (если потребуется).

Эти пункты можно добавить отдельной задачей после выбора production-хостинга и storage-подхода.
