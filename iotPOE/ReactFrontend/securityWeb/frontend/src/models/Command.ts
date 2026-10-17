// shapes of what the Devices and Commands endpoints send back

// one live device from GET /api/Devices
export interface DeviceState {
  sensorId: number;
  deviceIdentifier: string;
  category: string;
  status: string;
  state: string | null; // "on" or "off" for actuators, null for sensors
  lastReading: string | null;
  lastValue: number | null;
  lastSeenAt: string | null;
  isActuator: boolean;
}

// one device that a command changed, with the state it had before
export interface DeviceChange {
  deviceIdentifier: string;
  previousState: string | null;
  newState: string;
}

// one command on the undo stack
export interface CommandRecord {
  commandNumber: number;
  action: string;
  source: string;
  timestamp: string;
  changes: DeviceChange[];
}

// one saved row from GET /api/Commands/history
export interface CommandLogEntry {
  id: number;
  deviceIdentifier: string;
  action: string;
  previousState: string | null;
  newState: string | null;
  source: string;
  timestamp: string;
}

export type CommandAction = "turn_on" | "turn_off" | "toggle";