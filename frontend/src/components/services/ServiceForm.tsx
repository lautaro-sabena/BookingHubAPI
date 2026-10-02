"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { PageHeader } from "@/components/ui/page-header";
import { ServiceInput } from "@/hooks/queries/useServices";

interface ServiceFormProps {
  title: string;
  initial: ServiceInput;
  submitLabel: string;
  savingLabel: string;
  saving: boolean;
  error: unknown;
  errorFallback: string;
  onSubmit: (input: ServiceInput) => void;
}

/** The fields shared by "add service" and "edit service". The parent passes the initial values once (key it). */
export function ServiceForm({
  title,
  initial,
  submitLabel,
  savingLabel,
  saving,
  error,
  errorFallback,
  onSubmit,
}: ServiceFormProps) {
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [durationMinutes, setDurationMinutes] = useState(initial.durationMinutes);
  const [price, setPrice] = useState(initial.price);
  const router = useRouter();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ name, description, durationMinutes, price });
  };

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader title={title} description="Customers see the name, description, price and duration when they book." />
      <Card>
        <CardHeader>
          <CardTitle>Service Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-5">
            <ErrorNotice error={error} fallback={errorFallback} />
            <div className="space-y-2">
              <Label htmlFor="name">Service Name</Label>
              <Input id="name" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Haircut and beard trim" required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Input id="description" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="What is included and who it is for" />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="duration">Duration (minutes)</Label>
                <Input
                  id="duration"
                  type="number"
                  min="1"
                  value={durationMinutes}
                  onChange={(e) => setDurationMinutes(parseInt(e.target.value))}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="price">Price ($)</Label>
                <Input
                  id="price"
                  type="number"
                  min="0"
                  step="0.01"
                  value={price}
                  onChange={(e) => setPrice(parseFloat(e.target.value))}
                  required
                />
              </div>
            </div>
            <div className="flex flex-wrap gap-3 border-t border-border/70 pt-5">
              <Button type="submit" disabled={saving}>
                {saving ? savingLabel : submitLabel}
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
