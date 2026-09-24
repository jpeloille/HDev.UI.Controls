using HDev.UI.Controls;
using Xunit;

namespace HDev.UI.Controls.Tests;

public class HDevMaskEngineTests
{
    /// <summary>Simule une frappe séquentielle à travers TryInsert</summary>
    private static (string Text, bool AllAccepted) Type(string mask, string input,
        string initial = "", int caret = -1)
    {
        var text = initial;
        var pos = caret < 0 ? text.Length : caret;
        foreach (var ch in input)
        {
            if (!HDevMaskEngine.TryInsert(mask, text, pos, ch.ToString(), out text!, out pos))
                return (text, false);
        }
        return (text, true);
    }

    // ── Masque date 00/00/0000 ─────────────────────────────────────

    [Fact]
    public void DigitsOnly_AutoInsertsLiterals()
    {
        var (text, ok) = Type("00/00/0000", "10072026");
        Assert.True(ok);
        Assert.Equal("10/07/2026", text);
    }

    [Fact]
    public void ExplicitLiterals_AreConsumed()
    {
        var (text, ok) = Type("00/00/0000", "10/07/2026");
        Assert.True(ok);
        Assert.Equal("10/07/2026", text);
    }

    [Fact]
    public void Letter_IsRejected()
    {
        var (text, ok) = Type("00/00/0000", "1a");
        Assert.False(ok);
        Assert.Equal("1", text);
    }

    [Fact]
    public void Overflow_IsRejected()
    {
        var (text, ok) = Type("00/00/0000", "100720261");
        Assert.False(ok);
        Assert.Equal("10/07/2026", text);
    }

    [Fact]
    public void Overwrite_AtStart()
    {
        var (text, ok) = Type("00/00/0000", "2", initial: "10/07/2026", caret: 0);
        Assert.True(ok);
        Assert.Equal("20/07/2026", text);
    }

    [Fact]
    public void Overwrite_AfterLiteral()
    {
        var (text, ok) = Type("00/00/0000", "9", initial: "10/07/2026", caret: 3);
        Assert.True(ok);
        Assert.Equal("10/97/2026", text);
    }

    [Fact]
    public void LiteralTyped_OnLiteralPosition_IsConsumed()
    {
        var (text, ok) = Type("00/00/0000", "/", initial: "10", caret: 2);
        Assert.True(ok);
        Assert.Equal("10/", text);
    }

    [Fact]
    public void LiteralTyped_OnPlaceholderPosition_IsRejected()
    {
        var (_, ok) = Type("00/00/0000", "/");
        Assert.False(ok);
    }

    // ── IsComplete ─────────────────────────────────────────────────

    [Theory]
    [InlineData("10/07/2026", true)]
    [InlineData("10/07", false)]
    [InlineData("", false)]
    public void IsComplete_Date(string text, bool expected)
        => Assert.Equal(expected, HDevMaskEngine.IsComplete("00/00/0000", text));

    [Fact]
    public void IsComplete_OptionalDigit_IsNotRequired()
        // 9 = chiffre optionnel : un texte sans lui reste complet
        => Assert.True(HDevMaskEngine.IsComplete("009", "12"));

    // ── Masque heure ───────────────────────────────────────────────

    [Theory]
    [InlineData("1430", "14:30")]
    [InlineData("14:30", "14:30")]
    public void TimeMask(string input, string expected)
    {
        var (text, ok) = Type("00:00", input);
        Assert.True(ok);
        Assert.Equal(expected, text);
    }

    // ── Masque mixte lettres/chiffres ──────────────────────────────

    [Fact]
    public void MixedMask_LetterThenDigits()
    {
        var (text, ok) = Type("L-000", "A123");
        Assert.True(ok);
        Assert.Equal("A-123", text);
    }

    [Fact]
    public void MixedMask_DigitInLetterPosition_IsRejected()
    {
        var (text, ok) = Type("L-000", "1");
        Assert.False(ok);
        Assert.Equal("", text);
    }

    [Fact]
    public void AlphanumericPlaceholder_AcceptsBoth()
    {
        Assert.True(Type("AA", "a1").AllAccepted);
        Assert.False(Type("AA", "a-").AllAccepted);
    }

    // ── Classes de caractères ──────────────────────────────────────

    [Theory]
    [InlineData('0', '5', true)]
    [InlineData('0', 'a', false)]
    [InlineData('9', '5', true)]
    [InlineData('L', 'a', true)]
    [InlineData('L', '5', false)]
    [InlineData('A', 'a', true)]
    [InlineData('A', '5', true)]
    [InlineData('A', '-', false)]
    [InlineData('/', '/', true)]
    [InlineData('/', '5', false)]
    public void Matches_CharacterClasses(char maskChar, char input, bool expected)
        => Assert.Equal(expected, HDevMaskEngine.Matches(maskChar, input));

    [Theory]
    [InlineData('0', true)]
    [InlineData('9', true)]
    [InlineData('L', true)]
    [InlineData('A', true)]
    [InlineData('/', false)]
    [InlineData(':', false)]
    public void IsPlaceholder(char maskChar, bool expected)
        => Assert.Equal(expected, HDevMaskEngine.IsPlaceholder(maskChar));

    // ── Insertion multi-caractères (collage) ───────────────────────

    [Fact]
    public void MultiCharInsert_BehavesLikeSequentialTyping()
    {
        var ok = HDevMaskEngine.TryInsert("00/00/0000", "", 0, "10072026", out var text, out var caret);
        Assert.True(ok);
        Assert.Equal("10/07/2026", text);
        Assert.Equal(10, caret);
    }

    [Fact]
    public void MultiCharInsert_WithInvalidChar_IsRejectedAtomically()
        => Assert.False(HDevMaskEngine.TryInsert("00/00/0000", "", 0, "10a7", out _, out _));
}
