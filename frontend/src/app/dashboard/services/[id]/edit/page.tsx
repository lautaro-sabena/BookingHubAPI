"use client";

import { useParams, useRouter } from "next/navigation";
import { isAxiosError } from "axios";
import { Button } from "@/components/ui/button";
import { ServiceForm } from "@/components/services/ServiceForm";
import { ErrorNotice } from "@/components/ui/error-notice";
import { LoadingState } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import Link from "next/link";
import { RotateCw, SearchX } from "lucide-react";
import { useService, useUpdateService } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function EditServicePage() {
  const { allowed } = useRequireRole("Owner");
  const serviceId = useParams().id as string;
  const { data: service, isLoading, error, refetch, isFetching } = useService(serviceId, allowed);
  const update = useUpdateService(serviceId);
  const router = useRouter();

  if (!allowed || isLoading) {
    return <LoadingState variant="form" />;
  }

  if (!service) {
    return (
      <div className="max-w-2xl space-y-6">
        <PageHeader title="Edit Service" />
        {isAxiosError(error) && error.response?.status === 404 ? (
          <EmptyState
            icon={SearchX}
            title="Service not found"
            description="It may have been deleted."
            action={
              <Link href="/dashboard/services">
                <Button variant="outline">Back to services</Button>
              </Link>
            }
          />
        ) : error ? (
          <div className="space-y-4 rounded-xl border border-border/70 bg-card p-6 shadow-card">
            <ErrorNotice error={error} fallback="Failed to load the service" />
            <Button onClick={() => void refetch()} disabled={isFetching}>
              <RotateCw className={isFetching ? "h-4 w-4 animate-spin" : "h-4 w-4"} aria-hidden="true" />
              {isFetching ? "Retrying..." : "Retry"}
            </Button>
          </div>
        ) : null}
      </div>
    );
  }

  return (
    <ServiceForm
      // The form takes its initial values once: a different service starts a fresh one.
      key={service.id}
      title="Edit Service"
      initial={{
        name: service.name,
        description: service.description || "",
        durationMinutes: service.durationMinutes,
        price: service.price,
      }}
      submitLabel="Save Changes"
      savingLabel="Saving..."
      saving={update.isPending}
      error={update.error}
      errorFallback="Failed to update service"
      onSubmit={(input) => update.mutate(input, { onSuccess: () => router.push("/dashboard/services") })}
    />
  );
}
