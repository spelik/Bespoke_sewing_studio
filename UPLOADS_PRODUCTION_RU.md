# Production uploads / ClamAV — runbook (Bespoke Sewing Studio)

Production domain: `https://oksanalogosha.com`

Этот документ описывает, как подготовить и проверить production upload security
(storage + malware scanner) без хранения секретов и реальных файлов клиентов в
Git. Все значения ниже — только placeholder-примеры. Реальные пути, credentials,
tokens и uploaded files в репозиторий не добавляются.

## A. Краткое решение по архитектуре

- Текущий production-supported вариант: **local filesystem storage + ClamAV или
  другой command-line scanner**.
- Object storage adapter (S3/Azure/R2) пока не реализован — это future task.
  После Task 68 storage идёт через абстракцию `IUploadStorage`, но единственная
  реализация сейчас — `LocalUploadStorage`.
- Upload flow: **quarantine → extension/content-type/file-signature (magic bytes)
  validation → malware scan → final storage**.
- Если scanner clean — файл переносится в final storage; если infected / scan
  failed / rejected — upload отклоняется, quarantine/final leftovers удаляются, и
  запись не привязывается к order.
- Admin видит scan status в metadata attachment (Clean / Infected / ScanFailed /
  Skipped / Rejected).
- Не обещать пользователю «100% safe». Формулировка: «security scan completed».

## B. Что НЕ хранить в Git

- реальные uploads и архивы uploads;
- скриншоты с файлами клиентов;
- `.env`, `.env.local`, `.env.production`;
- `appsettings.Production.json`, `appsettings.Staging.json`;
- приватные server paths, раскрывающие инфраструктуру;
- secrets / tokens / passwords / API keys;
- backup dumps.

Каталог `backend/storage` уже исключён из Git через `.gitignore`.

## C. Production storage path

Production задаёт storage-параметры через environment variables / secret store /
серверный конфиг (не в Git). Пример (только placeholder-значения):

```
UploadStorage__RootPath=<absolute-production-upload-path>
UploadStorage__PublicBasePath=/api/uploads
UploadStorage__MaxFileSizeBytes=5242880
UploadStorage__MaxFilesPerRequest=5
UploadStorage__OrphanCleanupAgeMinutes=120
```

Важно:

- `RootPath` должен быть вне Git working tree и как минимум вне публикуемой
  frontend-папки;
- folder должен быть writable для пользователя, под которым работает backend
  process;
- storage folder **не должен** отдаваться как static public directory напрямую;
- доступ к файлам идёт только через backend API (authorization/validation), а не
  через прямую публикацию папки (файлы не обслуживаются из `wwwroot`);
- backup должен включать весь uploads storage целиком;
- в env vars двойное подчёркивание (`__`) соответствует `:` в конфиге
  (секция `UploadStorage`).

## D. Folder layout

Текущий layout внутри storage root:

- `quarantine/...`
- `order-attachments/yyyy/MM/...`
- `portfolio-images/yyyy/MM/...`
- `content-images/yyyy/MM/...`
- `brand-images/yyyy/MM/...`

Важно:

- `quarantine` находится в том же protected storage root;
- orphan cleanup и Admin Storage Maintenance scan сравнивают DB metadata
  (`UploadedFiles`) с physical files;
- storage keys остаются relative и safe; absolute server paths не попадают в API
  responses (в Admin scan невалидный ключ показывается как
  `invalid-storage-key/...`).

## E. ClamAV installation / configuration

Практический checklist. Конкретные команды адаптируйте под свою ОС/дистрибутив.

### Linux / Ubuntu (пример)

- установить `clamav` и `clamav-freshclam`;
- обновить virus definitions через `freshclam` (или службу `clamav-freshclam`);
- проверить `clamscan --version`;
- убедиться, что пользователь backend process может запускать `clamscan`;
- убедиться, что scanner может читать файлы в quarantine (права на storage root).

### Windows (пример)

- установить ClamAV for Windows (или другой command-line scanner);
- указать absolute path к `clamscan.exe` в `ExecutablePath`;
- проверить запуск из PowerShell от пользователя, под которым работает
  backend / IIS app pool identity;
- убедиться, что этому пользователю доступен storage root на чтение.

## F. Runtime configuration examples

Environment variables (без secrets):

