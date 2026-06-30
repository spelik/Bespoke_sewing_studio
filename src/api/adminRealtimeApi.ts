import { getAccessToken } from "./authTokenStorage";
import { appConfig } from "../config/appConfig";

const RECORD_SEPARATOR = "\u001e";

export type AdminRealtimeStatus = "connecting" | "connected" | "disconnected";

export type AdminRealtimeEventType =
  | "OrderCreated"
  | "OrderUpdated"
  | "ContactMessageCreated"
  | "ContactMessageUpdated"
  | "EmailDeliveryLogChanged";

export interface AdminRealtimeEvent {
  eventType: AdminRealtimeEventType;
  entity: "Order" | "ContactMessage" | "EmailDeliveryLog";
  entityId: string;
  referenceNumber: string | null;
  occurredAt: string;
}

interface SignalRNegotiateResponse {
  connectionToken?: string;
  connectionId?: string;
  availableTransports?: Array<{ transport: string }>;
}

interface SignalRInvocationMessage {
  type?: number;
  target?: string;
  arguments?: unknown[];
}

export interface AdminRealtimeConnection {
  stop(): void;
}

interface StartAdminRealtimeConnectionOptions {
  onStatusChange(status: AdminRealtimeStatus): void;
  onEvent(event: AdminRealtimeEvent): void;
}

export function startAdminRealtimeConnection({
  onStatusChange,
  onEvent,
}: StartAdminRealtimeConnectionOptions): AdminRealtimeConnection {
  let socket: WebSocket | null = null;
  let disposed = false;
  let reconnectTimer: number | null = null;

  const scheduleReconnect = () => {
    if (disposed || reconnectTimer !== null) {
      return;
    }

    onStatusChange("disconnected");
    reconnectTimer = window.setTimeout(() => {
      reconnectTimer = null;
      void connect();
    }, 5000);
  };

  const connect = async () => {
    const token = getAccessToken();
    if (!token) {
      onStatusChange("disconnected");
      return;
    }

    onStatusChange("connecting");

    try {
      const negotiation = await negotiate(token);
      if (disposed) {
        return;
      }

      const connectionToken = negotiation.connectionToken ?? negotiation.connectionId;
      if (!connectionToken) {
        throw new Error("SignalR negotiation did not return a connection token.");
      }

      socket = new WebSocket(buildWebSocketUrl(connectionToken, token));

      socket.addEventListener("open", () => {
        socket?.send(JSON.stringify({ protocol: "json", version: 1 }) + RECORD_SEPARATOR);
      });

      socket.addEventListener("message", (message) => {
        for (const frame of String(message.data).split(RECORD_SEPARATOR)) {
          if (!frame) {
            continue;
          }

          handleSignalRFrame(frame, onStatusChange, onEvent);
        }
      });

      socket.addEventListener("close", () => {
        socket = null;
        scheduleReconnect();
      });

      socket.addEventListener("error", () => {
        socket?.close();
      });
    } catch {
      scheduleReconnect();
    }
  };

  void connect();

  return {
    stop() {
      disposed = true;
      if (reconnectTimer !== null) {
        window.clearTimeout(reconnectTimer);
        reconnectTimer = null;
      }

      socket?.close();
      socket = null;
    },
  };
}

async function negotiate(token: string): Promise<SignalRNegotiateResponse> {
  const response = await fetch(
    `${getApiRootUrl()}/hubs/admin-notifications/negotiate?negotiateVersion=1`,
    {
      method: "POST",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
    },
  );

  if (!response.ok) {
    throw new Error("SignalR negotiation failed.");
  }

  return (await response.json()) as SignalRNegotiateResponse;
}

function handleSignalRFrame(
  frame: string,
  onStatusChange: (status: AdminRealtimeStatus) => void,
  onEvent: (event: AdminRealtimeEvent) => void,
) {
  const parsed = JSON.parse(frame) as SignalRInvocationMessage;

  if (Object.keys(parsed).length === 0) {
    onStatusChange("connected");
    return;
  }

  if (parsed.type !== 1 || parsed.target !== "AdminDataChanged") {
    return;
  }

  const candidate = parsed.arguments?.[0];
  if (!isAdminRealtimeEvent(candidate)) {
    return;
  }

  onEvent(candidate);
}

function isAdminRealtimeEvent(value: unknown): value is AdminRealtimeEvent {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Partial<AdminRealtimeEvent>;
  return Boolean(
    candidate.entityId &&
      candidate.eventType &&
      candidate.entity &&
      candidate.occurredAt,
  );
}

function buildWebSocketUrl(connectionToken: string, token: string): string {
  const root = getApiRootUrl();
  const wsRoot = root.replace(/^http:/, "ws:").replace(/^https:/, "wss:");
  const search = new URLSearchParams({
    id: connectionToken,
    access_token: token,
  });

  return `${wsRoot}/hubs/admin-notifications?${search.toString()}`;
}

function getApiRootUrl(): string {
  const baseUrl = appConfig.apiBaseUrl.replace(/\/$/, "");
  return baseUrl.endsWith("/api") ? baseUrl.slice(0, -4) : baseUrl;
}
