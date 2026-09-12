
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  useLocation,
} from "react-router-dom";

import { SideBar } from "./components/SideBar";
import { TopBar } from "./components/TopBar";

import { DashboardPage} from "./pages/Dashboard";
import { HealthCheckPage } from "./pages/HealthCheckPage";

import { Login } from "./pages/Login";

const AppContent: React.FC = () => {
  const location = useLocation();

  const isLoginPage = location.pathname === "/login";
 
  return (
    <div
      className={
        isLoginPage
          ? ""
          : `app-shell `
      }
    >

      {/* dont show sidebar when on login */}
      {!isLoginPage && <SideBar
       
      />}

      <div className={isLoginPage ? "" : "workspace"}>

          {/* dont show topbar when on login */}
        {!isLoginPage && <TopBar />}

        <main className={isLoginPage ? "" : "page-wrap"}>
          <Routes>

            {/* Login */}
            <Route
              path="/login"
              element={<Login />}
            />

            {/* Dashboard */}
            <Route
              path="/dashboard"
              element={<DashboardPage />}
            />

           
           
           

            {/* Backend health */}
            <Route
              path="/health"
              element={<HealthCheckPage />}
            />

            {/* Root */}
            <Route
              path="/"
              element={<Navigate to="/login" replace />}
            />

            {/* Unknown URL */}
            <Route
              path="*"
              element={<Navigate to="/dashboard" replace />}
            />

          </Routes>
        </main>

      </div>
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AppContent />
    </BrowserRouter>
  );
};

export default App;
{/*
  References
  rudderz243.2026. rudderz243/insy7314-library.  (Version 2.0) [Source code]. Available at: <https://github.com/rudderz243/insy7314-library> [Accessed 17 Aug. 2026].
           */}