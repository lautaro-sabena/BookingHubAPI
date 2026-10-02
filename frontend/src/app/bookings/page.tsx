"use client";

import Link from "next/link";
import { CalendarPlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { ReservationRow } from "@/components/reservations/ReservationRow";
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
    return <LoadingState variant="list" />;
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6 px-4 py-8 sm:px-6">
      <PageHeader title="My Reservations" />
      <ErrorNotice error={error ?? cancel.error} />
      {reservations.length > 0 ? (
        <div className="space-y-3">
          {reservations.map((reservation) => (
            <ReservationRow
              key={reservation.id}
              title={reservation.serviceName}
              when={toCompanyLocalDate(reservation.startTime).toLocaleString()}
              status={reservation.status}
              actions={
                (reservation.status === "Pending" || reservation.status === "Confirmed") && (
                  <Button variant="destructive-soft" size="sm" onClick={() => handleCancel(reservation.id)}>
                    Cancel Reservation
                  </Button>
                )
              }
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={CalendarPlus}
          title="Nothing booked yet"
          description="No reservations yet. Browse services to make a booking!"
          action={
            <Link href="/services">
              <Button>Browse services</Button>
            </Link>
          }
        />
      )}
    </div>
  );
}
