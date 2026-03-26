"use client";

import Link from "next/link";
import { useTheme } from "@/hooks/useTheme";
import { Button } from "@/components/ui/button";
import { Sun, Moon } from "lucide-react";

export default function Home() {
  const { theme, toggleTheme } = useTheme();

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-24">
      <div className="absolute top-4 right-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleTheme}
          aria-label="Toggle theme"
        >
          {theme === "dark" ? (
            <Sun className="h-5 w-5" />
          ) : (
            <Moon className="h-5 w-5" />
          )}
        </Button>
      </div>
      <h1 className="text-4xl font-bold mb-8">Welcome to BookingHub</h1>
      <div className="flex gap-4">
        <Link 
          href="/login"
          className="px-6 py-3 bg-primary text-primary-foreground rounded-md hover:opacity-90"
        >
          Login
        </Link>
        <Link 
          href="/register"
          className="px-6 py-3 bg-secondary text-secondary-foreground rounded-md hover:opacity-90"
        >
          Register
        </Link>
      </div>
    </main>
  );
}
