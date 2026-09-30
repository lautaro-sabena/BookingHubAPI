"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatCompanyDate, formatCompanyTime } from "@/lib/dateTime";
import { Reservation } from "@/types";

function Field({ label, children, className }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={className}>
      <p className="text-sm font-medium text-muted-foreground">{label}</p>
      <p className="text-sm">{children}</p>
    </div>
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
    <Card>
      <CardHeader>
        <CardTitle>Reservation Details</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="Customer Email">{reservation.customerEmail}</Field>
          <Field label="Service">{reservation.serviceName}</Field>
          <Field label="Date & Time">{whenLabel(reservation)}</Field>
          <Field label="Duration">{reservation.serviceDuration} minutes</Field>
          <Field label="Status">{reservation.status}</Field>
          <Field label="Created">{new Date(reservation.createdAt).toLocaleDateString()}</Field>
        </div>
        {reservation.notes && <Field label="Notes">{reservation.notes}</Field>}
        <div className="flex gap-2">
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
      </CardContent>
    </Card>
  );
}

export function CustomerReservationDetails({ reservation, onClose, onCancel, busy }: ReservationDetailsProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Reservation Details</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="Service">{reservation.serviceName}</Field>
          <Field label="Status">{reservation.status}</Field>
          <Field label="Date & Time">{whenLabel(reservation)}</Field>
          <Field label="Duration">{reservation.serviceDuration} minutes</Field>
          {reservation.notes && (
            <Field label="Notes" className="col-span-2">
              {reservation.notes}
            </Field>
          )}
        </div>
        {canCancel(reservation) && (
          <Button variant="destructive" size="sm" onClick={() => onCancel(reservation)} disabled={busy}>
            Cancel Reservation
          </Button>
        )}
        <Button variant="outline" size="sm" onClick={onClose}>
          Close
        </Button>
      </CardContent>
    </Card>
  );
}
