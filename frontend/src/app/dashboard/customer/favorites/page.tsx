"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useFavorites, useRemoveFavorite } from "@/hooks/queries/useFavorites";
import { useRequireRole } from "@/hooks/useRequireRole";
import { Star, Trash2 } from "lucide-react";

export default function CustomerFavoritesPage() {
  const { allowed } = useRequireRole("Customer");
  const { data: favorites = [], isLoading, error } = useFavorites();
  const remove = useRemoveFavorite();

  const handleRemoveFavorite = (serviceId: string) => {
    if (!confirm("Remove this service from favorites?")) return;
    remove.mutate(serviceId);
  };

  if (!allowed || isLoading) {
    return <div className="flex h-screen items-center justify-center">Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">My Favorites</h1>
      </div>
      <ErrorNotice error={error ?? remove.error} />

      {favorites.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {favorites.map((favorite) => (
            <Card key={favorite.id}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{favorite.serviceName}</CardTitle>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => handleRemoveFavorite(favorite.serviceId)}
                    disabled={remove.isPending}
                    className="text-red-500 hover:text-red-700"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
                <p className="text-sm text-muted-foreground">{favorite.companyName}</p>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground mb-4">
                  {favorite.serviceDescription || "No description"}
                </p>
                <div className="flex justify-between items-center">
                  <div>
                    <span className="text-lg font-bold">${favorite.price}</span>
                    <span className="text-sm text-muted-foreground ml-2">
                      ({favorite.durationMinutes} min)
                    </span>
                  </div>
                  <Link href={`/services/${favorite.serviceId}/book`}>
                    <Button size="sm">Book Now</Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            <Star className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>No favorites yet. Browse services and add your favorites!</p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
