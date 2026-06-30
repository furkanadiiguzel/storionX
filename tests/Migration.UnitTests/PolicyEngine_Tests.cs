using FluentAssertions;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Dto;
using EvStorionX.Application.Mapping;
using EvStorionX.Domain.Enums;
using EvStorionX.UnitTests.Builders;

namespace EvStorionX.UnitTests;

public sealed class PolicyEngine_Tests
{
    private static PolicyEngine MakeSut(LegalHoldPolicy policy = LegalHoldPolicy.Retain) =>
        new(Options.Create(new PolicyOptions { LegalHoldPolicy = policy }));

    // ── Legal hold ────────────────────────────────────────────────────────────

    [Fact]
    public void Decide_LegalHoldRetainPolicy_ReturnsLegalHoldRetain()
    {
        var archive = ArchiveBuilder.Default().WithLegalHold().Build();
        var item    = ItemBuilder.Default().Build();

        var result = MakeSut(LegalHoldPolicy.Retain).Decide(archive, item, new MigrationFilters());

        result.Outcome.Should().Be(PolicyOutcome.LegalHoldRetain);
    }

    [Fact]
    public void Decide_LegalHoldMigratePolicy_ReturnsLegalHoldMigrate()
    {
        var archive = ArchiveBuilder.Default().WithLegalHold().Build();
        var item    = ItemBuilder.Default().Build();

        var result = MakeSut(LegalHoldPolicy.Migrate).Decide(archive, item, new MigrationFilters());

        result.Outcome.Should().Be(PolicyOutcome.LegalHoldMigrate);
    }

    // ── Orphan ────────────────────────────────────────────────────────────────

    [Fact]
    public void Decide_MailboxWithNullOwner_ReturnsOrphaned()
    {
        var archive = ArchiveBuilder.Default().AsOrphan().Build();  // Mailbox, OwnerUpn=null
        var item    = ItemBuilder.Default().Build();

        var result = MakeSut().Decide(archive, item, new MigrationFilters());

        result.Outcome.Should().Be(PolicyOutcome.Orphaned);
    }

    // ── Filters ───────────────────────────────────────────────────────────────

    [Fact]
    public void Decide_ArchiveIdFilterMismatch_ReturnsSkippedByFilter()
    {
        var archive = ArchiveBuilder.Default().WithId("archive-A").Build();
        var item    = ItemBuilder.Default().WithArchiveId("archive-A").Build();
        var filters = new MigrationFilters { ArchiveId = "archive-B" };

        var result = MakeSut().Decide(archive, item, filters);

        result.Outcome.Should().Be(PolicyOutcome.SkippedByFilter);
    }

    [Fact]
    public void Decide_ItemBeforeFromUtc_ReturnsSkippedByFilter()
    {
        var archive = ArchiveBuilder.Default().Build();
        var item    = ItemBuilder.Default().WithSentDate(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Build();
        var filters = new MigrationFilters { FromUtc = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

        var result = MakeSut().Decide(archive, item, filters);

        result.Outcome.Should().Be(PolicyOutcome.SkippedByFilter);
    }

    [Fact]
    public void Decide_ItemAtOrAfterToUtc_ReturnsSkippedByFilter()
    {
        var anchor  = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var archive = ArchiveBuilder.Default().Build();
        var item    = ItemBuilder.Default().WithSentDate(anchor).Build();
        var filters = new MigrationFilters { ToUtc = anchor };          // exclusive upper bound

        var result = MakeSut().Decide(archive, item, filters);

        result.Outcome.Should().Be(PolicyOutcome.SkippedByFilter);
    }

    [Fact]
    public void Decide_FolderPathMismatch_ReturnsSkippedByFilter()
    {
        var archive = ArchiveBuilder.Default().Build();
        var item    = ItemBuilder.Default().WithFolderPath("Sent Items").Build();
        var filters = new MigrationFilters { FolderPath = "Inbox" };

        var result = MakeSut().Decide(archive, item, filters);

        result.Outcome.Should().Be(PolicyOutcome.SkippedByFilter);
    }

    // ── Proceed ───────────────────────────────────────────────────────────────

    [Fact]
    public void Decide_NormalMailboxNoFilters_ReturnsProceed()
    {
        var archive = ArchiveBuilder.Default().Build();
        var item    = ItemBuilder.Default().Build();

        var result = MakeSut().Decide(archive, item, new MigrationFilters());

        result.Outcome.Should().Be(PolicyOutcome.Proceed);
    }

    [Fact]
    public void Decide_JournalArchiveNoFilters_ReturnsProceed()
    {
        var archive = ArchiveBuilder.Default().AsJournal().Build();
        var item    = ItemBuilder.Default().Build();

        var result = MakeSut().Decide(archive, item, new MigrationFilters());

        result.Outcome.Should().Be(PolicyOutcome.Proceed,
            "journal archives never orphan (no UPN required) and are not on legal hold by default");
    }
}
