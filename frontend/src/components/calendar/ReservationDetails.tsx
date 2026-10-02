"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatCompanyDate, formatCompanyTime } from "@/lib/dateTime";
import { StatusBadge } from "@/components/ui/status-badge";
import { cn } from "@/lib/utils";
import { Reservation } from "@/types";
import { X } from "lucide-react";

function Field({ label, children, className }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={cn("min-w-0 rounded-lg bg-muted/50 px-3 py-2.5", className)}>
      <p className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">{label}</p>
      <div className="mt-1 break-words text-sm font-medium">{children}</div>
    </div>
  );
}

function DetailsShell({
  reservation,
  onClose,
  children,
}: {
  reservation: Reservation;
  onClose: () => void;
  children: React.ReactNode;
}) {
  return (
    <Card className="animate-in fade-in-0 slide-in-from-bottom-2 duration-200">
      <CardHeader className="flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle>Reservation Details</CardTitle>
          <p className="text-sm text-muted-foreground tabular">{whenLabel(reservation)}</p>
        </div>
        <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close details" className="-mr-2 -mt-2 h-9 w-9 shrink-0">
          <X className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-5">{children}</CardContent>
    </Card>
  );
}

function whenLabel(reservation: Reservation) {
  return `${formatCompanyDate(reservation.startTime)} at ${formatCompanyTime(reservation.startTime)}`;
}

interface ReservationDetailsProps {
  reservation: Reservation;
  onClose: () => void;
  onCancel: (reservation: Reservation) => void;
  /** A confirm or cancel request is in flight: the actions wait so it cannot be sent twice. */
  busy?: boolean;
}

function canCancel(reservation: Reservation) {
  return reservation.status === "Pending" || reservation.status === "Confirmed";
}

interface OwnerReservationDetailsProps extends ReservationDetailsProps {
  onConfirm: (reservation: Reservation) => void;
}

export function OwnerReservationDetails({ reservation, onClose, onCancel, onConfirm, busy }: OwnerReservationDetailsProps) {
  return (
    <DetailsShell reservation={reservation} onClose={onClose}>
        <div className="grid gap-2 sm:grid-cols-2">
          <Field label="Customer Email">{reservation.customerEmail}</Field>
          <Field label="Service">{reservation.serviceName}</Field>
          <Field label="Date & Time">{whenLabel(reservation)}</Field>
          <Field label="Duration">{reservation.serviceDuration} minutes</Field>
          <Field label="Status"><StatusBadge status={reservation.status} /></Field>
          <Field label="Created">{new Date(reservation.createdAt).toLocaleDateString()}</Field>
        </div>
        {reservation.notes && <Field label="Notes">{reservation.notes}</Field>}
        <div className="flex flex-wrap gap-2 border-t border-border/70 pt-4">
          {reservation.status === "Pending" && (
            <Button size="sm" onClick={() => onConfirm(reservation)} disabled={busy}>
              Confirm
            </Button>
          )}
          {canCancel(reservation) && (
            <Button variant="destructive" size="sm" onClick={() => onCancel(reservation)} disabled={busy}>
              Cancel
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={onClose}>
            Close
          </Button>
        </div>
    </DetailsShell>
  );
}

export function CustomerReservationDetails({ reservation, onClose, onCancel, busy }: ReservationDetailsProps) {
  return (
    <DetailsShell reservation={reservation} onClose={onClose}>
        <div className="grid gap-2 sm:grid-cols-2">
          <Field label="Service">{reservation.serviceName}</Field>
          <Field label="Status"><StatusBadge status={reservation.status} /></Field>
          <Field label="Date & Time">{whenLabel(reservation)}</Field>
          <Field label="Duration">{reservation.serviceDuration} minutes</Field>
          {reservation.notes && (
            <Field label="Notes" className="sm:col-span-2">
              {reservation.notes}
            </Field>
          )}
        </div>
        <div className="flex flex-wrap gap-2 border-t border-border/70 pt-4">
          {canCancel(reservation) && (
            <Button variant="destructive" size="sm" onClick={() => onCancel(reservation)} disabled={busy}>
              Cancel Reservation
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={onClose}>
            Close
          </Button>
        </div>
    </DetailsShell>
  );
}
