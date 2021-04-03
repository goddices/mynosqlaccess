using System;

namespace MyNoSqlAccessor.AzureTable
{

    public static class KeyHelper
    {
        public static string ToKey(int key) => key.ToString("D10");

        public static string ToKey(long key) => key.ToString("D19");

        public static string ToKey(Guid key) => key.ToString("N");

        public static string ToKey(string key) => key;

        public static string ToKey(DateTimeOffset key) => ToKey(long.MaxValue - key.Ticks);
        public static string ToKey(DateTime key) => ToKey(long.MaxValue - key.Ticks);

        public static string ToKey(bool key) => key.ToString();

        /// <summary>
        /// Be cautious, this could cause stack overflow.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string ToKey<T>(T key) => key == null ? throw new ArgumentNullException(nameof(key)) : typeof(T) switch
        {
            var keyType when keyType == typeof(int) => ToKey((dynamic)key),
            var keyType when keyType == typeof(long) => ToKey((dynamic)key),
            var keyType when keyType == typeof(Guid?) => ToKey((dynamic)key),
            var keyType when keyType == typeof(Guid) => ToKey((dynamic)key),
            var keyType when keyType == typeof(DateTimeOffset) => ToKey((dynamic)key),
            var keyType when keyType == typeof(DateTime) => ToKey((dynamic)key),
            var keyType when keyType == typeof(string) => ToKey((dynamic)key),
            var keyType when keyType == typeof(bool) => ToKey((dynamic)key),
            var keyType when keyType.IsEnum => ToKey(key.ToString()),
            var keyType => throw new NotSupportedException($"{keyType.Name} is not supported"),
        };

        public const string KeyCombinationChar = "-";

        public static string ToKey<T1, T2>(T1 key1, T2 key2)
            => ToKey(ToKey(key1), ToKey(key2));

        public static string ToKey<T1, T2, T3>(T1 key1, T2 key2, T3 key3)
            => ToKey(ToKey(key1), ToKey(key2), ToKey(key3));

        public static string ToKey<T1, T2, T3, T4>(T1 key1, T2 key2, T3 key3, T4 key4)
            => ToKey(ToKey(key1), ToKey(key2), ToKey(key3), ToKey(key4));

        public static string ToKey<T1, T2, T3, T4, T5>(T1 key1, T2 key2, T3 key3, T4 key4, T5 key5)
            => ToKey(ToKey(key1), ToKey(key2), ToKey(key3), ToKey(key4), ToKey(key5));

        public static string ToKey<T1, T2, T3, T4, T5, T6>(T1 key1, T2 key2, T3 key3, T4 key4, T5 key5, T6 key6)
            => ToKey(ToKey(key1), ToKey(key2), ToKey(key3), ToKey(key4), ToKey(key5), ToKey(key6));

        public static string ToKey(params string[] keys)
            => string.Join(KeyCombinationChar, keys);
    }
}