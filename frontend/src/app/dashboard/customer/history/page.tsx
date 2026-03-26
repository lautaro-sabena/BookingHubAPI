"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Reservation } from "@/types";

export default function CustomerHistoryPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && user?.role !== "Customer") {
      router.push("/dashboard");
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

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", {
      weekday: "short",
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString("en-US", {
      hour: "2-digit",
      minute: "2-digit",
      hour12: true,
    });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case "Confirmed":
        return "bg-green-100 text-green-800";
      case "Pending":
        return "bg-yellow-100 text-yellow-800";
      case "Cancelled":
        return "bg-red-100 text-red-800";
      case "Completed":
        return "bg-blue-100 text-blue-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  if (authLoading || loading) {
    return <div className="flex h-screen items-center justify-center">Loading...</div>;
  }

  // Sort by date, most recent first
  const sortedReservations = [...reservations].sort(
    (a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime()
  );

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Reservation History</h1>
      
      {sortedReservations.length > 0 ? (
        <div className="space-y-4">
          {sortedReservations.map((reservation) => (
            <Card key={reservation.id}>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <h3 className="text-lg font-semibold">{reservation.serviceName}</h3>
                      <span className={`px-2 py-1 text-xs rounded ${getStatusColor(reservation.status)}`}>
                        {reservation.status}
                      </span>
                    </div>
                    <div className="text-sm text-muted-foreground space-y-1">
                      <p>
                        <span className="font-medium">Date:</span> {formatDate(reservation.startTime)}
                      </p>
                      <p>
                        <span className="font-medium">Time:</span> {formatTime(reservation.startTime)} - {formatTime(reservation.endTime)}
                      </p>
                      <p>
                        <span className="font-medium">Duration:</span> {reservation.serviceDuration} minutes
                      </p>
                      {reservation.notes && (
                        <p>
                          <span className="font-medium">Notes:</span> {reservation.notes}
                        </p>
                      )}
                      <p>
                        <span className="font-medium">Booked on:</span> {new Date(reservation.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                </div>
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