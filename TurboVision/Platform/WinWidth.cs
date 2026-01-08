namespace TurboVision.Platform;

/// <summary>
/// Character width detection for determining if characters are single or double-width.
/// This is important for CJK (Chinese, Japanese, Korean) characters and emoji.
/// Matches upstream WinWidth in winwidth.h
/// </summary>
internal static class WinWidth
{
    private static bool _isLegacyConsole;

    /// <summary>
    /// Resets the character width detection based on console type.
    /// Matches upstream WinWidth::reset()
    /// </summary>
    /// <param name="isLegacyConsole">True if using legacy console (Windows 7/8), false for modern (Windows 10+)</param>
    public static void Reset(bool isLegacyConsole)
    {
        _isLegacyConsole = isLegacyConsole;
    }

    /// <summary>
    /// Gets the display width of a character (1 or 2 columns).
    /// Legacy console: Always returns 1 (Borland's behavior)
    /// Modern console: Uses Unicode width rules
    /// Matches upstream WinWidth::getWidth()
    /// </summary>
    /// <param name="ch">Character to measure</param>
    /// <returns>Character width in columns (1 or 2)</returns>
    public static int GetWidth(char ch)
    {
        if (_isLegacyConsole)
        {
            // Legacy console uses single-width for all characters
            // This matches Borland's Turbo Vision behavior
            return 1;
        }

        // Modern console: detect double-width characters
        return IsDoubleWidth(ch) ? 2 : 1;
    }

    /// <summary>
    /// Checks if a character is double-width.
    /// Based on Unicode East Asian Width property.
    /// </summary>
    private static bool IsDoubleWidth(char ch)
    {
        // Common double-width ranges:
        // - CJK Unified Ideographs: U+4E00-U+9FFF
        // - CJK Compatibility Ideographs: U+F900-U+FAFF
        // - CJK Unified Ideographs Extension A: U+3400-U+4DBF
        // - Hangul Syllables: U+AC00-U+D7AF
        // - Hiragana: U+3040-U+309F
        // - Katakana: U+30A0-U+30FF
        // - Emoji and symbols: U+1F000+ (surrogate pairs)

        if (ch >= 0x1100 && ch <= 0x115F) return true; // Hangul Jamo
        if (ch >= 0x2329 && ch <= 0x232A) return true; // Left/Right-Pointing Angle Brackets
        if (ch >= 0x2E80 && ch <= 0x2EFF) return true; // CJK Radicals Supplement
        if (ch >= 0x2F00 && ch <= 0x2FDF) return true; // Kangxi Radicals
        if (ch >= 0x2FF0 && ch <= 0x2FFF) return true; // Ideographic Description Characters
        if (ch >= 0x3000 && ch <= 0x303E) return true; // CJK Symbols and Punctuation
        if (ch >= 0x3040 && ch <= 0x309F) return true; // Hiragana
        if (ch >= 0x30A0 && ch <= 0x30FF) return true; // Katakana
        if (ch >= 0x3100 && ch <= 0x312F) return true; // Bopomofo
        if (ch >= 0x3130 && ch <= 0x318F) return true; // Hangul Compatibility Jamo
        if (ch >= 0x3190 && ch <= 0x319F) return true; // Kanbun
        if (ch >= 0x31A0 && ch <= 0x31BF) return true; // Bopomofo Extended
        if (ch >= 0x31C0 && ch <= 0x31EF) return true; // CJK Strokes
        if (ch >= 0x31F0 && ch <= 0x31FF) return true; // Katakana Phonetic Extensions
        if (ch >= 0x3200 && ch <= 0x32FF) return true; // Enclosed CJK Letters and Months
        if (ch >= 0x3300 && ch <= 0x33FF) return true; // CJK Compatibility
        if (ch >= 0x3400 && ch <= 0x4DBF) return true; // CJK Extension A
        if (ch >= 0x4E00 && ch <= 0x9FFF) return true; // CJK Unified Ideographs
        if (ch >= 0xA000 && ch <= 0xA48F) return true; // Yi Syllables
        if (ch >= 0xA490 && ch <= 0xA4CF) return true; // Yi Radicals
        if (ch >= 0xAC00 && ch <= 0xD7AF) return true; // Hangul Syllables
        if (ch >= 0xF900 && ch <= 0xFAFF) return true; // CJK Compatibility Ideographs
        if (ch >= 0xFE10 && ch <= 0xFE1F) return true; // Vertical Forms
        if (ch >= 0xFE30 && ch <= 0xFE4F) return true; // CJK Compatibility Forms
        if (ch >= 0xFE50 && ch <= 0xFE6F) return true; // Small Form Variants
        if (ch >= 0xFF00 && ch <= 0xFF60) return true; // Fullwidth Forms
        if (ch >= 0xFFE0 && ch <= 0xFFE6) return true; // Fullwidth Forms (currency)

        // High surrogates indicate potential emoji/symbols (requires surrogate pair check)
        if (char.IsHighSurrogate(ch))
            return true; // Assume double-width for emoji

        return false;
    }

    /// <summary>
    /// Gets the width of a grapheme cluster (may consist of multiple chars).
    /// </summary>
    /// <param name="text">Text to measure</param>
    /// <returns>Total width in columns</returns>
    public static int GetWidth(ReadOnlySpan<char> text)
    {
        int width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                // Surrogate pair (emoji, etc.) - assume double width
                width += _isLegacyConsole ? 1 : 2;
                i++; // Skip low surrogate
            }
            else
            {
                width += GetWidth(text[i]);
            }
        }
        return width;
    }
}
