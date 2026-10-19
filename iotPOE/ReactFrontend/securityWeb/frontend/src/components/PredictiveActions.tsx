import React, { useEffect, useState } from "react";
import { Glass } from "./Glass";
import { useCommandStream } from "../context/CommandStreamContext.tsx";
import { getPredictiveActions } from "../services/api.ts"; 
import type { PredictiveAction } from "../models/prediction.ts"; 
import type { CommandAction } from "../models/Command.ts";

const pillLabel = (type: string): string => {
  if (type === "command") return "SUGGESTED ACTION";
  if (type === "forecast") return "PREDICTION";
  return "ATTENTION";
};

const pillClass = (type: string): string => {
  return type === "command" ? "active" : "warning";
};

// Renders the automated insights panel (Gami, 2025)
export const PredictiveActions: React.FC = () => {
  const { sendDeviceCommand, busy } = useCommandStream();
  const [actions, setActions] = useState<PredictiveAction[]>([]);

  // Poll every 2 seconds using the interval inside useEffect pattern (Abramov, 2019)
  useEffect(() => {
    let cancelled = false;

    const loadActions = async () => {
      try {
        const data = await getPredictiveActions();
        if (!cancelled) {
          setActions(data);
        }
      } catch (err) {
        console.error("Failed to load predictive actions:", err);
      }
    };

    loadActions();
    const interval = setInterval(loadActions, 2000);

    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  return (
    <Glass className="card wide">
      <div className="table-header">
        <div className="label">Predictive Actions • Automated Insights</div>
      </div>

      {actions.length === 0 && (
        <div className="log-line">No predicted actions right now, system is stable.</div>
      )}

      {actions.map((action) => (
        <div
          key={`${action.type}-${action.deviceIdentifier}-${action.action}`}
          style={{ display: "flex", gap: "12px", alignItems: "center", padding: "6px 0", flexWrap: "wrap" }}
        >
          <span className={`severity-pill ${pillClass(action.type)}`}>
            {pillLabel(action.type)}
          </span>

          <strong>{action.title}</strong>
          <small>{action.detail}</small>

          {action.type === "command" && action.deviceIdentifier && action.action && (
            <button
              className="btn-primary"
              disabled={busy}
              onClick={() =>
                sendDeviceCommand(action.deviceIdentifier as string, action.action as CommandAction)
              }
            >
              Execute
            </button>
          )}
        </div>
      ))}
    </Glass>
  );
};

export default PredictiveActions;

/*References
  Abramov, D. 2019. Making setInterval Declarative with React Hooks — overreacted. (Version 10) [Source code] Available at: < https://overreacted.io/making-setinterval-declarative-with-react-hooks/ > [Accessed 12 Sept. 2026].
*/