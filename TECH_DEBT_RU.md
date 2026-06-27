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
- Site content, Contact form и admin panel всё ещё работают в `mock/prototype mode`; реальный HTTP сейчас используется только public Order submission.
- PostgreSQL и EF migrations проверены напрямую через connection string на `127.0.0.1:5433`; Docker CLI доступен, но sandbox не разрешил доступ к Docker daemon/pipe для отдельной проверки container health.
- Orders list/detail/status/note endpoints временно не защищены authentication/authorization и не должны публиковаться как admin API до добавления auth.
- CRUD/API endpoints для `Portfolio`, `Categories`, `Services` и `Uploads` пока не реализованы.
- Application services для остальных модулей и отдельные repository abstractions пока не реализованы.
- Value objects и правила нормализации/валидации для email, телефона и денежных значений пока не определены.
- Client matching пока не защищён уникальным normalized email/phone constraint; при конкурентных запросах возможны дубликаты.
- Ручную validation можно позже заменить или дополнить FluentValidation при росте числа команд и правил.
- Auth/admin login, JWT и role-based access пока не реализованы.
- Физическая загрузка файлов, frontend upload integration, file storage и email notifications пока не реализованы; Order form отправляет `attachmentIds: null`.
- Admin order list/status/notes integration остаётся prototype и будет отдельной задачей после authentication/authorization.

## Рекомендации на следующие задачи

- Подготовить фактическую production-конфигурацию хостинга с SPA fallback.
- Добить image pipeline для самых тяжёлых portfolio assets: AVIF или отдельные thumbnails под card layout.
- Оценить, можно ли уменьшить admin chunk через более узкий импорт графиков или дополнительное lazy splitting внутри admin prototype.
- Добавить authentication/authorization перед использованием Orders read/update/note endpoints будущей admin panel.
- Спроектировать нормализованные уникальные ключи client matching и обработку конкурентного создания клиентов.
- Подключать остальные frontend-модули к HTTP API постепенно; site content и admin сохранять в `mock/prototype mode` до появления соответствующих защищённых endpoints.
