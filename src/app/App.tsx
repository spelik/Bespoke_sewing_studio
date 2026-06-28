import { lazy, Suspense, useEffect, useState } from "react";
import {
  BrowserRouter,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";
import { SITE_SETTINGS } from "./appContent";
import { AuthProvider } from "./auth/AuthContext";
import { ProtectedAdminRoute } from "./auth/ProtectedAdminRoute";
import { RouteLoader } from "./components/RouteLoader";
import { Footer } from "./layout/Footer";
import { Header } from "./layout/Header";
import { SiteSettingsProvider } from "./siteSettings/SiteSettingsContext";
import { ServicesProvider } from "./services/ServicesContext";
import type { Language } from "./types";

const HomePage = lazy(() =>
  import("./pages/HomePage").then((module) => ({ default: module.HomePage })),
);
const ServicesPage = lazy(() =>
  import("./pages/ServicesPage").then((module) => ({ default: module.ServicesPage })),
);
const PortfolioPage = lazy(() =>
  import("./pages/PortfolioPage").then((module) => ({ default: module.PortfolioPage })),
);
const OrderPage = lazy(() =>
  import("./pages/OrderPage").then((module) => ({ default: module.OrderPage })),
);
const AboutPage = lazy(() =>
  import("./pages/AboutPage").then((module) => ({ default: module.AboutPage })),
);
const ContactPage = lazy(() =>
  import("./pages/ContactPage").then((module) => ({ default: module.ContactPage })),
);
const PrivacyPage = lazy(() =>
  import("./pages/PrivacyPage").then((module) => ({ default: module.PrivacyPage })),
);
const AdminPage = lazy(() =>
  import("./pages/AdminPage").then((module) => ({ default: module.AdminPage })),
);
const AdminLoginPage = lazy(() =>
  import("./pages/AdminLoginPage").then((module) => ({ default: module.AdminLoginPage })),
);
const NotFoundPage = lazy(() =>
  import("./pages/NotFoundPage").then((module) => ({ default: module.NotFoundPage })),
);

function RouteScrollReset() {
  const { pathname } = useLocation();

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [pathname]);

  return null;
}

function SiteShell() {
  const [lang, setLang] = useState<Language>(SITE_SETTINGS.defaultLanguage);
  const { pathname } = useLocation();
  const isAdminRoute = pathname.startsWith("/admin");

  return (
    <div className="min-h-screen bg-background text-foreground">
      <RouteScrollReset />
      <Header lang={lang} setLang={setLang} />
      <main>
        <Suspense fallback={<RouteLoader />}>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/services" element={<ServicesPage />} />
            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route path="/order" element={<OrderPage />} />
            <Route path="/about" element={<AboutPage />} />
            <Route path="/contact" element={<ContactPage />} />
            <Route path="/privacy" element={<PrivacyPage />} />
            <Route path="/admin/login" element={<AdminLoginPage />} />
            <Route element={<ProtectedAdminRoute />}>
              <Route path="/admin" element={<AdminPage />} />
            </Route>
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </Suspense>
      </main>
      {!isAdminRoute && <Footer />}
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <SiteSettingsProvider>
          <ServicesProvider>
            <SiteShell />
          </ServicesProvider>
        </SiteSettingsProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
