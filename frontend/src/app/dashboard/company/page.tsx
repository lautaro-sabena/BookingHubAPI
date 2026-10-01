"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useMyCompany } from "@/hooks/queries/useCompany";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function CompanyPage() {
  const { allowed } = useRequireRole("Owner");
  // A 404 (no company yet) leaves `company` undefined and shows the "Create Company" prompt.
  const { data: company, isLoading } = useMyCompany();

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  if (!company) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Company</h1>
        <Card>
          <CardContent className="py-8 text-center">
            <p className="mb-4 text-muted-foreground">You don&apos;t have a company yet.</p>
            <Link href="/dashboard/company/edit">
              <Button>Create Company</Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Company</h1>
        <Link href="/dashboard/company/edit">
          <Button>Edit</Button>
        </Link>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{company.name}</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="space-y-2">
            <div className="flex justify-between">
              <dt className="font-medium text-muted-foreground">Description:</dt>
              <dd>{company.description || "N/A"}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="font-medium text-muted-foreground">Time Zone:</dt>
              <dd>{company.timeZone}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="font-medium text-muted-foreground">Status:</dt>
              <dd>{company.isActive ? "Active" : "Inactive"}</dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
