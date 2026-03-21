using System;
using System.IO;
using Microsoft.Win32;

namespace Titan.Helper;

public class DLLHelper
{
	public static string GetiTunesMobileDeviceDllPath()
	{
		RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Apple Inc.\\Apple Mobile Device Support\\Shared");
		if (registryKey != null)
		{
			string text = registryKey.GetValue("MobileDeviceDLL") as string;
			if (!string.IsNullOrWhiteSpace(text))
			{
				FileInfo fileInfo = new FileInfo(text);
				if (fileInfo.Exists)
				{
					return fileInfo.DirectoryName;
				}
			}
		}
		string text2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + "\\Apple\\Mobile Device Support";
		string result;
		if (File.Exists(text2 + "\\MobileDevice.dll"))
		{
			result = text2;
		}
		else
		{
			text2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86) + "\\Apple\\Mobile Device Support";
			result = ((!File.Exists(text2 + "\\MobileDevice.dll")) ? string.Empty : text2);
		}
		return result;
	}

	public static string GetAppleApplicationSupportFolder()
	{
		RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Apple Inc.\\Apple Mobile Device Support");
		if (registryKey != null)
		{
			string text = registryKey.GetValue("InstallDir") as string;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		string text2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + "\\Apple\\Mobile Device Support";
		string result;
		if (File.Exists(text2 + "\\CoreFoundation.dll"))
		{
			result = text2;
		}
		else
		{
			text2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86) + "\\Apple\\Mobile Device Support";
			result = ((!File.Exists(text2 + "\\CoreFoundation.dll")) ? string.Empty : text2);
		}
		return result;
	}
}
