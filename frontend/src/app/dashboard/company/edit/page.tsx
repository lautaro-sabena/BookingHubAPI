"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import api from "@/lib/api";
import { Company } from "@/types";

export default function EditCompanyPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [company, setCompany] = useState<Company | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [timeZone, setTimeZone] = useState("");
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
      setName(response.data.name);
      setDescription(response.data.description || "");
      setTimeZone(response.data.timeZone);
    } catch (err) {
      console.error("Failed to fetch company:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError("");
    setSuccess(false);

    try {
      const response = await api.put<Company>("/companies/me", {
        name,
        description,
        timeZone,
      });
      setCompany(response.data);
      setSuccess(true);
    } catch (err: any) {
      setError(err.response?.data?.error || "Failed to update company");
    } finally {
      setSaving(false);
    }
  };

  if (authLoading || loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Edit Company</h1>
      <Card>
        <CardHeader>
          <CardTitle>Company Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {success && (
              <div className="p-3 text-sm text-green-600 bg-green-50 rounded-md">
                Company updated successfully!
              </div>
            )}
            {error && (
              <div className="p-3 text-sm text-red-500 bg-red-50 rounded-md">
                {error}
              </div>
            )}
            <div className="space-y-2">
              <Label htmlFor="name">Company Name</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Input
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="timeZone">Time Zone</Label>
              <Input
                id="timeZone"
                value={timeZone}
                onChange={(e) => setTimeZone(e.target.value)}
                placeholder="America/New_York"
                required
              />
            </div>
            <div className="flex gap-4">
              <Button type="submit" disabled={saving}>
                {saving ? "Saving..." : "Save Changes"}
              </Button>
              <Button type="button" variant="outline" onClick={() => router.back()}>
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
