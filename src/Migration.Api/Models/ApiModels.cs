using EvStorionX.Application.Dto;
using EvStorionX.Domain.Enums;

namespace EvStorionX.Api.Models;

/// <summary>Summary of a single run as returned by GET /runs.</summary>
public sealed record RunListItemResponse(
    Guid RunId,
    string Status,
    DateTimeOffset StartedAt
);

/// <summary>Full detail of a run as returned by GET /runs/{id}.</summary>
public sealed record RunDetailResponse(
    Guid RunId,
    string Status,
    DateTimeOffset StartedAt,
    RunSummary? Summary
);

/// <summary>Body of the 202 Accepted responses from POST /runs and POST /runs/{id}/resume.</summary>
public sealed record RunCreatedResponse(Guid RunId);

/// <summary>Single audit log entry as returned by GET /runs/{id}/audit.</summary>
public sealed record AuditEventResponse(
    Guid Id,
    DateTime TimestampUtc,
    string EventType,
    string? ItemId,
    string Payload,
    Guid RunId
);

/// <summary>EV archive as returned by GET /archives.</summary>
public sealed record ArchiveResponse(
    string ArchiveId,
    ArchiveType Type,
    string? OwnerUpn,
    bool LegalHold,
    string VaultStore
);
