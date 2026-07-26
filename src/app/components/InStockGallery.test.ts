import { describe, expect, it } from "vitest";
import {
  getInStockGalleryThumbnailAriaLabel,
  isInStockGalleryImageFailed,
  markInStockGalleryImageFailed,
} from "./InStockGallery";

describe("InStockGallery image failure handling", () => {
  it("failed thumbnail is tracked and shows placeholder while others stay usable", () => {
    let failedIds: ReadonlySet<string> = new Set();
    failedIds = markInStockGalleryImageFailed(failedIds, "img-bad");

    expect(isInStockGalleryImageFailed(failedIds, "img-bad")).toBe(true);
    expect(isInStockGalleryImageFailed(failedIds, "img-good")).toBe(false);
    expect(getInStockGalleryThumbnailAriaLabel(0, 2, true)).toBe(
      "Show photograph 1 of 2, unavailable",
    );
    expect(getInStockGalleryThumbnailAriaLabel(1, 2, false)).toBe(
      "Show photograph 2 of 2",
    );
  });

  it("selecting a failed thumbnail keeps main fallback while selection remains available", () => {
    let failedIds: ReadonlySet<string> = new Set();
    failedIds = markInStockGalleryImageFailed(failedIds, "img-bad");

    const selectedId = "img-bad";
    const showMainFallback = isInStockGalleryImageFailed(failedIds, selectedId);
    const showThumbPlaceholder = isInStockGalleryImageFailed(failedIds, "img-bad");

    expect(showMainFallback).toBe(true);
    expect(showThumbPlaceholder).toBe(true);
    // Button remains meaningful for keyboard/AT users.
    expect(getInStockGalleryThumbnailAriaLabel(0, 3, true)).toContain(
      "unavailable",
    );
  });

  it("selecting a healthy photograph after a failed one shows the image again", () => {
    let failedIds: ReadonlySet<string> = new Set();
    failedIds = markInStockGalleryImageFailed(failedIds, "img-bad");

    let selectedId = "img-bad";
    expect(isInStockGalleryImageFailed(failedIds, selectedId)).toBe(true);

    selectedId = "img-good";
    expect(isInStockGalleryImageFailed(failedIds, selectedId)).toBe(false);
    expect(isInStockGalleryImageFailed(failedIds, "img-bad")).toBe(true);
  });

  it("marking the same failed id twice is idempotent and does not hide the gallery set", () => {
    let failedIds: ReadonlySet<string> = new Set();
    failedIds = markInStockGalleryImageFailed(failedIds, "img-1");
    const again = markInStockGalleryImageFailed(failedIds, "img-1");
    failedIds = markInStockGalleryImageFailed(again, "img-2");

    expect(failedIds).toEqual(new Set(["img-1", "img-2"]));
    expect(isInStockGalleryImageFailed(failedIds, "img-3")).toBe(false);
  });
});
