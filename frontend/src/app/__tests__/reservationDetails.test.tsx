// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { CustomerReservationDetails, OwnerReservationDetails } from "@/components/calendar/ReservationDetails";
import { Reservation } from "@/types";

const reservation: Reservation = {
  id: "r1",
  serviceId: "s",
  customerId: "c",
  customerEmail: "c@x.com",
  companyId: "co",
  serviceName: "Haircut",
  serviceDuration: 30,
  startTime: "2030-01-07T09:00:00-03:00",
  endTime: "2030-01-07T09:30:00-03:00",
  status: "Pending",
  createdAt: "2030-01-01T00:00:00Z",
};

describe("reservation details actions", () => {
  afterEach(cleanup);

  it("owner panel: confirm and cancel are disabled while busy", () => {
    const onConfirm = vi.fn();
    const onCancel = vi.fn();
    render(
      <OwnerReservationDetails reservation={reservation} onClose={vi.fn()} onConfirm={onConfirm} onCancel={onCancel} busy />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Confirm" }));
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onConfirm).not.toHaveBeenCalled();
    expect(onCancel).not.toHaveBeenCalled();
  });

  it("owner panel: actions work when not busy", () => {
    const onConfirm = vi.fn();
    render(
      <OwnerReservationDetails reservation={reservation} onClose={vi.fn()} onConfirm={onConfirm} onCancel={vi.fn()} />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Confirm" }));

    expect(onConfirm).toHaveBeenCalledWith(reservation);
  });

  it("customer panel: cancel is disabled while busy", () => {
    const onCancel = vi.fn();
    render(<CustomerReservationDetails reservation={reservation} onClose={vi.fn()} onCancel={onCancel} busy />);

    fireEvent.click(screen.getByRole("button", { name: "Cancel Reservation" }));

    expect(onCancel).not.toHaveBeenCalled();
  });
});
