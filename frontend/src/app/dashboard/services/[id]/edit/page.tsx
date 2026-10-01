"use client";

import { useParams, useRouter } from "next/navigation";
import { isAxiosError } from "axios";
import { Button } from "@/components/ui/button";
import { ServiceForm } from "@/components/services/ServiceForm";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useService, useUpdateService } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function EditServicePage() {
  const { allowed } = useRequireRole("Owner");
  const serviceId = useParams().id as string;
  const { data: service, isLoading, error, refetch, isFetching } = useService(serviceId, allowed);
  const update = useUpdateService(serviceId);
  const router = useRouter();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  if (!service) {
    return (
      <div className="max-w-2xl">
        <h1 className="text-2xl font-bold mb-6">Edit Service</h1>
        {isAxiosError(error) && error.response?.status === 404 ? (
          <p className="text-muted-foreground">Service not found</p>
        ) : error ? (
          <div className="space-y-4">
            <ErrorNotice error={error} fallback="Failed to load the service" />
            <Button onClick={() => void refetch()} disabled={isFetching}>
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
