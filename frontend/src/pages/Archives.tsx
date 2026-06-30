import { Server, Shield, Mail, FolderOpen } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { TableSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { useArchives } from "@/hooks/useArchives";
import { cn } from "@/lib/utils";
import type { Archive, ArchiveType } from "@/lib/api-types";
import type { ElementType } from "react";

const TYPE_ICONS: Record<ArchiveType, ElementType> = {
  Mailbox: Mail,
  Journal: FolderOpen,
  Fsa:     Server,
};

function ArchiveCard({ archive }: { archive: Archive }) {
  const Icon = TYPE_ICONS[archive.type] ?? Server;
  return (
    <Card>
      <CardHeader className="flex flex-row items-center gap-3 pb-2">
        <Icon className="size-5 text-muted-foreground shrink-0" aria-hidden="true" />
        <div className="min-w-0">
          <CardTitle className="text-sm font-mono truncate">{archive.archiveId}</CardTitle>
          <p className="text-xs text-muted-foreground">{archive.ownerUpn ?? "No owner"}</p>
        </div>
        <div className="ml-auto flex gap-1.5 shrink-0">
          <Badge variant="secondary" className="text-xs">{archive.type}</Badge>
          {archive.legalHold && (
            <Badge variant="outline" className={cn("text-xs text-yellow-700 dark:text-yellow-400 border-yellow-500/30 bg-yellow-500/10")}>
              <Shield className="size-3 mr-1" aria-hidden="true" /> Legal Hold
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <p className="text-xs text-muted-foreground">Vault: <span className="font-mono">{archive.vaultStore}</span></p>
      </CardContent>
    </Card>
  );
}

export default function Archives() {
  const { data: archives = [], isLoading, isError } = useArchives();

  const byType = {
    Mailbox: archives.filter((a) => a.type === "Mailbox"),
    Journal: archives.filter((a) => a.type === "Journal"),
    Fsa:     archives.filter((a) => a.type === "Fsa"),
  };

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold">Archives</h1>
        <p className="text-sm text-muted-foreground">EV vault archives discovered by the migration service</p>
      </div>

      {isLoading && <TableSkeleton rows={4} />}

      {isError && (
        <p className="text-sm text-destructive">Failed to load archives from backend.</p>
      )}

      {!isLoading && !isError && archives.length === 0 && (
        <EmptyState
          icon={Server}
          title="No archives discovered"
          description="The backend returns an empty placeholder until the EV discovery service is connected."
        />
      )}

      {!isLoading && !isError && Object.entries(byType).filter(([, items]) => items.length > 0).map(([type, items]) => (
        <section key={type}>
          <h2 className="text-sm font-semibold text-muted-foreground mb-3 uppercase tracking-wide">{type} ({items.length})</h2>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {items.map((a) => <ArchiveCard key={a.archiveId} archive={a} />)}
          </div>
        </section>
      ))}
    </div>
  );
}
