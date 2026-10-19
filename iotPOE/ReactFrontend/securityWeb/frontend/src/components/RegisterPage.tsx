import React, { useState, useEffect } from "react";
import { UploadCloud, FileText } from "lucide-react";
import { createSensor } from "../services/api";
import type { Sensor } from "../models/sensor";

interface RegisterSensorModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSensorRegistered?: (sensor: Sensor) => void;
}

export const RegisterSensorModal: React.FC<RegisterSensorModalProps> = ({
  isOpen,
  onClose,
  onSensorRegistered,
}) => {
  const [deviceIdentifier, setDeviceIdentifier] = useState("");
  const [category, setCategory] = useState("Environmental");
  const [deploymentNodeId, setDeploymentNodeId] = useState<number>(1);
  const [status, setStatus] = useState("Active");
  const [lastReading, setLastReading] = useState("");
  const [file, setFile] = useState<File | null>(null);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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

  // --- FILE VALIDATION LOGIC ---
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;

    // 1. Check file size (Max 5MB)
    if (selected.size > 5 * 1024 * 1024) {
      setError("File is too large. Maximum size is 5MB.");
      e.target.value = ""; // Reset input
      return;
    }

    // 2. Check file type
    const validTypes = ['application/pdf', 'image/jpeg', 'image/png', 'text/plain'];
    if (!validTypes.includes(selected.type)) {
      setError("Invalid file type. Please upload a PDF, JPG, PNG, or TXT.");
      e.target.value = "";
      return;
    }

    setFile(selected);
    setError(null); // Clear errors if valid
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const formData = new FormData();
      formData.append("DeviceIdentifier", deviceIdentifier);
      formData.append("Category", category);
      formData.append("DeploymentNodeId", deploymentNodeId.toString());
      formData.append("Status", status);

      if (lastReading) {
        formData.append("LastReading", lastReading);
      }
      if (file) {
        formData.append("attachment", file); 
      }

      const createdSensor = await createSensor(formData);

      if (onSensorRegistered) {
        onSensorRegistered(createdSensor);
      }

      // Reset form fields
      setDeviceIdentifier("");
      setCategory("Environmental");
      setDeploymentNodeId(1);
      setStatus("Active");
      setLastReading("");
      setFile(null);

      onClose();
    } catch (err: any) {
      setError(err.message || "Failed to register sensor.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-dark" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h2 className="modal-title">Register Asset</h2>
            <p className="modal-subtitle">
              Deploy a new IoT sensor to the Smart Estate
            </p>
          </div>
          <button className="modal-close" onClick={onClose} type="button">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && <div className="error-alert">{error}</div>}

            <div className="form-group">
              <label>Device Identifier (MAC / Unique ID)</label>
              <input
                type="text"
                value={deviceIdentifier}
                onChange={(e) => setDeviceIdentifier(e.target.value)}
                placeholder="e.g. HVAC:SRV"
                required
              />
            </div>

            <div className="form-group">
              <label>Sensor Category</label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              >
                <option value="Environmental">Environmental (Temp / Humidity)</option>
                <option value="Power">Power (Wattage / Load)</option>
                <option value="Water">Water (Flow / Leak)</option>
                <option value="Occupancy">Occupancy (Motion / Access)</option>
                <option value="Actuator">Actuator (Smart Switch / Valve)</option>
              </select>
            </div>

            <div className="form-group">
              <label>Deployment Node ID (Location)</label>
              <input
                type="number"
                value={deploymentNodeId}
                onChange={(e) => setDeploymentNodeId(Number(e.target.value))}
                min={1}
                required
              />
            </div>

            <div className="form-group">
              <label>Initial Status</label>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value)}
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="Maintenance">Maintenance</option>
              </select>
            </div>

            <div className="form-group">
              <label>Baseline Reading (Optional)</label>
              <input
                type="text"
                value={lastReading}
                onChange={(e) => setLastReading(e.target.value)}
                placeholder="e.g. 24.5"
              />
            </div>

            {/* PREMIUM FILE UPLOAD */}
            <div className="form-group">
              <label>Maintenance Log / Datasheet (Optional)</label>
              <div style={{
                border: "1px dashed rgba(255,255,255,0.2)",
                borderRadius: "12px",
                padding: "16px",
                textAlign: "center",
                position: "relative",
                background: "rgba(255,255,255,0.02)"
              }}>
                <input
                  type="file"
                  onChange={handleFileChange}
                  style={{
                    position: "absolute",
                    inset: 0,
                    width: "100%",
                    height: "100%",
                    opacity: 0,
                    cursor: "pointer"
                  }}
                  accept=".pdf,.png,.jpg,.jpeg,.txt"
                />
                
                {file ? (
                  <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: "8px", color: "#1fd1b2" }}>
                    <FileText size={18} />
                    <span style={{ fontSize: "13px", fontWeight: 500 }}>{file.name}</span>
                  </div>
                ) : (
                  <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: "4px", color: "rgba(255,255,255,0.5)" }}>
                    <UploadCloud size={24} />
                    <span style={{ fontSize: "13px" }}>Click to upload PDF or Image</span>
                    <span style={{ fontSize: "11px", opacity: 0.7 }}>Max size: 5MB</span>
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? "Registering..." : "Register Asset"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};