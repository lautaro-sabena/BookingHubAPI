// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const { get, put } = vi.hoisted(() => ({ get: vi.fn(), put: vi.fn() }));
vi.mock("@/lib/api", () => ({ default: { get, put } }));
vi.mock("@/hooks/useAuth", () => ({
  useAuth: () => ({ user: { id: "u1", email: "o@b.com", role: "Owner" }, isLoading: false }),
}));
vi.mock("@/hooks/useRequireRole", () => ({
  useRequireRole: () => ({ user: { id: "u1", role: "Owner" }, authLoading: false, allowed: true }),
}));

import AvailabilityPage from "../page";

const stored = [1, 2, 3, 4, 5].map((dayOfWeek) => ({
  dayOfWeek,
  startTime: "08:00:00",
  endTime: "12:00:00",
  isActive: true,
}));

function renderPage(client?: QueryClient) {
  const queryClient =
    client ?? new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <AvailabilityPage />
    </QueryClientProvider>,
  );
}

describe("AvailabilityPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  it("does not render a savable form when the schedule fails to load, and recovers on retry", async () => {
    get.mockRejectedValueOnce(new Error("503")).mockResolvedValueOnce({ data: stored });
    renderPage();

    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Save Availability" })).toBeNull();
    expect(screen.queryByLabelText("Monday open")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Retry" }));

    await waitFor(() => expect(screen.getByRole("button", { name: "Save Availability" })).toBeTruthy());
    expect((screen.getByLabelText("Monday open") as HTMLInputElement).checked).toBe(true);
    expect(put).not.toHaveBeenCalled();
  });

  it("keeps unsaved edits when a refetch returns the same schedule", async () => {
    get.mockResolvedValue({ data: stored });
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    renderPage(queryClient);
    const sunday = (await screen.findByLabelText("Sunday open")) as HTMLInputElement;
    fireEvent.click(sunday);

    await queryClient.refetchQueries();

    expect((screen.getByLabelText("Sunday open") as HTMLInputElement).checked).toBe(true);
  });

  it("keeps the saved confirmation after the refetch that follows a successful save", async () => {
    // Saving turns Sunday on; the refetch returns that new schedule, which changes the form key and remounts it.
    get.mockResolvedValueOnce({ data: stored }).mockResolvedValue({
      data: [...stored, { dayOfWeek: 0, startTime: "09:00:00", endTime: "17:00:00", isActive: true }],
    });
    put.mockResolvedValue({});
    renderPage();
    fireEvent.click(await screen.findByLabelText("Sunday open"));

    fireEvent.click(screen.getByRole("button", { name: "Save Availability" }));

    await waitFor(() => expect(get).toHaveBeenCalledTimes(2));
    await waitFor(() => expect((screen.getByLabelText("Sunday open") as HTMLInputElement).checked).toBe(true));
    expect(await screen.findByText(/Availability saved successfully/)).toBeTruthy();
  });

  it("resyncs the form when a refetch returns a different schedule", async () => {
    get.mockResolvedValueOnce({ data: stored });
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    renderPage(queryClient);
    await screen.findByLabelText("Monday open");

    get.mockResolvedValueOnce({ data: stored.filter((d) => d.dayOfWeek !== 1) });
    await queryClient.refetchQueries();

    await waitFor(() => expect((screen.getByLabelText("Monday open") as HTMLInputElement).checked).toBe(false));
  });
});
