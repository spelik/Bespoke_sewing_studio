# TECH DEBT - Bespoke Sewing Studio

## Закрыто

- Admin-managed Gmail SMTP добавлен в Settings: владелец может выбрать Gmail SMTP, ввести Gmail address и Google App Password, пароль хранится как protected value на backend и никогда не возвращается в API.

- Production Email/SMTP checklist зафиксирован как обязательный pre-production блок: реальная SMTP-отправка, секреты вне Git, Gmail App Password, проверки test email/contact/order delivery, SPF/DKIM/DMARC, мониторинг, retry/background queue и ротация credentials.

- Contact Messages API реализован: public `POST /api/contact-messages` сохраняет сообщения Contact form в PostgreSQL, admin JWT endpoints позволяют просматривать сообщения и менять статусы `New` / `Read` / `Replied` / `Archived`.

- Public Contact page подключена к backend: добавлены loading/success/error states, backend validation handling, optional phone/subject и обязательное consent-подтверждение.

- Admin sidebar получил раздел **Contact Messages**: сообщения видны в списке, открываются в drawer, фильтруются по статусу и сохраняют изменения статуса после refresh.

- Contact messages подключены к существующей email notification foundation: при включённых email notifications и заданном email владельца Logging/SMTP provider получает уведомление о новом сообщении; ошибка отправки не отменяет создание сообщения.

- Неиспользуемая зависимость `recharts` удалена из `package.json`/`package-lock.json`; неиспользуемый shadcn/ui wrapper `src/app/components/ui/chart.tsx` удалён, так как больше не импортировался.

