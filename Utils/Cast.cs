using Microsoft.VisualBasic;

using System;
using System.ComponentModel;
using System.Text.Json;

namespace net.boilingwater.Utils
{
    public class Cast
    {
        public static int ToInteger(object obj)
        {
            if (obj is int @int) return @int;

            return (int)ToDecimal(obj);
        }

        public static long ToLong(object obj)
        {
            if (obj is long @long) return @long;

            return (long)ToDecimal(obj);
        }

        public static double ToDouble(object obj)
        {
            if (obj is double @double) return @double;

            return (double)ToDecimal(obj);
        }

        public static decimal ToDecimal(object obj)
        {
            if (obj is decimal @decimal) return @decimal;

            if(decimal.TryParse(ToString(obj), out var @result)) return @result;

            return default;
        }

        public static bool ToBoolean(object obj)
        {
            if (obj is bool @bool) return @bool;

            if (bool.TryParse(ToString(obj), out var @result)) return result;

            return default;
        }

        public static string ToString(object obj)
        {
            if (obj == null) return string.Empty;

            if (obj is string @string) return @string;

            if (Information.IsNumeric(obj)) return obj.ToString();

            if (!Information.IsReference(obj)) return obj.ToString();

            return JsonSerializer.Serialize(obj);
        }

        public static T ToObject<T>(object obj)
        {
            if (obj is T @t) return @t;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));

                if(converter != null)
                {
                    return (T)converter.ConvertFrom(obj);
                }
            }
            catch (Exception) {}

            return default(T);
        }
    }
}