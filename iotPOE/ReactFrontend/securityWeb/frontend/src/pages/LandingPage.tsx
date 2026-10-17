import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Radio, Activity, Network } from "lucide-react";
import "../landingPage.css"; 

export const LandingPage: React.FC = () => {
  const navigate = useNavigate();

  // tracks hover state for the disabled card so it can flash red
  const [topologyHover, setTopologyHover] = useState(false);

  return (
    <div className="landing-container">
      
      {/* TOP NAVIGATION */}
      <nav className="landing-nav">
        <div className="nav-logo">
          <h3>SMARTX</h3>
          <h4>IOT ECOSYSTEM</h4>
        </div>
        <div className="system-status">
          <div className="status-dot"></div>
          SYSTEM ONLINE
        </div>
      </nav>

      {/* CENTER HERO */}
      <div className="landing-hero">
        
        <div className="main-title-div">
          <h1 className="REALh1">SMARTX</h1>
          <h2 className="ESTATEh2">REAL ESTATE</h2>
          <div className="divider-line"></div>
        </div>

        {/* FLOATING MODULE CARDS */}
        <div className="module-cards-container">
          
          {/* Module 1 */}
          <div 
            className="landing-module-card" 
            onClick={() => navigate("/dashboard")}
          >
            <div>
              <div className="label">Data Ingestion & Telemetry</div>
              <div className="value">Dashboard</div>
            </div>
            <div className="metric-icon-wrapper" style={{ "--iconColor": "#1fd1b2" } as React.CSSProperties}>
              <Radio size={24} />
            </div>
          </div>

          {/* Module 2 */}
          <div 
            className="landing-module-card" 
            onClick={() => navigate("/commands")}
          >
            <div>
              <div className="label">Command Stream</div>
              <div className="value">Live Control</div>
            </div>
            <div className="metric-icon-wrapper" style={{ "--iconColor": "#3b82f6" } as React.CSSProperties}>
              <Activity size={24} />
            </div>
          </div>

          {/* Module 3 (Disabled) */}
          <div 
            className={`landing-module-card ${topologyHover ? 'disabled' : ''}`}
            onClick={() => setTopologyHover(true)}
            onMouseEnter={() => setTopologyHover(true)}
            onMouseLeave={() => setTopologyHover(false)}
          >
            <div>
              <div className="label" style={{ color: topologyHover ? "#ef4444" : undefined }}>
                Mesh Routing
              </div>
              <div className="value" style={{ color: topologyHover ? "#ef4444" : undefined }}>
                Disabled
              </div>
            </div>
            <div 
              className="metric-icon-wrapper" 
              style={{ "--iconColor": topologyHover ? "#ef4444" : "#a78bfa" } as React.CSSProperties}
            >
              <Network size={24} />
            </div>
          </div>

        </div>
      </div>
    </div>
  );
};

export default LandingPage;