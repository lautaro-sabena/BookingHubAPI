// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";

const { get, post, put, del, auth } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn(),
  auth: { user: { id: "u1", email: "a@b.com", role: "Customer" } as { id: string; email: string; role: string } | null },
}));
vi.mock("@/lib/api", () => ({ default: { get, post, put, delete: del } }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => ({ user: auth.user, isLoading: false }) }));

import { queryKeys } from "../queries/keys";
import { useAvailableSlots } from "../queries/useAvailability";
import { useFavoriteServiceIds, useAddFavorite } from "../queries/useFavorites";
import { useCancelReservation, useCreateReservation, useReservations } from "../queries/useReservations";
import { useCreateService } from "../queries/useServices";
import { useSaveWorkingHours } from "../queries/useWorkingHours";
import { defaultWeek } from "@/lib/workingHours";

let queryClient: QueryClient;

function wrapper({ children }: { children: ReactNode }) {
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

const invalidated = (key: readonly unknown[]) => queryClient.getQueryState(key)?.isInvalidated === true;

describe("queryKeys", () => {
  it("scopes user data by user id and nests variants under a prefix", () => {
    expect(queryKeys.reservations.list("u1")).toEqual(["reservations", "u1", "list"]);
    expect(queryKeys.reservations.list("u1")).not.toEqual(queryKeys.reservations.list("u2"));
    expect(queryKeys.reservations.list("u1").slice(0, 2)).toEqual(queryKeys.reservations.all("u1"));
    expect(queryKeys.services.detail("s1").slice(0, 1)).toEqual(queryKeys.services.all);
    expect(queryKeys.services.ownerList("u1").slice(0, 1)).toEqual(queryKeys.services.all);
    expect(queryKeys.availability.slots("s1", "2030-01-07").slice(0, 1)).toEqual(queryKeys.availability.all);
  });
});

describe("query hooks", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.user = { id: "u1", email: "a@b.com", role: "Customer" };
    queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  });
  afterEach(cleanup);

  it("useAvailableSlots sends the date input value as a query param, untouched", async () => {
    get.mockResolvedValue({ data: [{ startTime: "2030-01-07T09:00:00-03:00", endTime: "2030-01-07T09:30:00-03:00", isAvailable: true }] });

    const { result } = renderHook(() => useAvailableSlots("svc1", "2030-01-07"), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(get).toHaveBeenCalledWith("/availability/svc1", { params: { date: "2030-01-07" } });
  });

  it("useAvailableSlots does not request anything until a date is chosen", () => {
    renderHook(() => useAvailableSlots("svc1", ""), { wrapper });
    expect(get).not.toHaveBeenCalled();
  });

  it("useReservations waits for a signed-in user and keeps each user's cache separate", async () => {
    auth.user = null;
    const { result, rerender } = renderHook(() => useReservations(), { wrapper });
    expect(get).not.toHaveBeenCalled();
    expect(result.current.isLoading).toBe(false);

    auth.user = { id: "u2", email: "x@y.com", role: "Owner" };
    get.mockResolvedValue({ data: { items: [{ id: "r1" }] } });
    rerender();

    await waitFor(() => expect(result.current.data).toEqual([{ id: "r1" }]));
    expect(queryClient.getQueryData(queryKeys.reservations.list("u2"))).toEqual([{ id: "r1" }]);
    expect(queryClient.getQueryData(queryKeys.reservations.list("u1"))).toBeUndefined();
  });

  it("cancelling a reservation refreshes the user's reservations and every availability, nobody else's", async () => {
    put.mockResolvedValue({});
    queryClient.setQueryData(queryKeys.reservations.list("u1"), []);
    queryClient.setQueryData(queryKeys.reservations.list("u2"), []);
    queryClient.setQueryData(queryKeys.availability.slots("s1", "2030-01-07"), []);
    queryClient.setQueryData(queryKeys.services.publicList(), []);

    const { result } = renderHook(() => useCancelReservation(), { wrapper });
    result.current.mutate("r1");

    await waitFor(() => expect(invalidated(queryKeys.reservations.list("u1"))).toBe(true));
    expect(put).toHaveBeenCalledWith("/reservations/r1/cancel");
    expect(invalidated(queryKeys.availability.slots("s1", "2030-01-07"))).toBe(true);
    expect(invalidated(queryKeys.reservations.list("u2"))).toBe(false);
    expect(invalidated(queryKeys.services.publicList())).toBe(false);
  });

  it("creating a reservation posts the slot as given and refreshes reservations and availability", async () => {
    post.mockResolvedValue({ data: { id: "r9" } });
    queryClient.setQueryData(queryKeys.reservations.list("u1"), []);
    queryClient.setQueryData(queryKeys.availability.slots("s1", "2030-01-07"), []);

    const { result } = renderHook(() => useCreateReservation(), { wrapper });
    result.current.mutate({ serviceId: "s1", startTime: "2030-01-07T09:00:00-03:00", notes: null });

    await waitFor(() => expect(invalidated(queryKeys.reservations.list("u1"))).toBe(true));
    expect(post).toHaveBeenCalledWith("/reservations", {
      serviceId: "s1",
      startTime: "2030-01-07T09:00:00-03:00",
      notes: null,
    });
    expect(invalidated(queryKeys.availability.slots("s1", "2030-01-07"))).toBe(true);
  });

  it("a service write invalidates the catalogue, the owner list and the details together", async () => {
    post.mockResolvedValue({ data: {} });
    queryClient.setQueryData(queryKeys.services.publicList(), []);
    queryClient.setQueryData(queryKeys.services.ownerList("u1"), []);
    queryClient.setQueryData(queryKeys.services.detail("s1"), {});
    queryClient.setQueryData(queryKeys.reservations.list("u1"), []);

    const { result } = renderHook(() => useCreateService(), { wrapper });
    result.current.mutate({ name: "n", description: "", durationMinutes: 30, price: 1 });

    await waitFor(() => expect(invalidated(queryKeys.services.publicList())).toBe(true));
    expect(invalidated(queryKeys.services.ownerList("u1"))).toBe(true);
    expect(invalidated(queryKeys.services.detail("s1"))).toBe(true);
    expect(invalidated(queryKeys.reservations.list("u1"))).toBe(false);
  });

  it("saving working hours PUTs the whole week and refreshes the schedule and availability", async () => {
    put.mockResolvedValue({ data: [] });
    queryClient.setQueryData(queryKeys.workingHours.all("u1"), []);
    queryClient.setQueryData(queryKeys.availability.slots("s1", "2030-01-07"), []);

    const { result } = renderHook(() => useSaveWorkingHours(), { wrapper });
    result.current.mutate(defaultWeek());

    await waitFor(() => expect(invalidated(queryKeys.workingHours.all("u1"))).toBe(true));
    expect(put.mock.calls[0][0]).toBe("/workinghours");
    expect(put.mock.calls[0][1]).toHaveLength(7);
    expect(invalidated(queryKeys.availability.slots("s1", "2030-01-07"))).toBe(true);
  });

  it("favorites: exposes the ids for a customer and refreshes the list after adding one", async () => {
    get.mockResolvedValue({ data: [{ id: "f1", serviceId: "s1" }] });
    post.mockResolvedValue({});

    const ids = renderHook(() => useFavoriteServiceIds(), { wrapper });
    await waitFor(() => expect(ids.result.current.has("s1")).toBe(true));

    const add = renderHook(() => useAddFavorite(), { wrapper });
    add.result.current.mutate("s2");

    // The list is being observed, so the invalidation shows up as a refetch.
    await waitFor(() => expect(get).toHaveBeenCalledTimes(2));
    expect(post).toHaveBeenCalledWith("/favorites/s2");
  });

  it("favorites are not requested for an owner", () => {
    auth.user = { id: "u2", email: "x@y.com", role: "Owner" };
    renderHook(() => useFavoriteServiceIds(), { wrapper });
    expect(get).not.toHaveBeenCalled();
  });
});
