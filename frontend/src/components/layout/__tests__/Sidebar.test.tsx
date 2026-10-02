// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";

const { nav, auth } = vi.hoisted(() => ({
  nav: { pathname: "/dashboard" },
  auth: { user: { id: "u1", email: "o@x.com", role: "Owner" } as { id: string; email: string; role: string } | null, isLoading: false },
}));
vi.mock("next/navigation", () => ({ usePathname: () => nav.pathname }));
vi.mock("@/hooks/useAuth", () => ({ useAuth: () => auth }));

import { Sidebar } from "../Sidebar";

function renderAt(pathname: string, role: "Owner" | "Customer" = "Owner") {
  nav.pathname = pathname;
  auth.user = { id: "u1", email: "x@x.com", role };
  render(<Sidebar />);
}

function current() {
  return screen
    .getAllByRole("link")
    .filter((link) => link.getAttribute("aria-current") === "page")
    .map((link) => link.textContent);
}

describe("Sidebar", () => {
  afterEach(cleanup);

  it("marks the exact route as the current page", () => {
    renderAt("/dashboard/services");
    expect(current()).toEqual(["Services"]);
    expect(screen.getByRole("link", { name: "Company" }).getAttribute("aria-current")).toBeNull();
  });

  it("keeps the parent item active on its sub-pages", () => {
    renderAt("/dashboard/services/abc/edit");
    expect(current()).toEqual(["Services"]);
  });

  it("does not treat a sibling route with a shared prefix as a sub-page", () => {
    renderAt("/dashboard/servicesx");
    expect(current()).toEqual([]);
  });

  it("highlights Overview on the role home but not on other dashboard pages", () => {
    renderAt("/dashboard/owner");
    expect(current()).toEqual(["Overview"]);
    cleanup();

    renderAt("/dashboard/owner/calendar");
    expect(current()).toEqual(["Calendar"]);
  });

  it("uses the customer items and highlights Home on the customer home", () => {
    renderAt("/dashboard/customer", "Customer");
    expect(current()).toEqual(["Home"]);
    expect(screen.getByRole("navigation", { name: "Main" })).toBeTruthy();
  });

  it("renders nothing while the session is loading", () => {
    auth.isLoading = true;
    try {
      const { container } = render(<Sidebar />);
      expect(container.innerHTML).toBe("");
    } finally {
      auth.isLoading = false;
    }
  });
});
