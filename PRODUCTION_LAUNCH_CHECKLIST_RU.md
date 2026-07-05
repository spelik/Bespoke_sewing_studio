# Production launch checklist

Этот чек-лист нужен перед первым публичным запуском Bespoke Sewing Studio и перед переносом проекта на реальный домен/сервер.

Сейчас локальная dev-база может содержать только тестовые заявки, тестовые настройки и тестовых администраторов. Перед production важно не переносить случайные тестовые данные и не забыть заменить placeholder-значения.

## 1. Домен, SEO и публичные URL

Production-домен выбран: `https://oksanalogosha.com` (apex, без `www`). `www.oksanalogosha.com` не используется в canonical/sitemap; www → apex redirect настраивается отдельно на Cloudflare/сервере.

Перед публичным запуском обязательно:

- подключить production-домен `https://oksanalogosha.com` и настроить DNS/HTTPS;
- убедиться, что `public/robots.txt` ссылается на `https://oksanalogosha.com/sitemap.xml` (уже сделано);
- убедиться, что `public/sitemap.xml` использует `https://oksanalogosha.com` (уже сделано);
- задать `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com` для frontend production build;
- проверить canonical URL, Open Graph URL и Twitter card URL в HTML `<head>`;
- открыть `/robots.txt` и `/sitemap.xml` на `https://oksanalogosha.com` и убедиться, что там нет placeholder-домена;
- проверить, что `/admin` и `/admin/login` остаются `noindex, nofollow`;
- настроить www → apex redirect на Cloudflare/сервере (отдельный deployment-шаг, не часть этой задачи).

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
- применить migration `AddEmailOutboxMessages` через `dotnet ef database update`;
- проверить, что Order/Contact создают outbox jobs и Email Log состояния `Queued` → `Sent` или `Retrying`/`Failed`;
- проверить, что сбой SMTP или SignalR после сохранения Order/Contact записывается в backend logs, но публичный create endpoint всё равно возвращает success для сохранённой записи;
- настроить production monitoring/alerting для exhausted outbox jobs и `Failed` Email Log;
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

- проверить refresh cookie: `HttpOnly`, `Secure`, `SameSite=Lax`; production admin должен работать только через HTTPS;
- проверить, что admin access token отсутствует в `sessionStorage` и `localStorage` и хранится только в памяти приложения;
- перезагрузить admin page и убедиться, что сессия восстанавливается через HttpOnly refresh cookie и `/api/auth/refresh` без redirect loop;
- проверить logout, смену пароля и revoke current session: memory token очищается, а refresh больше не восстанавливает отозванную сессию;
- проверить SignalR connect/reconnect после refresh access token;
- проверить rotation через `/api/auth/refresh` и revocation через `/api/auth/logout`;
- проверить, что смена собственного пароля отзывает все refresh sessions, очищает cookie, отклоняет старый JWT и требует повторный вход;
- проверить, что disable admin user отзывает все refresh sessions и сразу отклоняет его старый JWT/refresh;
- проверить audit actions `auth.login_succeeded`, `auth.login_failed`, `auth.logout`, `auth.refresh_failed`, `auth.refresh_reuse_detected`, `auth.sessions_revoked`;
- проверить Admin → My account → Active sessions: current marker, safe browser/masked IP, revoke одной session и revoke остальных;
- проверить, что revoke current session очищает refresh cookie и выводит пользователя из admin;
- проверить audit actions `auth.session_revoked` и `auth.other_sessions_revoked`;
- проверить обычный login для admin без 2FA;
- включить 2FA в Admin → My account, сохранить показанные один раз recovery codes и убедиться, что setup secret больше не отображается после enable;
- после logout проверить двухступенчатый login сначала с TOTP, затем отдельным неиспользованным recovery code; до успешной второй ступени access/refresh tokens и refresh session не должны создаваться;
- после 2FA login перезагрузить страницу и проверить восстановление через HttpOnly refresh cookie, Active sessions и SignalR connect/reconnect;
- проверить regeneration recovery codes, disable 2FA и reset authenticator с текущим паролем, затем login без 2FA после disable;
- проверить отдельный `POST /api/auth/2fa/verify` rate limit и Identity lockout на неверных кодах; `/api/auth/refresh` и остальные admin endpoints не должны попасть под этот limit;
- проверить 2FA audit actions и убедиться, что TOTP, recovery codes, authenticator key, `otpauth://` URI, passwords и tokens отсутствуют в audit/logs;
- убедиться, что raw refresh/access tokens отсутствуют в logs, Git и browser storage;
- убедиться, что passwords, raw tokens, token hashes и cookie values отсутствуют в audit log;

Дополнительные обязательные backend checks:

