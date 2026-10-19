import type { SensorReading } from "../models/telemetry";
import type { AlertDto } from "../models/alert.ts";
import type { DeviceState, CommandRecord, CommandLogEntry, CommandAction } from "../models/Command.ts";

import type { Sensor } from "../models/sensor.ts"; // Imports sensor interface
import type { TimelineResponse } from "../models/timeline.ts";
import type { DeviceSearchResult} from "../models/search.ts";
import type { PredictiveAction } from "../models/prediction.ts";

const API_URL = "http://localhost:8080/api"; 



//rudderz,2026
// GET ALL SENSORS
export async function getSensors(): Promise<Sensor[]> {
  const response = await fetch(`${API_URL}/Sensors`);
  if (!response.ok) throw new Error("Failed to fetch sensors");
  return response.json();
}

// GET SINGLE SENSOR BY ID
export async function getSensor(id: number): Promise<Sensor> {
  const response = await fetch(`${API_URL}/Sensors/${id}`);
  if (!response.ok) throw new Error("Failed to fetch sensor details");
  return response.json();
}

// CREATE NEW SENSOR 
export async function createSensor(formData: FormData): Promise<Sensor> {
  const response = await fetch(`${API_URL}/Sensors`, {
    method: "POST",
    body: formData, // Browser automatically sets boundary header
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Failed to register sensor");
  }

  return response.json();
}



export interface IngestResult {
  ingested: number;
  skipped: string[];
  alertsRaised: number;
}

// SEND A JAGGED BATCH OF READINGS
export async function ingestBatch(sensorIds: string[], rawBatches: number[][]): Promise<IngestResult> {
  const response = await fetch(`${API_URL}/Telemetry/ingest-batch`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sensorIds, rawBatches }),
  });
  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Failed to send batch");
  }
  return response.json();
}

// DELETE SENSOR (204 has no body so no .json())
export async function deleteSensor(id: number): Promise<void> {
  const response = await fetch(`${API_URL}/Sensors/${id}`, { method: "DELETE" });
  if (!response.ok) throw new Error("Failed to delete sensor");
}
export const getTelemetryBySensorId = async (
  sensorId: string | number
): Promise<SensorReading[]> => {
  const response = await fetch(`${API_URL}/Telemetry/${sensorId}`);
  if (!response.ok) {
    throw new Error(`Failed to fetch telemetry for sensor ID: ${sensorId}`);
  }
  return response.json();
};

// ALERTS API SERVICES

export async function getAlerts(): Promise<AlertDto[]> {
  const response = await fetch(`${API_URL}/Alerts`);
  if (!response.ok) throw new Error("Failed to fetch alerts");
  return response.json();
}

export async function acknowledgeAlert(id: number): Promise<void> {
  const getResponse = await fetch(`${API_URL}/Alerts/${id}`);
  if (!getResponse.ok) throw new Error("Failed to fetch alert before acknowledging");
  const alert = await getResponse.json();

  const putResponse = await fetch(`${API_URL}/Alerts/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...alert, status: "acknowledged" }),
  });

  if (!putResponse.ok) throw new Error("Failed to acknowledge alert");
}

// the API sends a plain text message when it refuses a command, this reads it
async function readError(response: Response, fallback: string): Promise<string> {
  const text = await response.text();
  return text || fallback;
}

// COMMAND STREAM API SERVICES

// GET ALL LIVE DEVICES (from the in-memory registry)
export async function getDevices(): Promise<DeviceState[]> {
  const response = await fetch(`${API_URL}/Devices`);
  if (!response.ok) throw new Error("Failed to fetch devices");
  return response.json();
}

// SEND A COMMAND TO ONE ACTUATOR
export async function sendCommand(deviceIdentifier: string, action: CommandAction): Promise<CommandRecord> {
  const response = await fetch(`${API_URL}/Commands`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ deviceIdentifier, action }),
  });
  if (!response.ok) throw new Error(await readError(response, "Command failed"));
  return response.json();
}

// TURN OFF EVERY ACTUATOR THAT IS ON (one undo puts them all back)
export async function shutdownAll(): Promise<CommandRecord> {
  const response = await fetch(`${API_URL}/Commands/shutdown-all`, { method: "POST" });
  if (!response.ok) throw new Error(await readError(response, "Shutdown failed"));
  return response.json();
}

// UNDO THE NEWEST COMMAND
export async function undoCommand(): Promise<CommandRecord> {
  const response = await fetch(`${API_URL}/Commands/undo`, { method: "POST" });
  if (!response.ok) throw new Error(await readError(response, "Undo failed"));
  return response.json();
}

// THE UNDO STACK, NEWEST FIRST
export async function getCommandStack(): Promise<CommandRecord[]> {
  const response = await fetch(`${API_URL}/Commands/stack`);
  if (!response.ok) throw new Error("Failed to fetch the undo stack");
  return response.json();
}

// THE SAVED COMMAND HISTORY, NEWEST FIRST
export async function getCommandHistory(limit: number = 50): Promise<CommandLogEntry[]> {
  const response = await fetch(`${API_URL}/Commands/history?limit=${limit}`);
  if (!response.ok) throw new Error("Failed to fetch the command history");
  return response.json();
}

// THE TIMELINE OF ONE DEVICE, ALREADY IN TIME ORDER (oldest first)
export async function getTimeline(deviceIdentifier: string, limit: number = 100): Promise<TimelineResponse> {
  const response = await fetch(`${API_URL}/Timeline/${encodeURIComponent(deviceIdentifier)}?limit=${limit}`);
  if (!response.ok) throw new Error(`Failed to fetch the timeline for ${deviceIdentifier}`);
  return response.json();
}
 

// SEARCH AND FILTER DEVICES (the API does the filtering)
// track = true records the search in the search history, the automatic refresh uses false
export async function searchDevices(
  query: string,
  category: string,
  alertState: string,
  track: boolean
): Promise<DeviceSearchResult[]> {
  const params = new URLSearchParams();
 
  if (query) params.set("query", query);
  if (category) params.set("category", category);
  if (alertState) params.set("alertState", alertState);
  params.set("track", track ? "true" : "false");
 
  const response = await fetch(`${API_URL}/Devices/search?${params.toString()}`);
  if (!response.ok) throw new Error(await readError(response, "Search failed"));
  return response.json();
}
 


//get predictions
export async function getPredictiveActions(): Promise<PredictiveAction[]> {
  const response = await fetch(`${API_URL}/Prediction`);
  if (!response.ok) throw new Error("Failed to fetch predictive actions");
  return response.json();
}

// simulator controls 
export async function startSimulatorRamp(device: string): Promise<string> {
  const response = await fetch(`${API_URL}/Simulator/ramp?device=${encodeURIComponent(device)}`, { method: "POST" });
  if (!response.ok) throw new Error("Failed to start ramp");
  return response.text();
}

export async function stopSimulatorRamp(device: string): Promise<string> {
  const response = await fetch(`${API_URL}/Simulator/cool?device=${encodeURIComponent(device)}`, { method: "POST" });
  if (!response.ok) throw new Error("Failed to stop ramp");
  return response.text();
}


{/*
  References
  rudderz243.2026. rudderz243/insy7314-library.  (Version 2.0) [Source code]. Available at: <https://github.com/rudderz243/insy7314-library> [Accessed 17 Aug. 2026].
           */}