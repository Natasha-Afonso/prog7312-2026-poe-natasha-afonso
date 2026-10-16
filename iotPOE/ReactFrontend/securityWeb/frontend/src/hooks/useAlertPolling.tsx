import { useEffect, useRef } from "react";
import { getAlerts } from "../services/api.ts";
import { mapAlertDto } from "../models/alert.ts";
import { useAlert } from "../context/AlertContext.tsx";

// checks the backend every few seconds and pops the toast when a NEW unread alert shows up
// pattern for the interval inside useEffect (Abramov, 2019)
export const useAlertPolling = (intervalMs: number = 5000) => {
  const { triggerIncomingAlert } = useAlert();

  // ids we have already seen, so the same alert does not pop up again every 5 seconds
  const seenIds = useRef<Set<string>>(new Set());
  const hasLoadedOnce = useRef(false);

  useEffect(() => {
    const checkForNewAlerts = async () => {
      try {
        const dtos = await getAlerts();
        const alerts = dtos.map(mapAlertDto);

        // first load only remembers the old alerts so they dont all pop up on a page refresh
        if (!hasLoadedOnce.current) {
          alerts.forEach((a) => seenIds.current.add(String(a.id)));
          hasLoadedOnce.current = true;
          return;
        }

        const newUnread = alerts.filter(
          (a) => a.status === "unread" && !seenIds.current.has(String(a.id))
        );

        alerts.forEach((a) => seenIds.current.add(String(a.id)));

        // show the newest one
        if (newUnread.length > 0) {
          triggerIncomingAlert(newUnread[newUnread.length - 1]);
        }
      } catch (err) {
        console.error("Failed to check for new alerts:", err);
      }
    };

    checkForNewAlerts();
    const interval = setInterval(checkForNewAlerts, intervalMs);

    return () => clearInterval(interval);
  }, [intervalMs, triggerIncomingAlert]);
};

export default useAlertPolling;

/*References
  Abramov, D. 2019. Making setInterval Declarative with React Hooks — overreacted. (Version 10) [Source code] Available at: < https://overreacted.io/making-setinterval-declarative-with-react-hooks/ > [Accessed 12 Sept. 2026].
  */