import { describe, expect, it } from "vitest";
import { shouldShowAdminPortfolioEmptyState } from "./adminPortfolioListState";

describe("shouldShowAdminPortfolioEmptyState", () => {
  it("hides empty state while loading", () => {
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: true,
        hasLoadedSuccessfully: false,
        error: null,
        itemCount: 0,
      }),
    ).toBe(false);
  });

  it("hides empty state when an error is present", () => {
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: false,
        hasLoadedSuccessfully: false,
        error: "The portfolio request could not be completed. Please try again.",
        itemCount: 0,
      }),
    ).toBe(false);
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: false,
        hasLoadedSuccessfully: true,
        error: "The portfolio request could not be completed. Please try again.",
        itemCount: 0,
      }),
    ).toBe(false);
  });

  it("hides empty state before the first successful load", () => {
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: false,
        hasLoadedSuccessfully: false,
        error: null,
        itemCount: 0,
      }),
    ).toBe(false);
  });

  it("shows empty state after a successful load with zero items", () => {
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: false,
        hasLoadedSuccessfully: true,
        error: null,
        itemCount: 0,
      }),
    ).toBe(true);
  });

  it("hides empty state after a successful load with one item", () => {
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: false,
        hasLoadedSuccessfully: true,
        error: null,
        itemCount: 1,
      }),
    ).toBe(false);
  });

  it("does not treat a failed load as a successful empty list", () => {
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: false,
        hasLoadedSuccessfully: false,
        error: "The portfolio request could not be completed. Please try again.",
        itemCount: 0,
      }),
    ).toBe(false);
  });

  it("hides empty state while a reload is in progress after a prior successful empty load", () => {
    // load() resets hasLoadedSuccessfully to false together with loading=true
    expect(
      shouldShowAdminPortfolioEmptyState({
        loading: true,
        hasLoadedSuccessfully: false,
        error: null,
        itemCount: 0,
      }),
    ).toBe(false);
  });
});
