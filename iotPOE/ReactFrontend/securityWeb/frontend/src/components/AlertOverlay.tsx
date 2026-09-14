// src/components/AlertOverlay.tsx
import React, { useEffect, useRef, useState } from "react";
import { Eye, Check, List, X } from "lucide-react";
import { SoundEngine } from "../utils/soundEngine.ts";
import type { SensorAlert } from "../models/alert.ts";


interface AlertOverlayProps {
  alert: SensorAlert | null;
  isOpen: boolean;
  onClose: () => void;
  onAcknowledge: (id: string) => void;
  onViewSensor: (sensorId: string) => void;
  onViewAllAlerts: () => void;
}

export const AlertOverlay: React.FC<AlertOverlayProps> = ({
  alert,
  isOpen,
  onClose,
  onAcknowledge,
  onViewSensor,
  onViewAllAlerts,
}) => {
  const [visible, setVisible] = useState(false);
  const [typedTitle, setTypedTitle] = useState("");
  const [typedBody, setTypedBody] = useState("");
  const [showEventData, setShowEventData] = useState(false);
  const [showDecision, setShowDecision] = useState(false);
  const [showActions, setShowActions] = useState(false);
  const [confirmMessage, setConfirmMessage] = useState<string | null>(null);

  const cancelledRef = useRef(false);

  /**
   * Typewriter Helper Function
   * typeText() routine to handle state updates in React.

   * (holoMailAuthor, 2026)
   */
  const typeText = (
    text: string,
    setter: React.Dispatch<React.SetStateAction<string>>,
    speed = 30
  ): Promise<void> => {
    return new Promise((resolve) => {
      let index = 0;
      setter("");

      const writeCharacter = () => {
        if (cancelledRef.current || index >= text.length) {
          resolve();
          return;
        }

        const character = text.charAt(index);
        setter((prev) => prev + character);

        // Character audio trigger probability (holoMailAuthor, 2026)
        if (character.trim() && Math.random() > 0.18) {
          SoundEngine.key(character);
        }

        index++;

        let delay = speed + Math.random() * 22;

        if (/[.,!?]/.test(character)) {
          delay += 60;
        }

        setTimeout(writeCharacter, delay);
      };

      writeCharacter();
    });
  };

  const wait = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));

  /**
   * Holographic Transmission Sequence
   * (holoMailAuthor, 2026)
   */
  useEffect(() => {
    if (!isOpen || !alert) {
      return;
    }

    cancelledRef.current = false;

    const runSequence = async () => {
      await SoundEngine.init();

      SoundEngine.open();
      setVisible(true);

      await wait(100);
      SoundEngine.materialize();
      await wait(1050);

      if (cancelledRef.current) return;
      await typeText(alert.title, setTypedTitle, 38);
      SoundEngine.packet();
      await wait(220);

      if (cancelledRef.current) return;
      await typeText(alert.message, setTypedBody, 17);
      await wait(180);

      if (cancelledRef.current) return;
      SoundEngine.dataReveal();
      setShowEventData(true);
      await wait(500);

      if (cancelledRef.current) return;
      setShowDecision(true);
      SoundEngine.packet();
      await wait(240);

      if (cancelledRef.current) return;
      setShowActions(true);
      SoundEngine.dataReveal();
    };

    runSequence();

    return () => {
      cancelledRef.current = true;
    };
  }, [isOpen, alert]);

  const handleAction = async (action: "view" | "acknowledge", message: string) => {
    await SoundEngine.ensureActive();
    SoundEngine.confirm();

    setConfirmMessage(message);
    setTimeout(() => setConfirmMessage(null), 3200);

    if (action === "view" && alert) {
       onViewSensor(alert.sensorNumericId.toString());
    }

    if (action === "acknowledge" && alert) {
      onAcknowledge(alert.id);
    }
  };

  const handleClose = () => {
    setVisible(false);
    setTypedTitle("");
    setTypedBody("");
    setShowEventData(false);
    setShowDecision(false);
    setShowActions(false);
    onClose();
  };

  if (!isOpen || !alert) {
    return null;
  }

  /* Markup structure, class naming, and holographic overlay design adapted from CodePen (holoMailAuthor, 2026) */
  return (
    <div className="terminal alert-terminal">

      <div className="grid-floor"></div>
      <div className="scanlines"></div>

      <button className="modal-close alert-close" onClick={handleClose}>
        <X aria-hidden="true" />
      </button>

      <div className="transmission-zone alert-zone">

        <article className={`hologram ${visible ? "visible" : ""}`} aria-live="polite">

          <div className="holo-corners">
            <i></i><i></i><i></i><i></i>
          </div>

          <div className="holo-header">

            <div className="transmission-badge">
              <span></span>
              PRIORITY ALERT
            </div>

            <span className="message-code">ALERT // {alert.id}</span>

          </div>

          <div className="sender-line">

            <div className="sender-symbol">{alert.sensorId.charAt(0)}</div>

            <div>
              <span className="micro-label">SENSOR NODE</span>
              <strong>{alert.sensorId}</strong>
              <small>{alert.location}</small>
            </div>

          </div>

          <div className="separator">
            <span></span>
          </div>

          <div className="message-body">

            <span className="micro-label red">STATUS // THRESHOLD EXCEEDED</span>

            <h1>
              {typedTitle}
              {typedTitle.length < alert.title.length && <span className="type-cursor">▋</span>}
            </h1>

            <p className="typed-copy">
              {typedBody}
              {typedTitle.length === alert.title.length &&
                typedBody.length < alert.message.length && (
                  <span className="type-cursor">▋</span>
                )}
            </p>

            <div className={`event-data ${showEventData ? "visible" : ""}`}>

              <div>
                <span>SENSOR</span>
                <strong>{alert.sensorId}</strong>
              </div>

              <div>
                <span>METRIC</span>
                <strong>{alert.metric}</strong>
              </div>

              <div>
                <span>VALUE</span>
                <strong>{alert.value}</strong>
              </div>

              <div>
                <span>THRESHOLD</span>
                <strong>{alert.threshold}</strong>
              </div>

            </div>

            <div className={`decision-title ${showDecision ? "visible" : ""}`}>
              <span></span>
              SELECT RESOLUTION PROTOCOL
              <span></span>
            </div>

            <div className={`actions ${showActions ? "visible" : ""}`}>

              <button onClick={() => handleAction("view", "OPENING SENSOR DETAIL")}>
                <span className="action-index">01</span>
                <span className="action-icon"><Eye aria-hidden="true" /></span>
                <strong>VIEW SENSOR</strong>
                <small>INSPECT LIVE TELEMETRY</small>
              </button>

              <button onClick={() => handleAction("acknowledge", "ALERT ACKNOWLEDGED")}>
                <span className="action-index">02</span>
                <span className="action-icon"><Check aria-hidden="true" /></span>
                <strong>ACKNOWLEDGE</strong>
                <small>MARK ALERT AS SEEN</small>
              </button>

              <button onClick={onViewAllAlerts}>
                <span className="action-index">03</span>
                <span className="action-icon"><List aria-hidden="true" /></span>
                <strong>VIEW ALERTS</strong>
                <small>OPEN ALERT HISTORY</small>
              </button>

            </div>

          </div>

          <footer className="holo-footer">
            <span>ALERT LOGGED {alert.timestamp}</span>
            <span>ENCRYPTION: AES-256</span>
          </footer>

          <div className="holo-scan"></div>

        </article>

      </div>

      <div className={`confirmation ${confirmMessage ? "visible" : ""}`}>
        <span className="confirmation-icon">✓</span>
        <div>
          <small>PROTOCOL ACCEPTED</small>
          <strong>{confirmMessage}</strong>
        </div>
      </div>

    </div>
  );
};

export default AlertOverlay;