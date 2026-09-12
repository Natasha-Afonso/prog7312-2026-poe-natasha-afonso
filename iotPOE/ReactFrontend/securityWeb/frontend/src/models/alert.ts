export type AlertStatus = "unread" | "acknowledged" | "resolved";

export interface SensorAlert {
  id: string;
  sensorId: string;
  location: string;
  metric: string;
  value: string;
  threshold: string;
  title: string;
  message: string;
  timestamp: string;
  status: AlertStatus;
}