"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { WorkingHoursResponse } from "@/types";

const daysOfWeek = [
  { value: 0, label: "Sunday" },
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" },
  { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
];

interface DayAvailability {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

export default function AvailabilityPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [availability, setAvailability] = useState<DayAvailability[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState(false);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && user?.role !== "Owner") {
      router.push("/dashboard");
      return;
    }

    if (user?.role === "Owner") {
      fetchAvailability();
    }
  }, [user, authLoading, router]);

  const fetchAvailability = async () => {
    try {
      const response = await api.get<WorkingHoursResponse[]>("/workinghours");
      const days = response.data.map((wh: WorkingHoursResponse) => ({
        dayOfWeek: typeof wh.dayOfWeek === 'number' ? wh.dayOfWeek : parseInt(wh.dayOfWeek),
        startTime: typeof wh.startTime === 'string' ? wh.startTime : "09:00",
        endTime: typeof wh.endTime === 'string' ? wh.endTime : "17:00",
        isActive: wh.isActive,
      }));
      setAvailability(days);
    } catch (err) {
      console.error("Failed to fetch availability:", err);
      setAvailability(
        daysOfWeek.map((d) => ({
          dayOfWeek: d.value,
          startTime: "09:00",
          endTime: "17:00",
          isActive: false,
        }))
      );
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setSuccess(false);

    try {
      const payload = availability
        .filter((a) => a.isActive)
        .map((a) => ({
          dayOfWeek: a.dayOfWeek,
          startTime: a.startTime,
          endTime: a.endTime,
          isActive: a.isActive,
        }));
      await api.put("/workinghours", payload);
      setSuccess(true);
      fetchAvailability();
    } catch (err) {
      console.error("Failed to save availability:", err);
    } finally {
      setSaving(false);
    }
  };

  const updateAvailability = (day: number, field: string, value: string | boolean) => {
    setAvailability((prev) =>
      prev.map((a) =>
        a.dayOfWeek === day
          ? { ...a, [field]: value }
          : a
      )
    );
  };

  useEffect(() => {
    if (!loading && availability.length === 0) {
      setAvailability(
        daysOfWeek.map((d) => ({
          dayOfWeek: d.value,
          startTime: "09:00",
          endTime: "17:00",
          isActive: false,
        }))
      );
    }
  }, [loading]);

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Working Hours</h1>
      <Card>
        <CardHeader>
          <CardTitle>Configure Your Availability</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {success && (
              <div className="p-3 text-sm text-green-600 bg-green-50 rounded-md">
                Availability saved successfully!
              </div>
            )}
            {daysOfWeek.map((day) => {
              const dayAvailability = availability.find(
                (a) => a.dayOfWeek === day.value
              );
              return (
                <div key={day.value} className="flex items-center gap-4">
                  <div className="w-8">
                    <input
                      type="checkbox"
                      checked={dayAvailability?.isActive || false}
                      onChange={(e) =>
                        updateAvailability(day.value, "isActive", e.target.checked)
                      }
                      className="w-4 h-4"
                    />
                  </div>
                  <div className="w-24 font-medium">{day.label}</div>
                  <Input
                    type="time"
                    value={dayAvailability?.startTime || "09:00"}
                    onChange={(e) =>
                      updateAvailability(day.value, "startTime", e.target.value)
                    }
                    className="w-32"
                    disabled={!dayAvailability?.isActive}
                  />
                  <span>to</span>
                  <Input
                    type="time"
                    value={dayAvailability?.endTime || "17:00"}
                    onChange={(e) =>
                      updateAvailability(day.value, "endTime", e.target.value)
                    }
                    className="w-32"
                    disabled={!dayAvailability?.isActive}
                  />
                </div>
              );
            })}
            <Button type="submit" disabled={saving}>
              {saving ? "Saving..." : "Save Availability"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
