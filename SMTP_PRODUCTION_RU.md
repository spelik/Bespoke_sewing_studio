# Production SMTP — runbook (Bespoke Sewing Studio)

Production domain: `https://oksanalogosha.com`

Этот документ описывает, как подготовить и проверить production email без хранения
секретов в Git. Все значения ниже — только placeholder-примеры. Реальные SMTP
credentials, Gmail App Password, SMTP password и приватные DNS-значения в
репозиторий не добавляются.

## A. Краткое решение по архитектуре

На production поддержаны два варианта отправки почты:

1. **Developer-managed SMTP** через environment variables / secret store:
   `Notifications__Email__Provider=Smtp` и параметры `Notifications__Email__Smtp__*`.
   Значения задаются на сервере (env vars или secret store), не в Git.
2. **Owner-managed Gmail SMTP** через Admin → Settings → Email delivery:
   provider `GmailSmtp`, Gmail address, sender name и Google App Password.
   App Password хранится на backend в защищённом виде через ASP.NET Core Data
   Protection.

Дополнительно:

- `Provider=Logging` — только local/dev fallback. На production он НЕ используется
  как способ доставки: письма только пишутся в лог и не уходят наружу
  (`sentExternally=false`).
- **Cloudflare DNS** управляет DNS-записями домена (в т.ч. SPF/DKIM/DMARC), но сам
  по себе НЕ является SMTP sender. Cloudflare Email Routing — это форвардинг
  входящей почты, а не исходящий SMTP provider, и не может использоваться как
  sender для этого приложения.
- Для отправки писем с адреса вида `name@oksanalogosha.com` нужен отдельный
  почтовый/SMTP provider, который выдаст SMTP settings и требуемые DNS records.
- Если используется обычный Gmail-адрес, нужно включить Google 2-Step Verification
  и создать Google App Password.

## B. Что НЕ хранить в Git

- Gmail App Password;
- SMTP password;
- SMTP username, если он приватный;
- API keys / tokens;
- `.env`, `.env.local`, `.env.production`;
- `appsettings.Production.json`, `appsettings.Staging.json`;
- скриншоты с credentials;
- backup dumps, содержащие незащищённые secrets.

## C. Вариант 1 — developer-managed SMTP

Пример environment variables (только placeholder-значения):

```
Notifications__Email__Provider=Smtp
Notifications__Email__Smtp__Host=<smtp-host-from-provider>
Notifications__Email__Smtp__Port=587
Notifications__Email__Smtp__Username=<smtp-username>
Notifications__Email__Smtp__Password=<smtp-password-from-secret-store>
Notifications__Email__Smtp__FromEmail=<sender-email>
Notifications__Email__Smtp__FromName=Bespoke Sewing Studio
Notifications__Email__Smtp__UseSsl=true
```

Важно:

- реальные значения берутся из выбранного SMTP provider;
- secrets хранятся в environment variables / secret store / server secrets, не в Git;
- `FromEmail` должен быть разрешён provider-ом (иначе письма будут отклонены);
- если sender использует домен `oksanalogosha.com`, DNS auth records (SPF/DKIM/DMARC)
  provider выдаёт отдельно — их нужно добавить в Cloudflare DNS;
- в коде эти значения читаются как секция `Notifications:Email` (двойное
  подчёркивание в env vars соответствует `:` в конфиге).

## D. Вариант 2 — owner-managed Gmail SMTP через Admin

Пошагово:

1. войти в Admin;
2. открыть **Settings → Contact**;
3. задать public/owner email;
4. сохранить Contact settings;
5. открыть **Settings → Email delivery**;
6. выбрать **Gmail SMTP**;
7. указать Gmail address;
8. указать Sender name;
9. вставить Google App Password;
10. сохранить;
11. включить owner notifications (Email notifications enabled);
12. нажать **Send test email**;
13. проверить **Email Log**.

Важно:

- обычный пароль Gmail использовать нельзя;
- требуется Google 2-Step Verification;
- Google App Password показывается один раз — сохраните его в secret store;
- backend хранит App Password только в защищённом виде (ASP.NET Core Data
  Protection) в singleton `SiteSettings`; admin API его не возвращает;
- при redeploy/смене инфраструктуры на production обязательно сохранять persistent
  Data Protection keys, иначе ранее сохранённый protected App Password станет
  нечитаемым и письма перестанут уходить (упадут в logging fallback);
- если App Password скомпрометирован — удалить/rotate его в Google и заново
  сохранить в Admin.

## E. DNS / Cloudflare checklist для `oksanalogosha.com`

Конкретные значения SPF/DKIM/DMARC не выдумывать — брать у выбранного email
provider.

- SPF, DKIM и DMARC records берутся из инструкций выбранного email/SMTP provider;
- в Cloudflare DNS добавить записи ровно по инструкции provider-а;
- если provider даёт DKIM как CNAME/TXT — добавить именно их;
- если provider даёт SPF TXT — объединить с существующим SPF в одну запись, не
  создавать несколько конфликтующих SPF records;
- DMARC начать с безопасной monitoring-политики. Пример (placeholder, значения и
  политику выбрать самостоятельно):

  ```
  _dmarc.oksanalogosha.com TXT "v=DMARC1; p=none; rua=mailto:<dmarc-report-email>"
  ```

  Это только пример; реальный report-адрес и политику (`p=none` → позже возможно
  `quarantine`/`reject`) нужно выбрать осознанно;
- `www.oksanalogosha.com` не используется как sender/canonical origin;
- после настройки проверить доставку на Gmail и Outlook, включая папку «Спам».

## F. Production smoke test

1. применить migrations;
2. проверить путь Data Protection keys (`DataProtection__KeysPath`) и что он
   persistent;
3. запустить backend;
4. зайти в Admin;
5. настроить email provider (Вариант 1 или Вариант 2);
6. отправить **test email**;
7. проверить Email Log: статус `Sent` и `sentExternally=true`;
8. отправить публичную **Contact** форму;
9. проверить owner notification;
10. включить customer confirmation и проверить письмо клиенту;
11. отправить публичную **Order** форму;
12. проверить outbox statuses: `Queued` → `Sent` (или `Retrying`/`Failed`);
13. проверить backend logs — SMTP credentials / App Password в логах быть не должно;
14. убедиться, что публичный create endpoint остаётся успешным при временном сбое
    email, если бизнес-запись (order/contact) сохранена.

## G. Troubleshooting

Типичные причины проблем:

- не включён Google 2-Step Verification;
- вставлен обычный Gmail password вместо App Password;
- App Password скопирован с пробелами — backend нормализует whitespace, но лучше
  хранить значение без лишних пробелов;
- потеряны Data Protection keys после redeploy → protected App Password стал
  нечитаемым;
- SMTP host/port/firewall недоступны из production-среды;
- provider блокирует `FromEmail` (адрес не подтверждён/не разрешён);
- не настроены SPF/DKIM/DMARC → письма уходят в спам или отклоняются;
- Email Log показывает `Retrying`/`Failed` — смотреть result/error summary и логи;
- на production случайно остался `Provider=Logging` → письма не уходят наружу.
