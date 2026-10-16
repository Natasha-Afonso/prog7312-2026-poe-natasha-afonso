import React, { useState, useEffect } from "react";
import { ingestBatch } from "../services/api";
import type { IngestResult } from "../services/api";

interface IngestBatchModalProps {
  isOpen: boolean;
  onClose: () => void;
  onIngested?: (result: IngestResult) => void;
}

// one row = one sensor and the readings it sent
// the rows together make the jagged array that the backend expects
interface BatchRow {
  sensorId: string;
  readings: string;
}

export const IngestBatchModal: React.FC<IngestBatchModalProps> = ({
  isOpen,
  onClose,
  onIngested,
}) => {
  const [rows, setRows] = useState<BatchRow[]>([{ sensorId: "", readings: "" }]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<IngestResult | null>(null);

  // Close overlay on 'Escape' key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };

    if (isOpen) {
      window.addEventListener("keydown", handleKeyDown);
    }

    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  // copy the array and change one row, state must not be changed directly (React, 2026)
  const updateRow = (index: number, field: keyof BatchRow, value: string) => {
    setRows((prev) =>
      prev.map((row, i) => (i === index ? { ...row, [field]: value } : row))
    );
  };

  const addRow = () => {
    setRows((prev) => [...prev, { sensorId: "", readings: "" }]);
  };

  const removeRow = (index: number) => {
    setRows((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setResult(null);

    const sensorIds: string[] = [];
    const rawBatches: number[][] = [];

    // turn each row of text into a sensor id and an array of numbers
    for (let i = 0; i < rows.length; i++) {
      const id = rows[i].sensorId.trim();

      const numbers = rows[i].readings
        .split(",")
        .map((text) => text.trim())
        .filter((text) => text !== "")
        .map((text) => Number(text));

      if (id === "") {
        setError("Row " + (i + 1) + ": enter a sensor ID.");
        return;
      }

      if (numbers.length === 0 || numbers.some((n) => Number.isNaN(n))) {
        setError("Row " + (i + 1) + ": readings must be numbers separated by commas.");
        return;
      }

      sensorIds.push(id);
      rawBatches.push(numbers);
    }

    setLoading(true);

    try {
      const ingestResult = await ingestBatch(sensorIds, rawBatches);
      setResult(ingestResult);

      if (onIngested) {
        onIngested(ingestResult);
      }
    } catch (err: any) {
      setError(err.message || "Failed to send the batch.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-dark" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h2 className="modal-title">Send Telemetry Batch</h2>
            <p className="modal-subtitle">
              One row per sensor, readings separated by commas
            </p>
          </div>
          <button className="modal-close" onClick={onClose} type="button">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && <div className="error-alert">{error}</div>}

            {rows.map((row, index) => (
              <div key={index}>
                <div className="form-group">
                  <label>Sensor ID</label>
                  <input
                    type="text"
                    value={row.sensorId}
                    onChange={(e) => updateRow(index, "sensorId", e.target.value)}
                    placeholder="e.g. SS:01"
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Readings</label>
                  <input
                    type="text"
                    value={row.readings}
                    onChange={(e) => updateRow(index, "readings", e.target.value)}
                    placeholder="e.g. 27.4, 28.1, 36.2"
                    required
                  />
                </div>

                {rows.length > 1 && (
                  <button
                    type="button"
                    className="btn-secondary"
                    onClick={() => removeRow(index)}
                  >
                    Remove row
                  </button>
                )}
              </div>
            ))}

            <button type="button" className="btn-secondary" onClick={addRow}>
              Add sensor row
            </button>

            {result && (
              <div className="form-group">
                <p className="modal-subtitle">
                  Saved {result.ingested} readings. Alerts raised: {result.alertsRaised}.
                </p>

                {result.skipped.length > 0 && (
                  <div className="error-alert">
                    Sensors not found and skipped: {result.skipped.join(", ")}
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>
              Close
            </button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? "Sending..." : "Send batch"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default IngestBatchModal;

/*References
  React. 2026. Updating Arrays in State. [online] Available at: <https://react.dev/learn/updating-arrays-in-state> [Accessed DD Month 2026] (read the page and check the year before keeping this reference)
  */