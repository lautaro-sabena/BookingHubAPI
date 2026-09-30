"use client";

import { CompanyForm } from "@/components/company/CompanyForm";
import { useMyCompany } from "@/hooks/queries/useCompany";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function EditCompanyPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: company, isLoading } = useMyCompany();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  // Without a company the form starts empty (saving creates it), exactly like before.
  return (
    <CompanyForm
      initial={{
        name: company?.name ?? "",
        description: company?.description || "",
        timeZone: company?.timeZone ?? "",
      }}
    />
  );
}
