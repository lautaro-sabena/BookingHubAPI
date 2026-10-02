// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const { get, post } = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }));
vi.mock("@/lib/api", () => ({ default: { get, post } }));
vi.mock("@/hooks/useAuth", () => ({
  useAuth: () => ({ user: { id: "u1", email: "a@b.com", role: "Customer" }, isLoading: false }),
}));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), back: vi.fn() }) }));

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

const slots = [{ startTime: "2030-01-07T09:00:00-03:00", endTime: "2030-01-07T09:30:00-03:00", isAvailable: true }];


function renderForm() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <BookingForm service={service} />
    </QueryClientProvider>,
  );
}

describe("BookingForm summary", () => {
  const originalTz = process.env.TZ;

  beforeEach(() => {
    vi.clearAllMocks();
    get.mockResolvedValue({ data: slots });
  });
  afterEach(() => {
    cleanup();
    if (originalTz === undefined) delete process.env.TZ;
    else process.env.TZ = originalTz;
  });

  it("asks for a time before showing a summary", () => {
    renderForm();
    expect(screen.getByText("Select a time to continue")).toBeTruthy();
  });

  // Honolulu (UTC-10) would turn a UTC-midnight "2030-01-07" into Jan 6; Kiritimati (UTC+14) into a late Jan 7.
  it.each(["Pacific/Honolulu", "Pacific/Kiritimati", "UTC"])(
    "labels the picked day as that same calendar day in %s",
    async (tz) => {
      process.env.TZ = tz;
      renderForm();

      fireEvent.change(screen.getByLabelText("Select Date"), { target: { value: "2030-01-07" } });
      fireEvent.click(await screen.findByRole("button", { name: "09:00 AM" }));

      const summary = screen.getByText((_, el) => el?.tagName === "P" && el.textContent?.includes(" · 09:00 AM") === true);
      expect(summary.textContent).toBe("Mon, Jan 7 · 09:00 AM");
      expect(summary.textContent).not.toContain("Jan 6");
    },
  );
});
