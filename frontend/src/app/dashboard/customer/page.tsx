"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { StatusBadge } from "@/components/ui/status-badge";
import { useCancelReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { toCompanyLocalDate } from "@/lib/dateTime";

export default function CustomerDashboardPage() {
  const { allowed } = useRequireRole("Customer", "/dashboard/owner");
  const { data: reservations = [], isLoading, error } = useReservations();
  const cancel = useCancelReservation();

  const handleCancel = (id: string) => {
    if (!confirm("Are you sure you want to cancel this reservation?")) return;
    cancel.mutate(id);
  };

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">My Reservations</h1>
        <Link href="/services">
          <Button>
            Book a Service
          </Button>
        </Link>
      </div>
      <ErrorNotice error={error ?? cancel.error} />
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
                      {toCompanyLocalDate(reservation.startTime).toLocaleString()}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <StatusBadge status={reservation.status} />
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
