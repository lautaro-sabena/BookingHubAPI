"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { StatusBadge } from "@/components/ui/status-badge";
import { useConfirmReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { toCompanyLocalDate } from "@/lib/dateTime";

export default function OwnerReservationsPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: reservations = [], isLoading, error } = useReservations();
  const confirm = useConfirmReservation();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Reservations</h1>
      <ErrorNotice error={error ?? confirm.error} />
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
                <div className="flex justify-between items-center">
                  <div>
                    <p className="text-sm text-muted-foreground">
                      Customer: {reservation.customerEmail}
                    </p>
                    <p className="text-sm">
                      {toCompanyLocalDate(reservation.startTime).toLocaleString()}
                    </p>
                  </div>
                  {reservation.status === "Pending" && (
                    <Button onClick={() => confirm.mutate(reservation.id)} disabled={confirm.isPending}>
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
