"use client";

import { WorkingHoursForm } from "@/components/availability/WorkingHoursForm";
import { useWorkingHours } from "@/hooks/queries/useWorkingHours";
import { useRequireRole } from "@/hooks/useRequireRole";
import { defaultWeek } from "@/lib/workingHours";

export default function AvailabilityPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: week, isLoading } = useWorkingHours();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  // If the schedule cannot be loaded the form starts from the closed default week, as before.
  return <WorkingHoursForm initial={week ?? defaultWeek()} />;
}
