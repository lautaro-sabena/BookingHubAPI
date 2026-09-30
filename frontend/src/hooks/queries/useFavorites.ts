"use client";

import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { useAuth } from "@/hooks/useAuth";
import { Favorite } from "@/types";
import { queryKeys } from "./keys";

/** The signed-in customer's favorite services. Owners have none, so nothing is requested for them. */
export function useFavorites() {
  const { user } = useAuth();
  return useQuery({
    queryKey: queryKeys.favorites.all(user?.id ?? ""),
    queryFn: async () => (await api.get<Favorite[]>("/favorites")).data,
    enabled: user?.role === "Customer",
  });
}

/** Ids of the favorite services, for marking stars in the catalogue. */
export function useFavoriteServiceIds(): Set<string> {
  const { data } = useFavorites();
  return useMemo(() => new Set((data ?? []).map((f) => f.serviceId)), [data]);
}

function useFavoriteMutation(mutationFn: (serviceId: string) => Promise<unknown>) {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.favorites.all(user?.id ?? "") }),
  });
}

export function useAddFavorite() {
  return useFavoriteMutation((serviceId) => api.post(`/favorites/${serviceId}`));
}

export function useRemoveFavorite() {
  return useFavoriteMutation((serviceId) => api.delete(`/favorites/${serviceId}`));
}
