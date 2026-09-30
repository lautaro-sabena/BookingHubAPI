// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const { get, put, auth } = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
  auth: { user: { id: "u1", email: "o@b.com", role: "Owner" } },
}));
vi.mock("@/lib/api", () => ({ default: { get, put } }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => ({ user: auth.user, isLoading: false }) }));
vi.mock("@/hooks/useRequireRole", () => ({
  useRequireRole: () => ({ user: auth.user, authLoading: false, allowed: true }),
}));

import OwnerReservationsPage from "../dashboard/reservations/page";

const pending = {
  id: "r1",
  serviceId: "s",
  customerId: "c",
  customerEmail: "c@x.com",
  companyId: "co",
  serviceName: "Haircut",
  serviceDuration: 30,
  startTime: "2030-01-07T09:00:00-03:00",
  endTime: "2030-01-07T09:30:00-03:00",
  status: "Pending",
  createdAt: "2030-01-01T00:00:00Z",
};

describe("OwnerReservationsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    get.mockResolvedValue({ data: { items: [pending] } });
  });
  afterEach(cleanup);

  it("disables Confirm while the request is in flight, so it is sent once", async () => {
    let finish!: () => void;
    put.mockReturnValue(new Promise<void>((resolve) => (finish = resolve)));
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <OwnerReservationsPage />
      </QueryClientProvider>,
    );
    const confirm = (await screen.findByRole("button", { name: "Confirm" })) as HTMLButtonElement;

    fireEvent.click(confirm);
    await waitFor(() => expect(confirm.disabled).toBe(true));
    fireEvent.click(confirm);

    expect(put).toHaveBeenCalledTimes(1);
    finish();
    await waitFor(() => expect(get).toHaveBeenCalledTimes(2));
  });
});
