import { describe, it, expect } from "vitest";
import { AxiosError, AxiosHeaders } from "axios";
import { createQueryClient, shouldRetryQuery } from "../queryClient";

function httpError(status: number) {
  const config = { headers: new AxiosHeaders() };
  return new AxiosError("failed", String(status), config, null, {
    status,
    statusText: "",
    data: {},
    headers: {},
    config,
  });
}

describe("shouldRetryQuery", () => {
  it.each([400, 401, 403, 404, 422])("never retries a %s (the answer would not change)", (status) => {
    expect(shouldRetryQuery(0, httpError(status))).toBe(false);
  });

  it("retries server errors and network failures a limited number of times", () => {
    expect(shouldRetryQuery(0, httpError(503))).toBe(true);
    expect(shouldRetryQuery(1, new Error("Network Error"))).toBe(true);
    expect(shouldRetryQuery(2, httpError(503))).toBe(false);
  });
});

describe("createQueryClient", () => {
  it("uses the retry policy for queries and never retries mutations", () => {
    const defaults = createQueryClient().getDefaultOptions();
    expect(defaults.queries?.retry).toBe(shouldRetryQuery);
    expect(defaults.mutations?.retry).toBe(false);
  });
});
