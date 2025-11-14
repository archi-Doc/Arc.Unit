// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Arc.InputConsole;

internal static class InputConsoleHelper
{
    public static int GetValidUtf8Length(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length;
        var i = length - 1;
        if (length == 0)
        {// Empty buffer
            return 0;
        }

        if (bytes[i] <= 0x7F)
        {// ASCII byte
            return length;
        }

        if ((bytes[i] & 0b1100_0000) == 0b1000_0000)
        {
            i--;
            if (i >= 0 && (bytes[i] & 0b1100_0000) == 0b1000_0000)
            {
                i--;
                if (i >= 0 && (bytes[i] & 0b1100_0000) == 0b1000_0000)
                {
                    i--;
                }
            }
        }

        if (i < 0)
        {
            return 0;
        }

        int seqLen;
        if ((bytes[i] & 0b1110_0000) == 0b1100_0000)
        {
            seqLen = 2;
        }
        else if ((bytes[i] & 0b1111_0000) == 0b1110_0000)
        {
            seqLen = 3;
        }
        else if ((bytes[i] & 0b1111_1000) == 0b1111_0000)
        {
            seqLen = 4;
        }
        else
        {
            return length;
        }

        if (length < i + seqLen)
        {
            return i;
        }
        else
        {
            return length;
        }
    }

    public static byte GetCharWidth(int codePoint)
    {
        // Control characters
        if (codePoint < 0x20 || (codePoint >= 0x7F && codePoint < 0xA0))
        {
            return 0;
        }

        // Extend characters (combining marks)
        var category = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        if (category == UnicodeCategory.NonSpacingMark ||
            category == UnicodeCategory.EnclosingMark ||
            category == UnicodeCategory.SpacingCombiningMark)
        {
            return 0;
        }

        // Kanji and other CJK characters
        if ((codePoint >= 0x4E00 && codePoint <= 0x9FFF) ||
            (codePoint >= 0x3400 && codePoint <= 0x4DBF) ||
            (codePoint >= 0x20000 && codePoint <= 0x2A6DF) ||
            (codePoint >= 0x2A700 && codePoint <= 0x2B73F) ||
            (codePoint >= 0x2B740 && codePoint <= 0x2B81F) ||
            (codePoint >= 0x2B820 && codePoint <= 0x2CEAF) ||
            (codePoint >= 0x2CEB0 && codePoint <= 0x2EBEF))
        {
            return 2;
        }

        // Fullwidth characters
        if ((codePoint >= 0xFF01 && codePoint <= 0xFF60) ||
            (codePoint >= 0xFFE0 && codePoint <= 0xFFE6))
        {
            return 2;
        }

        // Hiragana and Katakana
        if ((codePoint >= 0x3040 && codePoint <= 0x309F) ||
            (codePoint >= 0x30A0 && codePoint <= 0x30FF))
        {
            return 2;
        }

        // Hangul
        if ((codePoint >= 0xAC00 && codePoint <= 0xD7AF) ||
            (codePoint >= 0x1100 && codePoint <= 0x11FF) ||
            (codePoint >= 0x3130 && codePoint <= 0x318F) ||
            (codePoint >= 0xA960 && codePoint <= 0xA97F) ||
            (codePoint >= 0xD7B0 && codePoint <= 0xD7FF))
        {
            return 2;
        }

        // Other East Asian wide characters
        if ((codePoint >= 0x2E80 && codePoint <= 0x2EFF) ||
            (codePoint >= 0x2F00 && codePoint <= 0x2FDF) ||
            (codePoint >= 0x3000 && codePoint <= 0x303F) ||
            (codePoint >= 0x3200 && codePoint <= 0x32FF) ||
            (codePoint >= 0x3300 && codePoint <= 0x33FF) ||
            (codePoint >= 0xFE30 && codePoint <= 0xFE4F) ||
            (codePoint >= 0xF900 && codePoint <= 0xFAFF) ||
            (codePoint >= 0x2FF0 && codePoint <= 0x2FFF))
        {
            return 2;
        }

        // Emoji and other symbols
        if ((codePoint >= 0x1F300 && codePoint <= 0x1F9FF) ||
            (codePoint >= 0x1F600 && codePoint <= 0x1F64F) ||
            (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) ||
            (codePoint >= 0x2600 && codePoint <= 0x26FF) ||
            (codePoint >= 0x2700 && codePoint <= 0x27BF))
        {
            return 2;
        }

        // Default to single width
        return 1;
    }

    public static int GetWidth(ReadOnlySpan<char> text, int tabSize = 8)
    {//
        var width = 0;
        var position = 0;
        while (position < text.Length)
        {
            if (System.Text.Rune.DecodeFromUtf16(text.Slice(position), out var r, out int consumed) != OperationStatus.Done)
            {
                break;
            }

            position += consumed;
            var category = CharUnicodeInfo.GetUnicodeCategory((char)r.Value);
            if (IsControlOrFormat(category) || IsCombining(category))
            {// Zero width
                continue;
            }
            else if (r.Value == '\t')
            {// Tab
                if (tabSize <= 0)
                {
                    width += 1;
                    continue;
                }

                var toNext = tabSize - (width % tabSize);
                width += toNext;
                continue;
            }
            else if (IsWideEastAsian(r) || IsEmojiLikeWide(r))
            {
                width += 2;
                continue;
            }

            width += 1;
        }

        return width;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsControlOrFormat(UnicodeCategory category)
        => category is UnicodeCategory.Control
        or UnicodeCategory.Format
        or UnicodeCategory.OtherNotAssigned
        or UnicodeCategory.Surrogate;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCombining(UnicodeCategory category)
        => category is UnicodeCategory.NonSpacingMark
        or UnicodeCategory.EnclosingMark
        or UnicodeCategory.SpacingCombiningMark;

    private static bool IsWideEastAsian(System.Text.Rune r)
    {
        var u = r.Value;
        return
            (u is >= 0x4E00 and <= 0x9FFF) ||
            (u is >= 0x3400 and <= 0x4DBF) ||
            (u is >= 0x3040 and <= 0x30FF) || (u is >= 0x31F0 and <= 0x31FF) ||
            (u is >= 0xAC00 and <= 0xD7AF) || (u is >= 0x1100 and <= 0x11FF) ||
            (u is >= 0x3200 and <= 0x32FF) || (u is >= 0x3300 and <= 0x33FF) ||
            (u is >= 0xF900 and <= 0xFAFF) ||
            (u is >= 0xFF01 and <= 0xFF60) || (u is >= 0xFFE0 and <= 0xFFE6);
    }

    private static bool IsEmojiLikeWide(System.Text.Rune r)
    {
        var u = r.Value;
        return
            (u is >= 0x2600 and <= 0x27BF) ||
            (u is >= 0x1F300 and <= 0x1F5FF) ||
            (u is >= 0x1F600 and <= 0x1F64F) ||
            (u is >= 0x1F680 and <= 0x1F6FF) ||
            (u is >= 0x1F900 and <= 0x1F9FF) ||
            (u is >= 0x1FA70 and <= 0x1FAFF) ||
            (u is >= 0x1F1E6 and <= 0x1F1FF);
    }
}
