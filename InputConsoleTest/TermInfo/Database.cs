// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace Arc.InputConsole;

#pragma warning disable SA1203 // Constants should appear before fields

internal static partial class TermInfo
{
    /// <summary>Provides a terminfo database.</summary>
    internal sealed class Database
    {
        private readonly string _term;
        private readonly byte[] _data;
        private readonly int _nameSectionNumBytes;
        private readonly int _boolSectionNumBytes;
        private readonly int _numberSectionNumInts;
        private readonly int _stringSectionNumOffsets;
        private readonly int _stringTableNumBytes;
        private readonly bool _readAs32Bit;
        private readonly int _sizeOfInt;

        /// <summary>Extended / user-defined entries in the terminfo database.</summary>
        private readonly Dictionary<string, string>? _extendedStrings;

        internal Database(string term, byte[] data)
        {
            this._term = term;
            this._data = data;

            const int MagicLegacyNumber = 0x11A; // magic number octal 0432 for legacy ncurses terminfo
            const int Magic32BitNumber = 0x21E; // magic number octal 01036 for new ncruses terminfo
            short magic = ReadInt16(data, 0);
            this._readAs32Bit =
                magic == MagicLegacyNumber ? false :
                magic == Magic32BitNumber ? true :
                throw new InvalidOperationException(); // magic number was not recognized. Printing the magic number in octal.
            this._sizeOfInt = this._readAs32Bit ? 4 : 2;

            this._nameSectionNumBytes = ReadInt16(data, 2);
            this._boolSectionNumBytes = ReadInt16(data, 4);
            this._numberSectionNumInts = ReadInt16(data, 6);
            this._stringSectionNumOffsets = ReadInt16(data, 8);
            this._stringTableNumBytes = ReadInt16(data, 10);
            if (this._nameSectionNumBytes < 0 ||
                this._boolSectionNumBytes < 0 ||
                this._numberSectionNumInts < 0 ||
                this._stringSectionNumOffsets < 0 ||
                this._stringTableNumBytes < 0)
            {
                throw new InvalidOperationException();
            }

            // In addition to the main section of bools, numbers, and strings, there is also
            // an "extended" section.  This section contains additional entries that don't
            // have well-known indices, and are instead named mappings.  As such, we parse
            // all of this data now rather than on each request, as the mapping is fairly complicated.
            // This function relies on the data stored above, so it's the last thing we run.
            // (Note that the extended section also includes other Booleans and numbers, but we don't
            // have any need for those now, so we don't parse them.)
            int extendedBeginning = RoundUpToEven(this.StringsTableOffset + this._stringTableNumBytes);
            this._extendedStrings = ParseExtendedStrings(data, extendedBeginning, this._readAs32Bit);
        }

        public string Term => this._term;

        internal bool HasExtendedStrings => this._extendedStrings is not null;

        private const int NamesOffset = 12;

        private int BooleansOffset => NamesOffset + this._nameSectionNumBytes;

        private int NumbersOffset => RoundUpToEven(this.BooleansOffset + this._boolSectionNumBytes);

        private int StringOffsetsOffset => this.NumbersOffset + (this._numberSectionNumInts * this._sizeOfInt);

        /// <summary>Gets the offset into data where the string table exists.</summary>
        private int StringsTableOffset => this.StringOffsetsOffset + (this._stringSectionNumOffsets * 2);

        /// <summary>Gets a string from the strings section by the string's well-known index.</summary>
        /// <param name="stringTableIndex">The index of the string to find.</param>
        /// <returns>The string if it's in the database; otherwise, null.</returns>
        public string? GetString(WellKnownStrings stringTableIndex)
        {
            int index = (int)stringTableIndex;
            Debug.Assert(index >= 0);

            if (index >= this._stringSectionNumOffsets)
            {
                // Some terminfo files may not contain enough entries to actually
                // have the requested one.
                return null;
            }

            int tableIndex = ReadInt16(this._data, this.StringOffsetsOffset + (index * 2));
            if (tableIndex == -1)
            {
                // Some terminfo files may have enough entries, but may not actually
                // have it filled in for this particular string.
                return null;
            }

            return ReadString(this._data, this.StringsTableOffset + tableIndex);
        }

        public string? GetExtendedString(string name)
        {
            Debug.Assert(name != null);

            string? value;
            return this._extendedStrings is not null && this._extendedStrings.TryGetValue(name, out value) ? value : null;
        }

