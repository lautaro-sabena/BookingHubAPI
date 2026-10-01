import { Reservation } from "@/types";

/**
 * Most recent first, by the real instant the reservation starts. The API timestamps carry the company's offset,
 * so `Date` parsing them as instants orders reservations of companies in different zones correctly (the
 * company-local wall clock used for display would not).
 */
export function sortByInstantDescending(reservations: Reservation[]): Reservation[] {
  return [...reservations].sort((a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime());
}
