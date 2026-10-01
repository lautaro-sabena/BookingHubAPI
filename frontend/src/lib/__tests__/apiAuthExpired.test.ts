// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from "vitest";
import { AxiosError, AxiosHeaders } from "axios";
import { api, AUTH_EXPIRED_EVENT } from "../api";

async function requestAnswering401(url: string) {
  await api
    .request({
      url,
      adapter: async (config) => {
        throw new AxiosError("Unauthorized", "401", config, null, {
          status: 401,
          statusText: "Unauthorized",
          data: {},
          headers: {},
          config: { ...config, headers: new AxiosHeaders() },
        });
      },
    })
    .catch(() => undefined);
}

describe("401 handling", () => {
  afterEach(() => window.history.pushState({}, "", "/"));

  it("announces an expired session instead of navigating by itself", async () => {
    window.history.pushState({}, "", "/dashboard");
    const listener = vi.fn();
    window.addEventListener(AUTH_EXPIRED_EVENT, listener);

    await requestAnswering401("/reservations");

    window.removeEventListener(AUTH_EXPIRED_EVENT, listener);
    expect(listener).toHaveBeenCalledTimes(1);
    expect(window.location.pathname).toBe("/dashboard");
  });

  it.each([
    ["/auth/me", "/dashboard"],
    ["/reservations", "/login"],
  ])("stays silent for %s on %s (no redirect loop)", async (url, pathname) => {
    window.history.pushState({}, "", pathname);
    const listener = vi.fn();
    window.addEventListener(AUTH_EXPIRED_EVENT, listener);

    await requestAnswering401(url);

    window.removeEventListener(AUTH_EXPIRED_EVENT, listener);
    expect(listener).not.toHaveBeenCalled();
  });
});
