using System;
using System.Collections.Generic;

namespace MyNoSqlAccessor.AzureTable.Extensions
{
    /// <summary>
    /// http://www.52unicode.com/arrows-zifu
    /// </summary>
    public static class StringExtension
    {
        private static readonly Dictionary<string, string> separators;

        static StringExtension()
        {
            separators = new Dictionary<string, string>
            {
                { "/", '\u21C3'.ToString() },//⇃
                { @"\", '\u21C2'.ToString() }//⇂
            };
        }

        public static string KeyEncode(this string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                foreach (var separator in separators)
                {
                    key = key.Replace(separator.Key, separator.Value);
                }
            }
            return key;
        }

        public static string KeyDecode(this string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                foreach (var separator in separators)
                {
                    key = key.Replace(separator.Value, separator.Key);
                }
            }
            return key;
        }
    }
}
