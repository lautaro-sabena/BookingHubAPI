"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { LoadingState } from "@/components/ui/skeleton";

export default function DashboardPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && user) {
      if (user.role === "Owner") {
        router.replace("/dashboard/owner");
      } else {
        router.replace("/dashboard/customer");
      }
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return <LoadingState />;
  }

  return null;
}
