import React, { createContext, useContext, useState, useEffect, useCallback } from "react";
import type { DeviceState, CommandRecord, CommandLogEntry, CommandAction } from "../models/Command.ts"
import {
  getDevices,
  sendCommand,
  shutdownAll,
  undoCommand,
  getCommandStack,
  getCommandHistory,
} from "../services/api.ts";

// the command stream state lives here, ABOVE the routes, so moving to another page and back
// does not reset it and the polling keeps running (same context idea as AlertContext)
interface CommandStreamContextValue {
  devices: DeviceState[];
  undoStack: CommandRecord[];
  history: CommandLogEntry[];
  error: string | null;
  busy: boolean;
  lastUpdated: Date | null;
  sendDeviceCommand: (deviceIdentifier: string, action: CommandAction) => Promise<void>;
  shutdownAllActuators: () => Promise<void>;
  undoLastCommand: () => Promise<void>;
  clearError: () => void;
}

const CommandStreamContext = createContext<CommandStreamContextValue | undefined>(undefined);

export const CommandStreamProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [devices, setDevices] = useState<DeviceState[]>([]);
  const [undoStack, setUndoStack] = useState<CommandRecord[]>([]);
  const [history, setHistory] = useState<CommandLogEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  // loads devices, the undo stack and the history at the same time
  const refresh = useCallback(async () => {
    try {
      const [deviceList, stack, logs] = await Promise.all([
        getDevices(),
        getCommandStack(),
        getCommandHistory(30),
      ]);

      setDevices(deviceList);
      setUndoStack(stack);
      setHistory(logs);
      setLastUpdated(new Date());
    } catch (err) {
      console.error("Failed to refresh the command stream:", err);
    }
  }, []);

  // poll every 2 seconds (interval inside useEffect pattern, Abramov, 2019)
  useEffect(() => {
    refresh();
    const interval = setInterval(refresh, 2000);
    return () => clearInterval(interval);
  }, [refresh]);

  // runs a command, shows the API's message if it is refused, then refreshes straight away
  const runCommand = useCallback(
    async (work: () => Promise<unknown>) => {
      setBusy(true);
      setError(null);

      try {
        await work();
        await refresh();
      } catch (err: any) {
        setError(err.message || "Command failed");
      } finally {
        setBusy(false);
      }
    },
    [refresh]
  );

  const sendDeviceCommand = useCallback(
    (deviceIdentifier: string, action: CommandAction) =>
      runCommand(() => sendCommand(deviceIdentifier, action)),
    [runCommand]
  );

  const shutdownAllActuators = useCallback(() => runCommand(() => shutdownAll()), [runCommand]);

  const undoLastCommand = useCallback(() => runCommand(() => undoCommand()), [runCommand]);

  const clearError = useCallback(() => setError(null), []);

  return (
    <CommandStreamContext.Provider
      value={{
        devices,
        undoStack,
        history,
        error,
        busy,
        lastUpdated,
        sendDeviceCommand,
        shutdownAllActuators,
        undoLastCommand,
        clearError,
      }}
    >
      {children}
    </CommandStreamContext.Provider>
  );
};

export const useCommandStream = (): CommandStreamContextValue => {
  const context = useContext(CommandStreamContext);

  if (!context) {
    throw new Error("useCommandStream must be used within a CommandStreamProvider");
  }

  return context;
};

export default CommandStreamContext;

/*References
  Abramov, D. 2019. Making setInterval Declarative with React Hooks — overreacted. (Version 10) [Source code] Available at: < https://overreacted.io/making-setinterval-declarative-with-react-hooks/ > [Accessed 12 Sept. 2026].
  */