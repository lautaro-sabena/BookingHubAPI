// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, renderHook } from "@testing-library/react";

const { push, auth } = vi.hoisted(() => ({
  push: vi.fn(),
  auth: { user: null as { id: string; role: string } | null, isLoading: false },
}));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push }) }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => auth }));

import { useRequireRole } from "../useRequireRole";

describe("useRequireRole", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.user = null;
    auth.isLoading = false;
  });
  afterEach(cleanup);

  it("waits while auth is loading: no redirect, not allowed", () => {
    auth.isLoading = true;

    const { result } = renderHook(() => useRequireRole("Owner"));

    expect(push).not.toHaveBeenCalled();
    expect(result.current.allowed).toBe(false);
    expect(result.current.authLoading).toBe(true);
  });

  it("sends a signed-out visitor to /login", () => {
    const { result } = renderHook(() => useRequireRole("Owner"));

    expect(push).toHaveBeenCalledWith("/login");
    expect(result.current.allowed).toBe(false);
  });

  it("sends a user with another role to /dashboard by default", () => {
    auth.user = { id: "u1", role: "Customer" };

    const { result } = renderHook(() => useRequireRole("Owner"));

    expect(push).toHaveBeenCalledWith("/dashboard");
    expect(result.current.allowed).toBe(false);
  });

  it("sends a user with another role to the given fallback", () => {
    auth.user = { id: "u1", role: "Owner" };

    renderHook(() => useRequireRole("Customer", "/dashboard/owner"));

    expect(push).toHaveBeenCalledWith("/dashboard/owner");
  });

  it("allows the right role without redirecting", () => {
    auth.user = { id: "u1", role: "Owner" };

    const { result } = renderHook(() => useRequireRole("Owner"));

    expect(push).not.toHaveBeenCalled();
    expect(result.current.allowed).toBe(true);
    expect(result.current.user).toBe(auth.user);
  });
});
