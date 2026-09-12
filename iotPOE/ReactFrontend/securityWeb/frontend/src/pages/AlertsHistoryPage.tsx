import React, { useState } from "react";
import { AlertTriangle, AlertCircle, CheckCircle2 } from "lucide-react";
import { Glass } from "../components/Glass.tsx";
import type { SensorAlert } from "../models/alert.ts";
import { useAlert } from "../context/AlertContext.tsx";
// plain mock list of alerts
// (unread / acknowledged / resolved)
const MOCK_ALERTS: SensorAlert[] = [
  {
    id: "08-24-471",
    sensorId: "SX-003",
    location: "Zone C",
    metric: "Temperature",
    value: "38.7°C",
    threshold: "35.0°C",
    title: "TEMPERATURE THRESHOLD EXCEEDED",
    message: "Sensor SX-003 has reported a temperature outside the configured safe operating range.",
    timestamp: "14:31",
    status: "unread",
  },
  {
    id: "08-24-455",
    sensorId: "SX-008",
    location: "Zone A",
    metric: "Connection",
    value: "N/A",
    threshold: "N/A",
    title: "SENSOR CONNECTION INTERRUPTED",
    message: "Sensor SX-008 stopped reporting telemetry.",
    timestamp: "13:52",
    status: "acknowledged",
  },
  {
    id: "08-24-440",
    sensorId: "SX-002",
    location: "Zone B",
    metric: "Humidity",
    value: "71.2%",
    threshold: "65.0%",
    title: "HUMIDITY OUTSIDE NORMAL RANGE",
    message: "Sensor SX-002 humidity reading returned to normal after a brief spike.",
    timestamp: "12:18",
    status: "resolved",
  },
];



export const AlertsHistoryPage: React.FC = () => {
  const [alerts] = useState<SensorAlert[]>(MOCK_ALERTS);
 const { openAlert } = useAlert();
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