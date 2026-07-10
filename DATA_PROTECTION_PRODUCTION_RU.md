# Production Data Protection — runbook (Bespoke Sewing Studio)

Production domain: `https://oksanalogosha.com`

Этот документ описывает, как подготовить и проверить ASP.NET Core Data Protection
на production, чтобы после restart/redeploy/restore не потерять ключи и не сломать
protected values. Все значения ниже — только placeholder-примеры. Реальные пути,
ключи, secrets и key XML-файлы в Git не добавляются.

## A. Краткое решение по архитектуре

- ASP.NET Core Data Protection используется backend-ом для защиты protected
  values.
- `DataProtection:ApplicationName` фиксирован: `BespokeSewingStudio` (стабильный
  application discriminator).
- В Development допустим framework default local key store.
- В Production `DataProtection:KeysPath` обязателен: если он пустой, API намеренно
  не стартует (fail-fast против случайного ephemeral storage).
- Ключи сохраняются на диск через `PersistKeysToFileSystem(...)`; путь может быть
  absolute или relative от backend content root, но для production рекомендуется
  absolute persistent path.

## B. Что защищается Data Protection

- owner-managed Gmail SMTP App Password из Admin → Settings → Email delivery;
- 2FA challenge cookie;
- потенциально другие protected values/cookies, если появятся.

Важно понимать, что Data Protection keys — это отдельный secret/state:

- это НЕ JWT signing key;
- это НЕ PostgreSQL password;
- это НЕ замена SMTP/API secrets;
- эти ключи нужны именно для расшифровки protected values; без них ранее
  зашифрованные значения (например, Gmail App Password) станут нечитаемыми.

## C. Что НЕ хранить в Git

- Data Protection key XML files;
- `.env`, `.env.local`, `.env.production`;
- `appsettings.Production.json`, `appsettings.Staging.json`;
- скриншоты с путями/секретами;
- backup-архивы с ключами без защиты;
- raw Gmail App Password / SMTP password / JWT signing key.

## D. Production configuration examples

Только placeholder-значения, без реальных secrets.

Windows PowerShell:

```powershell
$env:DataProtection__ApplicationName = "BespokeSewingStudio"
$env:DataProtection__KeysPath = "D:\BespokeStudioSecrets\DataProtectionKeys"
```

Linux / netcup Docker Compose environment:

```
DataProtection__ApplicationName=BespokeSewingStudio
DataProtection__KeysPath=/appdata/keys
```

On the netcup host this is mounted from `/opt/apps/projects/bespoke-studio/data/keys`.
Older `/var/lib/bespoke-studio/data-protection-keys` examples belong to the
deprecated home-server deployment and are used only as a migration source.

Важно:

- для production использовать absolute path;
- path должен быть persistent между deploy/restart;
- path должен быть writable для пользователя backend process;
- path должен быть readable только backend process user / admin;
- path не должен находиться внутри Git working tree или publish folder, который
  может быть очищен при redeploy;
- в контейнере path должен быть смонтирован как persistent volume;
- в env vars двойное подчёркивание (`__`) соответствует `:` в конфиге (секция
  `DataProtection`).

## E. Folder permissions checklist

- создать folder до старта API;
- дать права backend process identity;
- запретить публичный доступ к папке;
- не отдавать эту папку через static files / reverse proxy;
- включить folder в encrypted/protected backup;
- после старта проверить, что появились key XML files;
- проверить, что повторный restart использует те же key files, а не создаёт новую
  отдельную папку (иначе path не persistent).

## F. Smoke test перед production launch

1. задать `ASPNETCORE_ENVIRONMENT=Production`;
2. задать `DataProtection__ApplicationName=BespokeSewingStudio`;
3. временно проверить, что без `DataProtection__KeysPath` production startup падает
   с понятной ошибкой `DataProtection:KeysPath is required in Production.`;
4. задать корректный `DataProtection__KeysPath`;
5. запустить backend и убедиться, что API стартует;
6. убедиться, что key file появился в keys folder;
7. настроить owner-managed Gmail SMTP через Admin;
8. отправить test email;
9. перезапустить backend;
10. снова отправить test email;
11. убедиться, что protected App Password остался decryptable (test email
    `Sent`, `sentExternally=true`);
12. проверить 2FA flow, если 2FA включена;
13. проверить logs: keys/secrets/passwords не логируются.

Не коммитить реальные key files или production env files.

## G. Restore / redeploy test

1. restore PostgreSQL dump;
2. restore uploads storage;
3. restore Data Protection keys folder;
4. restore env vars/secrets из secret store;
5. запустить backend;
6. проверить Admin login;
7. проверить owner-managed Gmail SMTP test email;
8. проверить 2FA challenge flow;
9. если keys потеряны — protected App Password может стать нечитаемым; нужно
   rotate/re-enter Gmail App Password в Admin (и при необходимости пересохранить
   другие protected values).

## H. Troubleshooting

- production startup падает с `DataProtection:KeysPath is required in Production.`
  — не задан `DataProtection__KeysPath`;
- folder не существует — создать до старта;
- permission denied — backend process user не имеет прав на folder;
- keys folder очищается при redeploy — path внутри publish/release folder;
  вынести в persistent location;
- mounted volume не подключён (контейнер) — примонтировать persistent volume;
- App Password could not be decrypted — потеряны/заменены ключи или изменён
  `ApplicationName`;
- 2FA challenge cookie перестала приниматься после redeploy — те же причины
  (ключи/ApplicationName);
- несколько instances используют разные key folders — использовать общий key ring;
- `ApplicationName` случайно изменён — вернуть `BespokeSewingStudio`;
- backup восстановил DB, но не восстановил keys — protected values нечитаемы,
  нужно re-enter/rotate.

## I. Multi-instance note

- если появится несколько backend instances, все они должны использовать общий
  Data Protection key ring;
- local filesystem path подходит для multi-instance только если он реально общий и
  persistent для всех instances;
- для single-instance production достаточно protected local persistent folder;
- external key store (например, для cloud scale/multi-instance) остаётся future
  task, если понадобится.

## J. Operational checklist

- `DataProtection__ApplicationName=BespokeSewingStudio`;
- `DataProtection__KeysPath` задан;
- path absolute и persistent;
- folder writable/readable только для backend process user;
- folder не публичный и не отдаётся как static;
- folder включён в protected backup;
- key file появляется после startup;
- после restart Gmail App Password остаётся decryptable;
- restore test включает keys;
- logs не раскрывают key material/secrets.
