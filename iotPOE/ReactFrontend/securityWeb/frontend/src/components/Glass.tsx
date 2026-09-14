import React from "react";


interface GlassProps {
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}

export const Glass: React.FC<GlassProps> = ({ children, className = "", onClick }) => {
  return (
   <div className={`glass ${className}`} onClick={onClick}>
      {children}
    </div>
  );
};

export default Glass;
