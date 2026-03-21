using System;

namespace Titan.Unitiy;

public static class SafeConvert
{
	public static bool ToBoolean(string s)
	{
		bool result;
		return bool.TryParse(s, out result) && result;
	}

	public static bool[] ToBoolean(string[] s)
	{
		return Array.ConvertAll(s, ToBoolean);
	}

	public static byte ToByte(string s)
	{
		byte result;
		return (byte)(byte.TryParse(s, out result) ? result : 0);
	}

	public static byte[] ToByte(string[] s)
	{
		return Array.ConvertAll(s, ToByte);
	}

	public static sbyte ToSbyte(string s)
	{
		sbyte result;
		return (sbyte)(sbyte.TryParse(s, out result) ? result : 0);
	}

	public static sbyte[] ToSbyte(string[] s)
	{
		return Array.ConvertAll(s, ToSbyte);
	}

	public static short ToInt16(string s)
	{
		return ToInt16(s, 0);
	}

	public static short ToInt16(string s, short defaultValue)
	{
		short result;
		return short.TryParse(s, out result) ? result : defaultValue;
	}

	public static short[] ToInt16(string[] s)
	{
		return Array.ConvertAll(s, ToInt16);
	}

	public static ushort ToUInt16(string s)
	{
		return ToUInt16(s, 0);
	}

	public static ushort ToUInt16(string s, ushort defaultValue)
	{
		ushort result;
		return ushort.TryParse(s, out result) ? result : defaultValue;
	}

	public static ushort[] ToUInt16(string[] s)
	{
		return Array.ConvertAll(s, ToUInt16);
	}

	public static int ToInt32(string s)
	{
		return ToInt32(s, 0);
	}

	public static int ToInt32(string s, int defaultValue)
	{
		int result;
		return int.TryParse(s, out result) ? result : defaultValue;
	}

	public static int[] ToInt32(string[] s)
	{
		return Array.ConvertAll(s, ToInt32);
	}

	public static uint ToUInt32(string s)
	{
		uint result;
		return uint.TryParse(s, out result) ? result : 0u;
	}

	public static uint ToUInt32(string s, uint defalutValue)
	{
		uint result;
		return uint.TryParse(s, out result) ? result : defalutValue;
	}

	public static uint[] ToUInt32(string[] s)
	{
		return Array.ConvertAll(s, ToUInt32);
	}

	public static long ToInt64(string s, long defalutValue)
	{
		long result;
		return long.TryParse(s, out result) ? result : defalutValue;
	}

	public static long ToInt64(string s)
	{
		return ToInt64(s, 0L);
	}

	public static long[] ToInt64(string[] s)
	{
		return Array.ConvertAll(s, ToInt64);
	}

	public static ulong ToUInt64(string s)
	{
		return ToUInt64(s, 0u);
	}

	public static ulong ToUInt64(string s, uint defaultValue)
	{
		ulong result;
		return ulong.TryParse(s, out result) ? result : defaultValue;
	}

	public static ulong[] ToUInt64(string[] s)
	{
		return Array.ConvertAll(s, ToUInt64);
	}

	public static float ToSingle(string s, float defaultValue)
	{
		float result;
		return float.TryParse(s, out result) ? result : defaultValue;
	}

	public static float ToSingle(string s)
	{
		return ToSingle(s, 0f);
	}

	public static float[] ToSingle(string[] s)
	{
		return Array.ConvertAll(s, ToSingle);
	}

	public static double ToDouble(string s, double defaultValue)
	{
		double result;
		return double.TryParse(s, out result) ? result : defaultValue;
	}

	public static double ToDouble(string s)
	{
		return ToDouble(s, 0.0);
	}

	public static double[] ToDouble(string[] s)
	{
		return Array.ConvertAll(s, ToDouble);
	}

	public static float ToFloat(string s, float defaultValue = 0f)
	{
		float result;
		return float.TryParse(s, out result) ? result : defaultValue;
	}

	public static decimal ToDecimal(string s, decimal defaultValue)
	{
		decimal result;
		return decimal.TryParse(s, out result) ? result : defaultValue;
	}

	public static decimal ToDecimal(string s)
	{
		return ToDecimal(s, 0m);
	}

	public static decimal[] ToDecimal(string[] s)
	{
		return Array.ConvertAll(s, ToDecimal);
	}

	public static DateTime ToDateTime(string s, DateTime defaultValue)
	{
		DateTime result;
		return DateTime.TryParse(s, out result) ? result : defaultValue;
	}

	public static DateTime ToDateTime(string s)
	{
		return ToDateTime(s, DateTime.MinValue);
	}

	public static DateTime[] ToDateTime(string[] s)
	{
		return Array.ConvertAll(s, ToDateTime);
	}

	public static TimeSpan ToTimeSpan(string s, TimeSpan defaultValue)
	{
		TimeSpan result;
		return TimeSpan.TryParse(s, out result) ? result : defaultValue;
	}

	public static TimeSpan ToTimeSpan(string s)
	{
		return ToTimeSpan(s, TimeSpan.Zero);
	}

	public static TimeSpan[] ToTimeSpan(string[] s)
	{
		return Array.ConvertAll(s, ToTimeSpan);
	}

	public static object ToEnum(object obj, Type enumType)
	{
		object result;
		if (Enum.IsDefined(enumType, obj))
		{
			string[] names = Enum.GetNames(enumType);
			string text = obj.ToString();
			for (int i = 0; i < names.Length; i++)
			{
				if (text == names[i])
				{
					return Enum.Parse(enumType, text);
				}
			}
			result = Enum.ToObject(enumType, obj);
		}
		else
		{
			result = null;
		}
		return result;
	}

	public static T ToEnum<T>(object obj) where T : struct
	{
		int result = 0;
		bool flag = int.TryParse(obj.ToString(), out result);
		T result2;
		if (!Enum.IsDefined(typeof(T), obj))
		{
			result2 = ((!flag) ? default(T) : ((T)Enum.ToObject(typeof(T), result)));
		}
		else
		{
			string[] names = Enum.GetNames(typeof(T));
			string text = obj.ToString();
			for (int i = 0; i < names.Length; i++)
			{
				if (text == names[i])
				{
					return (T)Enum.Parse(typeof(T), text);
				}
			}
			result2 = (T)Enum.ToObject(typeof(T), obj);
		}
		return result2;
	}

	public static T[] ToEnum<T>(object[] s) where T : struct
	{
		return Array.ConvertAll(s, ToEnum<T>);
	}

	public static string ToString(string p)
	{
		return p;
	}

	public static DateTime? ToDateTime(long timeStamp)
	{
		DateTime? result;
		if (timeStamp == 0)
		{
			result = null;
		}
		else
		{
			DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
			long ticks = long.Parse(timeStamp + "0000000");
			TimeSpan value = new TimeSpan(ticks);
			result = dateTime.Add(value);
		}
		return result;
	}

	public static long ToDateTimeInt(DateTime time)
	{
		DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
		return (long)(time - dateTime).TotalSeconds;
	}
}
