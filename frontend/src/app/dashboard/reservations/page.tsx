"use client";

import { CalendarCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { ReservationRow } from "@/components/reservations/ReservationRow";
import { useConfirmReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { toCompanyLocalDate } from "@/lib/dateTime";

export default function OwnerReservationsPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: reservations = [], isLoading, error } = useReservations();
  const confirm = useConfirmReservation();

  if (!allowed || isLoading) {
    return <LoadingState variant="list" />;
  }

  const pending = reservations.filter((r) => r.status === "Pending").length;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Reservations"
        description={
          pending > 0
            ? `${pending} ${pending === 1 ? "booking is" : "bookings are"} waiting for your confirmation.`
            : "All bookings for your services."
        }
      />
      <ErrorNotice error={error ?? confirm.error} />
      {reservations.length > 0 ? (
        <div className="space-y-3">
          {reservations.map((reservation) => (
            <ReservationRow
              key={reservation.id}
              title={reservation.serviceName}
              status={reservation.status}
              when={toCompanyLocalDate(reservation.startTime).toLocaleString()}
              meta={<>Customer: {reservation.customerEmail}</>}
              className={reservation.status === "Pending" ? "border-warning/40" : undefined}
              actions={
                reservation.status === "Pending" && (
                  <Button onClick={() => confirm.mutate(reservation.id)} disabled={confirm.isPending}>
                    Confirm
                  </Button>
                )
              }
            />
          ))}
        </div>
      ) : (
        <EmptyState icon={CalendarCheck} title="No reservations yet." description="New bookings will show up here." />
      )}
    </div>
  );
}
