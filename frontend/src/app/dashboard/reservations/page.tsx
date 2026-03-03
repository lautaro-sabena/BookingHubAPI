"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Reservation } from "@/types";

export default function OwnerReservationsPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && user?.role !== "Owner") {
      router.push("/dashboard");
      return;
    }

    if (user?.role === "Owner") {
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

  const handleConfirm = async (id: string) => {
    try {
      await api.put(`/reservations/${id}/confirm`);
      setReservations(
        reservations.map((r) =>
          r.id === id ? { ...r, status: "Confirmed" } : r
        )
      );
    } catch (err) {
      console.error("Failed to confirm reservation:", err);
    }
  };

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Reservations</h1>
      {reservations.length > 0 ? (
        <div className="space-y-4">
          {reservations.map((reservation) => (
            <Card key={reservation.id}>
              <CardHeader>
                <div className="flex justify-between items-center">
                  <CardTitle>{reservation.serviceName}</CardTitle>
                  <span
                    className={`px-2 py-1 text-xs rounded ${
                      reservation.status === "Confirmed"
                        ? "bg-green-100 text-green-800"
                        : reservation.status === "Pending"
                        ? "bg-yellow-100 text-yellow-800"
                        : reservation.status === "Cancelled"
                        ? "bg-red-100 text-red-800"
                        : "bg-gray-100 text-gray-800"
                    }`}
                  >
                    {reservation.status}
                  </span>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex justify-between items-center">
                  <div>
                    <p className="text-sm text-muted-foreground">
                      Customer: {reservation.customerEmail}
                    </p>
                    <p className="text-sm">
                      {new Date(reservation.startTime).toLocaleString()}
                    </p>
                  </div>
                  {reservation.status === "Pending" && (
                    <Button onClick={() => handleConfirm(reservation.id)}>
                      Confirm
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            No reservations yet.
          </CardContent>
        </Card>
      )}
    </div>
  );
}