- Routing и deep links переведены на `react-router-dom`: доступны маршруты `/`, `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/admin`.
- Неизвестные URL больше не редиректят на главную. Вместо этого используется отдельная `404` page.
- Основной `App.tsx` сокращён до router/layout orchestration.
- Lazy loading страниц включён; основной JS chunk после задачи N4 уменьшился примерно с `646 KB` до `~212 KB` (`212.42 KB` в текущей production-сборке).
- Добавлена отдельная строгая TypeScript-проверка: `npm.cmd run typecheck`.
- `typecheck`, `build` и `npm audit` проходят на текущем состоянии проекта.
- `npm audit` больше не показывает high-severity уязвимости после обновления `react-router`, `react-router-dom` и `vite` в пределах текущих major-веток.
- Home hero и About image вынесены в responsive-структуру `src/assets/images/optimized/` с `WebP + JPEG` fallback, оригиналы сохранены в `src/imports/`.
- Portfolio/card images переведены на оптимизированные локальные derivative-файлы и теперь загружаются с `loading="lazy"` и `decoding="async"`.
- Внешняя decorative image dependency в `HomeHero` больше не использует `images.unsplash.com`; фон переведён на локальный optimized asset.
- Production SPA fallback задокументирован в `DEPLOYMENT_NOTES_RU.md`.
- Backend skeleton создан в `backend/` как отдельный ASP.NET Core Web API solution на `net10.0` с проектами `BespokeStudio.Api`, `BespokeStudio.Domain`, `BespokeStudio.Application`, `BespokeStudio.Infrastructure`.
- В backend уже есть базовые system endpoints: `/api/health`, `/api/version`, Swagger UI и dev CORS под локальный frontend.
- Создан независимый от persistence черновик domain models для `Orders`, `Clients`, `Portfolio`, `Categories`, `Services` и upload metadata.
- Созданы application contracts/DTO и сервисные интерфейсы для будущих модулей `Orders`, `Clients`, `Portfolio`, `Services`, `Uploads`; domain entities не используются как transport responses.
- Backend `bin/obj` удалены из Git tracking без удаления физических файлов; build artefacts теперь игнорируются через `.gitignore`, housekeeping debt закрыт.
- Создан EF Core persistence skeleton для PostgreSQL: `BespokeStudioDbContext`, восемь `IEntityTypeConfiguration<T>`, Fluent API relationships, ограничения и строковый mapping enum находятся в Infrastructure.
- В development-конфигурацию добавлен локальный `ConnectionStrings:BespokeStudioDb`, а в корень проекта — `docker-compose.postgres.yml` для PostgreSQL 16.
- Создана initial migration `InitialCreate` в `BespokeStudio.Infrastructure/Persistence/Migrations`; migration применяется явно, без automatic migration при старте API.
- Реализован Orders/Enquiries API: создание и чтение заявок, обновление статуса, внутренние заметки, базовая validation и PostgreSQL persistence через `IOrderService`.
- Реализован простой client matching: сначала нормализованный email, затем точное совпадение trimmed phone; повторная заявка переиспользует существующего клиента.
- Migration `AllowClientsWithoutEmail` разрешает phone-only enquiries; обе migration применены к локальной PostgreSQL на порту `5433`.
- JSON enum сериализуются строками, поэтому API принимает значения вроде `Dressmaking`, `Contacted` и `MemoryBear`.
- Public Order form подключена к реальному `POST /api/orders`: payload mapping, loading/success/error states и обработка validation problem изолированы во frontend API layer.
- Добавлена backend-аутентификация на ASP.NET Core Identity + JWT Bearer; Identity users/roles хранятся в PostgreSQL, migration `AddIdentityAuth` применена локально.
- `POST /api/orders` остаётся публичным, а Orders list/detail/status/notes защищены policy `AdminOnly` и ролью `Admin`.
- Добавлены `POST /api/auth/login`, защищённый `GET /api/auth/me`, Swagger Bearer authorization и безопасный development seed без credentials в репозитории.
- Frontend admin login подключён к JWT API: access token хранится в `sessionStorage`, `/admin` защищён route guard, logout очищает сессию.
- Admin Orders page использует реальные list/detail/status/note endpoints; изменения статуса и заметки обновляют UI без перезагрузки.
- Order attachments реализованы двухшаговым flow: public multipart upload возвращает IDs, `POST /api/orders` связывает metadata с заявкой, а admin скачивает файл через JWT-protected endpoint.
- PostgreSQL хранит upload metadata и scan status; физические dev-файлы находятся в ignored `backend/storage/uploads`, с generated filenames, allowlist типов, file signature validation, quarantine flow и лимитом `5 MB` на файл.
- Public `POST /api/uploads/order-attachments` и `POST /api/orders` защищены configurable fixed-window rate limiting по remote IP; превышение возвращает `429`, JSON problem details и `Retry-After`.
- Добавлены `IUploadCleanupService`/`UploadCleanupService`: cleanup удаляет только OrderAttachment uploads старше configurable TTL, повторно проверяя отсутствие связи с order перед удалением.
- Ручной `POST /api/uploads/cleanup-orphans` доступен только Admin JWT и возвращает summary по scanned/deleted/missing/skipped; linked attachments не удаляются.
- Добавлен strongly typed singleton `SiteSettings` с migration `AddSiteSettings`, EF configuration, validation, application DTO и `ISiteSettingsService`/`SiteSettingsService`.
- Public contact/social/footer settings доступны через `GET /api/site-settings/public`; admin чтение и изменение защищены policy `AdminOnly` через `GET/PATCH /api/admin/site-settings`.
- Site Settings содержат один email и один phone: email используется публичным сайтом и как email notification destination, phone остаётся только публичным контактом. Legacy-колонки удаляются migrations `NormalizeSiteSettingsContacts` и `RemoveWhatsAppNotifications`.
- Admin Settings page сохраняет единые contact settings и notification toggles; Footer, Home contact section и Contact page используют backend settings с typed fallback при недоступном API.
- Добавлены `INotificationService`, `IEmailNotificationSender`, Logging и SMTP email providers. Новая заявка запускает email владельцу, а ошибки SMTP логируются, переходят на logging fallback и не отменяют создание order. WhatsApp/SMS notifications убраны и сейчас не планируются.
- Добавлен Services & Prices CMS: dynamic `ServiceOffering`, дочерние `ServicePriceOption`, CRUD API, Admin Services editor и public `GET /api/services` с typed frontend fallback.
- Public Home/Services и Order form используют active services из PostgreSQL; новые orders сохраняют nullable `ServiceOfferingId` и `ServiceNameSnapshot`, а legacy enum остаётся fallback для старых клиентов и заказов.
- Delete-or-archive закрыт: неиспользованная услуга удаляется, использованная архивируется и исчезает из новых заявок без потери истории order/email notification.
- English-only cleanup выполнен: переключатели EN/UA удалены из Header/MobileMenu flow, `Language`/`defaultLanguage` state удалён из frontend types/data, UI и fallback/default content остаются английскими.
- Repeatable Content CMS реализован: process steps, studio values, testimonials и privacy subsections перенесены в backend-backed модель `RepeatableContentItem` с public API, protected admin CRUD, EF migration и frontend fallback.
- Public Home/About/Privacy sections подключены к `GET /api/repeatable-content`; Admin sidebar получил раздел **Repeatable Content** для add/edit/hide/show/archive, карточки админки выровнены и расширены после visual polish.

