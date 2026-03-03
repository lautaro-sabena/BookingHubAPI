"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Reservation } from "@/types";

export default function BookingsPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login");
      return;
    }

    if (user?.role === "Customer") {
      fetchReservations();
    }
  }, [user, authLoading, router]);

  const fetchReservations = async () => {
    try {
      const response = await api.get<{ items: Reservation[] }>("/reservations");
      setReservations(response.data.items);
    } catch (err) {
      console.error("Failed to fetch reservations:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = async (id: string) => {
    if (!confirm("Are you sure you want to cancel this reservation?")) return;
    
    try {
      await api.put(`/reservations/${id}/cancel`);
      setReservations(
        reservations.map((r) =>
          r.id === id ? { ...r, status: "Cancelled" } : r
        )
      );
    } catch (err) {
      console.error("Failed to cancel reservation:", err);
    }
  };

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">My Reservations</h1>
      {reservations.length > 0 ? (
        <div className="space-y-4">
          {reservations.map((reservation) => (
            <Card key={reservation.id}>
              <CardHeader>
                <div className="flex justify-between items-center">
                  <CardTitle>{reservation.serviceName}</CardTitle>
                  <span className={`px-2 py-1 text-xs rounded ${
                    reservation.status === "Confirmed" ? "bg-green-100 text-green-800" :
                    reservation.status === "Pending" ? "bg-yellow-100 text-yellow-800" :
                    reservation.status === "Cancelled" ? "bg-red-100 text-red-800" :
                    "bg-gray-100 text-gray-800"
                  }`}>
                    {reservation.status}
                  </span>
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {new Date(reservation.startTime).toLocaleString()}
                </p>
                {(reservation.status === "Pending" || reservation.status === "Confirmed") && (
                  <Button 
                    variant="destructive" 
                    size="sm"
                    className="mt-2"
                    onClick={() => handleCancel(reservation.id)}
                  >
                    Cancel Reservation
                  </Button>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            No reservations yet. Browse services to make a booking!
          </CardContent>
        </Card>
      )}
    </div>
  );
}
