# Production launch checklist

Этот чек-лист нужен перед первым публичным запуском Bespoke Sewing Studio и перед переносом проекта на реальный домен/сервер.

Сейчас локальная dev-база может содержать только тестовые заявки, тестовые настройки и тестовых администраторов. Перед production важно не переносить случайные тестовые данные и не забыть заменить placeholder-значения.

## 1. Домен, SEO и публичные URL

Перед публичным запуском обязательно:

- выбрать и подключить реальный production-домен;
- заменить `https://replace-with-production-domain.example` в `public/robots.txt`;
- заменить `https://replace-with-production-domain.example` в `public/sitemap.xml`;
- задать `VITE_PUBLIC_SITE_URL=https://your-production-domain.example` для frontend production build;
- проверить canonical URL, Open Graph URL и Twitter card URL в HTML `<head>`;
- открыть `/robots.txt` и `/sitemap.xml` на production-домене и убедиться, что там нет placeholder-домена;
- проверить, что `/admin` и `/admin/login` остаются `noindex, nofollow`.

## 2. Public content and legal notices

Перед запуском владелец сайта должен финально проверить:

- публичный email и телефон;
- список услуг и цены;
- тексты Home, Services, About, Portfolio, Contact, Order;
- Privacy Policy;
- Terms & Service Information;
- wording по uploaded files, cancellation/payment rules и data retention;
- отсутствие выдуманного адреса, графика работы, WhatsApp или географии, если это не подтверждено владельцем.

## 3. Database and migrations

Перед deploy:

- убедиться, что используется production PostgreSQL, а не dev database;
- применить EF migrations через `dotnet ef database update` или production-safe migration process;
- проверить, что в production нет тестовых admin users, тестовых orders и тестовых contact messages;
- создать основного production admin user с сильным паролем;
- проверить Admin → Users и удалить/отключить лишние тестовые accounts;
- проверить Admin → Audit Log после первых admin-действий.

## 4. Backups

Перед запуском и перед каждым серьёзным обновлением:

- сделать PostgreSQL backup;
- сделать backup `backend/storage`;
- проверить dump через `pg_restore --list`;
- хранить backups вне Git repository;
- отдельно сохранить production secrets и Data Protection keys;
- свериться с `BACKUP_RESTORE_RU.md`.

## 5. Email delivery

Перед запуском email-уведомлений:

- настроить production SMTP или owner-managed Gmail SMTP;
- не хранить Gmail App Password или SMTP password в Git;
- проверить test email из Admin → Settings → Email delivery;
- проверить owner notification на Order и Contact Message;
- проверить customer confirmation emails;
- настроить SPF, DKIM и DMARC для production-домена, если используется доменная почта;
- проверить, что email templates не содержат тестовых данных.

## 6. Upload security and storage

Перед приёмом реальных файлов клиентов:

- включить и протестировать ClamAV/production malware scanner;
- проверить лимиты размера и количества файлов;
- проверить upload quarantine/final storage flow;
- убедиться, что `backend/storage` не коммитится в Git;
- решить, остаётся ли local storage на сервере или нужен object storage later;
- проверить backup/restore uploads.

## 7. Hosting, HTTPS and secrets

Перед public launch:

- включить HTTPS;
- настроить reverse proxy;
- настроить environment variables/secrets вне repository;
- проверить CORS/API base URL;
- настроить persistent ASP.NET Core Data Protection keys;
- проверить logs и disk space;
- убедиться, что `appsettings.Production.json`, `.env`, `.env.local` и secrets не попадают в Git.

## 8. Final smoke test

Перед объявлением сайта публичным:

```powershell
npm.cmd run typecheck
npm.cmd run build
dotnet build backend\BespokeStudio.sln
```

На production проверить вручную:

- Home, Services, Portfolio, About, Contact, Order, Privacy, Terms;
- Order form submission;
- Contact form submission;
- file upload and attachment scan;
- admin login;
- Admin Dashboard;
- Orders and Contact Messages;
- Email delivery;
- Audit Log;
- Users;
- My account password change;
- `/robots.txt`;
- `/sitemap.xml`;
- SEO tags in `<head>`.
