using FluentAssertions;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Dto;
using EvStorionX.Application.Transform;
using EvStorionX.Domain.Entities;
using EvStorionX.UnitTests.Builders;

namespace EvStorionX.UnitTests;

public sealed class Transformer_Tests
{
    private static EvToStorionXTransformer MakeSut(Guid? runId = null) =>
        new(Options.Create(new TransformerOptions
        {
            ToolVersion = "1.2.3",
            RunId       = runId ?? Guid.NewGuid(),
        }), TimeProvider.System);

    private static RehydratedItem MakeContent(string itemId, params (string id, byte[] bytes)[] parts)
    {
        var rParts = parts.Select(p =>
        {
            var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(p.bytes));
            return new RehydratedPart(p.id, hash, p.bytes.Length, p.bytes.AsMemory());
        }).ToList();
        return new RehydratedItem(itemId, rParts, DateTime.UtcNow);
    }

    // ── Archive class slug ────────────────────────────────────────────────────

    [Fact]
    public void Transform_MailboxArchive_ArchiveClassIsUserMailbox()
    {
        var archive = ArchiveBuilder.Default().Build();       // Mailbox
        var item    = ItemBuilder.Default().Build();
        var content = MakeContent(item.ItemId, ("p1", [0x01]));

        var msg = MakeSut().Transform(item, archive, content, "user_mailbox:alice@contoso.com");

        msg.ArchiveClass.Should().Be("user_mailbox");
        msg.TargetArchive.Should().Be("user_mailbox:alice@contoso.com");
    }

    [Fact]
    public void Transform_JournalArchive_ArchiveClassIsComplianceJournalAndImmutableMetadata()
    {
        var archive = ArchiveBuilder.Default().AsJournal().WithId("j1").Build();
        var item    = ItemBuilder.Default().WithArchiveId("j1").Build();
        var content = MakeContent(item.ItemId, ("p1", [0xFF]));

        var msg = MakeSut().Transform(item, archive, content, "compliance_journal:j1");

        msg.ArchiveClass.Should().Be("compliance_journal");
        msg.Metadata.Should().ContainKey("immutable")
           .WhoseValue.Should().Be("true");
    }

    [Fact]
    public void Transform_FsaArchive_ArchiveClassIsFileArchiveAndFolderPathPreserved()
    {
        const string folder  = "Projects/2024/Q1";
        var archive = ArchiveBuilder.Default().AsFsa().WithId("fsa1").Build();
        var item    = ItemBuilder.Default().WithArchiveId("fsa1").WithFolderPath(folder).Build();
        var content = MakeContent(item.ItemId, ("p1", [0x42]));

        var msg = MakeSut().Transform(item, archive, content, "file_archive:fsa1");

        msg.ArchiveClass.Should().Be("file_archive");
        msg.Metadata.Should().ContainKey("folderPath").WhoseValue.Should().Be(folder);
        msg.Metadata.Should().ContainKey("sourceFolderPath").WhoseValue.Should().Be(folder);
    }

    // ── Retention ─────────────────────────────────────────────────────────────

    [Fact]
    public void Transform_RetentionCategory7Y_ExpiresUtcIsSentDatePlusSeven()
    {
        var sentDate = new DateTime(2020, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var archive  = ArchiveBuilder.Default().Build();
        var item     = ItemBuilder.Default()
                           .WithSentDate(sentDate)
                           .WithRetentionCategory("Legal-7Y")
                           .Build();
        var content  = MakeContent(item.ItemId, ("p1", [0xAA]));

        var msg = MakeSut().Transform(item, archive, content, "user_mailbox:alice@contoso.com");

        msg.Retention.ExpiresUtc.Should().Be(sentDate.AddYears(7));
        msg.Retention.Category.Should().Be("Legal-7Y");
    }

    [Fact]
    public void Transform_RetentionCategoryWithoutYears_ExpiresUtcIsNull()
    {
        var archive = ArchiveBuilder.Default().Build();
        var item    = ItemBuilder.Default().WithRetentionCategory("NoExpiry").Build();
        var content = MakeContent(item.ItemId, ("p1", [0x00]));

        var msg = MakeSut().Transform(item, archive, content, "user_mailbox:alice@contoso.com");

        msg.Retention.ExpiresUtc.Should().BeNull();
    }

    // ── Cc / Bcc ──────────────────────────────────────────────────────────────

    [Fact]
    public void Transform_JournalItemWithCcAndBcc_MetadataContainsBothFields()
    {
        var archive = ArchiveBuilder.Default().AsJournal().WithId("j2").Build();
        var item    = ItemBuilder.Default()
                          .WithArchiveId("j2")
                          .WithCc("cc1@x.com", "cc2@x.com")
                          .WithBcc("bcc1@x.com")
                          .Build();
        var content = MakeContent(item.ItemId, ("p1", [0x01]));

        var msg = MakeSut().Transform(item, archive, content, "compliance_journal:j2");

        msg.Metadata.Should().ContainKey("cc").WhoseValue.Should().Contain("cc1@x.com");
        msg.Metadata.Should().ContainKey("bcc").WhoseValue.Should().Contain("bcc1@x.com");
    }

    // ── IdempotencyKey ────────────────────────────────────────────────────────

    [Fact]
    public void Transform_AnyItem_IdempotencyKeyMatchesExpectedFormat()
    {
        var archive = ArchiveBuilder.Default().WithId("arch1").WithVaultStore("vaultA").Build();
        var item    = ItemBuilder.Default().WithId("item99").WithArchiveId("arch1").Build();
        var content = MakeContent(item.ItemId, ("p1", [0x01]));

        var msg = MakeSut().Transform(item, archive, content, "user_mailbox:alice@contoso.com");

        ((string)msg.IdempotencyKey).Should().Be("ev:vaultA:arch1:item99");
    }

    // ── LegalHold flag propagation ────────────────────────────────────────────

    [Fact]
    public void Transform_LegalHoldArchive_MessageLegalHoldIsTrue()
    {
        var archive = ArchiveBuilder.Default().WithLegalHold().Build();
        var item    = ItemBuilder.Default().Build();
        var content = MakeContent(item.ItemId, ("p1", [0x01]));

        var msg = MakeSut().Transform(item, archive, content, "user_mailbox:alice@contoso.com");

        msg.LegalHold.Should().BeTrue();
    }
}
