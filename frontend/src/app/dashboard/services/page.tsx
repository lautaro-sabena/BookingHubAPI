"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
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
    return <div>Loading...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Services</h1>
        <Link href="/dashboard/services/new">
          <Button>Add Service</Button>
        </Link>
      </div>
      <ErrorNotice error={error ?? remove.error} />
      {services.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {services.map((service) => (
            <Card key={service.id}>
              <CardHeader>
                <CardTitle className="text-lg">{service.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground mb-2">
                  {service.description || "No description"}
                </p>
                <div className="flex justify-between text-sm mb-4">
                  <span>${service.price}</span>
                  <span>{service.durationMinutes} min</span>
                </div>
                <div className="flex gap-2">
                  <Link href={`/dashboard/services/${service.id}/edit`}>
                    <Button variant="outline" size="sm">Edit</Button>
                  </Link>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => handleDelete(service.id)}
                    disabled={remove.isPending}
                  >
                    Delete
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            No services yet. Add your first service!
          </CardContent>
        </Card>
      )}
    </div>
  );
}
