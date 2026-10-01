"use client";

import { use } from "react";
import { BookingForm } from "@/components/booking/BookingForm";
import { useService } from "@/hooks/queries/useServices";
import { useRequireRole } from "@/hooks/useRequireRole";

export default function BookServicePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { allowed } = useRequireRole("Customer");
  const { data: service, isLoading } = useService(id, allowed);

  if (!allowed || isLoading) {
    return <div>Loading...</div>;
  }

  if (!service) {
    return <div>Service not found</div>;
  }

  return <BookingForm service={service} />;
}
