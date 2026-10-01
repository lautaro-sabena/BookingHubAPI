"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { UserRole } from "@/types";

/**
 * Route guard for a page that belongs to one role. Once auth has settled, a signed-out visitor is sent to /login and
 * a user with another role to `fallback`. `allowed` is true only for the right role, so a page can skip its data
 * requests and rendering until then.
 */
export function useRequireRole(role: UserRole, fallback = "/dashboard") {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;
    if (!user) router.push("/login");
    else if (user.role !== role) router.push(fallback);
  }, [user, isLoading, role, fallback, router]);

  return { user, authLoading: isLoading, allowed: !isLoading && user?.role === role };
}
