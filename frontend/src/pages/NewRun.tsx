import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription, SheetFooter,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { useCreateRun } from "@/hooks/useRuns";
import { toast } from "sonner";

interface Props { open: boolean; onOpenChange: (open: boolean) => void; }

export function NewRunSheet({ open, onOpenChange }: Props) {
  const navigate = useNavigate();
  const { mutate, isPending } = useCreateRun();

  const [dryRun, setDryRun] = useState(false);
  const [legalHold, setLegalHold] = useState<"Retain" | "Migrate">("Retain");
  const [archiveId, setArchiveId] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    mutate(
      { dryRun },
      {
        onSuccess: ({ runId }) => {
          toast.success("Run started", { description: `Run ID: ${runId.slice(0, 8)}…` });
          onOpenChange(false);
          navigate(`/runs/${runId}`);
        },
        onError: () => toast.error("Failed to start run"),
      }
    );
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md overflow-y-auto">
        <SheetHeader>
          <SheetTitle>New Migration Run</SheetTitle>
          <SheetDescription>
            Configure and start an EV → storionX migration run.
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmit} className="space-y-5 py-4">
          {/* Dry Run */}
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="dryRun" className="text-sm font-medium">Dry Run</Label>
              <p className="text-xs text-muted-foreground">Simulate without ingesting</p>
            </div>
            <button
              type="button"
              role="switch"
              id="dryRun"
              aria-checked={dryRun}
              onClick={() => setDryRun(!dryRun)}
              className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${dryRun ? "bg-primary" : "bg-input"}`}
            >
              <span className={`inline-block size-4 rounded-full bg-white shadow transition-transform ${dryRun ? "translate-x-6" : "translate-x-1"}`} />
            </button>
          </div>

          <Separator />

          {/* Legal Hold Policy */}
          <div className="space-y-1.5">
            <Label htmlFor="legalHold">Legal Hold Policy</Label>
            <Select value={legalHold} onValueChange={(v) => setLegalHold(v as "Retain" | "Migrate")}>
              <SelectTrigger id="legalHold" aria-label="Legal hold policy">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Retain">Retain (skip legal-hold items)</SelectItem>
                <SelectItem value="Migrate">Migrate (include legal-hold items)</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <Separator />
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Filters (optional)</p>

          {/* Archive ID Filter */}
          <div className="space-y-1.5">
            <Label htmlFor="archiveId">Archive ID</Label>
            <Input
              id="archiveId"
              value={archiveId}
              onChange={(e) => setArchiveId(e.target.value)}
              placeholder="Leave blank to migrate all archives"
              aria-label="Archive ID filter"
            />
          </div>

          {/* Date Range */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="fromDate">From Date</Label>
              <Input id="fromDate" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} aria-label="From date" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="toDate">To Date</Label>
              <Input id="toDate" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} aria-label="To date" />
            </div>
          </div>

          {dryRun && (
            <div className="rounded-md bg-blue-500/10 border border-blue-500/20 px-3 py-2 flex gap-2 items-center">
              <Badge variant="outline" className="bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30 shrink-0">DRY RUN</Badge>
              <p className="text-xs text-muted-foreground">No data will be written to storionX.</p>
            </div>
          )}

          <SheetFooter className="pt-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={isPending} aria-label="Start migration run">
              {isPending ? "Starting…" : "Start Run"}
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
