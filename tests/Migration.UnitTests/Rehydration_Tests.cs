using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Common;
using EvStorionX.Application.Rehydration;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Exceptions;
using EvStorionX.UnitTests.Builders;

namespace EvStorionX.UnitTests;

public sealed class Rehydration_Tests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Sha256Hex(byte[] data) =>
        Convert.ToHexStringLower(SHA256.HashData(data));

    private static SisPart PartFor(string partId, byte[] data) => new()
    {
        PartId = partId,
        Sha256 = Sha256Hex(data),
        SizeBytes = data.Length,
        DataRef = partId,
    };

    private static Rehydrator MakeSut(IPartReader reader, ICachePolicy<string, byte[]>? cache = null) =>
        new(reader, cache ?? new LruCache<string, byte[]>(100), NullLogger<Rehydrator>.Instance);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RehydrateAsync_SamePartIdThreeTimes_ReadPartCalledOnce()
    {
        // Arrange
        var bytes = new byte[] { 0x01, 0x02, 0x03 };
        var part = PartFor("part-1", bytes);

        var mockReader = new Mock<IPartReader>();
        mockReader.Setup(r => r.ReadPartAsync("part-1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(bytes.AsMemory());
        mockReader.Setup(r => r.GetMetadataAsync("part-1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(part);

        var item = ItemBuilder.Default().WithPartIds("part-1", "part-1", "part-1").Build();
        var sut = MakeSut(mockReader.Object);

        // Act
        var result = await sut.RehydrateAsync(item, CancellationToken.None);

        // Assert — cache avoids redundant reads
        mockReader.Verify(
            r => r.ReadPartAsync("part-1", It.IsAny<CancellationToken>()),
            Times.Once,
            "second and third occurrences of the same partId must be served from cache");

        result.Parts.Should().HaveCount(3, "one RehydratedPart per ContentPartId entry");
        result.Parts.Should().AllSatisfy(p => p.PartId.Should().Be("part-1"));
    }

    [Fact]
    public async Task RehydrateAsync_HashMismatch_ThrowsPermanentMigrationException()
    {
        // Arrange
        var bytes = new byte[] { 0xAA, 0xBB };
        var wrongHashPart = new SisPart
        {
            PartId = "part-x",
            Sha256 = new string('0', 64),   // 64 zero hex chars — guaranteed mismatch
            SizeBytes = bytes.Length,
            DataRef = "part-x",
        };

        var mockReader = new Mock<IPartReader>();
        mockReader.Setup(r => r.ReadPartAsync("part-x", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(bytes.AsMemory());
        mockReader.Setup(r => r.GetMetadataAsync("part-x", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(wrongHashPart);

        var item = ItemBuilder.Default().WithPartIds("part-x").Build();
        var sut = MakeSut(mockReader.Object);

        // Act
        var act = () => sut.RehydrateAsync(item, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<PermanentMigrationException>()
            .Where(ex => ex.ErrorCode == "HASH_MISMATCH");
    }

    [Fact]
    public async Task RehydrateAsync_MultiPartItem_BytesAssembledInOrder()
    {
        // Arrange
        var (bytesA, bytesB, bytesC) = (new byte[] { 0xAA }, new byte[] { 0xBB }, new byte[] { 0xCC });

        var mockReader = new Mock<IPartReader>();
        mockReader.Setup(r => r.ReadPartAsync("a", It.IsAny<CancellationToken>())).ReturnsAsync(bytesA.AsMemory());
        mockReader.Setup(r => r.ReadPartAsync("b", It.IsAny<CancellationToken>())).ReturnsAsync(bytesB.AsMemory());
        mockReader.Setup(r => r.ReadPartAsync("c", It.IsAny<CancellationToken>())).ReturnsAsync(bytesC.AsMemory());
        mockReader.Setup(r => r.GetMetadataAsync("a", It.IsAny<CancellationToken>())).ReturnsAsync(PartFor("a", bytesA));
        mockReader.Setup(r => r.GetMetadataAsync("b", It.IsAny<CancellationToken>())).ReturnsAsync(PartFor("b", bytesB));
        mockReader.Setup(r => r.GetMetadataAsync("c", It.IsAny<CancellationToken>())).ReturnsAsync(PartFor("c", bytesC));

        var item = ItemBuilder.Default().WithPartIds("a", "b", "c").Build();
        var sut = MakeSut(mockReader.Object);

        // Act
        var result = await sut.RehydrateAsync(item, CancellationToken.None);

        // Assert — order is preserved, no byte corruption
        result.Parts.Should().HaveCount(3);
        result.Parts[0].PartId.Should().Be("a");
        result.Parts[0].Bytes.ToArray().Should().Equal(bytesA);
        result.Parts[1].PartId.Should().Be("b");
        result.Parts[1].Bytes.ToArray().Should().Equal(bytesB);
        result.Parts[2].PartId.Should().Be("c");
        result.Parts[2].Bytes.ToArray().Should().Equal(bytesC);
    }
}
