"use client";

import { WorkingHoursForm } from "@/components/availability/WorkingHoursForm";
import { Button } from "@/components/ui/button";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useSaveWorkingHours, useWorkingHours } from "@/hooks/queries/useWorkingHours";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function AvailabilityPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: week, isLoading, error, refetch, isFetching } = useWorkingHours();
  const save = useSaveWorkingHours();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  // Never offer the form without the real schedule: saving the closed default week would overwrite it.
  if (!week) {
    return (
      <div className="max-w-2xl space-y-4">
        <h1 className="text-2xl font-bold">Working Hours</h1>
        <ErrorNotice error={error} fallback="Failed to load your working hours" />
        <Button onClick={() => void refetch()} disabled={isFetching}>
          {isFetching ? "Retrying..." : "Retry"}
        </Button>
      </div>
    );
  }

  return (
    <WorkingHoursForm
      // The form takes the saved week once. Keyed by its content: a refetch that returns the same schedule keeps
      // the owner's unsaved edits, one that returns a different schedule (changed elsewhere) resyncs the form.
      key={JSON.stringify(week)}
      initial={week}
      save={save}
    />
  );
}
