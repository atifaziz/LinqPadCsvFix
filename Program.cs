#region Copyright (C) 2017 Atif Aziz
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
#endregion

namespace LinqPadCsvFix
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Jayrock.Json.Conversion;
    using Mannex.Collections.Generic;
    using MoreLinq;

    #endregion

    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return Wain(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 0xbad;
            }
        }

        static int Wain(IEnumerable<string> args)
        {
            var argList = args.ToList();

            var debugFlagIndex = argList.IndexOf("--debug");
            if (debugFlagIndex >= 0)
            {
                argList.RemoveAt(debugFlagIndex);
                Debugger.Launch();
            }

            var map =
                argList.Select(arg => arg.Split(new[] { '=' }, 2)
                                         .Fold((outName, inName) => inName.AsKeyTo(outName)))
                       .ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

            var reader = Console.In;
            var i = 0;
            using (var e = Read(reader))
            {
                while (e.MoveNext())
                {
                    var obj = e.Current;

                    const string stackTraceKey = "StackTrace";
                    const string messageKey = "Message";
                    if (obj.ContainsKey(stackTraceKey) && obj.ContainsKey(messageKey))
                    {
                        Console.Error.WriteLine(obj[messageKey]);
                        var stackTrace = obj[stackTraceKey];
                        if (stackTrace != null)
                            Console.Error.WriteLine(stackTrace);
                        return 0xbad;
                    }

                    if (i++ == 0)
                    {
                        Console.WriteLine(string.Join(",",
                            from name in obj.Keys
                            select map.Find(name) ?? SnakeCase.ScreamingFromPascal(name) into name
                            select "\"" + name + "\""));
                    }

                    var fields =
                        from value in obj.Values
                        select (value as string)?.Replace("\"", "\"\"") ?? value?.ToString()
                        into v
                        select "\"" + v + "\"";

                    Console.WriteLine(string.Join(",", fields));
                }
            }

            return 0;
        }

        static IEnumerator<IDictionary<string, object>> Read(TextReader reader)
        {
            string line;
            var sb = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line[0] == '}')
                {
                    sb.Append('}').AppendLine();
                    yield return JsonConvert.Import<IDictionary<string, object>>(sb.ToString());
                    sb.Clear();
                    if (line.Length == 1)
                        break;
                    line = line.Substring(1);
                }
                sb.AppendLine(line);
            }
        }

        public static class SnakeCase
        {
            static string FromPascalCore(string input) =>
                Regex.Replace(input, @"((?<![A-Z]|^)[A-Z]|(?<=[A-Z]+)[A-Z](?=[a-z]))", m => "_" + m.Value);

            public static string FromPascal(string input) =>
                FromPascalCore(input).ToLowerInvariant();

            public static string ScreamingFromPascal(string input) =>
                FromPascalCore(input).ToUpperInvariant();
        }
    }
}

#region License, Terms and Author(s)
//
// Mannex - Extension methods for .NET
// Copyright (c) 2009 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Mannex.Collections.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Extension methods for pairing keys and values as 
    /// <see cref="KeyValuePair{TKey,TValue}"/>.
    /// </summary>

    static partial class PairingExtensions
    {
        /// <summary>
        /// Pairs a value with a key.
        /// </summary>

        public static KeyValuePair<TKey, TValue> AsKeyTo<TKey, TValue>(this TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>

    static partial class DictionaryExtensions
    {
        /// <summary>
        /// Finds the value for a key, returning the default value for
        /// <typeparamref name="TValue"/> if the key is not present.
        /// </summary>

        [DebuggerStepThrough]
        public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return Find(dict, key, default(TValue));
        }

        /// <summary>
        /// Finds the value for a key, returning a given default value if the
        /// key is not present.
        /// </summary>

        [DebuggerStepThrough]
        public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            if (dict == null) throw new ArgumentNullException("dict");
            TValue value;
            return dict.TryGetValue(key, out value) ? value : @default;
        }
    }
}