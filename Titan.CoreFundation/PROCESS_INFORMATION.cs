using System;

namespace Titan.CoreFundation;

public struct PROCESS_INFORMATION
{
	public IntPtr hProcess;

	public IntPtr hThread;

	public int dwProcessId;

	public int dwThreadId;

	public bool IsEmpty => hProcess == IntPtr.Zero && hThread == IntPtr.Zero && dwProcessId == 0 && dwThreadId == 0;
}