- Admin Dashboard добавлен как backend-backed обзор: карточки новых Orders/Contact Messages, recent Orders, recent Contact Messages, статус Email delivery и подсказка по upload security помогают владельцу быстрее увидеть, что требует внимания.

## Оптимизация изображений

- `src/imports/d2-1.png` (`5.22 MB`, Home hero) -> responsive derivatives:
  - `home-hero-768.webp` `42.3 KB`
  - `home-hero-1280.webp` `90.8 KB`
  - `home-hero-1920.webp` `156.8 KB`
  - JPEG fallback до `291.2 KB` на desktop
- `src/imports/d1-1.png` (`4.44 MB`, About image) -> responsive derivatives:
  - `about-hero-768.webp` `42.5 KB`
  - `about-hero-1280.webp` `77.1 KB`
  - `about-hero-1920.webp` `124.3 KB`
  - JPEG fallback до `226.8 KB` на desktop
- Portfolio/card assets переведены на локальные derivative-файлы шириной до `960px`:
  - `1a.jpg` `333.4 KB` -> `portfolio-1a-960.webp` `245.4 KB`
  - `2.jpg` `429.7 KB` -> `portfolio-2-960.webp` `307.2 KB`
  - остальные portfolio derivatives находятся в диапазоне примерно `35-139 KB` для WebP и `74-205 KB` для JPEG fallback

## Осталось

