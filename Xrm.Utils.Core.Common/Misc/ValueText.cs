namespace Xrm.Utils.Core.Common.Misc
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Writes and reads attribute values in serialized data without depending on the culture of
    /// the machine that runs the code.
    /// </summary>
    /// <remarks>
    /// Values used to be written with ToString() and read with the current culture, so a file
    /// moved between a machine that writes 1234,5 and one that expects 1234.5 was misread or
    /// rejected. They are now written in the invariant culture. Reading tries the invariant
    /// culture first and falls back to the current culture for files written the old way.
    /// Neither attempt allows thousands separators, so a comma-decimal value can never be read as
    /// a number ten or a hundred times larger - it is either understood or rejected.
    /// </remarks>
    public static class ValueText
    {
        /// <summary>Formats a base value for serialized data: dates as round-trip "O", numbers in the invariant culture.</summary>
        /// <param name="value">A base value, as returned by AttributeAsBaseType</param>
        /// <returns>The text to write, or null for a null value</returns>
        public static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return null;

                case DateTime date:
                    return date.ToString("O", CultureInfo.InvariantCulture);

                case double number:
                    return number.ToString("R", CultureInfo.InvariantCulture);

                case float number:
                    return number.ToString("R", CultureInfo.InvariantCulture);

                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);

                default:
                    return value.ToString();
            }
        }

        /// <summary>Reads a decimal written by <see cref="Format"/>, or by the current culture.</summary>
        public static decimal ParseDecimal(string text) =>
            Parse<decimal>(text, decimal.TryParse);

        /// <summary>Reads a double written by <see cref="Format"/>, or by the current culture.</summary>
        public static double ParseDouble(string text) =>
            Parse<double>(text, double.TryParse);

        /// <summary>Reads an integer written by <see cref="Format"/>, or by the current culture.</summary>
        public static int ParseInt(string text) =>
            Parse<int>(text, int.TryParse, NumberStyles.Integer);

        /// <summary>Reads a long integer written by <see cref="Format"/>, or by the current culture.</summary>
        public static long ParseLong(string text) =>
            Parse<long>(text, long.TryParse, NumberStyles.Integer);

        private delegate bool TryParse<T>(string text, NumberStyles style, IFormatProvider provider, out T result);

        private static T Parse<T>(string text, TryParse<T> tryParse, NumberStyles style = NumberStyles.Float)
        {
            if (tryParse(text, style, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            if (tryParse(text, style, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }
            throw new FormatException($"'{text}' is not a {typeof(T).Name} in invariant notation or in the current culture ({CultureInfo.CurrentCulture.Name}).");
        }
    }
}
