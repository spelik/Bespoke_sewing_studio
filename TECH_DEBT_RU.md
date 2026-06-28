# TECH DEBT - Bespoke Sewing Studio

## Закрыто

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
- PostgreSQL хранит только upload metadata; физические dev-файлы находятся в ignored `backend/storage/uploads`, с generated filenames, allowlist типов и лимитом `5 MB` на файл.
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
- Admin bundle остаётся крупным из-за `recharts`: `439.21 KB` в текущей production-сборке.
- SPA fallback всё ещё должен быть настроен на production-сервере. В репозитории добавлена только документация, не серверная конфигурация.
- Marketing content, Contact form и admin-разделы Overview metrics, Clients, Campaigns и Analytics остаются в `mock/prototype mode`; реальный HTTP используется public Order submission, Services CMS, admin Orders flow и Site Settings.
- PostgreSQL и EF migrations проверены напрямую через connection string на `127.0.0.1:5433`; Docker CLI доступен, но sandbox не разрешил доступ к Docker daemon/pipe для отдельной проверки container health.
- Portfolio/Gallery CMS реализован: категории, items, active/featured/order, Admin image upload и backend-first public gallery работают через PostgreSQL. Локальные frontend assets остаются typed fallback при недоступном API.
- Website Content CMS реализован для основных текстов и page images Home/About/Services/Portfolio/Order/Contact/Privacy; public frontend использует backend-first данные с typed fallback.
- Application services для остальных модулей и отдельные repository abstractions пока не реализованы.
- Value objects и правила нормализации/валидации для email, телефона и денежных значений пока не определены.
- Client matching пока не защищён уникальным normalized email/phone constraint; при конкурентных запросах возможны дубликаты.
- Ручную validation можно позже заменить или дополнить FluentValidation при росте числа команд и правил.
- Для production auth остаются refresh-token/session strategy, password reset, email confirmation/MFA, rate limiting login и ротация JWT signing key через внешний secret store.
- Production storage provider (S3/Azure Blob/R2), antivirus/deep content scanning, thumbnail/AVIF generation и image cropper пока не реализованы. Portfolio upload использует локальное dev storage.
- Автоматическая очистка orphan `PortfolioImage` пока не реализована; существующий cleanup обрабатывает только orphan order attachments. Архивирование portfolio item намеренно сохраняет физический файл.
- Автоматический background orphan cleanup пока не реализован; доступен защищённый ручной endpoint. Для production нужны distributed rate limiting/abuse protection и trusted forwarded-header configuration за reverse proxy.
- SMTP provider реализован; production credentials должны задаваться через user-secrets/env/secret store. До production остаются настройка deliverability (SPF/DKIM/DMARC), мониторинг bounce/rejection и операционная ротация credentials.
- Background notification queue и retry policy пока не реализованы: отправка выполняется inline после сохранения заявки. Customer confirmation email также не реализован.
- Service image upload пока не реализован; advanced money/currency model и drag-and-drop reorder для Services/Portfolio можно добавить позже. Rich text page CMS ещё не реализован.
- Полноценный rich-text editor/page builder не реализован: Content CMS использует безопасные plain-text поля. Logo upload, version history/drafts и локализация контента остаются будущими задачами.
- Production secret management для admin seed и JWT signing key ещё требует внешнего secret store и operational rotation process.

## Рекомендации на следующие задачи

- Подготовить фактическую production-конфигурацию хостинга с SPA fallback.
- Добить image pipeline для самых тяжёлых portfolio assets: AVIF или отдельные thumbnails под card layout.
- Оценить, можно ли уменьшить admin chunk через более узкий импорт графиков или дополнительное lazy splitting внутри admin prototype.
- Заменять оставшиеся admin prototype sections реальными API постепенно, не смешивая их с уже подключённым Orders flow.
- Спроектировать нормализованные уникальные ключи client matching и обработку конкурентного создания клиентов.
- Подключать остальные frontend-модули к HTTP API постепенно; site content и admin сохранять в `mock/prototype mode` до появления соответствующих защищённых endpoints.
