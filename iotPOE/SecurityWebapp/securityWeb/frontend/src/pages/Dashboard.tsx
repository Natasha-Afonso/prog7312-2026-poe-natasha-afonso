import React, { useState, useEffect } from "react";
import { Clock, Radio, Wifi, AlertTriangle } from "lucide-react";
import { Glass } from "../components/Glass";
import { MonitoringPanel } from "../components/MonitoringPanel";
import { SensorTable } from "../components/SensorTable";
import type { Sensor } from "../models/sensor";

const MOCK_SENSORS: Sensor[] = [
  { id: "SX-001", location: "Zone A", type: "Temperature", status: "active", lastReading: "27.4°C" },
  { id: "SX-002", location: "Zone B", type: "Humidity", status: "active", lastReading: "61.2%" },
  { id: "SX-003", location: "Zone C", type: "Environmental", status: "warning", lastReading: "38.7°C" },
  { id: "SX-004", location: "Zone D", type: "Power", status: "active", lastReading: "12.1V" },
];

export const DashboardPage: React.FC = () => {
  const [systemTime, setSystemTime] = useState(new Date());

  useEffect(() => {
    const interval = setInterval(() => setSystemTime(new Date()), 1000);
    return () => clearInterval(interval);
  }, []);

  const handleView = (id: string) => console.log("view", id);
  const handleEdit = (id: string) => console.log("edit", id);
  const handleDelete = (id: string) => console.log("delete", id);

  return (
    <div className="content">

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">System Time</div>
            <div className="value">{systemTime.toLocaleTimeString()}</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#22d3ee" } as React.CSSProperties}>
            <Clock aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Sensor Nodes</div>
            <div className="value">24 Online</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#1fd1b2" } as React.CSSProperties}>
            <Radio aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Network Signal</div>
            <div className="value">98.7%</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#3b82f6" } as React.CSSProperties}>
            <Wifi aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Open Alerts</div>
            <div className="value">03</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#ff9f43" } as React.CSSProperties}>
            <AlertTriangle aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <MonitoringPanel nodesOnline={24} />

      <Glass className="card wide">

        <div className="table-header">
          <div className="label">Sensor Data Ingestion</div>
          <button className="btn-primary">+ Register Sensor</button>
        </div>

        <SensorTable
          sensors={MOCK_SENSORS}
          onView={handleView}
          onEdit={handleEdit}
          onDelete={handleDelete}
        />

      </Glass>

    </div>
  );
};

export default DashboardPage;