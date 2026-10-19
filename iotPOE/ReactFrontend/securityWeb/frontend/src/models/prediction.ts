export interface PredictiveAction {
  type: "command" | "forecast" | "highlight";
  title: string;
  detail: string;
  deviceIdentifier: string | null;
  action: string | null;
  score: number;
}