import { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import type { Page } from "../types";
import { PAGE_PATHS } from "./routes";

export function usePageNavigation() {
  const navigate = useNavigate();

  return useCallback(
    (page: Page) => {
      navigate(PAGE_PATHS[page]);
    },
    [navigate],
  );
}
