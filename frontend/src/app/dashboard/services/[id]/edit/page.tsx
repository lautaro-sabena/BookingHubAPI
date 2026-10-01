"use client";

import { useParams, useRouter } from "next/navigation";
import { ServiceForm } from "@/components/services/ServiceForm";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useService, useUpdateService } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function EditServicePage() {
  const { allowed } = useRequireRole("Owner");
  const serviceId = useParams().id as string;
  const { data: service, isLoading, isError } = useService(serviceId, allowed);
  const update = useUpdateService(serviceId);
  const router = useRouter();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  if (!service) {
    return (
      <div className="max-w-2xl">
        <h1 className="text-2xl font-bold mb-6">Edit Service</h1>
        {isError && <ErrorNotice error={new Error("Service not found")} fallback="Service not found" />}
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
