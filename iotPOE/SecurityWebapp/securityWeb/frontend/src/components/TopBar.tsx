import React from "react";
import { LayoutGrid, ChevronRight, Search, Sliders, Bell } from "lucide-react";

interface TopBarProps {
  breadcrumbs?: string[];
  onOpenApps?: () => void;
  onOpenSettings?: () => void;
}

export const TopBar: React.FC<TopBarProps> = ({
  breadcrumbs = ["Dashboard"],
  onOpenApps = () => {},
  onOpenSettings = () => {},
}) => {
  return (
    <div className="navbar">

      <div className="nav-left">

        <div className="nav-grid" onClick={onOpenApps}>
          <LayoutGrid aria-hidden="true" />
        </div>

        <div className="breadcrumbs">

          {breadcrumbs.map((crumb, index) => (
            <React.Fragment key={crumb}>

              <div
                className={`crumb-chip ${
                  index === breadcrumbs.length - 1 ? "current" : ""
                }`}
              >
                {crumb}
              </div>

              {index < breadcrumbs.length - 1 && (
                <ChevronRight className="crumb-sep-icon" aria-hidden="true" />
              )}

            </React.Fragment>
          ))}

        </div>

      </div>

      <div className="nav-center">

        <div className="global-search">
          <Search aria-hidden="true" />
          <input placeholder="Search sensors, alerts..." />
        </div>

      </div>

      <div className="nav-right">

        <div className="nav-icon clickable" onClick={onOpenSettings}>
          <Sliders aria-hidden="true" />
        </div>

        <div className="nav-icon">
          <Bell aria-hidden="true" />
          <span className="notif-dot"></span>
        </div>

        <span className="username">Admin</span>

        <img src="https://i.pravatar.cc/40" className="avatar" alt="User avatar" />

      </div>

    </div>
  );
};

export default TopBar;