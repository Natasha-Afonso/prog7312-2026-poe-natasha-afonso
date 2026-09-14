import React from "react";
import { Eye, Pencil, Trash2 } from "lucide-react";
import type { Sensor } from "../models/sensor.ts";

interface SensorTableProps {
  sensors: Sensor[];
  onView: (id: number) => void;
  onEdit: (id: number) => void;
  onDelete: (id: number) => void;
}

export const SensorTable: React.FC<SensorTableProps> = ({
  sensors,
  onView,
  onEdit,
  onDelete,
}) => {
  return (
    <div className="table-wrapper">

      <table>

        <thead>
          <tr>
            <th>Device Identifier</th>
            <th>Location</th>
            <th>Category</th>
            <th>Status</th>
            <th>Last Reading</th>
            <th className="actions-col">Actions</th>
          </tr>
        </thead>

        <tbody>

          {sensors.map((sensor) => (
            <tr key={sensor.id}>

              <td>{sensor.deviceIdentifier}</td>
              <td>{sensor.deploymentNode?.name ?? "—"}</td>
              <td>{sensor.category}</td>

              <td>
                <span className={`severity-pill ${sensor.status}`}>
                  {sensor.status}
                </span>
              </td>

              <td>{sensor.lastReading ?? "—"}</td>

              <td className="table-actions">

                <button className="action-btn view" onClick={() => onView(sensor.id)}>
                  <Eye aria-hidden="true" />
                </button>

                <button className="action-btn edit" onClick={() => onEdit(sensor.id)}>
                  <Pencil aria-hidden="true" />
                </button>

                <button className="action-btn delete" onClick={() => onDelete(sensor.id)}>
                  <Trash2 aria-hidden="true" />
                </button>

              </td>

            </tr>
          ))}

        </tbody>

      </table>

      <div className="pagination">
        <span>Showing 1-{sensors.length} of {sensors.length}</span>
        <div className="pager">
          <button>{"<"}</button>
          <button className="active">1</button>
          <button>{">"}</button>
        </div>
      </div>

    </div>
  );
};

export default SensorTable;