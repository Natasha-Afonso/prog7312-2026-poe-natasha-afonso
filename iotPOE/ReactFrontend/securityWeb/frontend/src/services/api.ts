import type { SensorReading } from "../models/telemetry";
import type { AlertDto } from "../models/alert.ts";

import type { Sensor } from "../models/sensor.ts"; // Imports sensor interface

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

// DELETE SENSOR
export async function deleteSensor(id: number): Promise<Sensor> {
  const response = await fetch(`${API_URL}/Sensors/${id}`, {
    method: "DELETE",
  });
  if (!response.ok) throw new Error("Failed to delete sensor");
  return response.json();
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



{/*
  References
  rudderz243.2026. rudderz243/insy7314-library.  (Version 2.0) [Source code]. Available at: <https://github.com/rudderz243/insy7314-library> [Accessed 17 Aug. 2026].
           */}