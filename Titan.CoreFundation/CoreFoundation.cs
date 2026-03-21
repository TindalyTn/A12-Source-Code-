using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic;

namespace Titan.CoreFundation;

public class CoreFoundation
{
	public static IntPtr kCFAllocatorDefault;

	public static IntPtr kCFBooleanFalse;

	public static IntPtr kCFBooleanTrue;

	public static IntPtr kCFStreamPropertyDataWritten;

	public static IntPtr kCFTypeArrayCallBacks;

	public static IntPtr kCFTypeDictionaryKeyCallBacks;

	public static IntPtr kCFTypeDictionaryValueCallBacks;

	private static IntPtr coreFundationDllIntPtr;

	static CoreFoundation()
	{
		kCFAllocatorDefault = IntPtr.Zero;
		kCFBooleanFalse = IntPtr.Zero;
		kCFBooleanTrue = IntPtr.Zero;
		kCFStreamPropertyDataWritten = IntPtr.Zero;
		kCFTypeArrayCallBacks = IntPtr.Zero;
		kCFTypeDictionaryKeyCallBacks = IntPtr.Zero;
		kCFTypeDictionaryValueCallBacks = IntPtr.Zero;
		coreFundationDllIntPtr = LoadLibrary("CoreFoundation.dll");
		kCFStreamPropertyDataWritten = EnumToCFEnum("kCFStreamPropertyDataWritten");
		kCFTypeDictionaryKeyCallBacks = ConstToCFConst("kCFTypeDictionaryKeyCallBacks");
		kCFTypeDictionaryValueCallBacks = ConstToCFConst("kCFTypeDictionaryValueCallBacks");
		kCFTypeArrayCallBacks = ConstToCFConst("kCFTypeArrayCallBacks");
		kCFBooleanFalse = CFBoolean.GetCFBoolean(flag: false);
		kCFBooleanTrue = CFBoolean.GetCFBoolean(flag: true);
	}

	internal static IntPtr EnumToCFEnum(string enmName)
	{
		IntPtr intPtr = IntPtr.Zero;
		if (coreFundationDllIntPtr != IntPtr.Zero)
		{
			intPtr = GetProcAddress(coreFundationDllIntPtr, enmName);
		}
		if (intPtr != IntPtr.Zero)
		{
			intPtr = Marshal.ReadIntPtr(intPtr, 0);
		}
		return intPtr;
	}

	public static IntPtr ConstToCFConst(string constName)
	{
		IntPtr result = IntPtr.Zero;
		if (coreFundationDllIntPtr != IntPtr.Zero)
		{
			result = GetProcAddress(coreFundationDllIntPtr, constName);
		}
		return result;
	}

