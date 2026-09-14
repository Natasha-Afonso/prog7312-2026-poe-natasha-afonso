import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Radio, Activity, Network } from "lucide-react";
import { Glass } from "../components/Glass";

export const LandingPage: React.FC = () => {
  const navigate = useNavigate();

  // tracks hover state for the two disabled cards so they can flash red
  const [commandHover, setCommandHover] = useState(false);
  const [topologyHover, setTopologyHover] = useState(false);

  return (
    <div className="content">

      <Glass
        className="card compact metric-card"
        onClick={() => navigate("/dashboard")}
      >
        <div className="metric-content" style={{ cursor: "pointer" }}>
          <div>
            <div className="label">Sensor Data Ingestion and Telemetry</div>
            <div className="value">Dashboard</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#1fd1b2" } as React.CSSProperties}>
            <Radio aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass
        className="card compact metric-card"
        onClick={() => setCommandHover(true)}
      >
        <div
          className="metric-content"
          style={{ cursor: "not-allowed" }}
          onMouseEnter={() => setCommandHover(true)}
          onMouseLeave={() => setCommandHover(false)}
        >
          <div>
            <div className="label" style={{ color: commandHover ? "#ef4444" : undefined }}>
              Real-Time Command Stream and History
            </div>
            <div className="value" style={{ color: commandHover ? "#ef4444" : undefined }}>
              Disabled
            </div>
          </div>

          <div
            className="metric-icon-wrapper"
            style={{ "--iconColor": commandHover ? "#ef4444" : "#3b82f6" } as React.CSSProperties}
          >
            <Activity aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass
        className="card compact metric-card"
        onClick={() => setTopologyHover(true)}
      >
        <div
          className="metric-content"
          style={{ cursor: "not-allowed" }}
          onMouseEnter={() => setTopologyHover(true)}
          onMouseLeave={() => setTopologyHover(false)}
        >
          <div>
            <div className="label" style={{ color: topologyHover ? "#ef4444" : undefined }}>
              Network Topology and Mesh Routing
            </div>
            <div className="value" style={{ color: topologyHover ? "#ef4444" : undefined }}>
              Disabled
            </div>
          </div>

          <div
            className="metric-icon-wrapper"
            style={{ "--iconColor": topologyHover ? "#ef4444" : "#a78bfa" } as React.CSSProperties}
          >
            <Network aria-hidden="true" />
          </div>
        </div>
      </Glass>

    </div>
  );
};

export default LandingPage;