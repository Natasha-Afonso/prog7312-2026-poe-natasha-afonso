import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Radio, Activity, Network } from "lucide-react";
import { Glass } from "../components/Glass";

export const LandingPage: React.FC = () => {
  const navigate = useNavigate();

  // tracks hover state for the disabled card so it can flash red
  const [topologyHover, setTopologyHover] = useState(false);

  return (
    <div className="content">

      {/* module 1 (Part 1) */}
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

      {/* module 2 (Part 2), now enabled */}
      <Glass
        className="card compact metric-card"
        onClick={() => navigate("/commands")}
      >
        <div className="metric-content" style={{ cursor: "pointer" }}>
          <div>
            <div className="label">Real-Time Command Stream and History</div>
            <div className="value">Command Stream</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#3b82f6" } as React.CSSProperties}>
            <Activity aria-hidden="true" />
          </div>
        </div>
      </Glass>

      {/* module 3 (final PoE), still disabled */}
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