"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { useAuth } from "@/hooks/useAuth";
import { Company } from "@/types";
import { queryKeys } from "./keys";

/** The signed-in owner's company. A 404 ("no company yet") is a query error: `data` stays undefined. */
export function useMyCompany() {
  const { user } = useAuth();
  return useQuery({
    queryKey: queryKeys.company.me(user?.id ?? ""),
    queryFn: async () => (await api.get<Company>("/companies/me")).data,
    enabled: user?.role === "Owner",
  });
}

export interface CompanyInput {
  name: string;
  description: string;
  timeZone: string;
}

export function useUpdateCompany() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  return useMutation({
    mutationFn: async (input: CompanyInput) => (await api.put<Company>("/companies/me", input)).data,
    onSuccess: async (company) => {
      const userId = user?.id ?? "";
      queryClient.setQueryData(queryKeys.company.me(userId), company);
      await Promise.all([
        // Company name and time zone show up in services and in every reservation time.
        queryClient.invalidateQueries({ queryKey: queryKeys.services.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.reservations.all(userId) }),
      ]);
    },
  });
}
