// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Data;

namespace EventFlow.ReadStores.MsSql.TableGeneration
{
    public class DbColumn
    {
        public string Name { get; private set; }
        public SqlDbType DbType { get; private set; }
        public bool IsNullable { get; set; }
        public int? MaxLength { get; set; }
        public int? DateTimePrecision { get; set; }
        public Type Type { get { return GetManagedType(); } }

        public DbColumn(
            string name,
            SqlDbType dbType,
            bool isNullable,
            int? maxLength,
            int? dateTimePrecision)
        {
            Name = name;
            DbType = dbType;
            IsNullable = isNullable;
            MaxLength = maxLength;
            DateTimePrecision = dateTimePrecision;
        }

        public override string ToString()
        {
            var parts = new List<string>
                {
                    string.Format("[{0}]", Name),
                    string.Format("[{0}]{1}", DbType.ToString().ToLowerInvariant(), GetPrecision()),
                };
            parts.Add(IsNullable ? "NULL" : "NOT NULL");
            return string.Join(" ", parts);
        }

        private Type GetManagedType()
        {
            switch (DbType)
            {
                case SqlDbType.Bit: return typeof(bool);
                case SqlDbType.BigInt: return typeof(long);
                case SqlDbType.DateTimeOffset: return typeof(DateTimeOffset);
                case SqlDbType.NVarChar: return typeof(string);
                case SqlDbType.Int: return typeof(int);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private string GetPrecision()
        {
            switch (DbType)
            {
                case SqlDbType.NVarChar:
                    return MaxLength.GetValueOrDefault() == -1
                        ? "(MAX)"
                        : string.Format("({0})", MaxLength);
                case SqlDbType.DateTimeOffset:
                    return string.Format("({0})", DateTimePrecision.GetValueOrDefault());
                default: return string.Empty;
            }
        }
    }
}
