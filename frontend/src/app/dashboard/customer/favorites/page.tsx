"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Favorite, Service } from "@/types";
import { Star, Trash2 } from "lucide-react";

export default function CustomerFavoritesPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [favorites, setFavorites] = useState<Favorite[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && user?.role !== "Customer") {
      router.push("/dashboard");
      return;
    }

    if (user?.role === "Customer") {
      fetchFavorites();
    }
  }, [user, authLoading, router]);

  const fetchFavorites = async () => {
    try {
      const response = await api.get<Favorite[]>("/favorites");
      setFavorites(response.data);
    } catch (error) {
      console.error("Failed to fetch favorites:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveFavorite = async (serviceId: string) => {
    if (!confirm("Remove this service from favorites?")) return;
    
    try {
      await api.delete(`/favorites/${serviceId}`);
      setFavorites(favorites.filter(f => f.serviceId !== serviceId));
    } catch (error) {
      console.error("Failed to remove favorite:", error);
    }
  };

  if (authLoading || loading) {
    return <div className="flex h-screen items-center justify-center">Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">My Favorites</h1>
      </div>
      
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