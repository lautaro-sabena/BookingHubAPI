"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useMyCompany } from "@/hooks/queries/useCompany";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function OwnerDashboardPage() {
  const { allowed } = useRequireRole("Owner", "/dashboard/customer");
  const { data: company, isLoading } = useMyCompany();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">Dashboard</h1>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Company</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{company?.name || "No company"}</div>
            <p className="text-xs text-muted-foreground">
              {company?.description || "Set up your company"}
            </p>
          </CardContent>
        </Card>
      </div>
      {company && (
        <Card>
          <CardHeader>
            <CardTitle>Company Details</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="space-y-2">
              <div className="flex justify-between">
                <dt className="font-medium">Name:</dt>
                <dd>{company.name}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="font-medium">Time Zone:</dt>
                <dd>{company.timeZone}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="font-medium">Status:</dt>
                <dd>{company.isActive ? "Active" : "Inactive"}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
