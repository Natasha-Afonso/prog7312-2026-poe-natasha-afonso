import React, { useEffect, useMemo, useState } from "react";
import { Power, RotateCcw, Radio } from "lucide-react";
import { Glass } from "../components/Glass";
import { TimelineChart } from "../components/TimelineChart";
import { useCommandStream } from "../context/CommandStreamContext.tsx";
import { getTimeline, searchDevices } from "../services/api.ts";
import type { CommandRecord, CommandLogEntry } from "../models/Command.ts";
import type { TimelineResponse } from "../models/timeline.ts";
import type { DeviceSearchResult } from "../models/search.ts";

// the API sends UTC times, the saved history comes without a "Z" on the end
const formatTime = (timestamp: string | null): string => {
  if (!timestamp) {
    return "—";
  }

  const iso = /(Z|[+-]\d\d:\d\d)$/.test(timestamp) ? timestamp : timestamp + "Z";
  return new Date(iso).toLocaleTimeString([], { hour12: false });
};

// "PUMP:A: off → on | RELAY:01: off → on"
const describeChanges = (record: CommandRecord): string => {
  return record.changes
    .map((change) => `${change.deviceIdentifier}: ${change.previousState ?? "?"} → ${change.newState}`)
    .join(" | ");
};

const describeLog = (entry: CommandLogEntry): string => {
  return `${entry.deviceIdentifier}: ${entry.previousState ?? "?"} → ${entry.newState ?? "?"}`;
};

// the alert state filter chips
const alertStateOptions = [
  { value: "", label: "Any state" },
  { value: "alerting", label: "Alerting" },
  { value: "offline", label: "Offline" },
  { value: "normal", label: "Normal" },
];

// which pill colour goes with which alert state
const alertPillClass = (alertState: string): string => {
  if (alertState === "alerting") return "warning";
  if (alertState === "offline") return "inactive";
  return "active";
};

const searchBoxStyle: React.CSSProperties = {
  padding: "8px 12px",
  borderRadius: "8px",
  border: "1px solid rgba(255,255,255,0.2)",
  background: "rgba(255,255,255,0.05)",
  color: "inherit",
  minWidth: "240px",
};

