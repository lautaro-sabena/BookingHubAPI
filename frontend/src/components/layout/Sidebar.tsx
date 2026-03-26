"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { cn } from "@/lib/utils";
import { LayoutDashboard, Calendar, Clock, Star, History } from "lucide-react";

const ownerNavItems = [
  { href: "/dashboard", label: "Overview", icon: LayoutDashboard },
  { href: "/dashboard/company", label: "Company", icon: Calendar },
  { href: "/dashboard/services", label: "Services", icon: Calendar },
  { href: "/dashboard/availability", label: "Availability", icon: Clock },
  { href: "/dashboard/reservations", label: "Reservations", icon: Calendar },
  { href: "/dashboard/owner/calendar", label: "Calendar", icon: Calendar },
];

const customerNavItems = [
  { href: "/dashboard", label: "Home", icon: LayoutDashboard },
  { href: "/dashboard/customer/history", label: "History", icon: History },
  { href: "/dashboard/customer/calendar", label: "Calendar", icon: Calendar },
  { href: "/dashboard/customer/favorites", label: "Favorites", icon: Star },
  { href: "/services", label: "Book a Service", icon: Calendar },
];

export function Sidebar() {
  const pathname = usePathname();
  const { user } = useAuth();
  
  const isOwner = pathname.startsWith("/dashboard/owner") || 
    (pathname === "/dashboard" && user?.role === "Owner");
  
  const isCustomer = user?.role === "Customer";
  
  // Show customer nav for customer, owner nav for owner
  const navItems = isCustomer ? customerNavItems : (isOwner ? ownerNavItems : []);

  return (
    <aside className="w-64 border-r bg-background">
      <nav className="flex flex-col gap-1 p-4">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <Icon className="h-4 w-4" />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
