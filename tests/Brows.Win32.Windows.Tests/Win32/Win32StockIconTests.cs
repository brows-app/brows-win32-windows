using Brows.Win32.PlatformInvoke;
using System;
using System.Text.RegularExpressions;

namespace Brows.Win32;

[TestFixture]
public sealed class Win32StockIconTests {
    [Test]
    public void Members_ArePascalCase() {
        foreach (var name in Enum.GetNames(typeof(Win32StockIcon))) {
            Assert.That(Regex.IsMatch(name, "^(?=.*[a-z])[A-Z][A-Za-z0-9]*$"), Is.True,
                $"{name} is not PascalCase.");
        }
    }

    [Test]
    public void Members_MatchNativeStockIconIdsAndValues() {
        var thisNames = Enum.GetNames(typeof(Win32StockIcon));
        var otherNames = Enum.GetNames(typeof(SHSTOCKICONID));

        Assert.That(thisNames, Has.Length.EqualTo(otherNames.Length));

        foreach (var otherName in otherNames) {
            var thisName = Array.Find(
                thisNames,
                name => name.ToUpperInvariant() == otherName.Replace("_", "").ToUpperInvariant());

            Assert.That(thisName, Is.Not.Null, $"No Win32StockIcon member matches {otherName}.");

            var otherValue = Convert.ToUInt32(Enum.Parse(typeof(SHSTOCKICONID), otherName));
            var thisValue = Convert.ToUInt32(Enum.Parse(typeof(Win32StockIcon), thisName));

            Assert.That(thisValue, Is.EqualTo(otherValue), $"Value mismatch for {otherName}.");
        }
    }

    [Test]
    public void MaxIcons_MatchesWindowsSdkValue() {
        Assert.That((long)Win32StockIcon.MaxIcons, Is.EqualTo(181));
    }
}