- задать точные `ForwardedHeaders__KnownProxies` и/или `ForwardedHeaders__KnownNetworks` для proxy, который напрямую подключается к Kestrel;
- проверить, что `X-Forwarded-Proto` корректно восстанавливает HTTPS scheme, `X-Forwarded-For` — реальный client IP, а headers от недоверенного адреса игнорируются;
- проверить, что в production ответах присутствуют security headers: `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY`, `Permissions-Policy` и `Content-Security-Policy` (`curl.exe -i` по любому endpoint);
- убедиться, что HSTS (`Strict-Transport-Security`) отдаётся вне Development и что HTTPS redirection работает;
- проверить, что baseline CSP не ломает публичный сайт, admin, portfolio/service/uploaded images, downloads и SignalR realtime; помнить, что этот backend не отдаёт SPA HTML — document CSP (включая `connect-src` для API и `wss:`/`ws:` SignalR) задаёт хост фронтенда/reverse proxy;
- проверить login rate limit: несколько неверных попыток `POST /api/auth/login` с одного IP приводят к `429`; нормальный вход не заблокирован в пределах окна; `/api/auth/refresh` и admin API не затронуты; за reverse proxy лимит опирается на корректные Forwarded Headers (реальный client IP);
- задать `DataProtection__KeysPath` для persistent ASP.NET Core Data Protection keys; без него production startup намеренно завершается ошибкой;
- убедиться, что keys path находится вне repository и release/deployment directory, закрыт правами API process identity и включён в защищённый backup;

Перед public launch:

- включить HTTPS;
- настроить reverse proxy;
- настроить environment variables/secrets вне repository;
- проверить CORS/API base URL;
- настроить persistent ASP.NET Core Data Protection keys;
- проверить logs и disk space;
- убедиться, что `appsettings.Production.json`, `.env`, `.env.local` и secrets не попадают в Git.

## 8. Final smoke test

Backend health checks на production:

- `/health` и `/health/live` возвращают `200`;
- `/healthz` возвращает тот же liveness status и не зависит от PostgreSQL;
- `/health/ready` возвращает `200` при доступном PostgreSQL;
- `/readyz` возвращает тот же readiness status;
- `/health/ready` и `/readyz` возвращают non-`200`, когда PostgreSQL недоступен, при этом liveness остаётся `200`;
- `/api/version` возвращает только safe version/build metadata; проверить application/version/environment/framework, optional commit/build time и startedAt;
- health responses не раскрывают connection string, stack trace или exception details;
- `/api/version` не раскрывает credentials, SMTP settings, internal paths или другие secrets;
- HTTPS/proxy headers дают ожидаемые scheme, host и client IP.

Public output cache:

- два последовательных `GET /api/services` возвращают `200`, а повторный запрос в пределах TTL подтверждает server-side cache hit, например через `Age` header;
- минимум один content/portfolio endpoint возвращает `200` через server-side output cache и не раскрывает private/admin data;
- `/api/admin/*`, `/api/auth/*`, Order/Contact POST, uploads/downloads, images, health и version endpoints не используют public cache policy;
- после admin CMS update публичный ответ обновляется не позднее истечения 60-секундного TTL;
- browser/CDN `Cache-Control` не считается частью Task 65; отдельные reverse proxy/CDN rules не должны кэшировать authenticated/admin responses;
- production monitoring учитывает cache hit/miss behavior и нагрузку на PostgreSQL.

Перед объявлением сайта публичным:

- убедиться, что последний GitHub Actions workflow `CI` для release commit завершился успешно;
- не продолжать release при красных frontend typecheck/build или backend build/tests;

```powershell
npm.cmd run typecheck
npm.cmd run build
dotnet test backend\BespokeStudio.sln
dotnet build backend\BespokeStudio.sln
```

Backend tests нужно запускать также перед commit, который затрагивает backend.
Текущий набор является foundation unit-тестов и не заменяет ручные production
проверки, PostgreSQL-backed integration tests и полную проверку auth/2FA,
Order, Contact и upload flows.

На production проверить вручную:

- Home, Services, Portfolio, About, Contact, Order, Privacy, Terms;
- Order form submission;
- Contact form submission;
- file upload and attachment scan;
- admin login;
- Admin Dashboard;
- Orders: проверить server-side search/status filter, total, page size 10/25/50/100, Previous/Next, details drawer и действия;
- Contact Messages: проверить server-side search/status filter, total, page size 10/25/50/100, Previous/Next, details/status/delete actions и realtime refresh без дубликатов;
- Email delivery;
- Audit Log: проверить server-side filters, total, page size 10/25/50/100 и Previous/Next;
- Email Log: проверить server-side filters, total, page size, Previous/Next и realtime refresh без дубликатов;
- Users;
- My account password change;
- `/robots.txt`;
- `/sitemap.xml`;
- SEO tags in `<head>`.