- Две самые тяжёлые portfolio карточки (`portfolio-1a`, `portfolio-2`) всё ещё заметно крупнее остальных даже после downscale. Следующий шаг по изображениям - отдельные crop-aware thumbnails или AVIF pipeline.
- SPA fallback всё ещё должен быть настроен на production-сервере. В репозитории добавлена только документация, не серверная конфигурация.
- Contact Messages API реализован, поэтому Contact form больше не является prototype-only. Public Order form, Contact form и admin-разделы используют реальные backend endpoints.
- PostgreSQL и EF migrations проверены напрямую через connection string на `127.0.0.1:5433`; Docker CLI доступен, но sandbox не разрешил доступ к Docker daemon/pipe для отдельной проверки container health.
- Portfolio/Gallery CMS реализован: категории, items, active/featured/order, Admin image upload и backend-first public gallery работают через PostgreSQL. Локальные frontend assets остаются typed fallback при недоступном API.
- Website Content CMS реализован для основных текстов и page images Home/About/Services/Portfolio/Order/Contact/Privacy; public frontend использует backend-first данные с typed fallback.
- Repeatable Content CMS реализован для повторяемых блоков Home/About/Privacy: process steps, studio values, testimonials и privacy sections больше не являются только статическим typed data.
- Application services для остальных модулей и отдельные repository abstractions пока не реализованы.
- Value objects и правила нормализации/валидации для email, телефона и денежных значений пока не определены.
- Client matching пока не защищён уникальным normalized email/phone constraint; при конкурентных запросах возможны дубликаты.
- Ручную validation можно позже заменить или дополнить FluentValidation при росте числа команд и правил.
- Для production auth остаются refresh-token/session strategy, password reset, email confirmation/MFA, rate limiting login и ротация JWT signing key через внешний secret store.
- Production storage provider (S3/Azure Blob/R2), deep content inspection, thumbnail/AVIF generation и image cropper пока не реализованы. Для локального storage добавлены quarantine flow, scan metadata и configurable ClamAV/command-line scanner; production ещё требует фактической настройки ClamAV и мониторинга обновления signatures.
- Автоматическая очистка orphan `PortfolioImage` пока не реализована; существующий cleanup обрабатывает только orphan order attachments. Архивирование portfolio item намеренно сохраняет физический файл.
- Автоматический background orphan cleanup пока не реализован; доступен защищённый ручной endpoint. Для production нужны distributed rate limiting/abuse protection и trusted forwarded-header configuration за reverse proxy.
- SMTP provider реализован; есть два режима: developer-managed SMTP через user-secrets/env/secret store и owner-managed Gmail SMTP через Admin Settings с protected App Password. До production остаются настройка deliverability (SPF/DKIM/DMARC), мониторинг bounce/rejection, production Data Protection key persistence и операционная ротация credentials.
- Background notification queue и retry policy пока не реализованы: отправка owner notifications и customer confirmation emails для orders/contact messages выполняется inline после сохранения.
- Service image upload пока не реализован; advanced money/currency model и drag-and-drop reorder для Services/Portfolio можно добавить позже. Rich text page CMS ещё не реализован.
- Полноценный rich-text editor/page builder не реализован: Content CMS и Repeatable Content CMS используют безопасные plain-text поля. Version history/drafts остаются будущими задачами. Multilingual CMS не планируется: проект принят как English-only.
- Production secret management для admin seed и JWT signing key ещё требует внешнего secret store и operational rotation process.


## Обязательный pre-production блок: Email / SMTP

- Выбрать режим реальной отправки: developer-managed `Provider=Smtp` через конфигурацию или owner-managed **Admin Settings > Email delivery > Gmail SMTP**.
- Для developer-managed SMTP настроить `Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName`, `UseSsl`.
- Для Gmail использовать Google App Password после включения 2-Step Verification; обычный пароль Gmail для SMTP не использовать.
- Локально хранить developer-managed SMTP credentials только в `dotnet user-secrets`; в production — только environment variables или внешний secret store.
- Owner-managed Gmail App Password хранить только как protected value в базе; API не должен возвращать пароль на frontend.
- Для production с owner-managed Gmail SMTP настроить persistent ASP.NET Core Data Protection keys.
- Не хранить raw SMTP credentials, Gmail App Passwords и production отправителей в Git, `appsettings*.json`, screenshots или документации.
- Проверить в Admin Settings включение email notifications и owner/public email.
- Проверить реальную доставку через **Send test email**, затем через public Contact form и public Order form.
- Разделять owner notifications и customer confirmation emails; подтверждения клиенту имеют отдельный toggle и редактируемые plain-text templates в Admin Settings.
- До production настроить SPF, DKIM, DMARC, мониторинг SMTP errors/bounce/rejection, operational credential rotation.
- Позже заменить inline отправку на background queue + retry policy, чтобы временная SMTP-недоступность не влияла на user flow.

## Рекомендации на следующие задачи

- Протестировать Task 30 после применения migration: customer confirmations OFF/ON для Contact form и Order form, отдельно от owner notifications.
- Подготовить фактическую production-конфигурацию хостинга с SPA fallback.
- Добить image pipeline для самых тяжёлых portfolio assets: AVIF или отдельные thumbnails под card layout.
- Спроектировать нормализованные уникальные ключи client matching и обработку конкурентного создания клиентов.

