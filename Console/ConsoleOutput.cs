using System.Runtime.CompilerServices;

namespace Powmada.Console
{
    internal static class ConsoleOutput
    {
        public const int LineWidth = 80;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSectionHeader(string title, char fillChar = '-')
        {
            WriteLine(FormatCenteredLine(title, fillChar));
        }

        public static void WriteRule(char fillChar = '-')
        {
            WriteLine(string.Create(LineWidth, fillChar, static (span, c) => span.Fill(c)));
        }

        public static void WriteError(string message)
        {
            WriteColored(ConsoleColor.Red, message);
        }

        public static void WriteWarning(string message, string prefix = "[BOOK-WARN]")
        {
            WriteColored(ConsoleColor.Yellow, $"{prefix} {message}");
        }

        // string.Create yields one 80-char allocation with no intermediate strings.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatCenteredLine(string title, char fillChar)
        {
            ReadOnlySpan<char> span = title.AsSpan();
            int trimStart = 0;
            while (trimStart < span.Length && char.IsWhiteSpace(span[trimStart]))
                trimStart++;
            int trimEnd = span.Length;
            while (trimEnd > trimStart && char.IsWhiteSpace(span[trimEnd - 1]))
                trimEnd--;
            int trimLength = trimEnd - trimStart;

            int coreLen = trimLength + 2;
            int titleCopyLength;
            int leftPad;
            if (coreLen > LineWidth)
            {
                titleCopyLength = LineWidth - 2;
                leftPad = 0;
            }
            else
            {
                titleCopyLength = trimLength;
                leftPad = (LineWidth - coreLen) / 2;
            }

            var state = new CenteredLineState(title, trimStart, trimLength, titleCopyLength, fillChar, leftPad);
            return string.Create(LineWidth, state, static (dest, s) =>
            {
                dest[..s.LeftPad].Fill(s.FillChar);
                int pos = s.LeftPad;
                dest[pos++] = ' ';
                s.Title.AsSpan(s.TrimStart, s.TitleCopyLength).CopyTo(dest[pos..]);
                pos += s.TitleCopyLength;
                dest[pos++] = ' ';
                dest[pos..].Fill(s.FillChar);
            });
        }

        public static void WriteLine(string message) => System.Console.WriteLine(message);

        private static void WriteColored(ConsoleColor color, string message)
        {
            System.Console.ForegroundColor = color;
            WriteLine(message);
            System.Console.ResetColor();
        }

        private readonly struct CenteredLineState
        {
            public readonly string Title;
            public readonly int TrimStart, TrimLength, TitleCopyLength;
            public readonly char FillChar;
            public readonly int LeftPad;

            public CenteredLineState(
                string title,
                int trimStart,
                int trimLength,
                int titleCopyLength,
                char fillChar,
                int leftPad)
            {
                Title = title;
                TrimStart = trimStart;
                TrimLength = trimLength;
                TitleCopyLength = titleCopyLength;
                FillChar = fillChar;
                LeftPad = leftPad;
            }
        }
    }
}
