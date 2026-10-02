"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { cn } from "@/lib/utils";
import {
  LayoutDashboard,
  Building2,
  ClipboardList,
  Clock,
  CalendarCheck,
  CalendarDays,
  CalendarPlus,
  Star,
  History,
} from "lucide-react";

const ownerNavItems = [
  { href: "/dashboard", label: "Overview", icon: LayoutDashboard },
  { href: "/dashboard/company", label: "Company", icon: Building2 },
  { href: "/dashboard/services", label: "Services", icon: ClipboardList },
  { href: "/dashboard/availability", label: "Availability", icon: Clock },
  { href: "/dashboard/reservations", label: "Reservations", icon: CalendarCheck },
  { href: "/dashboard/owner/calendar", label: "Calendar", icon: CalendarDays },
];

const customerNavItems = [
  { href: "/dashboard", label: "Home", icon: LayoutDashboard },
  { href: "/dashboard/customer/history", label: "History", icon: History },
  { href: "/dashboard/customer/calendar", label: "Calendar", icon: CalendarDays },
  { href: "/dashboard/customer/favorites", label: "Favorites", icon: Star },
  { href: "/services", label: "Book a Service", icon: CalendarPlus },
];

/** "/dashboard" only matches itself (the role home redirects there); other items also match their sub-pages. */
function isActivePath(pathname: string, href: string) {
  if (pathname === href) return true;
  if (href === "/dashboard") return pathname === "/dashboard/owner" || pathname === "/dashboard/customer";
  return pathname.startsWith(`${href}/`);
}

/**
 * Role navigation. One list of links: a horizontal, scrollable tab bar on phones and a sticky sidebar from md up.
 */
export function Sidebar() {
  const pathname = usePathname();
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return null;
  }

  const isOwner = user?.role === "Owner";
  const isCustomer = user?.role === "Customer";

  // Show customer nav for customer, owner nav for owner
  const navItems = isCustomer ? customerNavItems : (isOwner ? ownerNavItems : []);

  return (
    <aside className="border-b border-border/70 bg-background md:sticky md:top-16 md:h-[calc(100vh-4rem)] md:w-64 md:shrink-0 md:border-b-0 md:border-r">
      <nav
        aria-label="Main"
        className="flex gap-1 overflow-x-auto px-4 py-2 [scrollbar-width:none] md:flex-col md:overflow-visible md:p-4 [&::-webkit-scrollbar]:hidden"
      >
        {(isOwner || isCustomer) && (
          <p className="hidden px-3 pb-2 pt-1 text-[11px] font-semibold uppercase tracking-[0.08em] text-muted-foreground md:block">
            {isOwner ? "Business" : "My account"}
          </p>
        )}
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = isActivePath(pathname, item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isActive ? "page" : undefined}
              className={cn(
                "flex shrink-0 items-center gap-3 whitespace-nowrap rounded-[10px] px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                isActive
                  ? "bg-accent text-accent-foreground font-semibold"
                  : "text-muted-foreground hover:bg-secondary hover:text-foreground"
              )}
            >
              <Icon className={cn("h-4 w-4", isActive ? "text-primary" : "")} aria-hidden="true" />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
