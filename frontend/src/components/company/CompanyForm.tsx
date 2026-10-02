"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { SuccessNotice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { CompanyInput, useUpdateCompany } from "@/hooks/queries/useCompany";

/** Company details form. It takes its initial values once: key it by the company it edits. */
export function CompanyForm({ initial }: { initial: CompanyInput }) {
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [timeZone, setTimeZone] = useState(initial.timeZone);
  const update = useUpdateCompany();
  const router = useRouter();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    update.mutate({ name, description, timeZone });
  };

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader title="Edit Company" description="This is how your business appears to customers." />
      <Card>
        <CardHeader>
          <CardTitle>Company Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-5">
            {update.isSuccess && <SuccessNotice>Company updated successfully!</SuccessNotice>}
            <ErrorNotice error={update.error} fallback="Failed to update company" />
            <div className="space-y-2">
              <Label htmlFor="name">Company Name</Label>
              <Input id="name" value={name} onChange={(e) => setName(e.target.value)} required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Input id="description" value={description} onChange={(e) => setDescription(e.target.value)} />
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
              <p className="text-xs text-muted-foreground">
                Use an IANA name like America/Argentina/Buenos_Aires. All slots are shown in this zone.
              </p>
            </div>
            <div className="flex flex-wrap gap-3 border-t border-border/70 pt-5">
              <Button type="submit" disabled={update.isPending}>
                {update.isPending ? "Saving..." : "Save Changes"}
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
