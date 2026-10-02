"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { ServiceCover, ServiceMeta } from "@/components/services/ServiceCard";
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
    return <LoadingState />;
  }

  return (
    <div className="space-y-6">
      <PageHeader title="My Favorites" description="Services you saved for quick booking." />
      <ErrorNotice error={error ?? remove.error} />

      {favorites.length > 0 ? (
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {favorites.map((favorite) => (
            <Card key={favorite.id} className="flex flex-col overflow-hidden transition-shadow hover:shadow-lift">
              <ServiceCover id={favorite.serviceId} name={favorite.serviceName}>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleRemoveFavorite(favorite.serviceId)}
                  disabled={remove.isPending}
                  aria-label="Remove from favorites"
                  className="absolute right-3 top-3 h-9 w-9 rounded-full bg-card/90 text-destructive shadow-sm backdrop-blur hover:bg-destructive-soft hover:text-destructive"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </ServiceCover>
              <CardHeader className="gap-1 pb-3 pt-9">
                <CardTitle className="text-lg">{favorite.serviceName}</CardTitle>
                <p className="text-sm font-medium text-muted-foreground">{favorite.companyName}</p>
              </CardHeader>
              <CardContent className="flex flex-1 flex-col">
                <p className="mb-5 line-clamp-3 text-sm text-muted-foreground">
                  {favorite.serviceDescription || "No description"}
                </p>
                <div className="mt-auto flex items-center justify-between gap-3 border-t border-border/70 pt-4">
                  <ServiceMeta price={favorite.price} durationMinutes={favorite.durationMinutes} />
                  <Link href={`/services/${favorite.serviceId}/book`}>
                    <Button size="sm">Book Now</Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={Star}
          title="Nothing saved yet"
          description="No favorites yet. Browse services and add your favorites!"
          action={
            <Link href="/services">
              <Button variant="outline">Browse services</Button>
            </Link>
          }
        />
      )}
    </div>
  );
}
