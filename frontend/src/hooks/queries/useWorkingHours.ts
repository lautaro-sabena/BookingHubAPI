"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { useAuth } from "@/hooks/useAuth";
import { DayAvailability, normalizeWorkingHours, toWorkingHoursPayload } from "@/lib/workingHours";
import { WorkingHoursResponse } from "@/types";
import { queryKeys } from "./keys";

/** The owner's week: always seven days, inactive defaults for days the API has no row for. */
export function useWorkingHours() {
  const { user } = useAuth();
  return useQuery({
    queryKey: queryKeys.workingHours.all(user?.id ?? ""),
    queryFn: async () => normalizeWorkingHours((await api.get<WorkingHoursResponse[]>("/workinghours")).data),
    enabled: user?.role === "Owner",
  });
}

export function useSaveWorkingHours() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  return useMutation({
    mutationFn: async (days: DayAvailability[]) => {
      await api.put("/workinghours", toWorkingHoursPayload(days));
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.workingHours.all(user?.id ?? "") }),
        // Opening hours decide which slots exist.
        queryClient.invalidateQueries({ queryKey: queryKeys.availability.all }),
      ]);
    },
  });
}
