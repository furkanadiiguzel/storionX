import { cn } from "@/lib/utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CheckCircle2, XCircle, SkipForward, Database } from "lucide-react";
import type { RunSummary } from "@/lib/api-types";

interface Props { summary: RunSummary; }

export function RunStatsCards({ summary }: Props) {
  const cards = [
    { title: "Ingested",    value: summary.migrated,   icon: CheckCircle2, color: "text-green-600 dark:text-green-400" },
    { title: "Skipped",     value: summary.skipped,    icon: SkipForward,  color: "text-yellow-600 dark:text-yellow-400" },
    { title: "Failed",      value: summary.failed,     icon: XCircle,      color: "text-red-600 dark:text-red-400" },
    { title: "Total Items", value: summary.totalItems, icon: Database,     color: "text-blue-600 dark:text-blue-400" },
  ];

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {cards.map(({ title, value, icon: Icon, color }) => (
        <Card key={title}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
            <Icon className={cn("size-4", color)} />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold tabular-nums">{value.toLocaleString()}</p>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
