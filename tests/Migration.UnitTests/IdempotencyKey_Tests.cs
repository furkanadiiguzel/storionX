using FluentAssertions;
using EvStorionX.Domain.ValueObjects;

namespace EvStorionX.UnitTests;

public sealed class IdempotencyKey_Tests
{
    [Fact]
    public void Create_ValidParts_ProducesExpectedString()
    {
        var key = IdempotencyKey.Create("vault-1", "arch-1", "item-1");

        key.ToString().Should().Be("ev:vault-1:arch-1:item-1");
    }

    [Fact]
    public void Parse_ValidString_RoundTripPreservesValue()
    {
        const string raw = "ev:vault-1:arch-1:item-1";

        var key = IdempotencyKey.Parse(raw);

        key.ToString().Should().Be(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-ev-format")]
    [InlineData("ev:only-two")]
    public void Parse_InvalidString_ThrowsException(string invalid)
    {
        var act = () => IdempotencyKey.Parse(invalid);

        act.Should().Throw<Exception>("the format must be ev:<vaultStore>:<archiveId>:<itemId>");
    }

    [Fact]
    public void Equality_SameParts_KeysAreEqual()
    {
        var a = IdempotencyKey.Create("v", "a", "i");
        var b = IdempotencyKey.Create("v", "a", "i");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentItemId_KeysAreNotEqual()
    {
        var a = IdempotencyKey.Create("v", "a", "i1");
        var b = IdempotencyKey.Create("v", "a", "i2");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsFullKey()
    {
        var key = IdempotencyKey.Create("v", "a", "i");
        string str = key;   // implicit operator

        str.Should().Be("ev:v:a:i");
    }

    [Fact]
    public void Create_Then_Parse_AreEqual()
    {
        var original = IdempotencyKey.Create("myVault", "archX", "item42");
        var parsed = IdempotencyKey.Parse(original.ToString());

        parsed.Should().Be(original);
    }
}
