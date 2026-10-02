"use client";

import Link from "next/link";
import { Building2, Globe2, Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { useMyCompany } from "@/hooks/queries/useCompany";
import { useRequireRole } from "@/hooks/useRequireRole";
import { cn } from "@/lib/utils";

export default function CompanyPage() {
  const { allowed } = useRequireRole("Owner");
  // A 404 (no company yet) leaves `company` undefined and shows the "Create Company" prompt.
  const { data: company, isLoading } = useMyCompany();

  if (!allowed || isLoading) {
    return <LoadingState variant="form" />;
  }

  if (!company) {
    return (
      <div className="space-y-6">
        <PageHeader title="Company" />
        <EmptyState
          icon={Building2}
          title="You don't have a company yet."
          description="Create it to start publishing services and taking bookings."
          action={
            <Link href="/dashboard/company/edit">
              <Button>Create Company</Button>
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <div className="max-w-3xl space-y-6">
      <PageHeader
        title="Company"
        description="How your business appears to customers."
        actions={
          <Link href="/dashboard/company/edit">
            <Button variant="outline">
              <Pencil className="h-4 w-4" aria-hidden="true" />
              Edit
            </Button>
          </Link>
        }
      />
      <Card className="overflow-hidden">
        <div aria-hidden="true" className="h-20 bg-gradient-to-br from-accent to-primary/30" />
        <CardContent className="-mt-8 space-y-6">
          <div className="flex flex-wrap items-end justify-between gap-4">
            <div className="flex items-end gap-4">
              <span
                aria-hidden="true"
                className="flex h-16 w-16 items-center justify-center rounded-2xl border-4 border-card bg-primary text-2xl font-bold text-primary-foreground shadow-card"
              >
                {company.name.charAt(0).toUpperCase()}
              </span>
              <h2 className="pb-1 text-xl font-bold tracking-tight">{company.name}</h2>
            </div>
            <span
              className={cn(
                "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold",
                company.isActive ? "bg-success-soft text-success" : "bg-muted text-muted-foreground"
              )}
            >
              <span className="h-1.5 w-1.5 rounded-full bg-current" aria-hidden="true" />
              {company.isActive ? "Active" : "Inactive"}
            </span>
          </div>
          <dl className="grid gap-4 sm:grid-cols-[160px_1fr]">
            <dt className="text-sm font-medium text-muted-foreground">Description:</dt>
            <dd className="text-sm">{company.description || "N/A"}</dd>
            <dt className="text-sm font-medium text-muted-foreground">Time Zone:</dt>
            <dd className="flex items-center gap-2 text-sm">
              <Globe2 className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
              {company.timeZone}
            </dd>
            <dt className="text-sm font-medium text-muted-foreground">Status:</dt>
            <dd className="text-sm">{company.isActive ? "Active" : "Inactive"}</dd>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
