import { Link } from "react-router-dom";
import { formatDistanceToNow } from "date-fns";
import { Archive, CheckCircle2, Play, TrendingUp } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { RunStatusBadge } from "@/components/runs/RunStatusBadge";
import { IngestionChart } from "@/components/charts/IngestionChart";
import { CardSkeleton, TableSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { useRuns } from "@/hooks/useRuns";
import { useRunAudit } from "@/hooks/useRunAudit";
import type { RunListItem } from "@/lib/api-types";
import type { ElementType } from "react";

function KpiCard({ title, value, sub, icon: Icon }: {
  title: string; value: string | number; sub?: string; icon: ElementType;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="size-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <p className="text-2xl font-bold tabular-nums">{value}</p>
        {sub && <p className="text-xs text-muted-foreground mt-1">{sub}</p>}
      </CardContent>
    </Card>
  );
}

function RecentRunsTable({ runs }: { runs: RunListItem[] }) {
  const recent = runs.slice(0, 5);
  if (recent.length === 0) return <EmptyState icon={Play} title="No runs yet" description="Start your first migration run." />;
  return (
    <div className="rounded-md border overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="px-4 py-2 text-left font-medium text-muted-foreground">Run ID</th>
            <th className="px-4 py-2 text-left font-medium text-muted-foreground">Started</th>
            <th className="px-4 py-2 text-left font-medium text-muted-foreground">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {recent.map((run) => (
            <tr key={run.runId} className="hover:bg-muted/30 transition-colors">
              <td className="px-4 py-3">
                <Link to={`/runs/${run.runId}`} className="font-mono text-xs text-primary hover:underline">
                  {run.runId.slice(0, 8)}…
                </Link>
              </td>
              <td className="px-4 py-3 text-muted-foreground">
                {formatDistanceToNow(new Date(run.startedAt), { addSuffix: true })}
              </td>
              <td className="px-4 py-3">
                <RunStatusBadge status={run.status} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// Chart uses the most-recent run's audit events
function DashboardChart({ runs }: { runs: RunListItem[] }) {
  const lastCompleted = runs.find((r) => r.status === "Completed");
  const { data: events = [] } = useRunAudit(lastCompleted?.runId ?? "", !!lastCompleted);
  return <IngestionChart events={events} />;
}

export default function Dashboard() {
  const { data: runs = [], isLoading } = useRuns();

  const completed   = runs.filter((r) => r.status === "Completed").length;
  const total       = runs.length;
  const running     = runs.filter((r) => r.status === "Running").length;
  const successRate = total > 0 ? Math.round((completed / total) * 100) : 0;

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">EV → storionX migration overview</p>
      </div>

      {/* KPI Cards */}
      {isLoading ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => <CardSkeleton key={i} />)}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <KpiCard title="Total Runs"     value={total}             icon={Play}         sub={`${running} active`} />
          <KpiCard title="Completed"      value={completed}         icon={CheckCircle2} />
          <KpiCard title="Success Rate"   value={`${successRate}%`} icon={TrendingUp}   />
          <KpiCard title="Total Archives" value="—"                 icon={Archive}      sub="Not yet tallied" />
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base">Recent Runs</CardTitle>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/runs">View all</Link>
            </Button>
          </CardHeader>
          <CardContent>
            {isLoading ? <TableSkeleton rows={3} /> : <RecentRunsTable runs={runs} />}
          </CardContent>
        </Card>

        {!isLoading && <DashboardChart runs={runs} />}
      </div>
    </div>
  );
}
