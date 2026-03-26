"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Reservation } from "@/types";

export default function CustomerDashboardPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && user?.role !== "Customer") {
      router.push("/dashboard/owner");
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
    } catch (error) {
      console.error("Failed to fetch reservations:", error);
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
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">My Reservations</h1>
        <Link href="/services">
          <Button className="bg-black text-white hover:bg-gray-800">
            Book a Service
          </Button>
        </Link>
      </div>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Reservations</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{reservations.length}</div>
          </CardContent>
        </Card>
      </div>
      {reservations.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Your Bookings</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {reservations.map((reservation) => (
                <div key={reservation.id} className="flex justify-between items-center p-4 border rounded-lg">
                  <div>
                    <p className="font-medium">{reservation.serviceName}</p>
                    <p className="text-sm text-muted-foreground">
                      {new Date(reservation.startTime).toLocaleString()}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded ${
                      reservation.status === "Confirmed" ? "bg-green-100 text-green-800" :
                      reservation.status === "Pending" ? "bg-yellow-100 text-yellow-800" :
                      reservation.status === "Cancelled" ? "bg-red-100 text-red-800" :
                      "bg-gray-100 text-gray-800"
                    }`}>
                      {reservation.status}
                    </span>
                    {(reservation.status === "Pending" || reservation.status === "Confirmed") && (
                      <Button 
                        variant="destructive" 
                        size="sm"
                        onClick={() => handleCancel(reservation.id)}
                      >
                        Cancel
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
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
