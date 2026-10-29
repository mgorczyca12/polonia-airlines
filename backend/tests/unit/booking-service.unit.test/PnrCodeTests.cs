using booking_service.Domain.ValueObjects;

namespace booking_service.unit.test;

public class PnrCodeTests
{
    [Theory]
    [InlineData("ABC123", "ABC123")]
    [InlineData("abc123", "ABC123")]
    [InlineData("abcdef", "ABCDEF")]
    [InlineData("012345", "012345")]
    public void From_ValidCode_StoresUppercaseValue(string input, string expected)
    {
        var code = PnrCode.From(input);

        Assert.Equal(expected, code.Value);
        Assert.Equal(expected, code.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("      ")]
    [InlineData("ABC12")]
    [InlineData("ABC1234")]
    [InlineData(" ABC123 ")]
    [InlineData("ABC12 ")]
    [InlineData("ABC-12")]
    [InlineData("ABC_12")]
    [InlineData("ABC12\n")]
    [InlineData("ABC12\u00e9")]
    [InlineData("ABC12\u0131")]
    [InlineData("ABC12\u0661")]
    public void From_InvalidCode_ThrowsArgumentException(string? input)
    {
        Assert.ThrowsAny<ArgumentException>(() => PnrCode.From(input!));
    }

    [Fact]
    public void From_EquivalentCodes_HaveValueEquality()
    {
        var code = PnrCode.From("abc123");
        var equivalent = PnrCode.From("ABC123");

        Assert.Equal(code, equivalent);
        Assert.Equal(code.GetHashCode(), equivalent.GetHashCode());
        Assert.NotEqual(code, PnrCode.From("ABC124"));
    }

    [Fact]
    public void Generate_ReturnsSixUppercaseAsciiLettersOrDigits()
    {
        for (var index = 0; index < 1000; index++)
        {
            var code = PnrCode.Generate();

            Assert.Matches(@"\A[A-Z0-9]{6}\z", code.Value);
            Assert.Equal(code, PnrCode.From(code.Value));
        }
    }
}
