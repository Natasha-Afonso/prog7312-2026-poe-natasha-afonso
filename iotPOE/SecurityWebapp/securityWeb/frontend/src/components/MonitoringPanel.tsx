import React, { useState, useEffect } from "react";
import { Glass } from "./Glass";

interface MonitoringPanelProps {
  nodesOnline: number;
}

//(holoMailAuthor,2026)
export const MonitoringPanel: React.FC<MonitoringPanelProps> = ({ nodesOnline }) => {

  // not from either pen — swapped for a live last-sync readout
  const [lastSync, setLastSync] = useState(new Date());

  useEffect(() => {
    const interval = setInterval(() => setLastSync(new Date()), 5000);
    return () => clearInterval(interval);
  }, []);

  return (
    <Glass className="card wide monitor-card">

      <div className="label">Sensor Network Monitoring</div>

      {/* scanner-ring / radar-line / core-orb structure reused from HOLO//MAIL's
          waiting state, kept as an always-on idle visual rather than a
          pre-alert countdown */}

      <div className="transmission-zone">

        <div className="scanner-ring ring-one"></div>
        <div className="scanner-ring ring-two"></div>
        <div className="scanner-ring ring-three"></div>

        <div className="radar-line"></div>

        <div className="waiting-state">

          <div className="core-orb">
            <span></span>
          </div>

          <p>MONITORING ACTIVE</p>
          <small>RECEIVING SENSOR TELEMETRY</small>

        </div>

      </div>

      <div className="monitor-footer">
        <span>{nodesOnline} NODES ONLINE</span>
        <span>LAST SYNC {lastSync.toLocaleTimeString()}</span>
      </div>

    </Glass>
  );
};

export default MonitoringPanel;

{/*References
  holoMailAuthor. 2026. <Pen title>. (Version X) [Source code] Available at: <URL> [Accessed DATE].
  */}