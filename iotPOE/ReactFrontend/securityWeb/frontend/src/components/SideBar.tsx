import React from "react";
import { NavLink, useLocation } from "react-router-dom";
import { Home, Radio, Activity, Network, Bell } from "lucide-react";

export const SideBar: React.FC = () => {
  const location = useLocation();

  // the telemetry module also covers the sensor detail pages
  const telemetryActive =
    location.pathname.startsWith("/dashboard") || location.pathname.startsWith("/sensorDetail");

  return (
    <div className="sidebar">

      <div className="sidebar-top">

        {/* back to the gateway page with the three module cards */}
        <NavLink
          to="/landing"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <Home aria-hidden="true" />
          <span>Home</span>
        </NavLink>

        {/* module 1 (Part 1) */}
        <NavLink to="/dashboard" className={`side-item ${telemetryActive ? "active" : ""}`}>
          <Radio aria-hidden="true" />
          <span>Sensor Telemetry</span>
        </NavLink>

        {/* module 2 (Part 2) */}
        <NavLink
          to="/commands"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <Activity aria-hidden="true" />
          <span>Command Stream</span>
        </NavLink>

        {/* module 3 (final PoE), shown but not clickable yet */}
        <div
          className="side-item"
          style={{ opacity: 0.45, cursor: "not-allowed" }}
          title="Network Topology and Mesh Routing arrives in the final PoE"
        >
          <Network aria-hidden="true" />
          <span>Network Topology</span>
        </div>

        <NavLink
          to="/alerts"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <Bell aria-hidden="true" />
          <span>Alerts</span>
        </NavLink>

      </div>

    </div>
  );
};

export default SideBar;