import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { RunStatus } from "@/lib/types";

const CONFIG: Record<RunStatus, { label: string; className: string }> = {
  Running:   { label: "Running",   className: "bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30" },
  Completed: { label: "Completed", className: "bg-green-500/15 text-green-700 dark:text-green-400 border-green-500/30" },
  Failed:    { label: "Failed",    className: "bg-red-500/15 text-red-700 dark:text-red-400 border-red-500/30" },
  Cancelled: { label: "Cancelled", className: "bg-yellow-500/15 text-yellow-700 dark:text-yellow-400 border-yellow-500/30" },
};

export function RunStatusBadge({ status }: { status: RunStatus }) {
  const { label, className } = CONFIG[status] ?? CONFIG.Failed;
  return (
    <Badge variant="outline" className={cn("font-medium", className)}>
      {label}
    </Badge>
  );
}
