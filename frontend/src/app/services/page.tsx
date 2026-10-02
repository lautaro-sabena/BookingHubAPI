"use client";

import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { ServiceCover, ServiceMeta } from "@/components/services/ServiceCard";
import { useAddFavorite, useFavoriteServiceIds, useRemoveFavorite } from "@/hooks/queries/useFavorites";
import { usePublicServices } from "@/hooks/queries/useServices";
import { Store, Star } from "lucide-react";

export default function ServicesPage() {
  const { user } = useAuth();
  const { data: services = [], isLoading, error } = usePublicServices();
  const favoriteIds = useFavoriteServiceIds();
  const addFavorite = useAddFavorite();
  const removeFavorite = useRemoveFavorite();

  const toggleFavorite = (serviceId: string, e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (favoriteIds.has(serviceId)) {
      removeFavorite.mutate(serviceId);
    } else {
      addFavorite.mutate(serviceId);
    }
  };

  if (isLoading) {
    return <LoadingState />;
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Available Services"
        description="Find a service, pick a time that suits you and book it in a minute."
      />
      <ErrorNotice error={error ?? addFavorite.error ?? removeFavorite.error} />
      {services.length > 0 ? (
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {services.map((service) => {
            const isFavorite = favoriteIds.has(service.id);
            return (
              <Card
                key={service.id}
                className="group relative flex flex-col overflow-hidden transition-shadow hover:shadow-lift"
              >
                <ServiceCover id={service.id} name={service.name}>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="absolute right-3 top-3 h-9 w-9 rounded-full bg-card/90 shadow-sm backdrop-blur hover:bg-card"
                    onClick={(e) => toggleFavorite(service.id, e)}
                    disabled={addFavorite.isPending || removeFavorite.isPending}
                    aria-label={isFavorite ? "Remove from favorites" : "Add to favorites"}
                    aria-pressed={isFavorite}
                  >
                    <Star
                      className={`h-[18px] w-[18px] transition-colors ${isFavorite ? "fill-amber-400 text-amber-500" : "text-muted-foreground"}`}
                    />
                  </Button>
                </ServiceCover>
                <CardHeader className="gap-1 pb-3 pt-9">
                  <CardTitle>{service.name}</CardTitle>
                  <p className="text-sm font-medium text-muted-foreground">{service.companyName}</p>
                </CardHeader>
                <CardContent className="flex flex-1 flex-col">
                  <p className="mb-5 line-clamp-3 text-sm text-muted-foreground">
                    {service.companyDescription || service.description || "No description"}
                  </p>
                  <div className="mt-auto flex items-center justify-between gap-3 border-t border-border/70 pt-4">
                    <ServiceMeta price={service.price} durationMinutes={service.durationMinutes} />
                    {user?.role === "Customer" && (
                      <Link href={`/services/${service.id}/book`}>
                        <Button>Book Now</Button>
                      </Link>
                    )}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      ) : (
        <EmptyState
          icon={Store}
          title="No services available at the moment."
          description="Businesses are still setting up. Check back soon."
        />
      )}
    </div>
  );
}
