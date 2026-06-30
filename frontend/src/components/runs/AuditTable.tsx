import { useState, useMemo } from "react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { format } from "date-fns";
import { cn } from "@/lib/utils";
import type { AuditEvent } from "@/lib/api-types";

const EVENT_COLORS: Record<string, string> = {
  RunStarted:         "bg-blue-500/10 text-blue-700 dark:text-blue-300",
  RunCompleted:       "bg-green-500/10 text-green-700 dark:text-green-300",
  ItemMigrated:       "bg-green-500/10 text-green-700 dark:text-green-300",
  ItemAlreadyPresent: "bg-slate-500/10 text-slate-600 dark:text-slate-300",
  ItemSkipped:        "bg-yellow-500/10 text-yellow-700 dark:text-yellow-300",
  ItemPermanentFailed:"bg-red-500/10 text-red-700 dark:text-red-300",
  ItemTransientFailed:"bg-orange-500/10 text-orange-700 dark:text-orange-300",
  ArchiveOrphaned:    "bg-purple-500/10 text-purple-700 dark:text-purple-300",
};

interface Props { events: AuditEvent[]; }

export function AuditTable({ events }: Props) {
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("all");

  const eventTypes = useMemo(
    () => ["all", ...Array.from(new Set(events.map((e) => e.eventType))).sort()],
    [events]
  );

  const filtered = useMemo(() => {
    return events.filter((e) => {
      const matchType = typeFilter === "all" || e.eventType === typeFilter;
      const matchSearch =
        !search ||
        e.itemId?.toLowerCase().includes(search.toLowerCase()) ||
        e.eventType.toLowerCase().includes(search.toLowerCase());
      return matchType && matchSearch;
    });
  }, [events, search, typeFilter]);

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-2 sm:flex-row">
        <Input
          placeholder="Search item ID or event type…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="sm:max-w-64"
          aria-label="Search audit events"
        />
        <Select value={typeFilter} onValueChange={setTypeFilter}>
          <SelectTrigger className="sm:max-w-48" aria-label="Filter by event type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {eventTypes.map((t) => (
              <SelectItem key={t} value={t}>
                {t === "all" ? "All event types" : t}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <p className="self-center text-sm text-muted-foreground">
          {filtered.length} / {events.length} events
        </p>
      </div>

      <div className="rounded-md border overflow-auto max-h-[500px]">
        <Table>
          <TableHeader className="sticky top-0 bg-background z-10">
            <TableRow>
              <TableHead className="w-44">Timestamp</TableHead>
              <TableHead className="w-52">Event Type</TableHead>
              <TableHead>Item ID</TableHead>
              <TableHead>Payload</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((ev) => (
              <TableRow key={ev.id}>
                <TableCell className="text-xs text-muted-foreground font-mono">
                  {format(new Date(ev.timestampUtc), "HH:mm:ss.SSS")}
                </TableCell>
                <TableCell>
                  <Badge
                    variant="outline"
                    className={cn("text-xs font-mono", EVENT_COLORS[ev.eventType])}
                  >
                    {ev.eventType}
                  </Badge>
                </TableCell>
                <TableCell className="text-xs font-mono text-muted-foreground">
                  {ev.itemId ?? "—"}
                </TableCell>
                <TableCell className="text-xs font-mono text-muted-foreground max-w-xs truncate">
                  {ev.payload}
                </TableCell>
              </TableRow>
            ))}
            {filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                  No events match the current filter
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
