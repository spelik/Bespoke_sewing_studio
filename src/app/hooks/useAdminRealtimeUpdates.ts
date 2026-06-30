import { useEffect, useRef, useState } from "react";
import {
  startAdminRealtimeConnection,
  type AdminRealtimeEvent,
  type AdminRealtimeStatus,
} from "../../api/adminRealtimeApi";

interface UseAdminRealtimeUpdatesOptions {
  enabled: boolean;
  onEvent(event: AdminRealtimeEvent): void;
}

interface UseAdminRealtimeUpdatesResult {
  status: AdminRealtimeStatus;
  lastEventAt: string | null;
}

export function useAdminRealtimeUpdates({
  enabled,
  onEvent,
}: UseAdminRealtimeUpdatesOptions): UseAdminRealtimeUpdatesResult {
  const [status, setStatus] = useState<AdminRealtimeStatus>("disconnected");
  const [lastEventAt, setLastEventAt] = useState<string | null>(null);
  const onEventRef = useRef(onEvent);

  useEffect(() => {
    onEventRef.current = onEvent;
  }, [onEvent]);

  useEffect(() => {
    if (!enabled) {
      setStatus("disconnected");
      return;
    }

    const connection = startAdminRealtimeConnection({
      onStatusChange: setStatus,
      onEvent(event) {
        setLastEventAt(event.occurredAt);
        onEventRef.current(event);
      },
    });

    return () => connection.stop();
  }, [enabled]);

  return { status, lastEventAt };
}
