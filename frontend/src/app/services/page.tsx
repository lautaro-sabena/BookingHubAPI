"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Service } from "@/types";

export default function ServicesPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [services, setServices] = useState<Service[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchServices();
  }, []);

  const fetchServices = async () => {
    try {
      const response = await api.get<{ items: Service[] }>("/services");
      setServices(response.data.items.filter(s => s.isActive));
    } catch (err) {
      console.error("Failed to fetch services:", err);
    } finally {
      setLoading(false);
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
            <Card key={service.id}>
              <CardHeader>
                <CardTitle>{service.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground mb-4">
                  {service.description || "No description"}
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
