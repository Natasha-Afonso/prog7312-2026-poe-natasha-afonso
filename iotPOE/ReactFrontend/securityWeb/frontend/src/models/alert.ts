export type AlertStatus = "unread" | "acknowledged" | "resolved";

export interface SensorAlert {
  id: string;
  sensorId: string; // device identifier, for display only
  sensorNumericId: number; // FIXED: added so "View Sensor" can navigate to the correct route
  location: string;
  metric: string;
  value: string;
  threshold: string;
  title: string;
  message: string;
  timestamp: string;
  status: AlertStatus;
}


export interface AlertDto {
  id: number;
  sensorId: number;
  title: string;
  message: string;
  metric: string;
  value: string;
  threshold: string;
  status: AlertStatus;
  timestamp: string;
  sensor?: {
    deviceIdentifier: string;
    deploymentNode?: {
      name: string;
    };
  };
}


export function mapAlertDto(dto: AlertDto): SensorAlert {
  return {
    id: dto.id.toString(),
    sensorId: dto.sensor?.deviceIdentifier ?? `Sensor ${dto.sensorId}`,
    sensorNumericId: dto.sensorId,
    location: dto.sensor?.deploymentNode?.name ?? "Unknown",
    metric: dto.metric,
    value: dto.value,
    threshold: dto.threshold,
    title: dto.title,
    message: dto.message,
    timestamp: new Date(dto.timestamp).toLocaleString(),
    status: dto.status,
  };
}