import React from "react";
import { NavLink } from "react-router-dom";
import { LayoutDashboard, Radio, Bell, Settings } from "lucide-react";

export const SideBar: React.FC = () => {
  return (
    <div className="sidebar">

      <div className="sidebar-top">

        <NavLink
          to="/dashboard"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <LayoutDashboard aria-hidden="true" />
          <span>Dashboard</span>
        </NavLink>

        <NavLink
          to="/sensors"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <Radio aria-hidden="true" />
          <span>Sensors</span>
        </NavLink>

        <NavLink
          to="/alerts"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <Bell aria-hidden="true" />
          <span>Alerts</span>
        </NavLink>

      </div>

      <div className="sidebar-bottom">

        <NavLink
          to="/settings"
          className={({ isActive }) => `side-item ${isActive ? "active" : ""}`}
        >
          <Settings aria-hidden="true" />
          <span>Settings</span>
        </NavLink>

      </div>

    </div>
  );
};

export default SideBar;