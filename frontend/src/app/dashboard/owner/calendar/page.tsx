"use client";

import { useState } from "react";
import { MonthCalendar } from "@/components/calendar/MonthCalendar";
import { OwnerReservationDetails } from "@/components/calendar/ReservationDetails";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useCancelReservation, useConfirmReservation, useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { Reservation } from "@/types";

export default function OwnerCalendarPage() {
  const { allowed } = useRequireRole("Owner");
  const reservations = useReservations();
  const confirm = useConfirmReservation();
  const cancel = useCancelReservation();
  const [selected, setSelected] = useState<Reservation | null>(null);

  if (!allowed || reservations.isLoading) {
    return <div>Loading...</div>;
  }

  const act = (mutate: typeof confirm.mutate, reservation: Reservation) =>
    mutate(reservation.id, { onSuccess: () => setSelected(null) });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Reservation Calendar</h1>
      </div>
      <ErrorNotice error={reservations.error ?? confirm.error ?? cancel.error} />

      <MonthCalendar reservations={reservations.data ?? []} palette="themed" onSelect={setSelected} />

      {selected && (
        <OwnerReservationDetails
          reservation={selected}
          onClose={() => setSelected(null)}
          busy={confirm.isPending || cancel.isPending}
          onConfirm={(r) => act(confirm.mutate, r)}
          onCancel={(r) => act(cancel.mutate, r)}
        />
      )}
    </div>
  );
}
