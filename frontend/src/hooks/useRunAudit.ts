import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { AuditEvent } from "@/lib/types";

export function useRunAudit(runId: string, enabled = true) {
  return useQuery({
    queryKey: ["runs", runId, "audit"],
    queryFn: () =>
      api.get<AuditEvent[]>(`/runs/${runId}/audit`).then((r) => r.data),
    enabled,
  });
}
