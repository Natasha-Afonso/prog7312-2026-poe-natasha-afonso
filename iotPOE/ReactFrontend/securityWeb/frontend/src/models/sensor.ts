export interface Sensor {
  id: string;
  location: string;
  type: string;
  status: "active" | "warning";
  lastReading: string;
  filePath?: string;
  registeredAt?: string;
}