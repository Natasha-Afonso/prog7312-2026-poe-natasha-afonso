
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  useLocation,
  useNavigate,
} from "react-router-dom";

import { SideBar } from "./components/SideBar";
import { TopBar } from "./components/TopBar";
import { SensorDetailPage } from "./pages/SensorDetailPage";
import { DashboardPage } from "./pages/Dashboard";
import { AlertOverlay } from "./components/AlertOverlay";
import { IncomingAlertToast } from "./components/IncomingAlertToast";
import { AlertsHistoryPage } from "./pages/AlertsHistoryPage";
import { AlertProvider, useAlert } from "./context/AlertContext";
import { acknowledgeAlert } from "./services/api.ts";
import { useAlertPolling } from "./hooks/useAlertPolling.tsx";
import { LandingPage } from "./pages/LandingPage";
import { CommandStreamPage } from "./pages/CommandStreamPage.tsx";
import { CommandStreamProvider } from "./context/CommandStreamContext.tsx";

const AppContent: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const isLoginPage = location.pathname === "/landing";


  const {
    activeAlert,
    isOverlayOpen,
    closeAlert,
    incomingAlert,
    openAlert,
  } = useAlert();

  // inside AppContent, after the useAlert() line
  useAlertPolling();


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
              path="/"
              element={<Navigate to="/landing" replace />}
            />

            {/* Dashboard */}
            <Route
              path="/dashboard"
              element={<DashboardPage />}
            />

            <Route
              path="/sensorDetail/:id"
              element={<SensorDetailPage />}
            />


            <Route path="/alerts" element={<AlertsHistoryPage />}
            />

            {/* Backend health */}
            <Route
              path="/commands"
              element={<CommandStreamPage />}
            />

            {/* Root */}
          
            <Route path="/landing" element={<LandingPage />} />

            {/* Unknown URL */}
            <Route
              path="*"
              element={<Navigate to="/dashboard" replace />}
            />

          </Routes>
        </main>

      </div>
      {/* Global Alerts & Toasts */}
      {!isLoginPage && (
        <>
          <AlertOverlay
            alert={activeAlert}
            isOpen={isOverlayOpen}
            onClose={closeAlert}
            onAcknowledge={(id) => {
              acknowledgeAlert(Number(id)).catch((err) =>
                console.error("Failed to acknowledge alert:", err)
              );
              closeAlert();
            }}
            onViewSensor={(sensorId) => {
              closeAlert();
              navigate(`/sensorDetail/${sensorId}`);
            }}
            onViewAllAlerts={() => {
              closeAlert();
              navigate("/alerts");
            }}
          />

          {incomingAlert && (
            <IncomingAlertToast
              visible={true}
              sensorId={incomingAlert.sensorId}
              message={incomingAlert.title}
              onOpen={() => openAlert(incomingAlert)}
            />
          )}
        </>
      )}

    </div>
  );
};

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AlertProvider>
      <CommandStreamProvider>
        <AppContent />
        </CommandStreamProvider>
      </AlertProvider>
    </BrowserRouter>
  );
};

export default App;
{/*
  References
  rudderz243.2026. rudderz243/insy7314-library.  (Version 2.0) [Source code]. Available at: <https://github.com/rudderz243/insy7314-library> [Accessed 17 Aug. 2026].
           */}