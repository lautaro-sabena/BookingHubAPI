"use client";

import Link from "next/link";
import { ClipboardList, Pencil, Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { LoadingState } from "@/components/ui/skeleton";
import { ServiceMeta } from "@/components/services/ServiceCard";
import { useDeleteService, useOwnerServices } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function OwnerServicesPage() {
  const { allowed } = useRequireRole("Owner");
  const { data: services = [], isLoading, error } = useOwnerServices();
  const remove = useDeleteService();

  const handleDelete = (id: string) => {
    if (!confirm("Are you sure you want to delete this service?")) return;
    remove.mutate(id);
  };

  if (!allowed || isLoading) {
    return <LoadingState />;
  }

  const addButton = (
    <Link href="/dashboard/services/new">
      <Button>
        <Plus className="h-4 w-4" aria-hidden="true" />
        Add Service
      </Button>
    </Link>
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title="Services"
        description="What customers can book with you. Price and duration show on every booking."
        actions={services.length > 0 ? addButton : undefined}
      />
      <ErrorNotice error={error ?? remove.error} />
      {services.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {services.map((service) => (
            <Card key={service.id} className="flex flex-col">
              <CardHeader className="flex-row items-start gap-3 space-y-0 pb-3">
                <span
                  aria-hidden="true"
                  className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-accent font-bold text-accent-foreground"
                >
                  {service.name.charAt(0).toUpperCase()}
                </span>
                <CardTitle className="min-w-0 pt-2 text-base">{service.name}</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-1 flex-col">
                <p className="mb-4 line-clamp-2 text-sm text-muted-foreground">
                  {service.description || "No description"}
                </p>
                <div className="mb-4 mt-auto">
                  <ServiceMeta price={service.price} durationMinutes={service.durationMinutes} />
                </div>
                <div className="flex gap-2 border-t border-border/70 pt-4">
                  <Link href={`/dashboard/services/${service.id}/edit`}>
                    <Button variant="outline" size="sm">
                      <Pencil className="h-3.5 w-3.5" aria-hidden="true" />
                      Edit
                    </Button>
                  </Link>
                  <Button
                    variant="destructive-soft"
                    size="sm"
                    onClick={() => handleDelete(service.id)}
                    disabled={remove.isPending}
                  >
                    <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                    Delete
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={ClipboardList}
          title="No services yet. Add your first service!"
          description="Customers can only book once you publish at least one service."
          action={addButton}
        />
      )}
    </div>
  );
}