## Task 23 — Brand / Logo / SEO

- Brand/Logo/SEO settings добавлены в singleton `SiteSettings`; logo больше не является только hardcoded asset, но bundled logo сохранён как fallback.
- Header/footer logo, CTA, базовые meta/OG данные и labels/visibility навигации теперь backend-first.
- Brand images используют отдельный `BrandAsset` purpose и публичны только при ссылке из текущих settings; order attachments остаются private.
- SVG upload намеренно не реализован из-за security-рисков. Разрешены JPG, PNG и WebP.
- Future debt: advanced/per-page SEO, sitemap/robots generation, image cropper/thumbnails и production CDN/object storage.

## Task 24 — CMS completeness audit

- Public data flow проверен: SiteSettings управляет контактами/footer, BrandSettings — logo/navigation/CTA/SEO, PageContent — основными page sections, Services/Portfolio APIs — карточками и ценами/галереей, Orders API — заявками.
- Удалены устаревшие inline public values (`Logosha Studio`, старый телефон, часы работы, старые email) и hardcoded footer services. Typed fallback email теперь `null`, пока владелец не задаст его через Site Settings.
- Inline PageContent copy больше не подменяет скрытую backend-секцию. Fallback сосредоточен в `src/data/pageContentData.ts` и используется только при недоступности Content API.
- Admin sidebar очищен от mock Overview/Clients/Campaigns/Analytics; видимы только работающие Orders, Services, Portfolio, Content, Brand/SEO и Settings.
- На момент Task 24 process steps, studio values, testimonials и подробные privacy subsections ещё оставались статическими typed data; это закрыто в Task 26 через Repeatable Content CMS. Contact form остаётся prototype-only.
- Fallback не является основным источником при доступном backend. Multilingual CMS не планируется: сайт и админка English-only. Rich text editor/page builder остаётся future work.

## Task 25 — English-only cleanup

- Продуктовое решение зафиксировано: сайт и админка работают только на английском языке.
- Переключатели EN/UA удалены из публичного header/mobile navigation.
- Frontend `Language` type, `lang/setLang` state и `defaultLanguage` fallback удалены как неиспользуемые.
- Новые seed/default/fallback данные должны добавляться только на английском языке.
- Multilingual CMS больше не является будущей задачей; при необходимости локализация может быть переоценена отдельным продуктовым решением, но сейчас не планируется.
- Проверки: `npm.cmd run typecheck`, `npm.cmd run build`, `dotnet build backend/BespokeStudio.sln` прошли. Backend build предварительно требовал остановить запущенный `BespokeStudio.Api`, который блокировал DLL-файлы.


## Task 26 — Repeatable Content CMS

- Добавлена backend-модель `RepeatableContentItem`, EF configuration, `DbSet`, migration `AddRepeatableContentCms`, application contracts, validation и `IRepeatableContentService`/`RepeatableContentService`.
- Добавлены public endpoints `GET /api/repeatable-content` и `GET /api/repeatable-content/groups/{groupKey}`.
- Добавлены Admin JWT endpoints `/api/admin/repeatable-content` для просмотра, создания, изменения, hide/show и archive элементов.
- Seed data создан для групп `process-steps`, `studio-values`, `testimonials` и `privacy-sections` на основе текущих English-only fallback данных.
- Frontend public sections подключены backend-first: `ProcessSection`, `StudioValuesSection`, `TestimonialsSection`, About values block и Privacy subsections используют Repeatable Content API с typed fallback при недоступном backend.
- Admin sidebar получил раздел **Repeatable Content**. UI поддерживает фильтр групп, add/edit item, hide/show, archive и refresh публичного repeatable content после сохранения.
- Выполнен visual polish админки: рабочая область справа от sidebar центрирована, карточки шире, actions `Edit / Hide / Archive` отображаются в одну строку.
- Проверено вручную: `/api/health`, `/api/repeatable-content/groups/process-steps`, `/api/repeatable-content` возвращают `200`; frontend Network показывает успешные `200` для `process-steps`, `studio-values`, `testimonials`, `privacy-sections`.

