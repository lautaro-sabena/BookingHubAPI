"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { useAuth } from "@/hooks/useAuth";
import { Reservation } from "@/types";
import { queryKeys } from "./keys";

/** The signed-in user's reservations (a customer's own, or every reservation of the owner's company). */
export function useReservations() {
  const { user } = useAuth();
  return useQuery({
    queryKey: queryKeys.reservations.list(user?.id ?? ""),
    queryFn: async () => (await api.get<{ items: Reservation[] }>("/reservations")).data.items,
    enabled: !!user,
    // The other party can confirm or cancel at any time: always refetch when a page shows the list.
    staleTime: 0,
  });
}

function useReservationMutation<TVariables, TResult = unknown>(
  mutationFn: (variables: TVariables) => Promise<TResult>,
) {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.reservations.all(user?.id ?? "") }),
        // A booked or cancelled slot changes what the availability endpoint returns.
        queryClient.invalidateQueries({ queryKey: queryKeys.availability.all }),
      ]);
    },
  });
}

export interface CreateReservationInput {
  serviceId: string;
  /** The slot's ISO string exactly as the API returned it (with its offset). */
  startTime: string;
  notes?: string | null;
}

export function useCreateReservation() {
  return useReservationMutation(
    async (input: CreateReservationInput) => (await api.post<Reservation>("/reservations", input)).data,
  );
}

export function useConfirmReservation() {
  return useReservationMutation(async (id: string) => {
    await api.put(`/reservations/${id}/confirm`);
  });
}

export function useCancelReservation() {
  return useReservationMutation(async (id: string) => {
    await api.put(`/reservations/${id}/cancel`);
  });
}
