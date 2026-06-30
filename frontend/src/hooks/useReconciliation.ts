import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { ReconciliationReport } from "@/lib/types";

export function useReconciliation(runId: string, enabled = true) {
  return useQuery({
    queryKey: ["runs", runId, "reconciliation"],
    queryFn: () =>
      api
        .get<ReconciliationReport>(`/runs/${runId}/reconciliation`)
        .then((r) => r.data),
    enabled,
  });
}
