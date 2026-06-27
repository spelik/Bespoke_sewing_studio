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
- Создана initial migration `InitialCreate` в `BespokeStudio.Infrastructure/Persistence/Migrations`; migration сгенерирована без подключения к БД и не применялась.

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
- Frontend по-прежнему не подключён к реальному backend. API layer во frontend всё ещё работает в `mock/prototype mode`, без реальных HTTP-запросов.
- PostgreSQL persistence настроен, но development database ещё не поднималась; `InitialCreate` нужно применить локально командой `dotnet ef database update` после запуска PostgreSQL.
- `docker-compose.postgres.yml` подготовлен и статически проверен по параметрам PostgreSQL 16, database/user/password/port/volume, но команды `docker compose config/up/ps` не выполнялись: Docker CLI отсутствует в текущем окружении.
- CRUD/API endpoints для `Orders`, `Clients`, `Portfolio`, `Categories`, `Services` и `Uploads` пока не реализованы.
- Application services и repository/persistence implementations пока не реализованы и не зарегистрированы в DI.
- Value objects и правила нормализации/валидации для email, телефона и денежных значений пока не определены.
- JSON-представление enum и mapping между backend `MemoryBear` и текущим frontend label `Memory Bears` нужно зафиксировать при проектировании реальных endpoints.
- Auth/admin login, JWT и role-based access пока не реализованы.
- Физическая загрузка файлов, file storage и email integrations пока не реализованы; существует только модель upload metadata.

## Рекомендации на следующие задачи

- Подготовить фактическую production-конфигурацию хостинга с SPA fallback.
- Добить image pipeline для самых тяжёлых portfolio assets: AVIF или отдельные thumbnails под card layout.
- Оценить, можно ли уменьшить admin chunk через более узкий импорт графиков или дополнительное lazy splitting внутри admin prototype.
- Поднять локальный PostgreSQL, применить `InitialCreate`, затем добавить application service implementations и реальные API endpoints.
- Подключать frontend к HTTP API только после стабилизации endpoint contracts; до этого сохранять `mock/prototype mode`.
