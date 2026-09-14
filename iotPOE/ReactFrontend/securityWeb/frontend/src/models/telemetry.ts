export interface SensorReading {
  id: number;
  sensorId: number;
  metricName: string;
  value: number;
  timestamp: string;
}

export interface TelemetryLogLine {
  timestamp: string;
  temp: string;
  pressure: string;
  voltage: string;
}