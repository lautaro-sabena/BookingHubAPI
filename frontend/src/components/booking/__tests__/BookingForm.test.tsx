// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const { get, post, push, auth } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  push: vi.fn(),
  auth: { user: { id: "u1", email: "a@b.com", role: "Customer" } },
}));
vi.mock("@/lib/api", () => ({ default: { get, post } }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => ({ user: auth.user, isLoading: false }) }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push, back: vi.fn() }) }));

import { BookingForm } from "../BookingForm";
import { Service } from "@/types";

const service: Service = {
  id: "svc1",
  name: "Haircut",
  durationMinutes: 30,
  price: 25,
  isActive: true,
  companyId: "co1",
  companyName: "Barber Co",
};

// Offsets differ from the test machine's zone on purpose: neither the date nor the slot may be converted.
const slots = [
  { startTime: "2030-01-07T09:00:00-03:00", endTime: "2030-01-07T09:30:00-03:00", isAvailable: true },
  { startTime: "2030-01-07T23:30:00-03:00", endTime: "2030-01-08T00:00:00-03:00", isAvailable: true },
];

function renderForm() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <BookingForm service={service} />
    </QueryClientProvider>,
  );
}

describe("BookingForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    get.mockResolvedValue({ data: slots });
    post.mockResolvedValue({ data: { id: "r1" } });
  });
  afterEach(cleanup);

  it("requests availability with the date input value, not a UTC-shifted ISO date", async () => {
    renderForm();

    fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-07" } });

    await waitFor(() => expect(get).toHaveBeenCalledTimes(1));
    expect(get).toHaveBeenCalledWith("/availability/svc1", { params: { date: "2030-01-07" } });
    // Slots are labelled with the company's wall clock (09:00 AM), not the viewer's zone.
    expect(await screen.findByRole("button", { name: "09:00 AM" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "11:30 PM" })).toBeTruthy();
  });

  it("books the selected slot's ISO string unchanged, with notes", async () => {
    renderForm();
    fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-07" } });
    fireEvent.click(await screen.findByRole("button", { name: "11:30 PM" }));
    fireEvent.change(screen.getByLabelText("Notes (optional)"), { target: { value: "side fade" } });

    fireEvent.click(screen.getByRole("button", { name: "Confirm Booking" }));

    await waitFor(() => expect(post).toHaveBeenCalledTimes(1));
    expect(post).toHaveBeenCalledWith("/reservations", {
      serviceId: "svc1",
      startTime: "2030-01-07T23:30:00-03:00",
      notes: "side fade",
    });
    expect(await screen.findByText(/Reservation created successfully/)).toBeTruthy();
  });

  it("sends null notes when none were typed, and cannot book before a slot is chosen", async () => {
    renderForm();
    const confirm = screen.getByRole("button", { name: "Confirm Booking" }) as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);

    fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-07" } });
    fireEvent.click(await screen.findByRole("button", { name: "09:00 AM" }));
    fireEvent.click(confirm);

    await waitFor(() => expect(post).toHaveBeenCalled());
    expect(post.mock.calls[0][1].notes).toBeNull();
  });

  it("clears the chosen slot when the date changes", async () => {
    renderForm();
    fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-07" } });
    fireEvent.click(await screen.findByRole("button", { name: "09:00 AM" }));
    expect(screen.getByLabelText("Notes (optional)")).toBeTruthy();

    fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-08" } });

    expect(screen.queryByLabelText("Notes (optional)")).toBeNull();
    expect((screen.getByRole("button", { name: "Confirm Booking" }) as HTMLButtonElement).disabled).toBe(true);
  });

  it("shows the API's problem detail when booking fails", async () => {
    post.mockRejectedValue({ response: { data: { detail: "That slot was just taken." } } });
    renderForm();
    fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-07" } });
    fireEvent.click(await screen.findByRole("button", { name: "09:00 AM" }));

    fireEvent.click(screen.getByRole("button", { name: "Confirm Booking" }));

    expect(await screen.findByText("That slot was just taken.")).toBeTruthy();
    expect(push).not.toHaveBeenCalled();
  });
});
