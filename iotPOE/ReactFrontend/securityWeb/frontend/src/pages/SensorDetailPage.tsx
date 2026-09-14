import React, { useState, useEffect, useRef } from "react";
import { useParams } from "react-router-dom";
import { Thermometer, Gauge, Zap, Pause, Play } from "lucide-react";
import { Glass } from "../components/Glass.tsx";
import { TelemetryChart } from "../components/TelemetryChart.tsx";
import { TelemetryLog } from "../components/TelemetryLog.tsx";
import type { TelemetryLogLine } from "../models/telemetry.ts";

const MAX_DATA_POINTS = 25;

//(telemetryPanelAuthor,2026)
export const SensorDetailPage: React.FC = () => {
  const { sensorId } = useParams<{ sensorId: string }>();

  const [isRunning, setIsRunning] = useState(true);

  const [temp, setTemp] = useState("45.2");
  const [pressure, setPressure] = useState("120.4");
  const [voltage, setVoltage] = useState("24.01");

  const [labels, setLabels] = useState<string[]>([]);
  const [tempData, setTempData] = useState<number[]>([]);
  const [pressureData, setPressureData] = useState<number[]>([]);
  const [logEntries, setLogEntries] = useState<TelemetryLogLine[]>([]);

  // counter drives the sin/cos drift, 
  const counterRef = useRef(0);

  //Gareth, D. 2021 - lif
  useEffect(() => {
    const interval = setInterval(async () => {
      if (!isRunning || !sensorId) {
        return;
      }
      // Abramov, D. 2019
      try {
        const response = await fetch(`https://localhost:7123/api/Telemetry/${sensorId}`);
        if (!response.ok) return;

        const data = await response.json();
        if (!data || data.length === 0) return;

        // Take latest record from backend
        const latest = data[0]; 
        const timestamp = new Date(latest.timestamp || Date.now()).toLocaleTimeString([], { hour12: false });

        let currentTemp = temp;
        let currentPressure = pressure;
        let currentVoltage = voltage;

        // Parse metrics dynamically based on backend MetricName
        data.forEach((reading: any) => {
          const metric = reading.metricName?.toLowerCase() || "";
          if (metric.includes("temp") || reading.metricName === "Reading") {
            currentTemp = Number(reading.value).toFixed(1);
          } else if (metric.includes("pressure")) {
            currentPressure = Number(reading.value).toFixed(1);
          } else if (metric.includes("voltage")) {
            currentVoltage = Number(reading.value).toFixed(2);
          }
        });

        counterRef.current += 1;

        setTemp(currentTemp);
        setPressure(currentPressure);
        setVoltage(currentVoltage);

        // rolling window — drop the oldest point once we hit the cap
        setLabels((prev) => [...prev, timestamp].slice(-MAX_DATA_POINTS));
        setTempData((prev) => [...prev, Number(currentTemp)].slice(-MAX_DATA_POINTS));
        setPressureData((prev) => [...prev, Number(currentPressure)].slice(-MAX_DATA_POINTS));

        setLogEntries((prev) => [
          ...prev,
          { timestamp, temp: currentTemp, pressure: currentPressure, voltage: currentVoltage },
        ]);
      } catch (err) {
        console.error("Failed to poll telemetry:", err);
      }
    }, 600);

    return () => clearInterval(interval);
  }, [isRunning, sensorId, temp, pressure, voltage]);

  const handleClearLog = () => {
    setLogEntries([]);
  };

  return (
    <div className="content sensor-detail">

      <div className="sensor-detail-header full-width">

        <div>
          <div className="status-badge">
            <span className="status-dot-ping"></span>
            System Status: Operational
          </div>

          <h1>Sensor {sensorId} <small className="version-tag">Time-Series Monitor</small></h1>
        </div>

        <div className="sensor-detail-actions">

          <button className="btn-primary" onClick={() => setIsRunning(!isRunning)}>
            {isRunning ? <Pause aria-hidden="true" /> : <Play aria-hidden="true" />}
            {isRunning ? "Pause Stream" : "Resume Stream"}
          </button>

          <button className="btn-secondary" onClick={handleClearLog}>
            Clear Data Log
          </button>

        </div>

      </div>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Core Temperature</div>
            <div className="value">{temp}°C</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#f59e0b" } as React.CSSProperties}>
            <Thermometer aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Hydraulic Pressure</div>
            <div className="value">{pressure} bar</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#22d3ee" } as React.CSSProperties}>
            <Gauge aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">System Voltage</div>
            <div className="value">{voltage} V</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#a78bfa" } as React.CSSProperties}>
            <Zap aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card wide chart-panel">
        <div className="label">Real-Time Waveform Analytics</div>
        <TelemetryChart labels={labels} tempData={tempData} pressureData={pressureData} />
      </Glass>

      <Glass className="card compact log-panel">
        <TelemetryLog entries={logEntries} />
      </Glass>

    </div>
  );
};

export default SensorDetailPage;

{/*References
  Abramov, D. 2019. Making setInterval Declarative with React Hooks — overreacted. (Version 10) [Source code] Available at: < https://overreacted.io/making-setinterval-declarative-with-react-hooks/ > [Accessed 12 Sept. 2026].
  Gareth, D. 2021. Using setInterval and clearInterval with React Hooks.  (Version 10) [Source code] . Available at: < https://medium.com/@garethdavisrogers/using-setinterval-and-clearinterval-with-react-hooks-7fcf26dc8fdb > [Accessed 12 Sept. 2026].
  Gosavi,H.2026. GUI PANEL DESIGN ON The Time-Series Sensor Monitor. (Version 10) [Source code] Available at: https://codepen.io/HARSH-GOSAVI/details/NPbwGpN > [Accessed 12 September 2026].
  */}