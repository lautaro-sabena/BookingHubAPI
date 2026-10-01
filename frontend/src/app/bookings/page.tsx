"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { StatusBadge } from "@/components/ui/status-badge";
import { useCancelReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { toCompanyLocalDate } from "@/lib/dateTime";

export default function BookingsPage() {
  const { allowed } = useRequireRole("Customer");
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
      <h1 className="text-2xl font-bold">My Reservations</h1>
      <ErrorNotice error={error ?? cancel.error} />
      {reservations.length > 0 ? (
        <div className="space-y-4">
          {reservations.map((reservation) => (
            <Card key={reservation.id}>
              <CardHeader>
                <div className="flex justify-between items-center">
                  <CardTitle>{reservation.serviceName}</CardTitle>
                  <StatusBadge status={reservation.status} />
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {toCompanyLocalDate(reservation.startTime).toLocaleString()}
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
