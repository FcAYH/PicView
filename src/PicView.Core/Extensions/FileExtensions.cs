using System.Globalization;
using System.Numerics;

namespace PicView.Core.Extensions;

public static class FileExtensions
{
     private static ReadOnlySpan<char> Suffixes => ['B', 'K', 'M', 'G', 'T', 'P', 'E'];

     /// <summary>
     /// Returns the human-readable file size for an arbitrary, 64-bit file size
     /// The default format is "0.## XB", e.g. "4.2 KB" or "1.43 GB"
     /// </summary>
     /// <param name="fileSize">FileInfo.Length</param>
     /// <returns>A formatted string representing the file size with a suffix (e.g., "4.2 KB").</returns>
    public static string GetReadableFileSize(this long fileSize)
    {
        if (fileSize <= 0)
        {
            return "0 B";
        }

        var magnitude = BitOperations.Log2((ulong)fileSize) / 10;

        return magnitude is 0 ? FormatBytes(fileSize) : FormatWithSuffix(fileSize, magnitude);
    }

    /// <summary>
    /// Formats a file size in bytes using string. String.Create for zero-allocation construction.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        // Calculate the length of the number string: floor(log10(n)) + 1
        // (bytes is > 0 here due to the check in GetReadableFileSize)
        var numLength = (int)Math.Floor(Math.Log10(bytes) + 1);
        
        // Total Length: Number + " B" (2 chars)
        var totalLength = numLength + 2;

        return string.Create(totalLength, bytes, (span, value) =>
        {
            // Write number
            value.TryFormat(span, out var written);
            
            // Write suffix
            span[written] = ' ';
            span[written + 1] = 'B';
        });
    }

    /// <summary>
    /// Formats with suffix using string.Create.
    /// Pre-calculates the layout to write directly into the final string memory.
    /// </summary>
    private static string FormatWithSuffix(long fileSize, int magnitude)
    {
        var divisor = 1L << (magnitude * 10);
        var whole = fileSize / divisor;
        var fraction = fileSize % divisor;

        // 1. Calculate Decimal Part
        // We use double here to prevent overflow on 'fraction * 100' for Exabyte sizes
        // and to handle the percentage calculation simply.
        var decimalValue = 0;
        if (fraction > 0)
        {
            decimalValue = (int)((double)fraction / divisor * 100);
        }

        // 2. Analyze formatting requirements
        var hasDecimal = decimalValue > 0;
        var firstDigit = decimalValue / 10;
        var secondDigit = decimalValue % 10;
        var showSecond = secondDigit > 0;

        // 3. Calculate Exact String Length
        // Since 'whole' is scaled by magnitude, it is always < 1024 (1-4 digits).
        var wholeLen = whole < 10 ? 1 : (whole < 100 ? 2 : whole < 1000 ? 3 : 4);
        
        // Base length: Whole + " X" + "B" (3 chars for suffix part)
        var totalLen = wholeLen + 3;

        if (hasDecimal)
        {
            totalLen += 2; // + Separator + FirstDigit
            if (showSecond) totalLen += 1; // + SecondDigit
        }

        // 4. Create String
        var state = (whole, magnitude, hasDecimal, firstDigit, showSecond, secondDigit);
        
        return string.Create(totalLen, state, (span, s) =>
        {
            var (num, mag, decimalPart, d1, showD2, d2) = s;
            
            // Write Whole Number
            num.TryFormat(span, out var written);
            
            // Write Decimals
            if (decimalPart)
            {
                // Write Separator
                span[written++] = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
                
                // Write First Digit
                span[written++] = (char)('0' + d1);
                
                // Write Second Digit
                if (showD2)
                {
                    span[written++] = (char)('0' + d2);
                }
            }

            // Write Suffix
            span[written++] = ' ';
            span[written++] = Suffixes[mag];
            span[written] = 'B';
        });
    }
}