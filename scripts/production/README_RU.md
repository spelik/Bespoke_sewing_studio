# Production backup script (draft)

## A. Назначение

`Backup-Production.ps1` — **draft/reference** PowerShell script для production backup проекта Bespoke Sewing Studio.

Он **не заменяет** [`BACKUP_RESTORE_RU.md`](../../BACKUP_RESTORE_RU.md), а автоматизирует часть шагов из runbook: PostgreSQL dump, optional uploads/Data Protection keys archives, metadata и `pg_restore --list` verification.

Script требует **осознанной настройки operator-ом** перед реальным production использованием.

## B. Что script бэкапит

- PostgreSQL custom dump → `postgresql.dump`
- uploads archive → `uploads.zip` (если указан `-UploadsPath` и не `-SkipUploads`)
- Data Protection keys archive → `data-protection-keys.zip` (если указан `-DataProtectionKeysPath` и не `-SkipDataProtectionKeys`)
- metadata JSON → `backup-metadata.json`
- verification list → `postgresql.dump.list.txt` (если не `-SkipPgRestoreList`)

Каждый backup создаётся в отдельной папке:

`<BackupRoot>\yyyyMMdd-HHmmss\`

## C. Что script НЕ делает

- не хранит и не читает secrets из Git;
- не создаёт `.env` / production appsettings;
- не шифрует backup автоматически;
- не загружает backup offsite;
- не выполняет restore;
- не включает maintenance mode;
- не останавливает backend;
- не настраивает cron / Windows Task Scheduler.

## D. Preconditions

- `pg_dump` доступен в `PATH`;
- `pg_restore` доступен в `PATH` для verification (или используйте `-SkipPgRestoreList`);
- PostgreSQL password доступен через secure mechanism (`PGPASSWORD`, `.pgpass`, Windows Credential Manager, secret store) — **не в Git**;
- `-BackupRoot` находится **вне Git repository**;
- operator определил maintenance window / стратегию остановки writes при необходимости;
- paths к uploads / Data Protection keys заданы осознанно.

## E. Example dry run

Сначала всегда запускайте dry-run. Dry-run **не требует** реального PostgreSQL connection и **не требует** существования placeholder paths.

```powershell
cd C:\Projects\Bespoke_sewing_studio

.\scripts\production\Backup-Production.ps1 `
  -BackupRoot "D:\ProtectedBackups\BespokeStudio" `
  -DatabaseHost "<postgres-host>" `
  -DatabasePort 5432 `
  -DatabaseName "<database-name>" `
  -DatabaseUser "<database-user>" `
  -UploadsPath "<production-uploads-root>" `
  -DataProtectionKeysPath "<data-protection-keys-path>" `
  -RetentionDays 14 `
  -DryRun
```

## F. Example real run

Password must come from a secure source, not from Git.

```powershell
# Example only — replace with your secret store workflow.
$env:PGPASSWORD = "<postgres-password-from-secret-store>"

.\scripts\production\Backup-Production.ps1 `
  -BackupRoot "D:\ProtectedBackups\BespokeStudio" `
  -DatabaseHost "<postgres-host>" `
  -DatabasePort 5432 `
  -DatabaseName "<database-name>" `
  -DatabaseUser "<database-user>" `
  -UploadsPath "<production-uploads-root>" `
  -DataProtectionKeysPath "<data-protection-keys-path>" `
  -RetentionDays 14 `
  -ApplyRetention

Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
```

После run:

- очистите текущую shell-сессию от secrets;
- предпочтительнее использовать `.pgpass` или secret store вместо inline `$env:PGPASSWORD`.

## G. Verification

После real run проверьте:

- `postgresql.dump` существует и размер > 0;
- `postgresql.dump.list.txt` существует и не пустой (если verification включена);
- `uploads.zip` — если ожидался;
- `data-protection-keys.zip` — если ожидался;
- `backup-metadata.json` содержит `gitCommit`, timestamp и included flags;
- периодически выполняйте restore rehearsal по [`BACKUP_RESTORE_RU.md`](../../BACKUP_RESTORE_RU.md).

## H. Retention

- `-RetentionDays N` задаёт возраст candidate folders;
- `-ApplyRetention` включает удаление старых timestamp folders в `-BackupRoot`;
- по умолчанию retention **выключен** (`RetentionDays=0`, без `-ApplyRetention`);
- удаляются только direct child directories с именем `yyyyMMdd-HHmmss`;
- текущий backup folder не удаляется;
- сначала выполните dry-run с `-ApplyRetention`, чтобы увидеть candidates.

Backups содержат персональные данные — храните их защищённо.

## I. Scheduling

Script можно запускать через Windows Task Scheduler / cron / systemd timer, но это **future/manual setup**.

Scheduled execution должна использовать secret store, а не hardcoded passwords в task definition.

## J. Security

- backups содержат персональные данные (orders, contacts, users);
- шифруйте backups at rest;
- ограничьте доступ к backup storage;
- делайте offsite copy по operator policy;
- не прикрепляйте backups к chat/email;
- не коммитьте backups в Git;
- Data Protection keys и TLS private keys — особо чувствительные; никогда не добавляйте их в repository.

Полный runbook: [`BACKUP_RESTORE_RU.md`](../../BACKUP_RESTORE_RU.md).

## K. Release/deploy scripts

Production release для netcup собирается через:

```powershell
.\scripts\production\netcup-build-release.ps1
```

Build script создаёт `publish/netcup/bespoke-studio-release.zip` как
Linux-compatible ZIP: entry names используют только `/`, без Windows `\`
separators. Script валидирует опубликованный `BespokeStudio.Api.dll`,
`wwwroot/index.html`, idempotent migration SQL и сам archive.

Deploy script:

```powershell
.\scripts\production\netcup-deploy-release.ps1 `
  -ReleaseArchive .\publish\netcup\bespoke-studio-release.zip
```

не выполняет production deploy без явного запуска operator-ом. При запуске он
валидирует local archive/migration SQL, загружает archive и SQL, проверяет archive
на сервере, распаковывает его в `current.new`, валидирует `current.new`, затем
применяет migration SQL и только после этого переключает `current`. Ошибка до
switch не трогает существующий `current`; ошибка после switch печатает rollback
path через `current.previous`.
