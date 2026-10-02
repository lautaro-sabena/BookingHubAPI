"use client";

import Link from "next/link";
import { LogOut } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Logo } from "@/components/layout/Logo";
import { ThemeToggle } from "@/components/layout/ThemeToggle";

export function Navbar() {
  const { user, logout, logoutError, isAuthenticated } = useAuth();
  const initial = user?.email?.charAt(0).toUpperCase() ?? "?";

  return (
    <nav className="sticky top-0 z-40 border-b border-border/70 bg-background/80 backdrop-blur-md supports-[backdrop-filter]:bg-background/70">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-6">
        <Logo />
        <div className="flex min-w-0 items-center gap-2 sm:gap-3">
          {isAuthenticated ? (
            <>
              <div className="hidden min-w-0 items-center gap-2.5 rounded-full border border-border/70 bg-card py-1 pl-1 pr-3 sm:flex">
                <span
                  aria-hidden="true"
                  className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-accent text-xs font-bold text-accent-foreground"
                >
                  {initial}
                </span>
                <span className="truncate text-sm text-muted-foreground">{user?.email}</span>
                {user?.role && (
                  <span className="hidden rounded-full bg-secondary px-2 py-0.5 text-[11px] font-semibold text-secondary-foreground md:inline">
                    {user.role}
                  </span>
                )}
              </div>
              {logoutError && (
                <span role="alert" className="text-sm text-destructive">
                  Could not sign out. Try again.
                </span>
              )}
              <Button variant="outline" size="sm" onClick={() => void logout()}>
                <LogOut className="h-4 w-4" aria-hidden="true" />
                {logoutError ? "Retry logout" : "Logout"}
              </Button>
            </>
          ) : (
            <>
              <Link href="/login">
                <Button variant="ghost" size="sm">
                  Login
                </Button>
              </Link>
              <Link href="/register">
                <Button size="sm">Register</Button>
              </Link>
            </>
          )}
          <ThemeToggle />
        </div>
      </div>
    </nav>
  );
}
