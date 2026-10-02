"use client";

import { useState } from "react";
import { MonthCalendar } from "@/components/calendar/MonthCalendar";
import { CustomerReservationDetails } from "@/components/calendar/ReservationDetails";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { useCancelReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { Reservation } from "@/types";

export default function CustomerCalendarPage() {
  const { allowed } = useRequireRole("Customer");
  const reservations = useReservations();
  const cancel = useCancelReservation();
  const [selected, setSelected] = useState<Reservation | null>(null);

  if (!allowed || reservations.isLoading) {
    return <LoadingState variant="list" />;
  }

  const handleCancel = (reservation: Reservation) => {
    if (!confirm("Are you sure you want to cancel this reservation?")) return;
    cancel.mutate(reservation.id, { onSuccess: () => setSelected(null) });
  };

  return (
    <div className="space-y-6">
      <PageHeader title="My Calendar" description="Your appointments by day. Select one to see the details." />
      <ErrorNotice error={reservations.error ?? cancel.error} />

      <MonthCalendar reservations={reservations.data ?? []} palette="themed" onSelect={setSelected} />

      {selected && (
        <CustomerReservationDetails
          reservation={selected}
          onClose={() => setSelected(null)}
          busy={cancel.isPending}
          onCancel={handleCancel}
        />
      )}
    </div>
  );
}
