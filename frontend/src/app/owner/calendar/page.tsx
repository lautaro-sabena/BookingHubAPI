"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Reservation } from "@/types";

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
}

export default function OwnerCalendarPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentDate, setCurrentDate] = useState(new Date());
  const [selectedReservation, setSelectedReservation] = useState<Reservation | null>(null);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login");
      return;
    }

    if (user?.role !== "Owner") {
      router.push("/dashboard");
      return;
    }

    fetchReservations();
  }, [user, authLoading, router]);

  const fetchReservations = async () => {
    try {
      const response = await api.get<{ items: Reservation[] }>("/reservations");
      setReservations(response.data.items);
    } catch (err) {
      console.error("Failed to fetch reservations:", err);
    } finally {
      setLoading(false);
    }
  };

  const getDaysInMonth = (date: Date): CalendarDay[] => {
    const year = date.getFullYear();
    const month = date.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const days: CalendarDay[] = [];

    const startDayOfWeek = firstDay.getDay();
    for (let i = 0; i < startDayOfWeek; i++) {
      const d = new Date(year, month, -startDayOfWeek + i + 1);
      days.push({ date: d, isCurrentMonth: false, isToday: false });
    }

    for (let day = 1; day <= lastDay.getDate(); day++) {
      const d = new Date(year, month, day);
      days.push({
        date: d,
        isCurrentMonth: true,
        isToday: d.getTime() === today.getTime(),
      });
    }

    const remainingDays = 42 - days.length;
    for (let i = 1; i <= remainingDays; i++) {
      const d = new Date(year, month + 1, i);
      days.push({ date: d, isCurrentMonth: false, isToday: false });
    }

    return days;
  };

  const getReservationsForDate = (date: Date): Reservation[] => {
    return reservations.filter((r) => {
      const resDate = new Date(r.startTime);
      return (
        resDate.getFullYear() === date.getFullYear() &&
        resDate.getMonth() === date.getMonth() &&
        resDate.getDate() === date.getDate()
      );
    });
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit", hour12: true });
  };

  const formatDate = (date: Date) => {
    return date.toLocaleDateString("en-US", { weekday: "long", year: "numeric", month: "long", day: "numeric" });
  };

  const previousMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1));
  };

  const nextMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1));
  };

  const monthNames = [
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December"
  ];

  const dayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  const days = getDaysInMonth(currentDate);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Reservation Calendar</h1>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>{monthNames[currentDate.getMonth()]} {currentDate.getFullYear()}</CardTitle>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={previousMonth}>Previous</Button>
              <Button variant="outline" size="sm" onClick={nextMonth}>Next</Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-7 gap-1">
            {dayNames.map((day) => (
              <div key={day} className="text-center text-sm font-medium text-muted-foreground py-2">
                {day}
              </div>
            ))}
            {days.map((day, index) => {
              const dayReservations = getReservationsForDate(day.date);
              return (
                <div
                  key={index}
                  className={`min-h-[80px] border p-1 ${
                    day.isCurrentMonth ? "bg-white" : "bg-gray-50"
                  } ${day.isToday ? "border-blue-500 border-2" : ""}`}
                >
                  <div className="text-sm font-medium">{day.date.getDate()}</div>
                  <div className="space-y-1">
                    {dayReservations.slice(0, 2).map((res) => (
                      <div
                        key={res.id}
                        onClick={() => setSelectedReservation(res)}
                        className={`text-xs p-1 rounded cursor-pointer truncate ${
                          res.status === "Pending"
                            ? "bg-yellow-100 text-yellow-800"
                            : res.status === "Confirmed"
                            ? "bg-green-100 text-green-800"
                            : res.status === "Cancelled"
                            ? "bg-red-100 text-red-800"
                            : "bg-gray-100 text-gray-800"
                        }`}
                      >
                        {formatTime(res.startTime)} - {res.serviceName}
                      </div>
                    ))}
                    {dayReservations.length > 2 && (
                      <div className="text-xs text-muted-foreground">+{dayReservations.length - 2} more</div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </CardContent>
      </Card>

      {selectedReservation && (
        <Card>
          <CardHeader>
            <CardTitle>Reservation Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Customer Email</p>
                <p className="text-sm">{selectedReservation.customerEmail}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Service</p>
                <p className="text-sm">{selectedReservation.serviceName}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Date & Time</p>
                <p className="text-sm">
                  {formatDate(new Date(selectedReservation.startTime))} at {formatTime(selectedReservation.startTime)}
                </p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Duration</p>
                <p className="text-sm">{selectedReservation.serviceDuration} minutes</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Status</p>
                <p className="text-sm">{selectedReservation.status}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Created</p>
                <p className="text-sm">{new Date(selectedReservation.createdAt).toLocaleDateString()}</p>
              </div>
            </div>
            {selectedReservation.notes && (
              <div>
                <p className="text-sm font-medium text-muted-foreground">Notes</p>
                <p className="text-sm">{selectedReservation.notes}</p>
              </div>
            )}
            <div className="flex gap-2">
              {selectedReservation.status === "Pending" && (
                <Button
                  size="sm"
                  onClick={async () => {
                    try {
                      await api.put(`/reservations/${selectedReservation.id}/confirm`);
                      fetchReservations();
                      setSelectedReservation(null);
                    } catch (err) {
                      console.error("Failed to confirm reservation:", err);
                    }
                  }}
                >
                  Confirm
                </Button>
              )}
              {(selectedReservation.status === "Pending" || selectedReservation.status === "Confirmed") && (
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={async () => {
                    try {
                      await api.put(`/reservations/${selectedReservation.id}/cancel`);
                      fetchReservations();
                      setSelectedReservation(null);
                    } catch (err) {
                      console.error("Failed to cancel reservation:", err);
                    }
                  }}
                >
                  Cancel
                </Button>
              )}
              <Button variant="outline" size="sm" onClick={() => setSelectedReservation(null)}>
                Close
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
