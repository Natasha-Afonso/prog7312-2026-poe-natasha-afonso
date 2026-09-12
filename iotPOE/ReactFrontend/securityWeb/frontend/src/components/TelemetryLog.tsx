import React, { useEffect, useRef } from "react";
import { Terminal } from "lucide-react";
import type { TelemetryLogLine } from "../models/telemetry.ts";

interface TelemetryLogProps {
  entries: TelemetryLogLine[];
}

//(Gosavi,2026)
export const TelemetryLog: React.FC<TelemetryLogProps> = ({ entries }) => {
  const logRef = useRef<HTMLDivElement>(null);

  // auto-scroll to bottom on new entries
  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [entries]);

  return (
    <>
      <div className="telemetry-log-header">
        <Terminal aria-hidden="true" />
        Live Telemetry Log
      </div>

      <div className="telemetry-log" ref={logRef}>

        {entries.map((entry, index) => (
          <div key={index} className="log-line">
            <span className="log-timestamp">[{entry.timestamp}]</span>{" "}
            T: <span className="log-amber">{entry.temp}</span>{" "}
            | P: <span className="log-cyan">{entry.pressure}</span>{" "}
            | V: {entry.voltage}
          </div>
        ))}

      </div>

    </>
  );
};

export default TelemetryLog;

{/*References
  Gosavi,H.2026. GUI PANEL DESIGN ON The Time-Series Sensor Monitor. (Version 10) [Source code] Available at: https://codepen.io/HARSH-GOSAVI/details/NPbwGpN > [Accessed 12 September 2026].
  */}