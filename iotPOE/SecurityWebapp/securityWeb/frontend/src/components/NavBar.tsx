import {
  Bell,
  Settings,
  Search,
  LayoutGrid,
} from 'lucide-react';

interface NavbarProps {
  onOpenConfig?: () => void;
  onOpenApps?: () => void;
}

const Navbar = ({
  onOpenConfig,
  onOpenApps,
}: NavbarProps) => {
  return (
    <div className="navbar">

      {/* LEFT SIDE */}
      <div className="nav-left">

        {/* App Grid */}
        <div
          className="nav-grid"
          onClick={onOpenApps}
        >
          <LayoutGrid size={18} />
        </div>

        {/* Breadcrumbs */}
        <div className="breadcrumbs">

          <div className="crumb-chip">
            SMART-X
          </div>

          <div className="crumb-sep-icon">
            /
          </div>

          <div className="crumb-chip current">
            Monitoring
          </div>

        </div>

      </div>


      {/* CENTER */}
      <div className="nav-center">

        <div className="global-search">

          <Search size={17} />

          <input
            type="text"
            placeholder="Search sensors, zones..."
          />

        </div>

      </div>


      {/* RIGHT SIDE */}
      <div className="nav-right">

        {/* Settings */}
        <div
          className="nav-icon clickable"
          onClick={onOpenConfig}
        >
          <Settings size={18} />
        </div>


        {/* Notifications */}
        <div className="nav-icon">

          <Bell size={18} />

          <span className="notif-dot"></span>

        </div>


        {/* System Name */}
        <span className="username">
          SMART-X
        </span>

      </div>

    </div>
  );
};

export default Navbar;

