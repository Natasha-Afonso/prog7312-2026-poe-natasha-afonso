import React, { useState, useEffect } from "react";
import { AlertTriangle, AlertCircle, CheckCircle2 } from "lucide-react";
import { Glass } from "../components/Glass.tsx";
import { getAlerts } from "../services/api.ts";
import { mapAlertDto } from "../models/alert.ts";
import type { SensorAlert } from "../models/alert.ts";
import { useAlert } from "../context/AlertContext.tsx";

export const AlertsHistoryPage: React.FC = () => {
  const [alerts, setAlerts] = useState<SensorAlert[]>([]);
  const { openAlert } = useAlert();

  useEffect(() => {
    getAlerts()
      .then((dtos) => setAlerts(dtos.map(mapAlertDto)))
      .catch((err) => console.error("Failed to load alerts:", err));
  }, []);

  const statusIcon = (status: SensorAlert["status"]) => {
    if (status === "unread") return <AlertTriangle aria-hidden="true" />;
    if (status === "acknowledged") return <AlertCircle aria-hidden="true" />;
    return <CheckCircle2 aria-hidden="true" />;
  };

  return (
    <div className="content">

      <Glass className="card full-width">

        <div className="table-header">
          <div className="label">Alert History</div>
        </div>

        <div className="alert-history-list">

          {alerts.map((alert) => (
            <button
              key={alert.id}
              className={`alert-history-row status-${alert.status}`}
              onClick={() => openAlert(alert)}
            >

              <span className={`alert-status-icon ${alert.status}`}>
                {statusIcon(alert.status)}
              </span>

              <span className="alert-history-main">
                <strong>{alert.sensorId} • {alert.title}</strong>
                <small>{alert.location} • {alert.timestamp}</small>
              </span>

              <span className={`alert-status-label ${alert.status}`}>
                {alert.status.toUpperCase()}
              </span>

            </button>
          ))}

        </div>

      </Glass>

    </div>
  );
};

export default AlertsHistoryPage;