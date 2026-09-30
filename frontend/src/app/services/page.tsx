"use client";

import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useAddFavorite, useFavoriteServiceIds, useRemoveFavorite } from "@/hooks/queries/useFavorites";
import { usePublicServices } from "@/hooks/queries/useServices";
import { Star } from "lucide-react";

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
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Available Services</h1>
      <ErrorNotice error={error ?? addFavorite.error ?? removeFavorite.error} />
      {services.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {services.map((service) => (
            <Card key={service.id} className="relative">
              <Button
                variant="ghost"
                size="icon"
                className="absolute top-2 right-2"
                onClick={(e) => toggleFavorite(service.id, e)}
              >
                <Star
                  className={`h-5 w-5 ${favoriteIds.has(service.id) ? "fill-yellow-400 text-yellow-400" : "text-gray-400"}`}
                />
              </Button>
              <CardHeader>
                <CardTitle>{service.name}</CardTitle>
                <p className="text-sm text-muted-foreground">{service.companyName}</p>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground mb-4">
                  {service.companyDescription || service.description || "No description"}
                </p>
                <div className="flex justify-between items-center">
                  <div>
                    <span className="text-lg font-bold">${service.price}</span>
                    <span className="text-sm text-muted-foreground ml-2">
                      ({service.durationMinutes} min)
                    </span>
                  </div>
                  {user?.role === "Customer" && (
                    <Link href={`/services/${service.id}/book`}>
                      <Button>Book Now</Button>
                    </Link>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            No services available at the moment.
          </CardContent>
        </Card>
      )}
    </div>
  );
}
