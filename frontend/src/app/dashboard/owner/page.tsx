"use client";

import Link from "next/link";
import { ArrowRight, Building2, CalendarCheck, CalendarDays, ClipboardList, Clock, Globe2, Power } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { StatCard } from "@/components/ui/stat-card";
import { useMyCompany } from "@/hooks/queries/useCompany";
import { useRequireRole } from "@/hooks/useRequireRole";

const SHORTCUTS = [
  { href: "/dashboard/services", label: "Services", text: "Add or edit what you offer", icon: ClipboardList },
  { href: "/dashboard/availability", label: "Availability", text: "Set your weekly opening hours", icon: Clock },
  { href: "/dashboard/reservations", label: "Reservations", text: "Confirm pending bookings", icon: CalendarCheck },
  { href: "/dashboard/owner/calendar", label: "Calendar", text: "See every booking by day", icon: CalendarDays },
];

export default function OwnerDashboardPage() {
  const { allowed } = useRequireRole("Owner", "/dashboard/customer");
  const { data: company, isLoading } = useMyCompany();

  if (!allowed || isLoading) {
    return <LoadingState />;
  }

  return (
    <div className="space-y-8">
      <PageHeader title="Dashboard" description="An overview of your business on BookingHub." />

      {company ? (
        <div className="grid gap-4 sm:grid-cols-3">
          <StatCard label="Company" value={company.name} hint={company.description || "Set up your company"} icon={Building2} />
          <StatCard label="Time Zone" value={<span className="text-lg">{company.timeZone}</span>} hint="Customers see times in this zone" icon={Globe2} />
          <StatCard
            label="Status"
            value={
              <span className={company.isActive ? "text-success" : "text-muted-foreground"}>
                {company.isActive ? "Active" : "Inactive"}
              </span>
            }
            hint={company.isActive ? "Visible to customers" : "Hidden from customers"}
            icon={Power}
          />
        </div>
      ) : (
        <EmptyState
          icon={Building2}
          title="No company"
          description="Set up your company"
          action={
            <Link href="/dashboard/company/edit">
              <Button>Create Company</Button>
            </Link>
          }
        />
      )}

      <section className="space-y-3">
        <h2 className="text-lg font-semibold">Quick actions</h2>
        <div className="grid gap-3 sm:grid-cols-2">
          {SHORTCUTS.map(({ href, label, text, icon: Icon }) => (
            <Link
              key={href}
              href={href}
              className="group flex items-center gap-4 rounded-xl border border-border/70 bg-card p-4 shadow-card transition-all hover:border-primary/40 hover:shadow-lift focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-accent text-accent-foreground">
                <Icon className="h-5 w-5" aria-hidden="true" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block font-semibold">{label}</span>
                <span className="block text-sm text-muted-foreground">{text}</span>
              </span>
              <ArrowRight className="h-4 w-4 text-muted-foreground transition-transform group-hover:translate-x-0.5 group-hover:text-primary" aria-hidden="true" />
            </Link>
          ))}
        </div>
      </section>

      {company && (
        <Card>
          <CardHeader>
            <CardTitle>Company Details</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="divide-y divide-border/70 text-sm">
              <div className="flex justify-between gap-4 py-3 first:pt-0">
                <dt className="font-medium text-muted-foreground">Name:</dt>
                <dd className="text-right font-medium">{company.name}</dd>
              </div>
              <div className="flex justify-between gap-4 py-3">
                <dt className="font-medium text-muted-foreground">Time Zone:</dt>
                <dd className="text-right font-medium">{company.timeZone}</dd>
              </div>
              <div className="flex justify-between gap-4 py-3 last:pb-0">
                <dt className="font-medium text-muted-foreground">Status:</dt>
                <dd className="text-right font-medium">{company.isActive ? "Active" : "Inactive"}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
