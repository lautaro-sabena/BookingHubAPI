"use client";

import { useState } from "react";
import { MonthCalendar } from "@/components/calendar/MonthCalendar";
import { CustomerReservationDetails } from "@/components/calendar/ReservationDetails";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useCancelReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { Reservation } from "@/types";

export default function CustomerCalendarPage() {
  const { allowed } = useRequireRole("Customer");
  const reservations = useReservations();
  const cancel = useCancelReservation();
  const [selected, setSelected] = useState<Reservation | null>(null);

  if (!allowed || reservations.isLoading) {
    return <div className="flex h-screen items-center justify-center">Loading...</div>;
  }

  const handleCancel = (reservation: Reservation) => {
    if (!confirm("Are you sure you want to cancel this reservation?")) return;
    cancel.mutate(reservation.id, { onSuccess: () => setSelected(null) });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">My Calendar</h1>
      </div>
      <ErrorNotice error={reservations.error ?? cancel.error} />

      <MonthCalendar reservations={reservations.data ?? []} palette="light" onSelect={setSelected} />

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
