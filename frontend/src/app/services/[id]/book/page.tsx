"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import api from "@/lib/api";
import { Service, Reservation, AvailableSlot } from "@/types";

export default function BookServicePage({ params }: { params: { id: string } }) {
  const { user, isLoading: authLoading } = useAuth();
  const [service, setService] = useState<Service | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [availableSlots, setAvailableSlots] = useState<AvailableSlot[]>([]);
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState<AvailableSlot | null>(null);
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");
  const router = useRouter();

  const today = new Date().toISOString().split("T")[0];

  const fetchService = useCallback(async () => {
    try {
      const response = await api.get<Service>(`/services/${params.id}`);
      setService(response.data);
    } catch (err) {
      console.error("Failed to fetch service:", err);
    } finally {
      setLoading(false);
    }
  }, [params.id]);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login");
      return;
    }

    if (user?.role !== "Customer") {
      router.push("/dashboard");
      return;
    }

    fetchService();
  }, [user, authLoading, router, params.id, fetchService]);

  const fetchAvailableSlots = async (date: Date) => {
    setSlotsLoading(true);
    setSelectedSlot(null);
    try {
      const dateStr = date.toISOString().split("T")[0];
      const response = await api.get<AvailableSlot[]>(`/availability/${params.id}?date=${dateStr}`);
      setAvailableSlots(response.data);
    } catch (err) {
      console.error("Failed to fetch availability:", err);
      setAvailableSlots([]);
    } finally {
      setSlotsLoading(false);
    }
  };

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const dateValue = e.target.value;
    if (dateValue) {
      const date = new Date(dateValue + "T00:00:00");
      setSelectedDate(date);
      fetchAvailableSlots(date);
    } else {
      setSelectedDate(null);
      setAvailableSlots([]);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSlot) {
      setError("Please select a time slot");
      return;
    }

    setSaving(true);
    setError("");
    setSuccess(false);

    try {
      await api.post<Reservation>("/reservations", {
        serviceId: params.id,
        startTime: selectedSlot.startTime,
        notes: notes || null,
      });
      setSuccess(true);
      setTimeout(() => {
        router.push("/dashboard");
      }, 2000);
    } catch (err: any) {
      setError(err.response?.data?.error || "Failed to create reservation");
    } finally {
      setSaving(false);
    }
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit", hour12: true });
  };

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  if (!service) {
    return <div>Service not found</div>;
  }

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Book {service.name}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{service.name}</CardTitle>
          <CardDescription>
            ${service.price} - {service.durationMinutes} minutes
          </CardDescription>
          <p className="text-sm text-muted-foreground mt-2">
            {service.companyDescription || service.companyName}
          </p>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            {success && (
              <div className="p-3 text-sm text-green-600 bg-green-50 rounded-md">
                Reservation created successfully! Redirecting to dashboard...
              </div>
            )}
            {error && (
              <div className="p-3 text-sm text-red-500 bg-red-50 rounded-md">
                {error}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="date">Select Date</Label>
              <Input
                id="date"
                type="date"
                min={today}
                onChange={handleDateChange}
                required
              />
            </div>

            {selectedDate && (
              <div className="space-y-2">
                <Label>Available Time Slots</Label>
                {slotsLoading ? (
                  <p className="text-sm text-muted-foreground">Loading available slots...</p>
                ) : availableSlots.length > 0 ? (
                  <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
                    {availableSlots.map((slot, index) => (
                      <Button
                        key={index}
                        type="button"
                        variant={selectedSlot === slot ? "default" : "outline"}
                        size="sm"
                        onClick={() => setSelectedSlot(slot)}
                        className="text-sm"
                      >
                        {formatTime(slot.startTime)}
                      </Button>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    No available slots for this date. Please select another date.
                  </p>
                )}
              </div>
            )}

            {selectedSlot && (
              <div className="space-y-2">
                <Label htmlFor="notes">Notes (optional)</Label>
                <Input
                  id="notes"
                  type="text"
                  placeholder="Any special requests..."
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                />
              </div>
            )}

            <div className="flex gap-4">
              <Button type="submit" disabled={saving || !selectedSlot}>
                {saving ? "Booking..." : "Confirm Booking"}
              </Button>
              <Button type="button" variant="outline" onClick={() => router.back()}>
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
