import { Navigate, Outlet, useLocation } from "react-router-dom";
import { RouteLoader } from "../components/RouteLoader";
import { useAuth } from "./AuthContext";

export function ProtectedAdminRoute() {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <RouteLoader />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/admin/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
