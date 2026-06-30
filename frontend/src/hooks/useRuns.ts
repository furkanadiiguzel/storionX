import { useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { api } from "@/lib/api";
import type { RunDetail, RunListItem, StartRunRequest } from "@/lib/types";

const RUNS_KEY = ["runs"] as const;

export function useRuns() {
  return useQuery({
    queryKey: RUNS_KEY,
    queryFn: () => api.get<RunListItem[]>("/runs").then((r) => r.data),
    refetchInterval: (query) => {
      const hasRunning = query.state.data?.some((r) => r.status === "Running");
      return hasRunning ? 3000 : false;
    },
  });
}

export function useRun(runId: string) {
  const prevStatusRef = useRef<string | null>(null);

  const query = useQuery({
    queryKey: [...RUNS_KEY, runId],
    queryFn: () => api.get<RunDetail>(`/runs/${runId}`).then((r) => r.data),
    refetchInterval: (q) => (q.state.data?.status === "Running" ? 2000 : false),
  });

  useEffect(() => {
    const status = query.data?.status;
    if (!status) return;
    const prev = prevStatusRef.current;
    if (prev === "Running" && status === "Completed") {
      toast.success("Migration completed", { description: `Run ${runId.slice(0, 8)}… finished.` });
    } else if (prev === "Running" && status === "Failed") {
      toast.error("Migration failed", { description: `Run ${runId.slice(0, 8)}… encountered an error.` });
    }
    prevStatusRef.current = status;
  }, [query.data?.status, runId]);

  return query;
}

export function useCreateRun() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: StartRunRequest) =>
      api.post<{ runId: string }>("/runs", req).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: RUNS_KEY }),
  });
}

export function useResumeRun(runId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () =>
      api.post<{ runId: string }>(`/runs/${runId}/resume`).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: RUNS_KEY }),
  });
}
