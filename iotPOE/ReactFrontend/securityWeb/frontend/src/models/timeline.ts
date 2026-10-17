// shapes of what GET /api/Timeline/{deviceIdentifier} sends back

export interface TimelinePoint {
  timestamp: string;
  value: number;
  metricName: string;
}

export interface TimelineResponse {
  deviceIdentifier: string;
  category: string;
  count: number;
  points: TimelinePoint[]; // already in time order, oldest first
}