export const CommandStreamPage: React.FC = () => {
  const {
    devices,
    undoStack,
    history,
    error,
    busy,
    lastUpdated,
    sendDeviceCommand,
    shutdownAllActuators,
    undoLastCommand,
    clearError,
  } = useCommandStream();

  // the search and filter the operator has chosen, and the devices the API found
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState("");
  const [alertState, setAlertState] = useState("");
  const [results, setResults] = useState<DeviceSearchResult[]>([]);

  // which sensor's timeline is on the graph, and the timeline the API sent back
  const [timelineDevice, setTimelineDevice] = useState<string | null>(null);
  const [timeline, setTimeline] = useState<TimelineResponse | null>(null);

  const actuatorsOn = devices.filter((d) => d.isActuator && d.state === "on").length;
  const onlineCount = devices.filter((d) => d.status !== "offline").length;
  const sensors = devices.filter((d) => !d.isActuator);

  // the category chips come from the devices that exist
  const categories = useMemo(
    () => Array.from(new Set(devices.map((d) => d.category))).sort(),
    [devices]
  );

  const hasCriteria = query.trim() !== "" || category !== "" || alertState !== "";

  // a search the operator started: runs once typing pauses, and is recorded in the search history
  // the 400ms wait means typing "hvac" is one search and not four
  useEffect(() => {
    const timer = setTimeout(async () => {
      try {
        const found = await searchDevices(query.trim(), category, alertState, hasCriteria);
        setResults(found);
      } catch (err) {
        console.error("Failed to search the devices:", err);
      }
    }, 400);

    return () => clearTimeout(timer);
  }, [query, category, alertState, hasCriteria]);

  // quiet refresh every 2 seconds with the same search, not recorded in the search history
  useEffect(() => {
    const interval = setInterval(async () => {
      try {
        const found = await searchDevices(query.trim(), category, alertState, false);
        setResults(found);
      } catch (err) {
        console.error("Failed to refresh the devices:", err);
      }
    }, 2000);

    return () => clearInterval(interval);
  }, [query, category, alertState]);

  // pick the first sensor for the timeline once the devices have loaded
  useEffect(() => {
    if (!timelineDevice) {
      const firstSensor = devices.find((d) => !d.isActuator);

      if (firstSensor) {
        setTimelineDevice(firstSensor.deviceIdentifier);
      }
    }
  }, [devices, timelineDevice]);

  // load the selected sensor's timeline now and then every 2 seconds
  useEffect(() => {
    if (!timelineDevice) {
      return;
    }

    let cancelled = false;

    const loadTimeline = async () => {
      try {
        const data = await getTimeline(timelineDevice, 100);

        if (!cancelled) {
          setTimeline(data);
        }
      } catch (err) {
        console.error("Failed to load the timeline:", err);
      }
    };

    loadTimeline();
    const interval = setInterval(loadTimeline, 2000);

    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [timelineDevice]);

  // the points are already sorted by the API, the chart just draws them in order
  const chartLabels = useMemo(
    () => (timeline ? timeline.points.map((p) => formatTime(p.timestamp)) : []),
    [timeline]
  );
  const chartValues = useMemo(
    () => (timeline ? timeline.points.map((p) => p.value) : []),
    [timeline]
  );

  const clearSearch = () => {
    setQuery("");
    setCategory("");
    setAlertState("");
  };

  return (
    <div className="content">

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Devices Online</div>
            <div className="value">{onlineCount} / {devices.length}</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#1fd1b2" } as React.CSSProperties}>
            <Radio aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Actuators On</div>
            <div className="value">{actuatorsOn.toString().padStart(2, "0")}</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#f59e0b" } as React.CSSProperties}>
            <Power aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card compact metric-card">
        <div className="metric-content">
          <div>
            <div className="label">Undo Stack</div>
            <div className="value">{undoStack.length.toString().padStart(2, "0")}</div>
          </div>

          <div className="metric-icon-wrapper" style={{ "--iconColor": "#a78bfa" } as React.CSSProperties}>
            <RotateCcw aria-hidden="true" />
          </div>
        </div>
      </Glass>

      <Glass className="card wide">

        <div className="table-header">
          <div className="label">
            Device Control {lastUpdated && <small>• live, updated {lastUpdated.toLocaleTimeString([], { hour12: false })}</small>}
          </div>

          <button
            className="btn-secondary"
            disabled={busy || actuatorsOn === 0}
            onClick={shutdownAllActuators}
          >
            Shut Down All
          </button>

          <button
            className="btn-primary"
            disabled={busy || undoStack.length === 0}
            onClick={undoLastCommand}
          >
            Undo Last Command
          </button>
        </div>

        {/* search box and filter chips, the API does the searching */}
        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap", alignItems: "center", margin: "8px 0" }}>

          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by device ID or category, e.g. hvac"
            style={searchBoxStyle}
          />

          <button
            className={category === "" ? "btn-primary" : "btn-secondary"}
            onClick={() => setCategory("")}
          >
            All categories
          </button>

          {categories.map((name) => (
            <button
              key={name}
              className={category === name ? "btn-primary" : "btn-secondary"}
              onClick={() => setCategory(name)}
            >
              {name}
            </button>
          ))}

        </div>

        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap", alignItems: "center", marginBottom: "8px" }}>

          {alertStateOptions.map((option) => (
            <button
              key={option.value}
              className={alertState === option.value ? "btn-primary" : "btn-secondary"}
              onClick={() => setAlertState(option.value)}
            >
              {option.label}
            </button>
          ))}

          {hasCriteria && (
            <button className="btn-secondary" onClick={clearSearch}>
              Clear search
            </button>
          )}

          <small>Showing {results.length} of {devices.length} devices</small>

        </div>

        {error && (
          <div className="error-alert" onClick={clearError} style={{ cursor: "pointer" }}>
            {error} (click to dismiss)
          </div>
        )}

        <div className="table-wrapper">

          <table>

            <thead>
              <tr>
                <th>Device</th>
                <th>Category</th>
                <th>Reading / State</th>
                <th>Alert State</th>
                <th>Last Seen</th>
                <th>Manual Override</th>
              </tr>
            </thead>

            <tbody>

              {results.length === 0 && (
                <tr>
                  <td colSpan={6}>No devices match this search.</td>
                </tr>
              )}

              {results.map((device) => (
                <tr key={device.deviceIdentifier}>

                  <td>{device.deviceIdentifier}</td>
                  <td>{device.category}</td>

                  <td>
                    {device.isActuator ? (
                      <span className={`severity-pill ${device.state === "on" ? "active" : "inactive"}`}>
                        {device.state ?? "off"}
                      </span>
                    ) : (
                      device.lastReading ?? "—"
                    )}
                  </td>

                  <td>
                    <span className={`severity-pill ${alertPillClass(device.alertState)}`}>
                      {device.alertState}
                    </span>
                  </td>

                  <td>{formatTime(device.lastSeenAt)}</td>

                  <td>
                    {device.isActuator ? (
                      <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
                        <button
                          className="btn-secondary"
                          disabled={busy}
                          onClick={() => sendDeviceCommand(device.deviceIdentifier, "turn_on")}
                        >
                          On
                        </button>
                        <button
                          className="btn-secondary"
                          disabled={busy}
                          onClick={() => sendDeviceCommand(device.deviceIdentifier, "turn_off")}
                        >
                          Off
                        </button>
                        <button
                          className="btn-secondary"
                          disabled={busy}
                          onClick={() => sendDeviceCommand(device.deviceIdentifier, "toggle")}
                        >
                          Toggle
                        </button>
                      </div>
                    ) : (
                      "—"
                    )}
                  </td>

                </tr>
              ))}

            </tbody>

          </table>

        </div>

      </Glass>

      <Glass className="card wide chart-panel">

        <div className="label">
          Sensor Timeline {timeline && <small>• {timeline.count} points, sorted by timestamp</small>}
        </div>

        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap", margin: "8px 0" }}>
          {sensors.map((sensor) => (
            <button
              key={sensor.deviceIdentifier}
              className={sensor.deviceIdentifier === timelineDevice ? "btn-primary" : "btn-secondary"}
              onClick={() => setTimelineDevice(sensor.deviceIdentifier)}
            >
              {sensor.deviceIdentifier}
            </button>
          ))}
        </div>

        <TimelineChart
          labels={chartLabels}
          values={chartValues}
          seriesName={timelineDevice ?? "Timeline"}
        />

      </Glass>

      <Glass className="card compact log-panel">

        <div className="label">Undo Stack (newest on top)</div>

        <div className="telemetry-log">

          {undoStack.length === 0 && <div className="log-line">Nothing to undo.</div>}

          {undoStack.map((command, index) => (
            <div key={command.commandNumber} className="log-line">
              <span className="log-timestamp">[#{command.commandNumber}]</span>{" "}
              {index === 0 && <span className="log-cyan">NEXT UNDO </span>}
              <span className="log-amber">{command.action}</span> {describeChanges(command)}
            </div>
          ))}

        </div>

      </Glass>

      <Glass className="card compact log-panel">

        <div className="label">Command History</div>

        <div className="telemetry-log">

          {history.length === 0 && <div className="log-line">No commands sent yet.</div>}

          {history.map((entry) => (
            <div key={entry.id} className="log-line">
              <span className="log-timestamp">[{formatTime(entry.timestamp)}]</span>{" "}
              <span className="log-cyan">{entry.source.toUpperCase()}</span>{" "}
              <span className="log-amber">{entry.action}</span> {describeLog(entry)}
            </div>
          ))}

        </div>

      </Glass>

    </div>
  );
};

export default CommandStreamPage;