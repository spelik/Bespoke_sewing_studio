import { describe, expect, it } from "vitest";
import { resolveApiAssetUrl } from "./resolveApiAssetUrl";

describe("resolveApiAssetUrl", () => {
  it("returns null for null and undefined", () => {
    expect(resolveApiAssetUrl(null, "/api")).toBeNull();
    expect(resolveApiAssetUrl(undefined, "/api")).toBeNull();
  });

  it("returns null for empty and whitespace-only strings", () => {
    expect(resolveApiAssetUrl("", "/api")).toBeNull();
    expect(resolveApiAssetUrl("   ", "/api")).toBeNull();
    expect(resolveApiAssetUrl("\t\n", "http://127.0.0.1:5099/api")).toBeNull();
  });

  it("leaves absolute HTTP and HTTPS URLs unchanged", () => {
    expect(resolveApiAssetUrl("http://cdn.example.com/image.jpg", "/api")).toBe(
      "http://cdn.example.com/image.jpg",
    );
    expect(resolveApiAssetUrl("https://cdn.example.com/image.jpg", "/api")).toBe(
      "https://cdn.example.com/image.jpg",
    );
    expect(
      resolveApiAssetUrl("https://cdn.example.com/image.jpg", "http://127.0.0.1:5099/api"),
    ).toBe("https://cdn.example.com/image.jpg");
  });

  it("leaves protocol-relative URLs unchanged", () => {
    expect(resolveApiAssetUrl("//cdn.example.com/image.jpg", "/api")).toBe(
      "//cdn.example.com/image.jpg",
    );
    expect(
      resolveApiAssetUrl("//cdn.example.com/image.jpg", "https://example.com/api"),
    ).toBe("//cdn.example.com/image.jpg");
  });

  it("resolves root-relative assets against an absolute API base", () => {
    expect(
      resolveApiAssetUrl("/api/portfolio/images/id", "http://127.0.0.1:5099/api"),
    ).toBe("http://127.0.0.1:5099/api/portfolio/images/id");
    expect(
      resolveApiAssetUrl("/api/portfolio/images/id", "https://example.com/api"),
    ).toBe("https://example.com/api/portfolio/images/id");
  });

  it("resolves path-relative assets against an absolute API base with trailing slash", () => {
    expect(
      resolveApiAssetUrl("api/portfolio/images/id", "https://example.com/api/"),
    ).toBe("https://example.com/api/portfolio/images/id");
  });

  it("keeps root-relative assets when API base is relative /api or /api/", () => {
    expect(resolveApiAssetUrl("/api/portfolio/images/id", "/api")).toBe(
      "/api/portfolio/images/id",
    );
    expect(resolveApiAssetUrl("/api/portfolio/images/id", "/api/")).toBe(
      "/api/portfolio/images/id",
    );
  });

  it("normalizes path-relative assets when API base is relative /api", () => {
    expect(resolveApiAssetUrl("api/portfolio/images/id", "/api")).toBe(
      "/api/portfolio/images/id",
    );
  });

  it("does not throw TypeError for production relative API base /api", () => {
    expect(() =>
      resolveApiAssetUrl("/api/portfolio/images/11111111-1111-1111-1111-111111111111", "/api"),
    ).not.toThrow();
    expect(
      resolveApiAssetUrl("/api/portfolio/images/11111111-1111-1111-1111-111111111111", "/api"),
    ).toBe("/api/portfolio/images/11111111-1111-1111-1111-111111111111");
  });

  it("rejects disallowed schemes that are not used by current API contracts", () => {
    expect(resolveApiAssetUrl("javascript:alert(1)", "/api")).toBeNull();
    expect(resolveApiAssetUrl("data:image/png;base64,abc", "/api")).toBeNull();
    expect(resolveApiAssetUrl("blob:https://example.com/uuid", "http://127.0.0.1:5099/api")).toBeNull();
  });

  it("runs without window or document (Node test environment)", () => {
    expect(typeof globalThis.window).toBe("undefined");
    expect(typeof globalThis.document).toBe("undefined");
    expect(resolveApiAssetUrl("/api/portfolio/images/id", "/api")).toBe(
      "/api/portfolio/images/id",
    );
    expect(
      resolveApiAssetUrl("/api/portfolio/images/id", "http://127.0.0.1:5099/api"),
    ).toBe("http://127.0.0.1:5099/api/portfolio/images/id");
  });
});
