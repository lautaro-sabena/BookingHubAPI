"use client";

import { useMemo } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { StatusBadge } from "@/components/ui/status-badge";
import { useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { formatCompanyDate, formatCompanyTime } from "@/lib/dateTime";
import { sortByInstantDescending } from "@/lib/reservations";

const SHORT_DATE: Intl.DateTimeFormatOptions = { weekday: "short", year: "numeric", month: "short", day: "numeric" };

export default function CustomerHistoryPage() {
  const { allowed } = useRequireRole("Customer");
  const { data: reservations, isLoading, error } = useReservations();
  const sortedReservations = useMemo(() => sortByInstantDescending(reservations ?? []), [reservations]);

  if (!allowed || isLoading) {
    return <div className="flex h-screen items-center justify-center">Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Reservation History</h1>
      <ErrorNotice error={error} />

      {sortedReservations.length > 0 ? (
        <div className="space-y-4">
          {sortedReservations.map((reservation) => (
            <Card key={reservation.id}>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <h3 className="text-lg font-semibold">{reservation.serviceName}</h3>
                      <StatusBadge status={reservation.status} />
                    </div>
                    <div className="text-sm text-muted-foreground space-y-1">
                      <p>
                        <span className="font-medium">Date:</span> {formatCompanyDate(reservation.startTime, SHORT_DATE)}
                      </p>
                      <p>
                        <span className="font-medium">Time:</span> {formatCompanyTime(reservation.startTime)} - {formatCompanyTime(reservation.endTime)}
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
