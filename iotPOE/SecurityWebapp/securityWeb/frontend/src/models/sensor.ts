// src/models/sensor.ts

// plain type, not from either CodePen
export interface Sensor {
  id: string;
  location: string;
  type: string;
  status: "active" | "warning";
  lastReading: string;
}