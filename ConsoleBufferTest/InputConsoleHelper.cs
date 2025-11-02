// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Arc.Unit;

internal static class InputConsoleHelper
{
    public static int GetWidth(ReadOnlySpan<char> text, int tabSize = 4)
    {
        var width = 0;
        var position = 0;
        while (position < text.Length)
        {
            if (Rune.DecodeFromUtf16(text.Slice(position), out var r, out int consumed) != OperationStatus.Done)
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

    private static bool IsWideEastAsian(Rune r)
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

    private static bool IsEmojiLikeWide(Rune r)
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
