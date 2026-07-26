/** When to show the Admin Portfolio items empty state (not during load/error). */
export function shouldShowAdminPortfolioEmptyState(options: {
  loading: boolean;
  hasLoadedSuccessfully: boolean;
  error: string | null;
  itemCount: number;
}): boolean {
  return (
    !options.loading &&
    options.hasLoadedSuccessfully &&
    options.error == null &&
    options.itemCount === 0
  );
}
