"use client";

import { useRouter } from "next/navigation";
import { ServiceForm } from "@/components/services/ServiceForm";
import { useCreateService } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

const EMPTY_SERVICE = { name: "", description: "", durationMinutes: 30, price: 0 };

export default function NewServicePage() {
  const { allowed } = useRequireRole("Owner");
  const create = useCreateService();
  const router = useRouter();

  if (!allowed) {
    return <div>Loading...</div>;
  }

  return (
    <ServiceForm
      title="Add New Service"
      initial={EMPTY_SERVICE}
      submitLabel="Create Service"
      savingLabel="Creating..."
      saving={create.isPending}
      error={create.error}
      errorFallback="Failed to create service"
      onSubmit={(input) => create.mutate(input, { onSuccess: () => router.push("/dashboard/services") })}
    />
  );
}
