"use client";

import { use } from "react";
import Link from "next/link";
import { SearchX } from "lucide-react";
import { BookingForm } from "@/components/booking/BookingForm";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/skeleton";
import { useService } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function BookServicePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { allowed } = useRequireRole("Customer");
  const { data: service, isLoading } = useService(id, allowed);

  if (!allowed || isLoading) {
    return <LoadingState variant="form" />;
  }

  if (!service) {
    return (
      <EmptyState
        icon={SearchX}
        title="Service not found"
        description="It may have been removed by the business."
        action={
          <Link href="/services">
            <Button variant="outline">Browse services</Button>
          </Link>
        }
      />
    );
  }

  return <BookingForm service={service} />;
}
