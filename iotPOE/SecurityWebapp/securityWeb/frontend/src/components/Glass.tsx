import React from "react";

//(glassDashboardAuthor,2026)
interface GlassProps {
  children: React.ReactNode;
  className?: string;
}

export const Glass: React.FC<GlassProps> = ({ children, className = "" }) => {
  return (
    <div className={`glass ${className}`}>
      {children}
    </div>
  );
};

export default Glass;

{/*References
  glassDashboardAuthor. 2026. <Pen title>. (Version X) [Source code] Available at: <URL> [Accessed DATE].
  */}