"use client";

import { useQuery } from "@tanstack/react-query";
import api from "@/lib/api";
import { AvailableSlot } from "@/types";
import { queryKeys } from "./keys";

/**
 * Free slots of a service on one day. `date` is the `YYYY-MM-DD` value of the date input, sent as is: it names
 * the company's calendar day, so it must not go through `toISOString()` (which shifts it through UTC).
 */
export function useAvailableSlots(serviceId: string, date: string) {
  return useQuery({
    queryKey: queryKeys.availability.slots(serviceId, date),
    queryFn: async () => (await api.get<AvailableSlot[]>(`/availability/${serviceId}`, { params: { date } })).data,
    enabled: !!serviceId && !!date,
    // Somebody else may take a slot at any moment.
    staleTime: 0,
  });
}
