// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const { put } = vi.hoisted(() => ({ put: vi.fn() }));
vi.mock("@/lib/api", () => ({ default: { put } }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => ({ user: { id: "u1", role: "Owner" }, isLoading: false }) }));

import { WorkingHoursForm } from "../WorkingHoursForm";
import { defaultWeek } from "@/lib/workingHours";

function renderForm() {
  const week = defaultWeek().map((d) =>
    d.dayOfWeek === 1 ? { ...d, isActive: true, startTime: "08:00", endTime: "12:00" } : d,
  );
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <WorkingHoursForm initial={week} />
    </QueryClientProvider>,
  );
}

describe("WorkingHoursForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    put.mockResolvedValue({});
  });
  afterEach(cleanup);

  it("enables the time inputs only for open days", () => {
    renderForm();
    expect((screen.getByLabelText("Monday start") as HTMLInputElement).disabled).toBe(false);
    expect((screen.getByLabelText("Tuesday start") as HTMLInputElement).disabled).toBe(true);

    fireEvent.click(screen.getByLabelText("Tuesday open"));

    expect((screen.getByLabelText("Tuesday start") as HTMLInputElement).disabled).toBe(false);
    expect((screen.getByLabelText("Tuesday end") as HTMLInputElement).disabled).toBe(false);
  });

  it("sends all seven days, and a closed day keeps its edited times", async () => {
    renderForm();
    fireEvent.change(screen.getByLabelText("Monday start"), { target: { value: "10:00" } });
    fireEvent.click(screen.getByLabelText("Monday open")); // close it again

    fireEvent.click(screen.getByRole("button", { name: "Save Availability" }));

    await waitFor(() => expect(put).toHaveBeenCalledTimes(1));
    const [url, payload] = put.mock.calls[0];
    expect(url).toBe("/workinghours");
    expect(payload).toHaveLength(7);
    expect(payload.map((d: { dayOfWeek: number }) => d.dayOfWeek)).toEqual([0, 1, 2, 3, 4, 5, 6]);
    expect(payload[1]).toEqual({ dayOfWeek: 1, startTime: "10:00", endTime: "12:00", isActive: false });
    expect(payload[0]).toEqual({ dayOfWeek: 0, startTime: "09:00", endTime: "17:00", isActive: false });
    expect(await screen.findByText(/Availability saved successfully/)).toBeTruthy();
  });

  it("shows the API message when saving fails", async () => {
    put.mockRejectedValue({ response: { data: { detail: "End must be after start." } } });
    renderForm();

    fireEvent.click(screen.getByRole("button", { name: "Save Availability" }));

    expect(await screen.findByText("End must be after start.")).toBeTruthy();
    expect(screen.queryByText(/saved successfully/)).toBeNull();
  });
});
