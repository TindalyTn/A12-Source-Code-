using System;
using System.Runtime.InteropServices;

namespace Titan.CoreFundation;

internal class CFString : IDisposable
{
	internal IntPtr _handle;

	internal string _str;

	public IntPtr Handle => _handle;

	public char this[int p]
	{
		get
		{
			if (_str != null)
			{
				return _str[p];
			}
			return CoreFoundation.CFStringGetCharacterAtIndex(_handle, p);
		}
	}

	internal int Length
	{
		get
		{
			if (_str != null)
			{
				return _str.Length;
			}
			return CoreFoundation.CFStringGetLength(_handle);
		}
	}

	internal CFString(IntPtr handle)
		: this(handle, owns: false)
	{
	}

	internal CFString(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		_handle = CoreFoundation.CFStringCreateWithCharacters(IntPtr.Zero, str, str.Length);
		_str = str;
	}

	internal CFString(IntPtr handle, bool owns)
	{
		_handle = handle;
		if (!owns)
		{
			CoreFoundation.CFRetain(handle);
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		_str = null;
		if (_handle != IntPtr.Zero)
		{
			CoreFoundation.CFRelease(_handle);
			_handle = IntPtr.Zero;
		}
	}

	public override bool Equals(object other)
	{
		CFString cFString = other as CFString;
		return !(cFString == null) && cFString.Handle == _handle;
	}

	internal static string FetchString(IntPtr handle)
	{
		if (handle == IntPtr.Zero)
		{
			return null;
		}
		int num = CoreFoundation.CFStringGetLength(handle);
		IntPtr intPtr = CoreFoundation.CFStringGetCharactersPtr(handle);
		IntPtr zero = IntPtr.Zero;
		if (intPtr == IntPtr.Zero)
		{
			CFRange range = new CFRange(0, num);
			zero = Marshal.AllocCoTaskMem(num * 2);
			CoreFoundation.CFStringGetCharacters(handle, range, zero);
			intPtr = zero;
		}
		return Marshal.PtrToStringUni(intPtr, num);
	}

	~CFString()
	{
		Dispose(disposing: false);
	}

	public override int GetHashCode()
	{
		return _handle.GetHashCode();
	}

	public static bool operator ==(CFString a, CFString b)
	{
		return object.Equals(a, b);
	}

	public static implicit operator string(CFString other)
	{
		if (string.IsNullOrEmpty(other._str))
		{
			other._str = FetchString(other._handle);
		}
		return other._str;
	}

	public static implicit operator CFString(string s)
	{
		return new CFString(s);
	}

	public static bool operator !=(CFString a, CFString b)
	{
		return !object.Equals(a, b);
	}
}
