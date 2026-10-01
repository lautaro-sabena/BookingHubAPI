// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { act, cleanup, render, screen } from "@testing-library/react";
import { useContext } from "react";
import { ThemeContext, ThemeProvider } from "../ThemeProvider";

function Probe() {
  const { theme, toggleTheme } = useContext(ThemeContext)!;
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <button onClick={toggleTheme}>toggle</button>
    </div>
  );
}

function mockSystemDark(matches: boolean) {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches,
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  })) as unknown as typeof window.matchMedia;
}

const renderProvider = () =>
  render(
    <ThemeProvider>
      <Probe />
    </ThemeProvider>,
  );

describe("ThemeProvider", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    mockSystemDark(false);
  });
  afterEach(cleanup);

  it("keeps a saved dark theme and applies it to the document", () => {
    localStorage.setItem("theme", "dark");

    renderProvider();

    expect(screen.getByTestId("theme").textContent).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
    // Hydration must not overwrite the saved choice.
    expect(localStorage.getItem("theme")).toBe("dark");
  });

  it("toggles the dark class and persists the choice", () => {
    renderProvider();
    expect(document.documentElement.classList.contains("dark")).toBe(false);

    act(() => screen.getByText("toggle").click());

    expect(screen.getByTestId("theme").textContent).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(localStorage.getItem("theme")).toBe("dark");

    act(() => screen.getByText("toggle").click());

    expect(screen.getByTestId("theme").textContent).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(localStorage.getItem("theme")).toBe("light");
  });

  it("falls back to the system preference when nothing valid is saved", () => {
    localStorage.setItem("theme", "sepia");
    mockSystemDark(true);

    renderProvider();

    expect(screen.getByTestId("theme").textContent).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("uses light when the system prefers light and nothing is saved", () => {
    renderProvider();

    expect(screen.getByTestId("theme").textContent).toBe("light");
  });
});
