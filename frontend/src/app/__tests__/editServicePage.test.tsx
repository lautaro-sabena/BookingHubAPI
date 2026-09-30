// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AxiosError } from "axios";

const { get } = vi.hoisted(() => ({ get: vi.fn() }));
vi.mock("@/lib/api", () => ({ default: { get, put: vi.fn() } }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => ({ user: { id: "u1", role: "Owner" }, isLoading: false }) }));
vi.mock("@/hooks/useRequireRole", () => ({
  useRequireRole: () => ({ user: { id: "u1", role: "Owner" }, authLoading: false, allowed: true }),
}));
vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "svc1" }),
  useRouter: () => ({ push: vi.fn() }),
}));

import EditServicePage from "../dashboard/services/[id]/edit/page";

function httpError(status: number, detail?: string) {
  const error = new AxiosError("failed");
  error.response = { status, data: detail ? { detail } : {} } as AxiosError["response"];
  return error;
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <EditServicePage />
    </QueryClientProvider>,
  );
}

describe("EditServicePage load errors", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  it("says the service was not found only for a 404", async () => {
    get.mockRejectedValue(httpError(404));
    renderPage();

    expect(await screen.findByText("Service not found")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
  });

  it("shows the API message with a retry for other failures, and recovers", async () => {
    get.mockRejectedValueOnce(httpError(500, "Database unavailable")).mockResolvedValueOnce({
      data: { id: "svc1", name: "Haircut", description: "", durationMinutes: 30, price: 25 },
    });
    renderPage();

    expect(await screen.findByText("Database unavailable")).toBeTruthy();
    expect(screen.queryByText("Service not found")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Retry" }));

    await waitFor(() => expect(screen.getByDisplayValue("Haircut")).toBeTruthy());
  });
});
