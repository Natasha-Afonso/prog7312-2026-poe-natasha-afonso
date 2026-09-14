import React, { useState, useEffect } from "react";
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
  // call variables that match backend
  const [deviceIdentifier, setDeviceIdentifier] = useState("");
  const [category, setCategory] = useState("Temperature");
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      // Build FormData payload 
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

      // Call API helper from app.ts
      const createdSensor = await createSensor(formData);

      if (onSensorRegistered) {
        onSensorRegistered(createdSensor);
      }

      // Reset form fields
      setDeviceIdentifier("");
      setCategory("Temperature");
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
            <h2 className="modal-title">Register Sensor</h2>
            <p className="modal-subtitle">
              Add a sensor
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
              <label>Sensor ID</label>
              <input
                type="text"
                value={deviceIdentifier}
                onChange={(e) => setDeviceIdentifier(e.target.value)}
                placeholder="e.g. SS:04"
                required
              />
            </div>

            <div className="form-group">
              <label>Category</label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              >
                <option value="Temperature">Temperature</option>
                <option value="Humidity">Humidity</option>
                <option value="Motion">Motion</option>
                <option value="Power">Power</option>
                <option value="Pressure">Pressure</option>
              </select>
            </div>

            <div className="form-group">
              <label>Location</label>
              <input
                type="number"
                value={deploymentNodeId}
                onChange={(e) => setDeploymentNodeId(Number(e.target.value))}
                min={1}
                required
              />
            </div>

            <div className="form-group">
              <label>Status</label>
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
              <label>Baseline / Initial Reading (Optional)</label>
              <input
                type="text"
                value={lastReading}
                onChange={(e) => setLastReading(e.target.value)}
                placeholder="e.g. 24.5 °C"
              />
            </div>

            <div className="form-group">
              <label>Attachment File (Optional )</label>
              <input
                type="file"
                onChange={(e) => {
                  if (e.target.files && e.target.files[0]) {
                    setFile(e.target.files[0]);
                  }
                }}
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? "Registering..." : "Register Device"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};