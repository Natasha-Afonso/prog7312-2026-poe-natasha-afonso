import React from "react";
import { LayoutGrid, ChevronRight } from "lucide-react";

interface TopBarProps {
  breadcrumbs?: string[];
  onOpenApps?: () => void;
  onOpenSettings?: () => void;
}

export const TopBar: React.FC<TopBarProps> = ({
  breadcrumbs = ["Dashboard"],
  onOpenApps = () => {},
 
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

     
    </div>
  );
};

export default TopBar;