	public static string CreatePlistString(object objPlist)
	{
		string result = string.Empty;
		try
		{
			IntPtr propertyList = CFTypeFromManagedType(objPlist);
			IntPtr intPtr = CFWriteStreamCreateWithAllocatedBuffers(IntPtr.Zero, IntPtr.Zero);
			if (!(intPtr != IntPtr.Zero) || !CFWriteStreamOpen(intPtr))
			{
				return result;
			}
			IntPtr errorString = IntPtr.Zero;
			if (CFPropertyListWriteToStream(propertyList, intPtr, CFPropertyListFormat.kCFPropertyListXMLFormat_v1_0, ref errorString) > 0)
			{
				IntPtr propertyName = kCFStreamPropertyDataWritten;
				IntPtr intPtr2 = CFWriteStreamCopyProperty(intPtr, propertyName);
				if (CFDataGetLength(intPtr2) > 0)
				{
					byte[] bytes = ReadCFDataFromIntPtr(intPtr2);
					result = Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);
				}
				CFRelease(intPtr2);
			}
			CFWriteStreamClose(intPtr);
		}
		catch
		{
		}
		return result;
	}

	[DllImport("Kernel32.dll")]
	public static extern IntPtr LoadLibrary(string lpFileName);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "__CFStringMakeConstantString")]
	internal static extern IntPtr CFStringMakeConstantString(string cStr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFAbsoluteTimeGetGregorianDate(ref double at, ref IntPtr tz);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int CFArrayGetCount(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFArrayGetTypeID();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void CFArrayGetValues(IntPtr sourceRef, CFRange range, IntPtr values);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFBooleanGetTypeID();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFDateCreate(IntPtr intptr_2, double double_0);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFDataCreate(IntPtr allocator, IntPtr bytes, int length);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern bool CFBooleanGetValue(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void CFDataAppendBytes(IntPtr theData, IntPtr pointer, uint length);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFDataCreateMutable(IntPtr allocator, uint capacity);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFDataGetBytePtr(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void CFDataGetBytes(IntPtr theData, CFRange range, byte[] buffer);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int CFDataGetLength(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFDataGetTypeID();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern double CFDateGetAbsoluteTime(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFDateGetTypeID();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	internal static extern void CFDictionaryAddValue(IntPtr theDict, IntPtr keys, IntPtr values);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	internal static extern IntPtr CFDictionaryCreate(IntPtr allocator, ref IntPtr keys, ref IntPtr values, int numValues, IntPtr CFDictionaryKeyCallBacks, IntPtr CFDictionaryValueCallBacks);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator, int capacity, IntPtr CFDictionaryKeyCallBacks, IntPtr CFDictionaryValueCallBacks);

	public static IntPtr CFDictionaryFromManagedDictionary(Dictionary<object, object> sourceDict)
	{
		if (sourceDict == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = CFDictionaryCreateMutable(kCFAllocatorDefault, sourceDict.Count, kCFTypeDictionaryKeyCallBacks, kCFTypeDictionaryValueCallBacks);
			foreach (KeyValuePair<object, object> item in sourceDict)
			{
				IntPtr intPtr2 = CFTypeFromManagedType(item.Key);
				IntPtr intPtr3 = CFTypeFromManagedType(item.Value);
				if (intPtr2 != IntPtr.Zero && intPtr3 != IntPtr.Zero)
				{
					CFDictionaryAddValue(intPtr, intPtr2, intPtr3);
				}
			}
		}
		catch
		{
		}
		return intPtr;
	}

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFWriteStreamCreateWithAllocatedBuffers(IntPtr intptr_2, IntPtr intptr_3);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int CFPropertyListWriteToStream(IntPtr propertyList, IntPtr stream, CFPropertyListFormat format, ref IntPtr errorString);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool CFWriteStreamOpen(IntPtr intptr_2);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr CFNumberCreate(IntPtr intptr_2, CFNumberType enum20_0, IntPtr intptr_3);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr CFNumberCreate_Int16(IntPtr number, CFNumberType theType, ref short valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberCreate")]
	public static extern IntPtr CFNumberCreate_Int32(IntPtr number, CFNumberType theType, ref int valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberCreate")]
	public static extern IntPtr CFNumberCreate_Int64(IntPtr number, CFNumberType theType, ref long valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberCreate")]
	public static extern IntPtr CFNumberCreate_Float(IntPtr number, CFNumberType theType, ref float valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberCreate")]
	public static extern IntPtr CFNumberCreate_Double(IntPtr number, CFNumberType theType, ref double valuePtr);

	internal static IntPtr CFNumberCreateNumbers(object objVals)
	{
		Type type = objVals.GetType();
		if (type == typeof(short))
		{
			short valuePtr = Convert.ToInt16(objVals);
			return CFNumberCreate_Int16(kCFAllocatorDefault, CFNumberType.kCFNumberSInt16Type, ref valuePtr);
		}
		if (type == typeof(int) || type == typeof(ushort))
		{
			int valuePtr2 = Convert.ToInt32(objVals);
			return CFNumberCreate_Int32(kCFAllocatorDefault, CFNumberType.kCFNumberSInt32Type, ref valuePtr2);
		}
		if (type == typeof(long) || type == typeof(uint))
		{
			long valuePtr3 = Convert.ToInt64(objVals);
			return CFNumberCreate_Int64(kCFAllocatorDefault, CFNumberType.kCFNumberSInt64Type, ref valuePtr3);
		}
		if (type == typeof(float))
		{
			float valuePtr4 = Convert.ToSingle(objVals);
			return CFNumberCreate_Float(kCFAllocatorDefault, CFNumberType.kCFNumberFloat32Type, ref valuePtr4);
		}
		if (type != typeof(double))
		{
			throw new NotImplementedException();
		}
		double valuePtr5 = Convert.ToDouble(objVals);
		return CFNumberCreate_Double(kCFAllocatorDefault, CFNumberType.kCFNumberFloat64Type, ref valuePtr5);
	}

	public static IntPtr CFTypeFromManagedType(object objVal)
	{
		IntPtr result;
		if (objVal == null)
		{
			result = IntPtr.Zero;
		}
		else
		{
			IntPtr intPtr = IntPtr.Zero;
			Type type = objVal.GetType();
			try
			{
				if (type == typeof(string))
				{
					return StringToCFString((string)objVal);
				}
				if (type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(double) || type == typeof(float))
				{
					return CFNumberCreateNumbers(objVal);
				}
				if (type == typeof(bool))
				{
					return CFBoolean.GetCFBoolean(Convert.ToBoolean(objVal));
				}
				if (type == typeof(DateTime))
				{
					return CFDateCreate(kCFAllocatorDefault, ((DateTime)objVal).Subtract(new DateTime(2001, 1, 1, 0, 0, 0)).TotalSeconds);
				}
				if (type == typeof(byte[]))
				{
					byte[] array = (byte[])objVal;
					IntPtr bytes = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
					intPtr = CFDataCreate(IntPtr.Zero, bytes, array.Length);
					return intPtr;
				}
				if (type == typeof(object[]))
				{
					return CFArrayFromManageArray((object[])objVal);
				}
				if (type == typeof(ArrayList))
				{
					return CFArrayFromManageArrayList(objVal as ArrayList);
				}
				if (type == typeof(List<object>))
				{
					return CFArrayFromManageList(objVal as List<object>);
				}
				if (type == typeof(Dictionary<object, object>))
				{
					intPtr = CFDictionaryFromManagedDictionary(objVal as Dictionary<object, object>);
				}
			}
			catch
			{
			}
			result = intPtr;
		}
		return result;
	}

	public static IntPtr CFArrayFromManageList(IList<object> arrySrc)
	{
		if (arrySrc == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = CFArrayCreateMutable(kCFAllocatorDefault, arrySrc.Count, kCFTypeArrayCallBacks);
		foreach (object item in arrySrc)
		{
			if (item != null)
			{
				CFArrayAppendValue(intPtr, CFTypeFromManagedType(item));
			}
		}
		return intPtr;
	}

	public static IntPtr CFArrayFromManageArrayList(ArrayList arrySrc)
	{
		if (arrySrc == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = CFArrayCreateMutable(kCFAllocatorDefault, arrySrc.Count, kCFTypeArrayCallBacks);
		foreach (object item in arrySrc)
		{
			if (item != null)
			{
				CFArrayAppendValue(intPtr, CFTypeFromManagedType(item));
			}
		}
		return intPtr;
	}

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFArrayCreateMutable(IntPtr allocator, int capacity, IntPtr callback);

	public static IntPtr CFArrayFromManageArray(object[] arrySrc)
	{
		if (arrySrc == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = CFArrayCreateMutable(kCFAllocatorDefault, arrySrc.Length, kCFTypeArrayCallBacks);
			foreach (object obj in arrySrc)
			{
				if (obj != null)
				{
					CFArrayAppendValue(intPtr, CFTypeFromManagedType(obj));
				}
			}
		}
		catch
		{
		}
		return intPtr;
	}

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFArrayAppendValue(IntPtr intptr_2, IntPtr intptr_3);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int CFDictionaryGetCount(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void CFDictionaryGetKeysAndValues(IntPtr sourceRef, IntPtr keys, IntPtr values);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFDictionaryGetTypeID();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFGetTypeID(IntPtr cf);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern CFNumberType CFNumberGetType(IntPtr sourceRef);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFNumberGetTypeID();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberGetValue", SetLastError = true)]
	internal static extern bool CFNumberGetValue_Float(IntPtr number, CFNumberType theType, ref float valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberGetValue")]
	internal static extern bool CFNumberGetValue_Double(IntPtr number, CFNumberType theType, ref double valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberGetValue")]
	internal static extern bool CFNumberGetValue_Int(IntPtr number, CFNumberType theType, ref int valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberGetValue")]
	internal static extern bool CFNumberGetValue_Long(IntPtr number, CFNumberType theType, ref long valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFNumberGetValue")]
	internal static extern bool CFNumberGetValue_Single(IntPtr number, CFNumberType theType, ref float valuePtr);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFPropertyListCreateFromXMLData(IntPtr allocator, IntPtr xmlData, CFPropertyListMutabilityOptions mutabilityOption, ref IntPtr errorString);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CFPropertyListCreateFromXMLData")]
	internal static extern IntPtr CFPropertyListCreatePtrFromXMLData(IntPtr allocator, IntPtr datas, CFPropertyListMutabilityOptions option, ref IntPtr errorString);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr CFPropertyListCreateXMLData(IntPtr allocator, IntPtr theData);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern bool CFPropertyListIsValid(IntPtr theData, CFPropertyListFormat format);

	private static IntPtr CFRangeMake(int loc, int len)
	{
		IntPtr intPtr = Marshal.AllocHGlobal(8);
		Marshal.WriteInt32(intPtr, 0, loc);
		Marshal.WriteInt32(intPtr, 4, len);
		return intPtr;
	}

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void CFRelease(IntPtr cf);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	internal static extern IntPtr CFRetain(IntPtr obj);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFStringCreateFromExternalRepresentation(IntPtr allocator, IntPtr data, CFStringEncoding encoding);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFStringCreateWithBytes(IntPtr allocator, byte[] data, ulong numBytes, CFStringEncoding encoding, bool isExternalRepresentation);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	internal static extern IntPtr CFStringGetCharacters(IntPtr handle, CFRange range, IntPtr buffer);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator, [MarshalAs(UnmanagedType.LPWStr)] string stingData, int strLength);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFStringCreateWithBytesNoCopy(IntPtr allocator, byte[] data, ulong numBytes, CFStringEncoding encoding, bool isExternalRepresentation, IntPtr contentsDeallocator);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string data, CFStringEncoding encoding);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern CFRange CFStringFind(IntPtr theString, IntPtr stringToFind, ushort compareOptions);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	internal static extern IntPtr CFStringGetCharactersPtr(IntPtr handle);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int CFStringGetBytes(IntPtr theString, CFRange range, uint encoding, byte lossByte, byte isExternalRepresentation, byte[] buffer, int maxBufLen, ref int usedBufLen);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void CFRunLoopRun();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern char CFStringGetCharacterAtIndex(IntPtr theString, int idx);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int CFStringGetLength(IntPtr cf);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int CFStringGetMaximumSizeForEncoding(int length, uint encoding);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr CFWriteStreamCopyProperty(IntPtr stream, IntPtr propertyName);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool CFWriteStreamClose(IntPtr stream);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint CFStringGetTypeID();

	public static string CFStringToString(byte[] value)
	{
		if (value.Length > 9)
		{
			return Encoding.UTF8.GetString(value, 9, value[9]);
		}
		return "";
	}

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFTimeZoneCopySystem();

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern bool CFURLCreateDataAndPropertiesFromResource(IntPtr allocator, IntPtr urlRef, ref IntPtr resourceData, ref IntPtr properties, IntPtr desiredProperties, ref int errorCode);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr CFURLCreateWithFileSystemPath(IntPtr allocator, IntPtr stringRef, CFURLPathStyle pathStyle, bool isDirectory);

	[DllImport("CoreFoundation.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool CFURLWriteDataAndPropertiesToResource(IntPtr urlRef, IntPtr dataToWrite, IntPtr propertiesToWrite, ref int errorCode);

	public static object ManagedPropertyListFromXMLData(byte[] inBytes)
	{
		if (inBytes != null)
		{
			IntPtr intPtr = CFDataCreateMutable(kCFAllocatorDefault, (uint)inBytes.Length);
			IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(inBytes, 0);
			CFDataAppendBytes(intPtr, pointer, (uint)inBytes.Length);
			IntPtr errorString = IntPtr.Zero;
			IntPtr srcRef = CFPropertyListCreatePtrFromXMLData(kCFAllocatorDefault, intPtr, CFPropertyListMutabilityOptions.kCFPropertyListImmutable, ref errorString);
			if (srcRef != IntPtr.Zero)
			{
				return ManagedTypeFromCFType(ref srcRef);
			}
		}
		return null;
	}

	public static object ManagedPropertyListFromXMLData(string fileOnPC)
	{
		byte[] array = new byte[0];
		using (FileStream fileStream = new FileStream(fileOnPC, FileMode.Open))
		{
			array = new byte[fileStream.Length];
			fileStream.Read(array, 0, (int)fileStream.Length);
		}
		return ManagedPropertyListFromXMLData(array);
	}

	public static object ManagedTypeFromCFType(ref IntPtr srcRef)
	{
		if (srcRef == IntPtr.Zero)
		{
			return string.Empty;
		}
		uint num = CFGetTypeID(srcRef);
		object obj = null;
		try
		{
			if (num == CFStringGetTypeID())
			{
				obj = ReadCFStringFromIntPtr(srcRef);
			}
			else if (num == CFArrayGetTypeID())
			{
				obj = ReadCFArrayFromIntPtr(srcRef);
			}
			else if (num == CFNumberGetTypeID())
			{
				obj = ReadCFNumberFromIntPtr(srcRef);
			}
			else if (num == CFDateGetTypeID())
			{
				obj = ReadCFDateFromIntPtr(srcRef);
			}
			else if (num == CFBooleanGetTypeID())
			{
				obj = CFBooleanGetValue(srcRef);
			}
			else if (num == CFDictionaryGetTypeID())
			{
				obj = ReadCFDictionaryFromIntPtr(srcRef);
			}
			else if (num == CFDataGetTypeID())
			{
				obj = ReadCFDataFromIntPtr(srcRef);
			}
			else
			{
				obj = ReadCFStringFromIntPtr(srcRef);
				if (obj == null)
				{
					obj = ReadCFDateFromIntPtr(srcRef);
				}
			}
		}
		catch (Exception)
		{
		}
		if (obj == null)
		{
			return string.Empty;
		}
		return obj;
	}

	public static string PropertyListToXML(byte[] propertyList)
	{
		try
		{
			int num = propertyList.Length;
			if (propertyList[0] == 98 && propertyList[1] == 112 && propertyList[2] == 108 && propertyList[3] == 105 && propertyList[4] == 115 && propertyList[5] == 116 && propertyList[6] == 48 && propertyList[7] == 48)
			{
				IntPtr errorString = IntPtr.Zero;
				IntPtr zero = IntPtr.Zero;
				zero = CFPropertyListCreateFromXMLData(IntPtr.Zero, zero, CFPropertyListMutabilityOptions.kCFPropertyListImmutable, ref errorString);
				if (zero != IntPtr.Zero)
				{
					zero = CFPropertyListCreateXMLData(IntPtr.Zero, zero);
					num = CFDataGetLength(zero) - 1;
					propertyList = new byte[num + 1];
					CFRange range = new CFRange(0, num);
					CFDataGetBytes(zero, range, propertyList);
				}
			}
			return Encoding.UTF8.GetString(propertyList);
		}
		catch (Exception)
		{
		}
		return null;
	}

	internal static object ReadCFArrayFromIntPtr(IntPtr srcRef)
	{
		if (srcRef == IntPtr.Zero)
		{
			return null;
		}
		int num = CFArrayGetCount(srcRef);
		if (num <= 0)
		{
			return null;
		}
		object[] array = new object[num];
		IntPtr intPtr = Marshal.AllocCoTaskMem(num * 4 * 2);
		CFArrayGetValues(srcRef, new CFRange(0, num), intPtr);
		for (int i = 0; i < num; i++)
		{
			IntPtr srcRef2 = Marshal.ReadIntPtr(intPtr, i * 4 * 2);
			array[i] = ManagedTypeFromCFType(ref srcRef2);
		}
		Marshal.FreeCoTaskMem(intPtr);
		return array;
	}

	internal static byte[] ReadCFDataFromIntPtr(IntPtr srcRef)
	{
		if (srcRef == IntPtr.Zero)
		{
			return null;
		}
		int num = CFDataGetLength(srcRef);
		if (num <= 0)
		{
			return null;
		}
		IntPtr ptr = CFDataGetBytePtr(srcRef);
		byte[] array = new byte[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = Marshal.ReadByte(ptr, i);
		}
		return array;
	}

	internal static DateTime ReadCFDateFromIntPtr(IntPtr srcRef)
	{
		DateTime result = DateTime.MinValue;
		if (srcRef != IntPtr.Zero)
		{
			double value = CFDateGetAbsoluteTime(srcRef);
			result = new DateTime(2001, 1, 1, 0, 0, 0).AddSeconds(value);
		}
		return result;
	}

	public static object ReadCFDictionaryFromIntPtr(IntPtr srcRef)
	{
		if (srcRef == IntPtr.Zero)
		{
			return null;
		}
		int num = CFDictionaryGetCount(srcRef);
		if (num <= 0)
		{
			return null;
		}
		IntPtr intPtr = Marshal.AllocCoTaskMem(num * 4 * 2);
		IntPtr values = Marshal.AllocCoTaskMem(num * 4 * 2);
		CFDictionary.GetKeysAndValues(srcRef, intPtr, ref values);
		Dictionary<object, object> dictionary = new Dictionary<object, object>();
		for (int i = 0; i < num; i++)
		{
			IntPtr srcRef2 = Marshal.ReadIntPtr(intPtr, i * 4 * 2);
			IntPtr srcRef3 = Marshal.ReadIntPtr(values, i * 4 * 2);
			object key = ManagedTypeFromCFType(ref srcRef2);
			object value = ManagedTypeFromCFType(ref srcRef3);
			dictionary.Add(key, value);
		}
		return dictionary;
	}

	public static object ReadCFNumberFromIntPtr(IntPtr srcRef)
	{
		object result;
		if (srcRef == IntPtr.Zero)
		{
			result = null;
		}
		else
		{
			object obj = null;
			switch (CFNumberGetType(srcRef))
			{
			case CFNumberType.kCFNumberSInt8Type:
			case CFNumberType.kCFNumberSInt16Type:
			case CFNumberType.kCFNumberSInt32Type:
			case CFNumberType.kCFNumberCharType:
			case CFNumberType.kCFNumberShortType:
			case CFNumberType.kCFNumberIntType:
			case CFNumberType.kCFNumberLongType:
			case CFNumberType.kCFNumberCFIndexType:
			case CFNumberType.kCFNumberNSIntegerType:
			{
				int valuePtr3 = 0;
				if (CFNumberGetValue_Int(srcRef, CFNumberType.kCFNumberIntType, ref valuePtr3))
				{
					obj = valuePtr3;
				}
				return obj;
			}
			case CFNumberType.kCFNumberSInt64Type:
			case CFNumberType.kCFNumberLongLongType:
			{
				long valuePtr2 = 0L;
				if (CFNumberGetValue_Long(srcRef, CFNumberType.kCFNumberSInt64Type, ref valuePtr2))
				{
					obj = valuePtr2;
				}
				break;
			}
			case CFNumberType.kCFNumberFloat32Type:
			case CFNumberType.kCFNumberFloatType:
			case CFNumberType.kCFNumberCGFloatType:
			{
				float valuePtr4 = 0f;
				if (CFNumberGetValue_Float(srcRef, CFNumberType.kCFNumberFloatType, ref valuePtr4))
				{
					obj = valuePtr4;
				}
				return obj;
			}
			case CFNumberType.kCFNumberFloat64Type:
			case CFNumberType.kCFNumberDoubleType:
			{
				double valuePtr = 0.0;
				if (CFNumberGetValue_Double(srcRef, CFNumberType.kCFNumberDoubleType, ref valuePtr))
				{
					obj = valuePtr;
				}
				return obj;
			}
			}
			result = obj;
		}
		return result;
	}

	public static string ReadCFStringFromIntPtr(IntPtr srcRef)
	{
		if (srcRef == IntPtr.Zero)
		{
			return null;
		}
		return CFString.FetchString(srcRef);
	}

	public static object ReadPlist_managed(string fileOnPC)
	{
		object result = null;
		try
		{
			IntPtr intPtr = IntPtr.Zero;
			int num = 0;
			using (FileStream fileStream = new FileStream(fileOnPC, FileMode.Open, FileAccess.Read))
			{
				num = (int)fileStream.Length;
				intPtr = Marshal.AllocCoTaskMem(num);
				byte[] array = new byte[num];
				fileStream.Read(array, 0, array.Length);
				Marshal.Copy(array, 0, intPtr, array.Length);
			}
			IntPtr zero = IntPtr.Zero;
			IntPtr srcRef = IntPtr.Zero;
			IntPtr errorString = IntPtr.Zero;
			zero = CFDataCreate(kCFAllocatorDefault, intPtr, num);
			if (zero != IntPtr.Zero)
			{
				srcRef = CFPropertyListCreateFromXMLData(kCFAllocatorDefault, zero, CFPropertyListMutabilityOptions.kCFPropertyListImmutable, ref errorString);
				if (srcRef == IntPtr.Zero)
				{
					return null;
				}
			}
			if (zero != IntPtr.Zero)
			{
				try
				{
					CFRelease(zero);
				}
				catch
				{
				}
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(intPtr);
			}
			result = ManagedTypeFromCFType(ref srcRef);
			if (srcRef != IntPtr.Zero)
			{
				CFRelease(srcRef);
			}
		}
		catch
		{
		}
		return result;
	}

	public static string SearchXmlByKey(string xmlText, string xmlKey)
	{
		string text = "";
		int num = Strings.InStr(xmlText, xmlKey, (CompareMethod)0);
		if (num > 0)
		{
			int num2 = Strings.InStr(num, xmlText, "<string>", (CompareMethod)0);
			if (num2 > num)
			{
				num2 += "<string>".Length;
				num = Strings.InStr(num2, xmlText, "</string>", (CompareMethod)0);
				if (num > num2)
				{
					text = Strings.Mid(xmlText, num2, num - num2);
				}
			}
		}
		return text.Trim();
	}

	public static IntPtr StringToCFString(string values)
	{
		IntPtr result = IntPtr.Zero;
		if (values != null)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(values);
			IntPtr bytes2 = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
			IntPtr intPtr = CFDataCreate(IntPtr.Zero, bytes2, bytes.Length);
			result = CFStringCreateFromExternalRepresentation(IntPtr.Zero, intPtr, (CFStringEncoding)134217984);
			if (intPtr != IntPtr.Zero)
			{
				CFRelease(intPtr);
			}
		}
		return result;
	}

	public static IntPtr StringToHeap(string src, Encoding encode)
	{
		if (string.IsNullOrEmpty(src))
		{
			return IntPtr.Zero;
		}
		if (encode == null)
		{
			return Marshal.StringToCoTaskMemAnsi(src);
		}
		int maxByteCount = encode.GetMaxByteCount(1);
		char[] array = src.ToCharArray();
		int num = encode.GetByteCount(array) + maxByteCount;
		byte[] array2 = new byte[0];
		array2 = new byte[num];
		if (encode.GetBytes(array, 0, array.Length, array2, 0) != array2.Length - maxByteCount)
		{
			throw new NotSupportedException("StringToHeap编码不支持");
		}
		IntPtr intPtr = Marshal.AllocCoTaskMem(array2.Length);
		if (intPtr == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		bool flag = false;
		try
		{
			Marshal.Copy(array2, 0, intPtr, array2.Length);
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				Marshal.FreeCoTaskMem(intPtr);
			}
		}
		return intPtr;
	}

	public static bool WritePlist(IntPtr dataPtr, string fileOnPC)
	{
		bool flag = false;
		bool result;
		if (dataPtr == IntPtr.Zero)
		{
			result = false;
		}
		else
		{
			IntPtr zero = IntPtr.Zero;
			try
			{
				zero = CFURLCreateWithFileSystemPath(kCFAllocatorDefault, StringToCFString(fileOnPC), CFURLPathStyle.kCFURLWindowsPathStyle, isDirectory: false);
				if (zero != IntPtr.Zero)
				{
					IntPtr intPtr = CFPropertyListCreateXMLData(kCFAllocatorDefault, dataPtr);
					if (intPtr != IntPtr.Zero)
					{
						IntPtr zero2 = IntPtr.Zero;
						int errorCode = -1;
						flag = CFURLWriteDataAndPropertiesToResource(zero, intPtr, zero2, ref errorCode);
						CFRelease(intPtr);
					}
					else
					{
						flag = false;
					}
					CFRelease(zero);
					return flag;
				}
				flag = false;
			}
			catch (Exception)
			{
			}
			result = flag;
		}
		return result;
	}

	public static bool WritePlist(object dict, string fileOnPC)
	{
		return WritePlist(CFTypeFromManagedType(dict), fileOnPC);
	}
}
