using booking_service.Domain.ValueObjects;

namespace booking_service.unit.test;

public class GuestTokenTests
{
    [Fact]
    public void Generate_ReturnsDistinctWellFormedTokensWithStableHashes()
    {
        var first = GuestToken.Generate();
        var second = GuestToken.Generate();

        Assert.NotEqual(first.Value, second.Value);
        Assert.True(GuestToken.IsWellFormed(first.Value));
        Assert.Equal(64, first.Hash.Length);
        Assert.NotEqual(first.Value, first.Hash);
        Assert.Equal(first.Hash, GuestToken.From(first.Value).Hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void IsWellFormed_InvalidValue_ReturnsFalseAndFromThrows(string? value)
    {
        Assert.False(GuestToken.IsWellFormed(value));
        Assert.Throws<ArgumentException>(() => GuestToken.From(value!));
    }

    [Fact]
    public void ToString_DoesNotExposeTheSecret()
    {
        var token = GuestToken.Generate();

        Assert.DoesNotContain(token.Value, token.ToString());
    }
}
