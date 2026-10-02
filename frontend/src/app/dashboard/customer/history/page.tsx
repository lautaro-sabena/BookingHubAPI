"use client";

import { useMemo } from "react";
import { History } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { ReservationRow } from "@/components/reservations/ReservationRow";
import { useReservations } from "@/hooks/queries/useReservations";
import { useRequireRole } from "@/hooks/useRequireRole";
import { formatCompanyDate, formatCompanyTime } from "@/lib/dateTime";
import { sortByInstantDescending } from "@/lib/reservations";

const SHORT_DATE: Intl.DateTimeFormatOptions = { weekday: "short", year: "numeric", month: "short", day: "numeric" };

function Meta({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <p>
      <span className="font-medium text-foreground/80">{label}:</span> {children}
    </p>
  );
}

export default function CustomerHistoryPage() {
  const { allowed } = useRequireRole("Customer");
  const { data: reservations, isLoading, error } = useReservations();
  const sortedReservations = useMemo(() => sortByInstantDescending(reservations ?? []), [reservations]);

  if (!allowed || isLoading) {
    return <LoadingState variant="list" />;
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Reservation History" description="Every booking you've made, newest first." />
      <ErrorNotice error={error} />

      {sortedReservations.length > 0 ? (
        <ol className="relative space-y-3">
          {sortedReservations.map((reservation) => (
            <li key={reservation.id}>
              <ReservationRow
                title={
                  // Keeps the level-3 heading per reservation that the page had before (ReservationRow wraps the
                  // title in a <p>, where a native <h3> is not valid).
                  <span role="heading" aria-level={3}>
                    {reservation.serviceName}
                  </span>
                }
                status={reservation.status}
                when={
                  <>
                    {formatCompanyDate(reservation.startTime, SHORT_DATE)} · {formatCompanyTime(reservation.startTime)} -{" "}
                    {formatCompanyTime(reservation.endTime)}
                  </>
                }
                meta={
                  <div className="mt-1 grid gap-x-6 gap-y-1 sm:grid-cols-2">
                    <Meta label="Duration">{reservation.serviceDuration} minutes</Meta>
                    <Meta label="Booked on">{new Date(reservation.createdAt).toLocaleDateString()}</Meta>
                    {reservation.notes && (
                      <div className="sm:col-span-2">
                        <Meta label="Notes">{reservation.notes}</Meta>
                      </div>
                    )}
                  </div>
                }
              />
            </li>
          ))}
        </ol>
      ) : (
        <EmptyState
          icon={History}
          title="No history yet"
          description="No reservations yet. Browse services to make a booking!"
        />
      )}
    </div>
  );
}
