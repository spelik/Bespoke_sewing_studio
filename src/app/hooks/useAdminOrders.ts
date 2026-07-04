import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "../../api/apiClient";
import {
  addAdminOrderNote,
  deleteAdminOrder,
  deleteAdminOrderAttachment,
  getAdminApiErrorMessage,
  getAdminOrder,
  getAdminOrders,
  type AdminOrderDetail,
  type AdminOrderListQuery,
  type AdminOrderListItem,
  type AdminOrderStatus,
  updateAdminOrderStatus,
} from "../../api/ordersApi";

interface UseAdminOrdersResult {
  orders: AdminOrderListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  selectedOrder: AdminOrderDetail | null;
  isLoading: boolean;
  isDetailLoading: boolean;
  isSaving: boolean;
  deletingAttachmentId: string | null;
  deletingOrderId: string | null;
  error: string | null;
  selectOrder(id: string): Promise<void>;
  changeStatus(status: AdminOrderStatus): Promise<void>;
  addNote(text: string): Promise<boolean>;
  deleteAttachment(attachmentId: string): Promise<boolean>;
  deleteOrder(orderId: string): Promise<boolean>;
  reload(): Promise<void>;
  clearSelection(): void;
}

export function useAdminOrders(
  onUnauthorized: () => void,
  query: AdminOrderListQuery,
): UseAdminOrdersResult {
  const [orders, setOrders] = useState<AdminOrderListItem[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedOrder, setSelectedOrder] = useState<AdminOrderDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [deletingAttachmentId, setDeletingAttachmentId] = useState<string | null>(null);
  const [deletingOrderId, setDeletingOrderId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const latestRequestIdRef = useRef(0);

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
    const requestId = ++latestRequestIdRef.current;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getAdminOrders(query);
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      setOrders(result.items);
      setPage(result.page);
      setPageSize(result.pageSize);
      setTotalItems(result.totalItems);
      setTotalPages(result.totalPages);
    } catch (requestError) {
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      handleError(requestError);
    } finally {
      if (requestId === latestRequestIdRef.current) {
        setIsLoading(false);
      }
    }
  }, [handleError, query]);

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
        await reload();
      } catch (requestError) {
        handleError(requestError);
      } finally {
        setIsSaving(false);
      }
    },
    [handleError, reload, selectedOrder],
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
        await reload();
        return true;
      } catch (requestError) {
        handleError(requestError);
        return false;
      } finally {
        setIsSaving(false);
      }
    },
    [handleError, reload, selectedOrder],
  );


  const deleteAttachment = useCallback(
    async (attachmentId: string) => {
      if (!selectedOrder) {
        return false;
      }

      setDeletingAttachmentId(attachmentId);
      setError(null);
      try {
        const updatedOrder = await deleteAdminOrderAttachment(selectedOrder.id, attachmentId);
        setSelectedOrder(updatedOrder);
        await reload();
        return true;
      } catch (requestError) {
        handleError(requestError);
        return false;
      } finally {
        setDeletingAttachmentId(null);
      }
    },
    [handleError, reload, selectedOrder],
  );


  const deleteOrder = useCallback(
    async (orderId: string) => {
      setDeletingOrderId(orderId);
      setError(null);
      try {
        await deleteAdminOrder(orderId);
        setSelectedOrder((currentOrder) =>
          currentOrder?.id === orderId ? null : currentOrder,
        );
        await reload();
        return true;
      } catch (requestError) {
        handleError(requestError);
        return false;
      } finally {
        setDeletingOrderId(null);
      }
    },
    [handleError, reload],
  );

  return {
    orders,
    page,
    pageSize,
    totalItems,
    totalPages,
    selectedOrder,
    isLoading,
    isDetailLoading,
    isSaving,
    deletingAttachmentId,
    deletingOrderId,
    error,
    selectOrder,
    changeStatus,
    addNote,
    deleteAttachment,
    deleteOrder,
    reload,
    clearSelection: () => setSelectedOrder(null),
  };
}
