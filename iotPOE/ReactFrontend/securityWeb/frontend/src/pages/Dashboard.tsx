import React, { useState, useEffect } from "react";
import { Clock, Radio, Wifi, AlertTriangle } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Glass } from "../components/Glass";
import { MonitoringPanel } from "../components/MonitoringPanel";
import { SensorTable } from "../components/SensorTable";
import { RegisterSensorModal } from "../components/RegisterPage.tsx";
import { getSensors, deleteSensor, getAlerts } from "../services/api.ts";
import { mapAlertDto } from "../models/alert.ts";
import type { Sensor } from "../models/sensor";
import type { SensorAlert } from "../models/alert.ts";
import { useAlert } from "../context/AlertContext.tsx";

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { openAlert, triggerIncomingAlert } = useAlert();

  const [isRegisterModalOpen, setIsRegisterModalOpen] = useState(false);
  const [systemTime, setSystemTime] = useState(new Date());
  const [sensors, setSensors] = useState<Sensor[]>([]);
  const [alerts, setAlerts] = useState<SensorAlert[]>([]);

  const loadSensors = () => {
    getSensors()
      .then(setSensors)
      .catch((err) => console.error("Failed to load sensors:", err));
  };

  const loadAlerts = () => {
    getAlerts()
      .then((dtos) => {
        const mapped = dtos.map(mapAlertDto);
        setAlerts(mapped);
        return mapped;
      })
      .catch((err) => console.error("Failed to load alerts:", err));
  };

  useEffect(() => {
    loadSensors();
    loadAlerts();
  }, []);

  useEffect(() => {
    const interval = setInterval(() => setSystemTime(new Date()), 1000);
    return () => clearInterval(interval);
  }, []);

  // fires an incoming toast for the first unread alert, once alerts
  // have loaded — replaces the old mock timer
  useEffect(() => {
    const unread = alerts.find((a) => a.status === "unread");
    if (unread) {
      const timer = setTimeout(() => triggerIncomingAlert(unread), 4000);
      return () => clearTimeout(timer);
    }
  }, [alerts]);

  const unreadCount = alerts.filter((a) => a.status === "unread").length;

  const handleOpenAlertsCard = () => {
    const unread = alerts.find((a) => a.status === "unread");
    if (unread) {
      openAlert(unread);
    } else {
      navigate("/alerts");
    }
  };

  const handleSensorRegistered = () => {
    loadSensors();
  };

  const handleView = (id: number) => navigate(`/sensors/${id}`);
  const handleEdit = (id: number) => console.log("edit", id);
  const handleDelete = async (id: number) => {
    try {
      await deleteSensor(id);
      loadSensors();
    } catch (err) {
      console.error("Failed to delete sensor:", err);
    }
  };

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
            <div className="value">{sensors.length} Registered</div>
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
        <div className="metric-content" onClick={handleOpenAlertsCard} style={{ cursor: "pointer" }}>

          <div>
            <div className="label">Open Alerts</div>
            <div className="value">{unreadCount.toString().padStart(2, "0")}</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#ff9f43" } as React.CSSProperties}>
            <AlertTriangle aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <MonitoringPanel nodesOnline={sensors.length} />

      <Glass className="card wide">

        <div className="table-header">
          <div className="label">Sensor Data Ingestion</div>
          <button className="btn-primary" onClick={() => setIsRegisterModalOpen(true)}>+ Register Sensor</button>
        </div>

        <SensorTable
          sensors={sensors}
          onView={handleView}
          onEdit={handleEdit}
          onDelete={handleDelete}
        />

      </Glass>

      <RegisterSensorModal
        isOpen={isRegisterModalOpen}
        onClose={() => setIsRegisterModalOpen(false)}
        onSensorRegistered={handleSensorRegistered}
      />

    </div>
  );
};

export default DashboardPage;