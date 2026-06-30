import { useParams, useNavigate } from "react-router-dom";
import { format } from "date-fns";
import { ArrowLeft, Download, RotateCcw } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { RunStatusBadge } from "@/components/runs/RunStatusBadge";
import { RunStatsCards } from "@/components/runs/RunStatsCards";
import { AuditTable } from "@/components/runs/AuditTable";
import { DetailSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { useRun, useResumeRun } from "@/hooks/useRuns";
import { useRunAudit } from "@/hooks/useRunAudit";
import { useReconciliation } from "@/hooks/useReconciliation";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import type { AuditEvent, RunStatus } from "@/lib/types";

const ERROR_EVENTS = new Set(["ItemPermanentFailed", "ItemTransientFailed", "ItemUnexpectedError"]);

function ReconciliationTab({ runId }: { runId: string }) {
  const { data, isLoading, isError } = useReconciliation(runId);
  if (isLoading) return <p className="text-sm text-muted-foreground p-4">Loading…</p>;
  if (isError || !data) return <p className="text-sm text-destructive p-4">Failed to load reconciliation.</p>;
  return (
    <div className="space-y-4 p-4">
      <div className={cn("rounded-md border p-4", data.isClean ? "border-green-500/30 bg-green-500/5" : "border-red-500/30 bg-red-500/5")}>
        <p className={cn("font-medium", data.isClean ? "text-green-700 dark:text-green-400" : "text-red-700 dark:text-red-400")}>
          {data.isClean ? "✓ Clean — source and target match" : "✗ Discrepancies detected"}
        </p>
        <p className="text-xs text-muted-foreground mt-1">Generated {format(new Date(data.generatedAtUtc), "PPp")}</p>
      </div>
      {!data.isClean && (
        <div className="space-y-3">
          {data.missingInTarget.length > 0 && (
            <div>
              <p className="text-sm font-medium text-muted-foreground mb-2">Missing in target ({data.missingInTarget.length})</p>
              <ul className="text-sm space-y-1 font-mono">{data.missingInTarget.map((m, i) => <li key={i} className="text-red-700 dark:text-red-400">{m}</li>)}</ul>
            </div>
          )}
          {data.unexpectedInTarget.length > 0 && (
            <div>
              <p className="text-sm font-medium text-muted-foreground mb-2">Unexpected in target ({data.unexpectedInTarget.length})</p>
              <ul className="text-sm space-y-1 font-mono">{data.unexpectedInTarget.map((m, i) => <li key={i} className="text-yellow-700 dark:text-yellow-400">{m}</li>)}</ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function ErrorsTab({ events }: { events: AuditEvent[] }) {
  const errors = events.filter((e) => ERROR_EVENTS.has(e.eventType));
  if (errors.length === 0) return <EmptyState title="No errors" description="All items processed successfully." className="p-8" />;
  return <AuditTable events={errors} />;
}

function ExportButton({ runId, format: fmt }: { runId: string; format: "json" | "csv" }) {
  const handleExport = async () => {
    const url = `${import.meta.env.VITE_API_URL ?? "http://localhost:8080"}/runs/${runId}/audit${fmt === "csv" ? "?format=csv" : ""}`;
    const a = document.createElement("a");
    a.href = url;
    a.download = `audit-${runId}.${fmt}`;
    a.click();
  };
  return (
    <Button variant="outline" size="sm" onClick={handleExport} aria-label={`Export audit as ${fmt.toUpperCase()}`}>
      <Download className="size-3.5 mr-1" aria-hidden="true" />
      Export {fmt.toUpperCase()}
    </Button>
  );
}

export default function RunDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const runId = id!;

  const { data, isLoading } = useRun(runId);
  const { data: auditEvents = [], isLoading: auditLoading } = useRunAudit(runId, !!data);
  const resumeMutation = useResumeRun(runId);

  if (isLoading) return <DetailSkeleton />;
  if (!data) return <p className="p-8 text-muted-foreground">Run not found.</p>;

  const canResume = data.status === "Cancelled" || data.status === "Failed";

  return (
    <div className="p-6 space-y-5 max-w-5xl">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate(-1)} aria-label="Go back">
          <ArrowLeft className="size-4" />
        </Button>
        <div className="flex-1 min-w-0">
          <h1 className="text-lg font-bold font-mono truncate">{runId}</h1>
          <p className="text-xs text-muted-foreground">Started {format(new Date(data.startedAt), "PPp")}</p>
        </div>
        <RunStatusBadge status={data.status as RunStatus} />
      </div>

      {/* Actions */}
      <div className="flex gap-2 flex-wrap">
        {canResume && (
          <Button
            size="sm"
            disabled={resumeMutation.isPending}
            onClick={() => resumeMutation.mutate(undefined, {
              onSuccess: () => toast.success("Run resumed"),
              onError: () => toast.error("Failed to resume run"),
            })}
            aria-label="Resume this run"
          >
            <RotateCcw className="size-3.5 mr-1" aria-hidden="true" /> Resume
          </Button>
        )}
        <ExportButton runId={runId} format="json" />
        <ExportButton runId={runId} format="csv" />
      </div>

      <Separator />

      {/* Stats cards — only when summary is available */}
      {data.summary && <RunStatsCards summary={data.summary} />}

      {/* Tabs */}
      <Tabs defaultValue="summary">
        <TabsList>
          <TabsTrigger value="summary">Summary</TabsTrigger>
          <TabsTrigger value="audit">Audit ({auditEvents.length})</TabsTrigger>
          <TabsTrigger value="reconciliation">Reconciliation</TabsTrigger>
          <TabsTrigger value="errors">
            Errors ({auditEvents.filter((e) => ERROR_EVENTS.has(e.eventType)).length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="summary" className="mt-4">
          {data.summary ? (
            <div className="grid gap-3 sm:grid-cols-2">
              {Object.entries({
                "Total Archives":  data.summary.totalArchives,
                "Total Items":     data.summary.totalItems,
                "Migrated":        data.summary.migrated,
                "Already Present": data.summary.alreadyPresent,
                "Orphaned":        data.summary.orphaned,
                "Skipped":         data.summary.skipped,
                "Failed":          data.summary.failed,
              }).map(([label, value]) => (
                <Card key={label} className="py-3">
                  <CardContent className="px-4 flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">{label}</span>
                    <span className="font-bold tabular-nums">{Number(value).toLocaleString()}</span>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <EmptyState title="Run in progress" description="Summary will appear when the run completes." />
          )}
        </TabsContent>

        <TabsContent value="audit" className="mt-4">
          {auditLoading ? <p className="text-sm text-muted-foreground">Loading audit…</p> : <AuditTable events={auditEvents} />}
        </TabsContent>

        <TabsContent value="reconciliation" className="mt-4">
          <ReconciliationTab runId={runId} />
        </TabsContent>

        <TabsContent value="errors" className="mt-4">
          <ErrorsTab events={auditEvents} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
