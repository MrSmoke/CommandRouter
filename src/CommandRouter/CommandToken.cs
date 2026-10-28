namespace CommandRouter
{
    using System;

    /// <summary>
    /// A single token from a command string.
    /// </summary>
    public readonly struct CommandToken : IEquatable<CommandToken>
    {
        private readonly string? _raw;

        /// <param name="raw">The raw command string the token belongs to</param>
        /// <param name="start">The zero based position in <paramref name="raw"/> where the token starts</param>
        /// <param name="length">The number of characters the token spans in <paramref name="raw"/>, including any quotes</param>
        /// <param name="isQuoted">True if the token is surrounded by quotes, which are not part of the value</param>
        public CommandToken(string raw, int start, int length, bool isQuoted)
        {
            ArgumentNullException.ThrowIfNull(raw);
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, raw.Length);

            if (isQuoted && length < 2)
                throw new ArgumentOutOfRangeException(nameof(length), "A quoted token needs both quotes");

            _raw = raw;
            Start = start;
            Length = length;
            IsQuoted = isQuoted;
        }

        /// <summary>
        /// The zero based position in the raw command string where the token starts
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// The number of characters the token spans in the raw command string, including any quotes
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// True if the token was surrounded by quotes
        /// </summary>
        public bool IsQuoted { get; }

        /// <summary>
        /// The zero based position in the raw command string just after the token
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// The token value, with any surrounding quotes removed, as a slice of the raw string
        /// </summary>
        public ReadOnlySpan<char> ValueSpan
        {
            get
            {
                if (_raw == null)
                    return default;

                return IsQuoted
                    ? _raw.AsSpan(Start + 1, Length - 2)
                    : _raw.AsSpan(Start, Length);
            }
        }

        /// <summary>
        /// The token value, with any surrounding quotes removed. This allocates a new string each time
        /// </summary>
        public string Value => _raw == null ? string.Empty : new string(ValueSpan);

        public bool Equals(CommandToken other)
        {
            return Start == other.Start &&
                   Length == other.Length &&
                   IsQuoted == other.IsQuoted &&
                   string.Equals(_raw, other._raw, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => obj is CommandToken other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Start, Length, IsQuoted);

        public override string ToString() => Value;

        public static bool operator ==(CommandToken left, CommandToken right) => left.Equals(right);

        public static bool operator !=(CommandToken left, CommandToken right) => !left.Equals(right);
    }
}
