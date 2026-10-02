"use client";

import { WorkingHoursForm } from "@/components/availability/WorkingHoursForm";
import { Button } from "@/components/ui/button";
import { ErrorNotice } from "@/components/ui/error-notice";
import { LoadingState } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/ui/page-header";
import { RotateCw } from "lucide-react";
import { useSaveWorkingHours, useWorkingHours } from "@/hooks/queries/useWorkingHours";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function AvailabilityPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: week, isLoading, error, refetch, isFetching } = useWorkingHours();
  const save = useSaveWorkingHours();

  if (!allowed || isLoading) {
    return <LoadingState variant="form" />;
  }

  // Never offer the form without the real schedule: saving the closed default week would overwrite it.
  if (!week) {
    return (
      <div className="max-w-2xl space-y-6">
        <PageHeader title="Working Hours" />
        <div className="space-y-4 rounded-xl border border-border/70 bg-card p-6 shadow-card">
          <ErrorNotice error={error} fallback="Failed to load your working hours" />
          <p className="text-sm text-muted-foreground">
            We need your saved schedule before you can edit it, so nothing gets overwritten.
          </p>
          <Button onClick={() => void refetch()} disabled={isFetching}>
            <RotateCw className={isFetching ? "h-4 w-4 animate-spin" : "h-4 w-4"} aria-hidden="true" />
            {isFetching ? "Retrying..." : "Retry"}
          </Button>
        </div>
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