## Task 27 — Remove unused recharts

- Удалена неиспользуемая npm dependency `recharts`.
- Удалён неиспользуемый wrapper `src/app/components/ui/chart.tsx`; поиск показал, что chart-компоненты больше нигде не импортировались.
- Изменены `package.json` и `package-lock.json`.
- Проверки: `npm.cmd run typecheck`, `npm.cmd run build`, `dotnet build backend\BespokeStudio.sln`.


## Task 28 — Contact Messages API

- Добавлена backend-модель `ContactMessage`, enum `ContactMessageStatus`, EF configuration, `DbSet`, migration `AddContactMessages`, application contracts, validation и `IContactMessageService`/`ContactMessageService`.
- Добавлен public endpoint `POST /api/contact-messages` с отдельным rate limit `RateLimiting:PublicContactPermitLimit`.
- Добавлены Admin JWT endpoints `GET /api/admin/contact-messages`, `GET /api/admin/contact-messages/{id}` и `PATCH /api/admin/contact-messages/{id}/status`.
- Public Contact page подключена к backend: форма отправляет реальные сообщения, показывает loading/success/error states, backend validation errors, optional phone/subject и consent checkbox.
- Admin sidebar получил раздел **Contact Messages**. UI поддерживает фильтр `All/New/Read/Replied/Archived`, drawer просмотра и изменение статуса с сохранением после refresh.
- Contact messages подключены к существующей email notification foundation. При включённых email notifications и заданном Site Settings email logging/SMTP provider получает owner notification; ошибка отправки логируется и не отменяет созданное сообщение.
- Runtime-проверки: прямой `POST /api/contact-messages` возвращает `201`, admin list возвращает сохранённые сообщения по Admin JWT, frontend Contact form создаёт сообщения, admin UI отображает их и меняет статус, logging email notification пишет письмо в backend log после clean rebuild/run.
- Проверки: `npm.cmd run typecheck`, `npm.cmd run build`, `dotnet build backend\BespokeStudio.sln`.

## Task 29.0 — Production SMTP checklist docs

- Production Email/SMTP checklist добавлен в обязательные pre-production требования.
- Зафиксировано, что `Provider=Logging` — только development fallback; реальная отправка owner notifications требует developer-managed `Provider=Smtp` или owner-managed `GmailSmtp`.
- Для Gmail зафиксировано требование использовать Google App Password и включённую 2-Step Verification, а не обычный пароль аккаунта.
- Зафиксировано правило хранения SMTP credentials: локально `dotnet user-secrets`, production environment variables/secret store, ничего не коммитить в Git/appsettings/docs.
- Добавлены обязательные проверки: Admin Settings email toggle/owner email, Send test email, Contact form delivery, Order form delivery.
- Customer confirmation emails вынесены в отдельную будущую задачу с отдельным toggle; owner notifications и клиентские подтверждения не смешивать.
- До production остаются SPF/DKIM/DMARC, SMTP error/bounce monitoring, credential rotation и background queue/retry policy.

## Task 29.2 — Admin-managed Gmail SMTP settings

- Добавлен backend/admin режим `GmailSmtp` для owner-managed отправки писем.
- `SiteSettings` расширен полями Email Delivery: provider, Gmail address, protected App Password, sender name, updated timestamp.
- Google App Password защищается через ASP.NET Core Data Protection и никогда не возвращается admin API/frontend.
- Добавлены Admin endpoints `GET/PATCH /api/admin/email-delivery`.
- `ConfiguredEmailNotificationSender` сначала проверяет admin-managed delivery mode, затем использует старый configuration-based Logging/SMTP режим.
- Admin Settings получил блок **Email delivery** с provider select, Gmail address, sender name, App Password replace/clear и короткой Gmail App Password инструкцией.
- Добавлена migration `AddAdminEmailDeliverySettings`.
- В sandbox прошли `npm run typecheck` и `npm run build`; `dotnet build` нужно выполнить локально, так как в sandbox нет .NET SDK.