```
UploadSecurity__MalwareScanner__Provider=ClamAV
UploadSecurity__MalwareScanner__DisplayName=ClamAV
UploadSecurity__MalwareScanner__ExecutablePath=clamscan
UploadSecurity__MalwareScanner__Arguments__0=--no-summary
UploadSecurity__MalwareScanner__Arguments__1={filePath}
UploadSecurity__MalwareScanner__TimeoutSeconds=30
UploadSecurity__MalwareScanner__CleanExitCodes__0=0
UploadSecurity__MalwareScanner__InfectedExitCodes__0=1
UploadSecurity__MalwareScanner__ErrorExitCodes__0=2
UploadSecurity__MalwareScanner__TreatScannerErrorAsRejection=true
```

Важно:

- `Provider=Disabled` — только для dev/local (результат `ScanStatus=Skipped`), НЕ
  для production;
- `TreatScannerErrorAsRejection=true` рекомендуется для production: при ошибке/
  таймауте scanner-а результат становится `ScanFailed` и upload отклоняется
  (fail-closed). При `false` ошибки scanner-а трактуются как `Skipped`
  (fail-open) — не для production;
- `{filePath}` должен оставаться placeholder-аргументом; backend подставляет
  фактический physical path сканируемого quarantine-файла;
- exit-code маппинг: clean → `Clean`, infected → `Infected`, error → `ScanFailed`,
  прочие неожиданные коды → `Rejected`;
- если используется другой scanner, `Provider=CommandLine`, но exit codes и
  аргументы нужно настроить по документации конкретного scanner-а.

## G. EICAR / smoke test

EICAR — это стандартная безопасная тестовая строка для проверки антивируса. Её
использовать только как controlled test malware scanner, НЕ хранить в репозитории
и НЕ коммитить.

Шаги:

1. убедиться, что backend запущен с production `UploadSecurity` config
   (`Provider=ClamAV`/`CommandLine`, не `Disabled`);
2. загрузить разрешённый clean JPG/PNG/PDF через публичную order form;
3. проверить, что upload accepted и в Admin у order attachment scan status
   `Clean`;
4. проверить, что Order/Email flow не сломан;
5. только в controlled test environment создать EICAR test file (в разрешённом
   формате/контейнере) и попытаться загрузить;
6. ожидаемый результат: upload rejected, файл не попадает в final storage,
   metadata не связывается с order;
7. проверить backend logs: не должно быть секретов и содержимого файлов клиентов;
8. проверить, что нет leftover-файлов в quarantine/final;
9. открыть Admin → Storage → Maintenance scan;
10. проверить delete orphan flow только на тестовых файлах.

## H. Failure behavior / troubleshooting

- scanner executable не найден (`ExecutablePath` неверный) → `ScanFailed`
  (при `TreatScannerErrorAsRejection=true`) → upload rejected;
- freshclam definitions устарели → возможны ложные результаты; обновить
  definitions;
- permission denied на quarantine files → scanner не может прочитать файл;
- timeout (`TimeoutSeconds`) → `ScanFailed`;
- wrong exit codes → неожиданный код трактуется как `Rejected`;
- scanner error при `TreatScannerErrorAsRejection=true` → upload отклоняется
  (это ожидаемое fail-closed поведение);
- `Provider=Disabled` случайно остался на production → файлы принимаются как
  `Skipped` без сканирования;
- storage root недоступен / wrong permissions → запись/чтение падают;
- disk full → запись в quarantine/final падает, upload не проходит;
- orphan physical files после аварийного падения → Admin Storage Maintenance
  scan/delete orphans;
- missing physical files после ручного удаления → видны в Storage Maintenance как
  missing;
- backup не содержит uploads → после restore файлы недоступны;
- restore восстановил DB, но не storage → metadata есть, физических файлов нет.

## I. Backup / restore checklist

- backup включает: PostgreSQL dump + uploads storage + Data Protection keys +
  config metadata (версия, применённые migrations);
- restore должен восстановить DB и тот же storage root layout;
- после restore выполнить Admin → Storage → Maintenance scan;
- отправить test upload (clean файл);
- проверить download существующих attachments;
- проверить delete attachment flow;
- не хранить backups в Git; см. также `BACKUP_RESTORE_RU.md`.

## J. Operational checklist перед приёмом реальных customer uploads

- production `UploadStorage__RootPath` выбран и writable;
- storage folder защищён от прямой static-публикации;
- backup uploads включён;
- ClamAV/scanner установлен;
- virus definitions обновляются;
- `Provider=ClamAV` или `CommandLine`, не `Disabled`;
- `TreatScannerErrorAsRejection=true`;
- clean file accepted (scan status `Clean`);
- EICAR rejected в controlled test;
- Admin scan status виден;
- Storage Maintenance scan чистый;
- запланирован мониторинг свободного места на диске;
- logs не содержат тел файлов/секретов.
