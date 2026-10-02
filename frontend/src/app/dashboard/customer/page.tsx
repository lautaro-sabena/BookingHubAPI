"use client";

import Link from "next/link";
import { CalendarCheck, CalendarPlus, CalendarX2, ListChecks } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { StatCard } from "@/components/ui/stat-card";
import { ReservationRow } from "@/components/reservations/ReservationRow";
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
    return <LoadingState variant="list" />;
  }

  const active = reservations.filter((r) => r.status === "Pending" || r.status === "Confirmed").length;
  const cancelled = reservations.filter((r) => r.status === "Cancelled").length;

  return (
    <div className="space-y-8">
      <PageHeader
        title="My Reservations"
        description="Your upcoming and past appointments in one place."
        actions={
          <Link href="/services">
            <Button>
              <CalendarPlus className="h-4 w-4" aria-hidden="true" />
              Book a Service
            </Button>
          </Link>
        }
      />
      <ErrorNotice error={error ?? cancel.error} />
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Total Reservations" value={reservations.length} icon={ListChecks} />
        <StatCard label="Active" value={active} hint="Pending or confirmed" icon={CalendarCheck} />
        <StatCard label="Cancelled" value={cancelled} icon={CalendarX2} />
      </div>
      {reservations.length > 0 ? (
        <section className="space-y-3">
          <h2 className="text-lg font-semibold">Your Bookings</h2>
          <div className="space-y-3">
            {reservations.map((reservation) => (
              <ReservationRow
                key={reservation.id}
                title={reservation.serviceName}
                when={toCompanyLocalDate(reservation.startTime).toLocaleString()}
                status={reservation.status}
                actions={
                  (reservation.status === "Pending" || reservation.status === "Confirmed") && (
                    <Button
                      variant="destructive-soft"
                      size="sm"
                      onClick={() => handleCancel(reservation.id)}
                      disabled={cancel.isPending}
                    >
                      Cancel
                    </Button>
                  )
                }
              />
            ))}
          </div>
        </section>
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
