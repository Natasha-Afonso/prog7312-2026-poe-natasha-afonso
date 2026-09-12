// src/components/SensorTable.tsx
import React from "react";
import { Eye, Pencil, Trash2 } from "lucide-react";
import type { Sensor } from "../models/sensor";

interface SensorTableProps {
  sensors: Sensor[];
  onView: (id: string) => void;
  onEdit: (id: string) => void;
  onDelete: (id: string) => void;
}

//(glassDashboardAuthor,2026)
export const SensorTable: React.FC<SensorTableProps> = ({
  sensors,
  onView,
  onEdit,
  onDelete,
}) => {
  return (
    <div className="table-wrapper">

      <table>

        {/* columns swapped from Timestamp/Speed/Plate/Severity to Smart-X sensor fields */}

        <thead>
          <tr>
            <th>Sensor ID</th>
            <th>Location</th>
            <th>Type</th>
            <th>Status</th>
            <th>Last Reading</th>
            <th className="actions-col">Actions</th>
          </tr>
        </thead>

        <tbody>

          {sensors.map((sensor) => (
            <tr key={sensor.id}>

              <td>{sensor.id}</td>
              <td>{sensor.location}</td>
              <td>{sensor.type}</td>

              <td>
                {/* reusing the pen's severity-pill class, values changed to active/warning for sensor status */}
                <span className={`severity-pill ${sensor.status}`}>
                  {sensor.status === "active" ? "Active" : "Warning"}
                </span>
              </td>

              <td>{sensor.lastReading}</td>

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

{/*References
  glassDashboardAuthor. 2026. <Pen title>. (Version X) [Source code] Available at: <URL> [Accessed DATE].
  */}