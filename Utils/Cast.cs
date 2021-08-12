using System;
using Microsoft.VisualBasic;
using System.Text.Json;

namespace net.boilingwater.Utils
{
    public class Cast
    {
        public static int ToInteger(object obj)
        {
            if (obj is int @int) return @int;

            try
            {
                return (int)decimal.Parse(ToString(obj));
            }
            catch (Exception) { }

            return default;
        }

        public static long ToLong(object obj)
        {
            if (obj is long @long) return @long;

            try
            {
                return (long)decimal.Parse(ToString(obj));
            }
            catch (Exception) { }

            return default;
        }

        public static double ToDouble(object obj)
        {
            if (obj is double @double) return @double;

            try
            {
                return (double)decimal.Parse(ToString(obj));
            }
            catch (Exception) { }

            return default;
        }

        public static decimal ToDecimal(object obj)
        {
            if (obj is decimal @decimal) return @decimal;

            try
            {
                return decimal.Parse(ToString(obj));
            }
            catch (Exception) { }

            return default;
        }

        public static bool ToBoolean(object obj)
        {
            if (obj is bool @bool) return @bool;

            try
            {
                return bool.Parse(ToString(obj));
            }
            catch (Exception) { }

            return default;
        }

        public static string ToString(object obj)
        {
            if (obj == null) return "";

            if (obj is string @string) return @string;

            if (Information.IsNumeric(obj))
            {
                return obj.ToString();
            }

            if (!Information.IsReference(obj))
            {
                return obj.ToString();
            }

            return JsonSerializer.Serialize(obj);
        }

        public static T ToObject<T>(object obj)
        {
            if (obj is T @t) return @t;
            return default;
        }
    }
}