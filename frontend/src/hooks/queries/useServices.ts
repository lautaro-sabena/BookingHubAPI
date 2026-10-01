"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { useAuth } from "@/hooks/useAuth";
import { Service } from "@/types";
import { queryKeys } from "./keys";

/** Active services of every company, for the public catalogue. */
export function usePublicServices() {
  return useQuery({
    queryKey: queryKeys.services.publicList(),
    queryFn: async () =>
      (await api.get<{ items: Service[] }>("/services/all")).data.items.filter((s) => s.isActive),
  });
}

/** The services of the signed-in owner's company. */
export function useOwnerServices() {
  const { user } = useAuth();
  return useQuery({
    queryKey: queryKeys.services.ownerList(user?.id ?? ""),
    queryFn: async () => (await api.get<{ items: Service[] }>("/services")).data.items,
    enabled: user?.role === "Owner",
  });
}

export function useService(serviceId: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.services.detail(serviceId),
    queryFn: async () => (await api.get<Service>(`/services/${serviceId}`)).data,
    enabled: enabled && !!serviceId,
  });
}

export interface ServiceInput {
  name: string;
  description: string;
  durationMinutes: number;
  price: number;
}

function useServiceMutation<TVariables>(mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    // One prefix covers the public catalogue, the owner's list and every detail entry.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.services.all }),
  });
}

export function useCreateService() {
  return useServiceMutation((input: ServiceInput) => api.post<Service>("/services", input));
}

export function useUpdateService(serviceId: string) {
  return useServiceMutation((input: ServiceInput) => api.put<Service>(`/services/${serviceId}`, input));
}

export function useDeleteService() {
  return useServiceMutation((id: string) => api.delete(`/services/${id}`));
}
