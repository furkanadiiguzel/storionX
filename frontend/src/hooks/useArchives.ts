import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { Archive } from "@/lib/types";

export function useArchives() {
  return useQuery({
    queryKey: ["archives"],
    queryFn: () => api.get<Archive[]>("/archives").then((r) => r.data),
  });
}
