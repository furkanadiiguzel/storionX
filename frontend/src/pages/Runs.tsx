import { useState, useMemo } from "react";
import { Link } from "react-router-dom";
import {
  useReactTable, getCoreRowModel, getSortedRowModel, getFilteredRowModel,
  flexRender, type ColumnDef, type SortingState,
} from "@tanstack/react-table";
import { formatDistanceToNow } from "date-fns";
import { ArrowUpDown, ExternalLink, Play, RotateCcw, Search } from "lucide-react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { RunStatusBadge } from "@/components/runs/RunStatusBadge";
import { TableSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { useRuns, useResumeRun } from "@/hooks/useRuns";
import { toast } from "sonner";
import type { RunListItem, RunStatus } from "@/lib/types";

function RunActions({ run }: { run: RunListItem }) {
  const resumeMutation = useResumeRun(run.runId);

  if (run.status === "Cancelled" || run.status === "Failed") {
    return (
      <Button
        size="sm"
        variant="ghost"
        disabled={resumeMutation.isPending}
        aria-label={`Resume run ${run.runId}`}
        onClick={() => {
          resumeMutation.mutate(undefined, {
            onSuccess: () => toast.success("Run resumed"),
            onError: () => toast.error("Failed to resume run"),
          });
        }}
      >
        <RotateCcw className="size-3.5 mr-1" aria-hidden="true" /> Resume
      </Button>
    );
  }
  return null;
}

function RunsTable({ data }: { data: RunListItem[] }) {
  const [sorting, setSorting] = useState<SortingState>([{ id: "startedAt", desc: true }]);
  const [globalFilter, setGlobalFilter] = useState("");

  const columns = useMemo<ColumnDef<RunListItem>[]>(() => [
    {
      accessorKey: "runId",
      header: "Run ID",
      cell: ({ row }) => (
        <Link to={`/runs/${row.original.runId}`} className="font-mono text-xs text-primary hover:underline flex items-center gap-1">
          {row.original.runId.slice(0, 8)}…
          <ExternalLink className="size-3 shrink-0" aria-hidden="true" />
        </Link>
      ),
      enableSorting: false,
    },
    {
      accessorKey: "startedAt",
      header: ({ column }) => (
        <Button variant="ghost" size="sm" className="-ml-2" onClick={() => column.toggleSorting()}>
          Started <ArrowUpDown className="ml-1 size-3" aria-hidden="true" />
        </Button>
      ),
      cell: ({ getValue }) => (
        <span className="text-sm text-muted-foreground">
          {formatDistanceToNow(new Date(getValue() as string), { addSuffix: true })}
        </span>
      ),
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => <RunStatusBadge status={row.original.status as RunStatus} />,
    },
    {
      id: "duration",
      header: "Duration",
      cell: () => <span className="text-sm text-muted-foreground">—</span>,
      enableSorting: false,
    },
    {
      id: "actions",
      header: "",
      cell: ({ row }) => <RunActions run={row.original} />,
      enableSorting: false,
    },
  ], []);

  const table = useReactTable({
    data,
    columns,
    state: { sorting, globalFilter },
    onSortingChange: setSorting,
    onGlobalFilterChange: setGlobalFilter,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
  });

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" aria-hidden="true" />
          <Input
            placeholder="Filter runs…"
            value={globalFilter}
            onChange={(e) => setGlobalFilter(e.target.value)}
            className="pl-8 sm:max-w-64"
            aria-label="Filter runs"
          />
        </div>
        <p className="ml-auto text-sm text-muted-foreground">{table.getFilteredRowModel().rows.length} runs</p>
      </div>
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((hg) => (
              <TableRow key={hg.id}>
                {hg.headers.map((h) => (
                  <TableHead key={h.id}>
                    {h.isPlaceholder ? null : flexRender(h.column.columnDef.header, h.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows.length > 0 ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                  No runs match the filter.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

export default function Runs() {
  const { data: runs = [], isLoading, isError, refetch } = useRuns();

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold">Migration Runs</h1>
        <p className="text-sm text-muted-foreground">All EV → storionX migration runs</p>
      </div>

      {isLoading && <TableSkeleton rows={5} />}
      {isError && (
        <div className="rounded-md border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
          Failed to load runs.{" "}
          <button onClick={() => refetch()} className="underline">Retry</button>
        </div>
      )}
      {!isLoading && !isError && runs.length === 0 && (
        <EmptyState icon={Play} title="No runs yet" description="Start a new migration run from the toolbar." />
      )}
      {!isLoading && !isError && runs.length > 0 && <RunsTable data={runs} />}
    </div>
  );
}
