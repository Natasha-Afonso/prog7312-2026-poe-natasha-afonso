import React from "react";

interface IncomingAlertToastProps {
  visible: boolean;
  sensorId: string;
  message: string;
  onOpen: () => void;
}

//(holoMailAuthor,2026)
export const IncomingAlertToast: React.FC<IncomingAlertToastProps> = ({
  visible,
  sensorId,
  message,
  onOpen,
}) => {
  return (
    <div className={`incoming-alert ${visible ? "visible" : ""}`}>

      <div className="alert-pulse"></div>

      <div>
        <small>INCOMING TRANSMISSION</small>
        <strong>{sensorId}</strong>
        <span>{message}</span>
      </div>

      <button onClick={onOpen}>OPEN</button>

    </div>
  );
};

export default IncomingAlertToast;