## Task 30 — Customer confirmation emails

- `SiteSettings` расширен отдельным toggle `CustomerConfirmationEmailsEnabled` и редактируемыми plain-text templates для Order и Contact confirmation emails.
- Admin Settings получил блок **Customer confirmations** с subject/body полями и подсказкой по placeholders.
- Customer confirmations отделены от owner notifications: `Notify me about new requests` отправляет письма владельцу, а `Send automatic confirmation to customers` отправляет письмо клиенту на email из формы.
- Поддерживаются placeholders `{{studioName}}`, `{{customerName}}`, `{{customerEmail}}`, `{{customerPhone}}`, Order-only `{{serviceName}}`, `{{preferredDate}}`, Contact-only `{{messageSubject}}`. Сырые технические GUID/reference не показываются в дефолтных клиентских письмах.
- Ошибки отправки confirmation email логируются и не отменяют уже сохранённые order/contact message.
- Добавлена migration `AddCustomerConfirmationEmailTemplates`.

## Task 31 — Human-readable request numbers

- Orders получили `ReferenceNumber` формата `BSS-ORD-YYYY-000001`; Contact Messages получили `ReferenceNumber` формата `BSS-CON-YYYY-000001`.
- Внутренние GUID `Id` остаются primary key/API routing key, но публичные success screens, admin list/detail, owner notifications и customer template placeholders используют человекочитаемый reference.
- Добавлены PostgreSQL sequences `OrderReferenceSequence` и `ContactMessageReferenceSequence`; migration backfill заполняет reference для существующих записей и добавляет unique indexes.
- Placeholder `{{orderReference}}` теперь рендерит человекочитаемый order reference, `{{contactReference}}` — человекочитаемый contact message reference.


## Task 32 — Search by request reference in admin lists

- Admin Orders получил frontend-поиск по `BSS-ORD-...`, имени клиента, email, телефону, услуге и тексту сообщения с сохранением status filter.
- Admin Contact Messages получил frontend-поиск по `BSS-CON-...`, имени отправителя, email, телефону, subject и preview сообщения с сохранением status filter.
- Backend API не менялся; поиск работает по уже загруженным list DTO и остаётся маленьким UI-only улучшением.

## Task 31.2 — Settings module save buttons

- Admin Settings больше не полагается на одну общую кнопку Save Settings внизу страницы.
- Для модулей General, Contact, Notifications, Customer confirmations и Social links добавлены отдельные кнопки сохранения и локальные success/error сообщения.
- Email delivery сохранил отдельную кнопку Save Email Delivery и отдельную отправку test email.
- Backend API не усложнялся: модульные кнопки используют существующий валидируемый Site Settings update endpoint.

## Task 33 — Public form anti-spam hardening and multi-file upload fix

- Public Order form теперь накапливает выбранные файлы через несколько последовательных selections/drop actions вместо замены предыдущего выбора; лимит 5 файлов и existing frontend validation сохранены.
- Public Order и Contact form отправляют скрытый honeypot field и timestamp открытия формы. Backend validators отклоняют заполненный honeypot, отсутствующий timestamp, слишком быструю отправку и stale submissions до сохранения заявки.
- Anti-spam защита является lightweight hardening поверх существующего rate limiting, без reCAPTCHA/Google dependencies и без изменения UX для реальных клиентов.
- Backend persistence model и migrations не менялись.

## Task 34 — Admin attention counters

- Admin sidebar показывает badges `N new` для Orders и Contact Messages, когда есть новые заявки/сообщения.
- Admin Orders и Contact Messages получили summary cards `New ...` и `Total ...`, чтобы владелец сразу видел объём новых обращений.
- Contact Messages при изменении статуса обновляют счётчики без перезагрузки страницы. Backend API и migrations не менялись.
