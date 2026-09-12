import React, { createContext, useContext, useState, useCallback } from "react";
import type { SensorAlert } from "../models/alert.ts";
//(Soyza ,2024)
// this is the plumbing that lets
// any page trigger the shared alert overlay/toast
interface AlertContextValue {
  activeAlert: SensorAlert | null;
  isOverlayOpen: boolean;
  openAlert: (alert: SensorAlert) => void;
  closeAlert: () => void;
  incomingAlert: SensorAlert | null;
  triggerIncomingAlert: (alert: SensorAlert) => void;
  dismissIncomingAlert: () => void;
}

const AlertContext = createContext<AlertContextValue | undefined>(undefined);

export const AlertProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [activeAlert, setActiveAlert] = useState<SensorAlert | null>(null);
  const [isOverlayOpen, setIsOverlayOpen] = useState(false);
  const [incomingAlert, setIncomingAlert] = useState<SensorAlert | null>(null);

  const openAlert = useCallback((alert: SensorAlert) => {
    setActiveAlert(alert);
    setIsOverlayOpen(true);
    setIncomingAlert(null);
  }, []);

  const closeAlert = useCallback(() => {
    setIsOverlayOpen(false);
  }, []);

  const triggerIncomingAlert = useCallback((alert: SensorAlert) => {
    setIncomingAlert(alert);
  }, []);

  const dismissIncomingAlert = useCallback(() => {
    setIncomingAlert(null);
  }, []);

  return (
    <AlertContext.Provider
      value={{
        activeAlert,
        isOverlayOpen,
        openAlert,
        closeAlert,
        incomingAlert,
        triggerIncomingAlert,
        dismissIncomingAlert,
      }}
    >
      {children}
    </AlertContext.Provider>
  );
};

export const useAlert = (): AlertContextValue => {
  const context = useContext(AlertContext);

  if (!context) {
    throw new Error("useAlert must be used within an AlertProvider");
  }

  return context;
};

export default AlertContext;

{/*References
 Soyza, C.2024. Mastering React’s Global Context : Building a Dynamic Alert System, React 18. [online] . Available at: < https://medium.com/@cmsoyza17/building-a-reusable-popup-alert-component-with-react-18-context-and-custom-hooks-9fa01b2a8e39 >[Accessed 12 September 2026].
  */}