using CgCar.Core.Models;

namespace CgCar.Tests;

public class LicensePlateTests
{
    [Theory]
    [InlineData("IKA1234", "IKA1234")]
    [InlineData("ika-1234", "IKA1234")]
    [InlineData("  IKA 1234 ", "IKA1234")]
    [InlineData("ΙΚΑ-1234", "IKA1234")]     // Greek capitals
    [InlineData("ικα 1234", "IKA1234")]     // Greek lowercase
    [InlineData("ΙΚά-1234", "IKA1234")]     // accented Greek letter
    [InlineData("ΙkΑ1234", "IKA1234")]      // mixed alphabets
    [InlineData("ΒΕΖΗΙΚΜΝΟΡΤΥΧ", "BEZHIKMNOPTYX")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData(" - ", "")]
    public void Normalize_ProducesCanonicalForm(string? input, string expected)
    {
        Assert.Equal(expected, LicensePlate.Normalize(input));
    }

    [Fact]
    public void Normalize_KeepsGreekLettersThatHaveNoLatinLookAlike()
    {
        Assert.Equal("ΓΔ12", LicensePlate.Normalize("γδ 12"));
    }

    [Theory]
    [InlineData("IKA1234", "IKA-1234")]
    [InlineData("MBA1103", "MBA-1103")]
    [InlineData("1234", "1234")]        // no letter prefix
    [InlineData("IKA", "IKA")]          // no digits
    [InlineData("AB12CD", "AB12CD")]    // not letters-then-digits
    [InlineData("", "")]
    public void Format_InsertsDashBetweenLettersAndDigits(string input, string expected)
    {
        Assert.Equal(expected, LicensePlate.Format(input));
    }
}