        /// <summary>Parses the extended string information from the terminfo data.</summary>
        /// <returns>
        /// A dictionary of the name to value mapping.  As this section of the terminfo isn't as well
        /// defined as the earlier portions, and may not even exist, the parsing is more lenient about
        /// errors, returning an empty collection rather than throwing.
        /// </returns>
        private static Dictionary<string, string>? ParseExtendedStrings(byte[] data, int extendedBeginning, bool readAs32Bit)
        {
            const int ExtendedHeaderSize = 10;
            int sizeOfIntValuesInBytes = readAs32Bit ? 4 : 2;
            if (extendedBeginning + ExtendedHeaderSize >= data.Length)
            {
                // Exit out as there's no extended information.
                return null;
            }

            // Read in extended counts, and exit out if we got any incorrect info
            int extendedBoolCount = ReadInt16(data, extendedBeginning);
            int extendedNumberCount = ReadInt16(data, extendedBeginning + (2 * 1));
            int extendedStringCount = ReadInt16(data, extendedBeginning + (2 * 2));
            int extendedStringNumOffsets = ReadInt16(data, extendedBeginning + (2 * 3));
            int extendedStringTableByteSize = ReadInt16(data, extendedBeginning + (2 * 4));
            if (extendedBoolCount < 0 ||
                extendedNumberCount < 0 ||
                extendedStringCount < 0 ||
                extendedStringNumOffsets < 0 ||
                extendedStringTableByteSize < 0)
            {
                // The extended header contained invalid data.  Bail.
                return null;
            }

            // Skip over the extended bools.  We don't need them now and can add this in later
            // if needed. Also skip over extended numbers, for the same reason.

            // Get the location where the extended string offsets begin.  These point into
            // the extended string table.
            int extendedOffsetsStart =
                extendedBeginning + // go past the normal data
                ExtendedHeaderSize + // and past the extended header
                RoundUpToEven(extendedBoolCount) + // and past all of the extended Booleans
                (extendedNumberCount * sizeOfIntValuesInBytes); // and past all of the extended numbers

            // Get the location where the extended string table begins.  This area contains
            // null-terminated strings.
            int extendedStringTableStart =
                extendedOffsetsStart +
                (extendedStringCount * 2) + // and past all of the string offsets
                ((extendedBoolCount + extendedNumberCount + extendedStringCount) * 2); // and past all of the name offsets

            // Get the location where the extended string table ends.  We shouldn't read past this.
            int extendedStringTableEnd =
                extendedStringTableStart +
                extendedStringTableByteSize;

            if (extendedStringTableEnd > data.Length)
            {
                // We don't have enough data to parse everything.  Bail.
                return null;
            }

            // Now we need to parse all of the extended string values.  These aren't necessarily
            // "in order", meaning the offsets aren't guaranteed to be increasing.  Instead, we parse
            // the offsets in order, pulling out each string it references and storing them into our
            // results list in the order of the offsets.
            var values = new List<string>(extendedStringCount);
            int lastEnd = 0;
            for (int i = 0; i < extendedStringCount; i++)
            {
                int offset = extendedStringTableStart + ReadInt16(data, extendedOffsetsStart + (i * 2));
                if (offset < 0 || offset >= data.Length)
                {
                    // If the offset is invalid, bail.
                    return null;
                }

                // Add the string
                int end = FindNullTerminator(data, offset);
                values.Add(Encoding.ASCII.GetString(data, offset, end - offset));

                // Keep track of where the last string ends.  The name strings will come after that.
                lastEnd = Math.Max(end, lastEnd);
            }

            // Now parse all of the names.
            var names = new List<string>(extendedBoolCount + extendedNumberCount + extendedStringCount);
            for (int pos = lastEnd + 1; pos < extendedStringTableEnd; pos++)
            {
                int end = FindNullTerminator(data, pos);
                names.Add(Encoding.ASCII.GetString(data, pos, end - pos));
                pos = end;
            }

            // The names are in order for the Booleans, then the numbers, and then the strings.
            // Skip over the bools and numbers, and associate the names with the values.
            var extendedStrings = new Dictionary<string, string>(extendedStringCount);
            for (int iName = extendedBoolCount + extendedNumberCount, iValue = 0;
                 iName < names.Count && iValue < values.Count;
                 iName++, iValue++)
            {
                extendedStrings.Add(names[iName], values[iValue]);
            }

            return extendedStrings;
        }

        private static int RoundUpToEven(int i)
        {
            return i % 2 == 1 ? i + 1 : i;
        }

        /// <summary>Read a 16-bit or 32-bit value from the buffer starting at the specified position.</summary>
        /// <param name="buffer">The buffer from which to read.</param>
        /// <param name="pos">The position at which to read.</param>
        /// <param name="readAs32Bit">Whether or not to read value as 32-bit. Will read as 16-bit if set to false.</param>
        /// <returns>The value read.</returns>
        private static int ReadInt(byte[] buffer, int pos, bool readAs32Bit) =>
            readAs32Bit ? ReadInt32(buffer, pos) : ReadInt16(buffer, pos);

        /// <summary>Read a 16-bit value from the buffer starting at the specified position.</summary>
        /// <param name="buffer">The buffer from which to read.</param>
        /// <param name="pos">The position at which to read.</param>
        /// <returns>The 16-bit value read.</returns>
        private static short ReadInt16(byte[] buffer, int pos)
        {
            return unchecked((short)((((int)buffer[pos + 1]) << 8) | ((int)buffer[pos] & 0xff)));
        }

        /// <summary>Read a 32-bit value from the buffer starting at the specified position.</summary>
        /// <param name="buffer">The buffer from which to read.</param>
        /// <param name="pos">The position at which to read.</param>
        /// <returns>The 32-bit value read.</returns>
        private static int ReadInt32(byte[] buffer, int pos)
            => BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(pos));

        /// <summary>Reads a string from the buffer starting at the specified position.</summary>
        /// <param name="buffer">The buffer from which to read.</param>
        /// <param name="pos">The position at which to read.</param>
        /// <returns>The string read from the specified position.</returns>
        private static string ReadString(byte[] buffer, int pos)
        {
            int end = FindNullTerminator(buffer, pos);
            return Encoding.ASCII.GetString(buffer, pos, end - pos);
        }

        /// <summary>Finds the null-terminator for a string that begins at the specified position.</summary>
        private static int FindNullTerminator(byte[] buffer, int pos)
        {
            int i = buffer.AsSpan(pos).IndexOf((byte)'\0');
            return i >= 0 ? pos + i : buffer.Length;
        }
    }
}
