"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Service } from "@/types";
import { Star } from "lucide-react";

export default function ServicesPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [services, setServices] = useState<Service[]>([]);
  const [loading, setLoading] = useState(true);
  const [favoriteIds, setFavoriteIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    fetchServices();
  }, []);

  const fetchServices = async () => {
    try {
      const response = await api.get<{ items: Service[] }>("/services/all");
      setServices(response.data.items.filter(s => s.isActive));
    } catch (err) {
      console.error("Failed to fetch services:", err);
    } finally {
      setLoading(false);
    }
  };

  const fetchFavorites = async () => {
    if (user?.role !== "Customer") return;
    
    try {
      const response = await api.get<{ serviceId: string }[]>("/favorites");
      const ids = new Set(response.data.map(f => f.serviceId));
      setFavoriteIds(ids);
    } catch (err) {
      console.error("Failed to fetch favorites:", err);
    }
  };

  useEffect(() => {
    if (user?.role === "Customer") {
      fetchFavorites();
    }
  }, [user]);

  const toggleFavorite = async (serviceId: string, e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    try {
      if (favoriteIds.has(serviceId)) {
        await api.delete(`/favorites/${serviceId}`);
        setFavoriteIds(prev => {
          const next = new Set(prev);
          next.delete(serviceId);
          return next;
        });
      } else {
        await api.post(`/favorites/${serviceId}`);
        setFavoriteIds(prev => new Set(prev).add(serviceId));
      }
    } catch (err) {
      console.error("Failed to toggle favorite:", err);
    }
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Available Services</h1>
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
