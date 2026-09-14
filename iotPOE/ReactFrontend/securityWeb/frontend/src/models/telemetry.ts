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

/** 
References
Rudman, G., 2026. insy7314-library. (Version 1.0) [Source Code] Available at: < https://github.com/rudderz243/insy7314-library > [Accessed 29 August 2026]
*/