using System;
using System.Text.RegularExpressions;
using EventFlow.ValueObjects;

namespace EventFlow.MsSql
{
    /// <summary>
    /// Represents an MSSQL Server schema name.
    /// </summary>
    public class Schema : SingleValueObject<string>
    {
        /// <summary>
        /// Creates an MSSQL Server schema name value object.
        /// </summary>
        /// <param name="value">
        /// Schema name that starts with a letter or underscore, followed by up to 127 letters,
        /// digits, or one of '@', '$', '#', '_'.
        /// </param>
        public Schema(string value) : base(value)
        {
            var regex = @"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$";
            if (!Regex.IsMatch(value, regex))
            {
                throw new ArgumentException("Invalid MSSQL schema name provided", nameof(value));
            }
        }
    }
}