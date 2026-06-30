import type { components } from "@/lib/api-types";

export type RunListItem = components["schemas"]["RunListItemResponse"];
export type RunDetail = components["schemas"]["RunDetailResponse"];
export type RunSummary = components["schemas"]["RunSummary"];
export type ArchiveSummary = components["schemas"]["ArchiveSummary"];
export type AuditEvent = components["schemas"]["AuditEventResponse"];
export type ReconciliationReport = components["schemas"]["ReconciliationReport"];
export type Archive = components["schemas"]["ArchiveResponse"];
export type ArchiveType = components["schemas"]["ArchiveType"];
export type StartRunRequest = components["schemas"]["StartRunRequest"];
export type RunCreatedResponse = components["schemas"]["RunCreatedResponse"];
// RunStatus is not in OpenAPI schema (it's a plain string in the response) - define as literal union:
export type RunStatus = "Running" | "Completed" | "Failed" | "Cancelled";
