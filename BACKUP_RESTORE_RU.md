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
3. Production Data Protection keys, если используется owner-managed Gmail SMTP или другие protected values.
4. Информацию о версии приложения: Git commit/tag, дата, применённые migrations.
5. Production конфигурацию окружения без публикации секретов в Git.

Не хранить backups внутри репозитория и не коммитить:

- `*.dump`, `*.sql`, `*.backup`, `*.bak`;
- архивы storage/uploads;
- копии `.env`, `appsettings.Production.json`, secrets, Google App Password, SMTP password;
- screenshots с credentials.

Backup содержит персональные данные клиентов и администраторов. Храни его в защищённом месте, лучше с шифрованием и ограниченным доступом.

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
11. Send a test email from Admin Settings if SMTP is configured.
12. Record the restore date, backup source and Git commit used.

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
- иметь rollback plan: предыдущий build + database/storage backup.

После deploy:

- применить EF migrations только после backup;
- проверить `/api/health`, `/api/version`, Swagger или API availability;
- проверить public Home/Services/Portfolio/Order/Contact;
- проверить Admin login;
- проверить Orders, Contact Messages, Users, My account, Audit Log;
- проверить загрузку/скачивание attachment;
- проверить test email и одну реальную Contact/Order отправку, если SMTP включён.

## Что пока не автоматизировано

На текущем этапе это ручная процедура. В проекте пока нет:

- автоматического scheduled backup job;
- backup encryption script;
- remote/offsite backup upload;
- автоматической проверки restore;
- retention policy enforcement;
- production object storage/CDN backup flow.

Эти пункты можно добавить отдельной задачей после выбора production-хостинга и storage-подхода.
