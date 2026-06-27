import { useCallback, useEffect, useState } from "react";
import { ApiError } from "../../api/apiClient";
import {
  addAdminOrderNote,
  getAdminApiErrorMessage,
  getAdminOrder,
  getAdminOrders,
  type AdminOrderDetail,
  type AdminOrderListItem,
  type AdminOrderStatus,
  updateAdminOrderStatus,
} from "../../api/ordersApi";

interface UseAdminOrdersResult {
  orders: AdminOrderListItem[];
  selectedOrder: AdminOrderDetail | null;
  isLoading: boolean;
  isDetailLoading: boolean;
  isSaving: boolean;
  error: string | null;
  selectOrder(id: string): Promise<void>;
  changeStatus(status: AdminOrderStatus): Promise<void>;
  addNote(text: string): Promise<boolean>;
  reload(): Promise<void>;
  clearSelection(): void;
}

export function useAdminOrders(onUnauthorized: () => void): UseAdminOrdersResult {
  const [orders, setOrders] = useState<AdminOrderListItem[]>([]);
  const [selectedOrder, setSelectedOrder] = useState<AdminOrderDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleError = useCallback(
    (requestError: unknown) => {
      if (requestError instanceof ApiError && [401, 403].includes(requestError.status)) {
        onUnauthorized();
        return;
      }

      setError(getAdminApiErrorMessage(requestError));
    },
    [onUnauthorized],
  );

  const reload = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setOrders(await getAdminOrders());
    } catch (requestError) {
      handleError(requestError);
    } finally {
      setIsLoading(false);
    }
  }, [handleError]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const selectOrder = useCallback(
    async (id: string) => {
      setIsDetailLoading(true);
      setError(null);
      try {
        setSelectedOrder(await getAdminOrder(id));
      } catch (requestError) {
        handleError(requestError);
      } finally {
        setIsDetailLoading(false);
      }
    },
    [handleError],
  );

  const updateListItem = useCallback((detail: AdminOrderDetail) => {
    setOrders((currentOrders) =>
      currentOrders.map((order) =>
        order.id === detail.id
          ? {
              ...order,
              status: detail.status,
              description: detail.description,
              clientName: detail.client.fullName,
              clientEmail: detail.client.email,
              clientPhone: detail.client.phone,
            }
          : order,
      ),
    );
  }, []);

  const changeStatus = useCallback(
    async (status: AdminOrderStatus) => {
      if (!selectedOrder || selectedOrder.status === status) {
        return;
      }

      setIsSaving(true);
      setError(null);
      try {
        const updatedOrder = await updateAdminOrderStatus(selectedOrder.id, status);
        setSelectedOrder(updatedOrder);
        updateListItem(updatedOrder);
      } catch (requestError) {
        handleError(requestError);
      } finally {
        setIsSaving(false);
      }
    },
    [handleError, selectedOrder, updateListItem],
  );

  const addNote = useCallback(
    async (text: string) => {
      if (!selectedOrder || !text.trim()) {
        return false;
      }

      setIsSaving(true);
      setError(null);
      try {
        const updatedOrder = await addAdminOrderNote(selectedOrder.id, text);
        setSelectedOrder(updatedOrder);
        updateListItem(updatedOrder);
        return true;
      } catch (requestError) {
        handleError(requestError);
        return false;
      } finally {
        setIsSaving(false);
      }
    },
    [handleError, selectedOrder, updateListItem],
  );

  return {
    orders,
    selectedOrder,
    isLoading,
    isDetailLoading,
    isSaving,
    error,
    selectOrder,
    changeStatus,
    addNote,
    reload,
    clearSelection: () => setSelectedOrder(null),
  };
}
