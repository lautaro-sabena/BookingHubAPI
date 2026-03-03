"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Company } from "@/types";

export default function CompanyPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [company, setCompany] = useState<Company | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && user?.role !== "Owner") {
      router.push("/dashboard");
      return;
    }

    if (user?.role === "Owner") {
      fetchCompany();
    }
  }, [user, authLoading, router]);

  const fetchCompany = async () => {
    try {
      const response = await api.get<Company>("/companies/me");
      setCompany(response.data);
    } catch (err) {
      console.error("Failed to fetch company:", err);
    } finally {
      setLoading(false);
    }
  };

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  if (!company) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Company</h1>
        <Card>
          <CardContent className="py-8 text-center">
            <p className="mb-4 text-muted-foreground">You don&apos;t have a company yet.</p>
            <Link href="/dashboard/company/edit">
              <Button>Create Company</Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Company</h1>
        <Link href="/dashboard/company/edit">
          <Button>Edit</Button>
        </Link>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{company.name}</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="space-y-2">
            <div className="flex justify-between">
              <dt className="font-medium text-muted-foreground">Description:</dt>
              <dd>{company.description || "N/A"}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="font-medium text-muted-foreground">Time Zone:</dt>
              <dd>{company.timeZone}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="font-medium text-muted-foreground">Status:</dt>
              <dd>{company.isActive ? "Active" : "Inactive"}</dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
