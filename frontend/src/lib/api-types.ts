// ─── Run ────────────────────────────────────────────────────────────────────
export type RunStatus = "Running" | "Completed" | "Failed" | "Cancelled";

export interface RunListItem {
  runId: string;
  status: RunStatus;
  startedAt: string; // ISO
}

export interface ArchiveSummary {
  migrated: number;
  alreadyPresent: number;
  skipped: number;
  failed: number;
}

export interface RunSummary {
  runId: string;
  startedAtUtc: string;
  finishedAtUtc: string | null;
  totalArchives: number;
  totalItems: number;
  migrated: number;
  alreadyPresent: number;
  orphaned: number;
  skipped: number;
  failed: number;
  byArchive: Record<string, ArchiveSummary>;
}

export interface RunDetail {
  runId: string;
  status: RunStatus;
  startedAt: string;
  summary: RunSummary | null;
}

// ─── Audit ──────────────────────────────────────────────────────────────────
export interface AuditEvent {
  id: string;
  timestampUtc: string;
  eventType: string;
  itemId: string | null;
  payload: string;
  runId: string;
}

// ─── Reconciliation ─────────────────────────────────────────────────────────
export interface ReconciliationReport {
  runId: string;
  generatedAtUtc: string;
  missingInTarget: string[];
  mismatchedInTarget: string[];
  unexpectedInTarget: string[];
  isClean: boolean;
}

// ─── Archive ────────────────────────────────────────────────────────────────
export type ArchiveType = "Mailbox" | "Journal" | "Fsa";

export interface Archive {
  archiveId: string;
  type: ArchiveType;
  ownerUpn: string | null;
  legalHold: boolean;
  vaultStore: string;
}

// ─── Requests ───────────────────────────────────────────────────────────────
export interface StartRunRequest {
  runId?: string;
  dryRun?: boolean;
}
