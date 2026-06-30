import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { RunDetail, RunListItem, StartRunRequest } from "@/lib/api-types";

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
  return useQuery({
    queryKey: [...RUNS_KEY, runId],
    queryFn: () => api.get<RunDetail>(`/runs/${runId}`).then((r) => r.data),
    refetchInterval: (query) =>
      query.state.data?.status === "Running" ? 2000 : false,
  });
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
