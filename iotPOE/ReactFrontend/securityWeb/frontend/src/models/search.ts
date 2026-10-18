import type { DeviceState } from "./Command.ts";
 
// one result from GET /api/Devices/search: the live device plus its alert state
export interface DeviceSearchResult extends DeviceState {
  alertState: string; // "alerting", "offline" or "normal"
}