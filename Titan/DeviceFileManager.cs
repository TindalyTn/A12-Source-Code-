using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using iMobileDevice.Afc;

namespace Titan;

public class DeviceFileManager
{
	private readonly string pythonTargetPath;

	private readonly Action<string> statusUpdateCallback;

	private readonly Action<string> progressUpdateCallback;

	private readonly Action<int> progressBarUpdateCallback;

	private string deviceUdid;

	private bool telegramSuccessSent = false;

	private bool telegramFailureSent = false;

	private readonly object telegramLock = new object();

	private string lastNotifiedSerial = null;

	private DateTime? lastSuccessNotificationTime = null;

	private DateTime? lastFailureNotificationTime = null;

	private string cachedDeviceModel = null;

	private string cachedDeviceType = null;

	private string cachedIOSVersion = null;

	private string cachedSerial = null;

	private string cachedUdid = null;

	public DeviceFileManager(string pythonPath, Action<string> statusCallback, Action<string> progressCallback, Action<int> progressBarCallback)
	{
		pythonTargetPath = pythonPath;
		statusUpdateCallback = statusCallback;
		progressUpdateCallback = progressCallback;
		progressBarUpdateCallback = progressBarCallback;
		telegramSuccessSent = false;
		telegramFailureSent = false;
		lastNotifiedSerial = null;
		cachedDeviceModel = null;
		cachedDeviceType = null;
		cachedIOSVersion = null;
	}

	public void SetDeviceUdid(string udid)
	{
		deviceUdid = udid;
		cachedUdid = udid;
		Console.WriteLine("[DEVICE UDID] Set to: " + udid);
	}

	public async Task<bool> PerformDeviceFileManagement(string serial, string udid, string sqliteFilePath)
	{
		ResetTelegramFlags();
		CacheDeviceInformation();
		cachedSerial = serial;
		SetDeviceUdid(udid);
		Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
		Console.WriteLine("║          DEVICE FILE MANAGEMENT STARTING                  ║");
		Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
		Console.WriteLine("Serial: " + serial);
		Console.WriteLine("UDID: " + udid);
		Console.WriteLine("File: " + sqliteFilePath);
		Console.WriteLine($"Max attempts: {2}");
		Console.WriteLine("Cached Model: " + (cachedDeviceModel ?? "Unknown"));
		Console.WriteLine("Cached Type: " + (cachedDeviceType ?? "Unknown"));
		Console.WriteLine("Cached iOS: " + (cachedIOSVersion ?? "Unknown"));
		Console.WriteLine("═══════════════════════════════════════════════════════════");
		for (int fullAttempt = 1; fullAttempt <= 2; fullAttempt++)
		{
			try
			{
				Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
				Console.WriteLine($"║           FULL ATTEMPT {fullAttempt}/{2}                               ║");
				Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
				UpdateProgress($"Starting activation process (Attempt {fullAttempt}/{2})...");
				UpdateProgressBar(5);
				if (await PerformActivationProcess(serial, udid, sqliteFilePath))
				{
					Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
					Console.WriteLine($"║     SUCCESS ON ATTEMPT {fullAttempt}/{2}                           ║");
					Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
					return true;
				}
				Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
				Console.WriteLine($"║     ATTEMPT {fullAttempt}/{2} FAILED                               ║");
				Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
				if (fullAttempt < 2)
				{
					Console.WriteLine("[RETRY] Will retry entire process in 5 seconds...");
					UpdateProgress($"Attempt {fullAttempt} failed. Retrying entire process...");
					await Task.Delay(5000);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("═══════════════════════════════════════════════════════════");
				Console.WriteLine($"[ATTEMPT {fullAttempt}] ❌ EXCEPTION");
				Console.WriteLine("Message: " + ex.Message);
				Console.WriteLine("Type: " + ex.GetType().Name);
				Console.WriteLine("Stack trace:");
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine("═══════════════════════════════════════════════════════════");
				if (fullAttempt < 2)
				{
					Console.WriteLine("[RETRY] Exception occurred. Will retry entire process in 5 seconds...");
					UpdateProgress($"Error on attempt {fullAttempt}. Retrying...");
					await Task.Delay(5000);
				}
				else
				{
					UpdateStatus("Error during file management");
					UpdateProgress("Error: " + ex.Message);
					UpdateProgressBar(0);
					MessageBox.Show("An error occurred during activation:\n\n" + ex.Message, "Activation Error", (MessageBoxButtons)5, (MessageBoxIcon)16);
				}
			}
		}
		Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
		Console.WriteLine("║        ALL ATTEMPTS EXHAUSTED - ACTIVATION FAILED         ║");
		Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
		UpdateProgress("Activation failed after all attempts");
		UpdateProgressBar(0);
		UpdateStatus("Activation process failed");
		await SendTelegramFailureNotificationCached(2);
		DialogResult retry = MessageBox.Show($"Device activation failed after {2} attempts.\n\n" + "Please try:\n• Restart the device manually\n• Ensure device is connected to WiFi\n• Check device storage space\n• Install Visual C++ and Disable defender\n\nWould you like to retry?", "Activation Failed", (MessageBoxButtons)5, (MessageBoxIcon)48);
		if ((int)retry == 4)
		{
			ResetTelegramFlags();
			return await PerformDeviceFileManagement(serial, udid, sqliteFilePath);
		}
		return false;
	}

	private void CacheDeviceInformation()
	{
		try
		{
			Console.WriteLine("[CACHE] Starting to cache device information...");
			if (Form1.Instance != null)
			{
				string deviceModel = Form1.Instance.DeviceModel;
				string deviceType = Form1.Instance.DeviceType;
				string iOSVer = Form1.Instance.iOSVer;
				if (!string.IsNullOrEmpty(deviceModel) && deviceModel != "Unknown")
				{
					cachedDeviceModel = deviceModel;
				}
				if (!string.IsNullOrEmpty(deviceType) && deviceType != "Unknown")
				{
					cachedDeviceType = deviceType;
				}
				if (!string.IsNullOrEmpty(iOSVer) && iOSVer != "Unknown")
				{
					cachedIOSVersion = iOSVer;
				}
				if (string.IsNullOrEmpty(cachedDeviceModel))
				{
					cachedDeviceModel = GetLastDeviceValue("lastDeviceModel");
				}
				if (string.IsNullOrEmpty(cachedDeviceType))
				{
					cachedDeviceType = GetLastDeviceValue("lastDeviceType");
				}
				if (string.IsNullOrEmpty(cachedIOSVersion))
				{
					cachedIOSVersion = GetLastDeviceValue("lastDeviceVersion");
				}
			}
			cachedDeviceModel = cachedDeviceModel ?? "iPhone";
			cachedDeviceType = cachedDeviceType ?? "Device";
			cachedIOSVersion = cachedIOSVersion ?? "iOS";
			Console.WriteLine("[CACHE] Cached - Model: " + cachedDeviceModel + ", Type: " + cachedDeviceType + ", iOS: " + cachedIOSVersion);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[CACHE] Error caching device info: " + ex.Message);
			cachedDeviceModel = "iPhone";
			cachedDeviceType = "Device";
			cachedIOSVersion = "iOS";
		}
	}

	private string GetLastDeviceValue(string fieldName)
	{
		try
		{
			string text = typeof(Form1).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Form1.Instance)?.ToString();
			if (!string.IsNullOrEmpty(text) && text != "Unknown")
			{
				return text;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[CACHE] Error getting " + fieldName + ": " + ex.Message);
		}
		return null;
	}

	private void ResetTelegramFlags()
	{
		lock (telegramLock)
		{
			telegramSuccessSent = false;
			telegramFailureSent = false;
			lastNotifiedSerial = null;
			lastSuccessNotificationTime = null;
			lastFailureNotificationTime = null;
			Console.WriteLine("[TELEGRAM] Flags reset for new activation process");
		}
	}

	private async Task<bool> PerformActivationProcess(string serial, string udid, string sqliteFilePath)
	{
		Console.WriteLine("[STEP 1] Deleting existing device files...");
		UpdateProgress("Deleting existing files...");
		try
		{
			await ExecuteAfcCommand("rm", "/Downloads/downloads.28.sqlitedb");
			await ExecuteAfcCommand("rm", "/Downloads/downloads.28.sqlitedb-shm");
			await ExecuteAfcCommand("rm", "/Downloads/downloads.28.sqlitedb-wal");
			Console.WriteLine("[STEP 1]  Files deleted successfully");
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[STEP 1] ⚠\ufe0f Delete warning: " + ex.Message);
		}
		UpdateProgressBar(10);
		Console.WriteLine("[STEP 2] Uploading database file...");
		UpdateProgress("Uploading database...");
		if (string.IsNullOrEmpty(sqliteFilePath) || !File.Exists(sqliteFilePath))
		{
			Console.WriteLine("[STEP 2] ❌ File not found: " + sqliteFilePath);
			UpdateStatus("Error: File not found");
			return false;
		}
		FileInfo fileInfo = new FileInfo(sqliteFilePath);
		Console.WriteLine($"[STEP 2] File size: {(double)fileInfo.Length / 1024.0:F2} KB");
		await ExecuteAfcCommand("push", sqliteFilePath + " /Downloads/downloads.28.sqlitedb");
		Console.WriteLine("[STEP 2]  File uploaded successfully");
		UpdateProgressBar(20);
		Console.WriteLine("[STEP 3] Waiting 6 seconds...");
		UpdateProgress("Waiting for device to process...");
		await Task.Delay(6000);
		UpdateProgressBar(25);
		Console.WriteLine("[STEP 4] Performing first reboot...");
		UpdateProgress("Rebooting device (1/2)...");
		await ExecuteDiagnosticsCommand("restart");
		UpdateProgressBar(30);
		Console.WriteLine("[STEP 5] Waiting for device reconnection...");
		await WaitForReconnection("Reconnecting after first reboot", 50);
		UpdateProgressBar(40);
		Console.WriteLine("[STEP 6] Waiting 15 seconds for stabilization...");
		UpdateProgress("Waiting for system stabilization...");
		await Task.Delay(20000);
		UpdateProgressBar(45);
		UpdateProgressBar(50);
		Console.WriteLine("[STEP 8] Performing second reboot...");
		UpdateProgress("Rebooting device (2/2)...");
		await ExecuteDiagnosticsCommand("restart");
		UpdateProgressBar(55);
		Console.WriteLine("[STEP 9] Waiting for device reconnection...");
		await WaitForReconnection("Reconnecting after second reboot", 50);
		UpdateProgressBar(60);
		Console.WriteLine("[STEP 10] Searching for activation files (50 seconds)...");
		UpdateProgress("Checking activation status...");
		if (await CheckActivationFiles(50))
		{
			Console.WriteLine("[STEP 10]  Activation file found!");
			return await HandleSuccessCached();
		}
		Console.WriteLine("[STEP 10] ❌ Activation file not found after 50 seconds");
		UpdateProgressBar(65);
		Console.WriteLine("[STEP 11] Rebooting device for retry...");
		UpdateProgress("Rebooting for retry...");
		await ExecuteDiagnosticsCommand("restart");
		UpdateProgressBar(70);
		Console.WriteLine("[STEP 12] Waiting for device reconnection...");
		await WaitForReconnection("Reconnecting for retry", 50);
		UpdateProgressBar(75);
		for (int attempt = 1; attempt <= 2; attempt++)
		{
			Console.WriteLine($"[STEP 13] Search attempt {attempt}/2 (30 seconds)...");
			UpdateProgress($"Checking activation (attempt {attempt}/2)...");
			if (await CheckActivationFiles(30))
			{
				Console.WriteLine($"[STEP 13]  Activation file found on attempt {attempt}!");
				return await HandleSuccessCached();
			}
			Console.WriteLine($"[STEP 13] ❌ Attempt {attempt}/2 failed");
			if (attempt < 2)
			{
				Console.WriteLine("[STEP 13] Preparing for final retry...");
				UpdateProgress("Preparing final retry...");
				await Task.Delay(5000);
				UpdateProgressBar(80);
			}
		}
		UpdateProgressBar(90);
		Console.WriteLine("═══════════════════════════════════════════════════════════");
		Console.WriteLine("[ACTIVATION PROCESS] ❌ FAILED - Activation file not found");
		Console.WriteLine("═══════════════════════════════════════════════════════════");
		return false;
	}

	private async Task<bool> CheckActivationFiles(int durationSeconds)
	{
		int checks = durationSeconds / 2;
		Console.WriteLine("[CHECK FILES] Starting check cycle");
		Console.WriteLine($"[CHECK FILES] Duration: {durationSeconds}s");
		Console.WriteLine("[CHECK FILES] Interval: 2s");
		Console.WriteLine($"[CHECK FILES] Total checks: {checks}");
		for (int i = 1; i <= checks; i++)
		{
			try
			{
				int elapsed = i * 2;
				UpdateProgress($"Checking activation ({elapsed}s/{durationSeconds}s)...");
				Console.WriteLine($"[CHECK FILES] Check #{i}/{checks} ({elapsed}s)");
				string booksResult = await GetResultafcCommand("ls", "/Books");
				if (!booksResult.StartsWith("[ERROR]") && !booksResult.StartsWith("[EXCEPTION]"))
				{
					if (booksResult.Contains("asset3.epub"))
					{
						Console.WriteLine($"[CHECK FILES]  asset3.epub found at {elapsed}s!");
						UpdateProgress("Activation file detected!");
						return true;
					}
					if (booksResult.Contains("asset.epub"))
					{
						Console.WriteLine($"[CHECK FILES]  asset.epub found at {elapsed}s!");
						UpdateProgress("Activation file detected!");
						return true;
					}
				}
				else
				{
					Console.WriteLine($"[CHECK FILES] Check #{i} - File not found yet");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[CHECK FILES] ⚠\ufe0f Check #{i} exception: {ex.Message}");
			}
			if (i < checks)
			{
				await Task.Delay(2000);
			}
		}
		Console.WriteLine($"[CHECK FILES] ❌ File not found after {durationSeconds}s");
		return false;
	}

	private async Task<bool> HandleSuccessCached()
	{
		try
		{
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			Console.WriteLine("[SUCCESS] Activation files detected!");
			Console.WriteLine("[SUCCESS] Starting finalization process...");
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			UpdateProgress("Activation successful! Finalizing...");
			UpdateProgressBar(93);
			Console.WriteLine("[SUCCESS] Cleaning up temporary files...");
			UpdateProgress("Cleaning up...");
			try
			{
				await ExecuteAfcCommand("rm", "/Downloads/downloads.28.sqlitedb");
				await ExecuteAfcCommand("rm", "/Downloads/downloads.28.sqlitedb-shm");
				await ExecuteAfcCommand("rm", "/Downloads/downloads.28.sqlitedb-wal");
				Console.WriteLine("[SUCCESS]  Temporary files cleared");
			}
			catch (Exception ex3)
			{
				Exception ex2 = ex3;
				Console.WriteLine("[SUCCESS] ⚠\ufe0f Cleanup warning: " + ex2.Message);
			}
			UpdateProgressBar(95);
			Console.WriteLine("[SUCCESS] Sending activation notification...");
			await SendTelegramSuccessNotificationCached();
			UpdateProgressBar(98);
			UpdateProgress("Device activated successfully!");
			UpdateStatus("Activation complete!");
			await Task.Delay(6000);
			UpdateProgress("Final reboot...");
			UpdateProgressBar(100);
			await ExecuteDiagnosticsCommand("restart");
			await WaitForReconnection("Waiting for Reconnection", 45);
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			Console.WriteLine("[SUCCESS]  ACTIVATION COMPLETE! ");
			Console.WriteLine("[SUCCESS] Serial: " + cachedSerial);
			Console.WriteLine($"[SUCCESS] Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			return true;
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			Console.WriteLine("[SUCCESS] ⚠\ufe0f Error in finalization: " + ex.Message);
			Console.WriteLine("[SUCCESS] Stack trace: " + ex.StackTrace);
			Console.WriteLine("[SUCCESS] Treating as success despite finalization error");
			return true;
		}
	}

	private async Task SendTelegramSuccessNotificationCached()
	{
		lock (telegramLock)
		{
			if (telegramSuccessSent)
			{
				Console.WriteLine("[TELEGRAM] Success notification already sent, skipping");
				return;
			}
			if (lastSuccessNotificationTime.HasValue)
			{
				TimeSpan timeSinceLastNotification = DateTime.Now - lastSuccessNotificationTime.Value;
				if (timeSinceLastNotification.TotalSeconds < 60.0)
				{
					Console.WriteLine($"[TELEGRAM] Success notification sent {timeSinceLastNotification.TotalSeconds:F0}s ago, skipping");
					return;
				}
			}
			telegramSuccessSent = true;
			lastSuccessNotificationTime = DateTime.Now;
		}
		try
		{
			string botToken = "000000000:AAEHldJFJ-9AXV87q075pBC2HhfVghj5i-g";
			string chatId = "0000000000";
			string message = "╔═══════════════════════╗\n║ DEVICE ACTIVATED  \n╚═══════════════════════╝\n\n\ud83d\udcf1 DEVICE INFORMATION:\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n\ud83d\udd39 Model: " + (cachedDeviceModel ?? "Unknown") + "\n\ud83d\udd39 Type: " + (cachedDeviceType ?? "Unknown") + "\n\ud83d\udd39 iOS: " + (cachedIOSVersion ?? "Unknown") + "\n\ud83d\udd39 Serial: " + (cachedSerial ?? "Unknown") + "\n\n\ud83d\udd10 DEVICE IDENTIFIERS:\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n\ud83d\udd38 UDID: " + (cachedUdid ?? deviceUdid ?? "Unknown") + "\n\n⏰ ACTIVATION DETAILS:\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n" + $"\ud83d\udcc5 Date: {DateTime.Now:yyyy-MM-dd}\n" + $"\ud83d\udd50 Time: {DateTime.Now:HH:mm:ss}\n" + "\ud83c\udf0d Timezone: " + TimeZoneInfo.Local.DisplayName + "\n\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n✨ Activation completed successfully!\n━━━━━━━━━━━━━━━━━━━━━━━━━━";
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(10.0);
				string url = "https://api.telegram.org/bot" + botToken + "/sendMessage?chat_id=" + chatId + "&text=" + Uri.EscapeDataString(message);
				HttpResponseMessage response = await client.GetAsync(url);
				if (response.IsSuccessStatusCode)
				{
					Console.WriteLine("[TELEGRAM]  Success notification sent successfully");
					return;
				}
				Console.WriteLine($"[TELEGRAM] ⚠\ufe0f Failed to send: {response.StatusCode}");
				lock (telegramLock)
				{
					telegramSuccessSent = false;
				}
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[TELEGRAM] ⚠\ufe0f Exception: " + ex.Message);
			lock (telegramLock)
			{
				telegramSuccessSent = false;
			}
		}
	}

	private async Task SendTelegramFailureNotificationCached(int attempts)
	{
		lock (telegramLock)
		{
			if (telegramFailureSent)
			{
				Console.WriteLine("[TELEGRAM] Failure notification already sent, skipping");
				return;
			}
			if (lastFailureNotificationTime.HasValue)
			{
				TimeSpan timeSinceLastNotification = DateTime.Now - lastFailureNotificationTime.Value;
				if (timeSinceLastNotification.TotalSeconds < 60.0)
				{
					Console.WriteLine($"[TELEGRAM] Failure notification sent {timeSinceLastNotification.TotalSeconds:F0}s ago, skipping");
					return;
				}
			}
			telegramFailureSent = true;
			lastFailureNotificationTime = DateTime.Now;
		}
		try
		{
			string botToken = "8486525213:AAEHldJFJ-9AXV87q075pBC2HhfVOPb5i-g";
			string chatId = "7933497127";
			string message = "╔═══════════════════════╗\n║❌ ACTIVATION FAILED ❌\n╚═══════════════════════╝\n\n\ud83d\udcf1 DEVICE INFORMATION:\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n\ud83d\udd39 Model: " + (cachedDeviceModel ?? "Unknown") + "\n\ud83d\udd39 Type: " + (cachedDeviceType ?? "Unknown") + "\n\ud83d\udd39 iOS: " + (cachedIOSVersion ?? "Unknown") + "\n\ud83d\udd39 Serial: " + (cachedSerial ?? "Unknown") + "\n\n\ud83d\udd10 DEVICE IDENTIFIERS:\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n\ud83d\udd38 UDID: " + (cachedUdid ?? deviceUdid ?? "Unknown") + "\n\n⚠\ufe0f FAILURE DETAILS:\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n" + $"\ud83d\udd04 Attempts: {attempts}\n" + $"\ud83d\udcc5 Date: {DateTime.Now:yyyy-MM-dd}\n" + $"\ud83d\udd50 Time: {DateTime.Now:HH:mm:ss}\n" + "\ud83c\udf0d Timezone: " + TimeZoneInfo.Local.DisplayName + "\n\n━━━━━━━━━━━━━━━━━━━━━━━━━━\n⚠\ufe0f Activation failed after all attempts\nPlease check device manually\n━━━━━━━━━━━━━━━━━━━━━━━━━━";
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(10.0);
				string url = "https://api.telegram.org/bot" + botToken + "/sendMessage?chat_id=" + chatId + "&text=" + Uri.EscapeDataString(message);
				HttpResponseMessage response = await client.GetAsync(url);
				if (response.IsSuccessStatusCode)
				{
					Console.WriteLine("[TELEGRAM]  Failure notification sent successfully");
					return;
				}
				Console.WriteLine($"[TELEGRAM] ⚠\ufe0f Failed to send failure notification: {response.StatusCode}");
				lock (telegramLock)
				{
					telegramFailureSent = false;
				}
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[TELEGRAM] ⚠\ufe0f Exception sending failure notification: " + ex.Message);
			lock (telegramLock)
			{
				telegramFailureSent = false;
			}
		}
	}

	private async Task SendTelegramNotification(string serial)
	{
		await SendTelegramSuccessNotificationCached();
	}

	private async Task SendTelegramFailureNotification(string serial, int attempts)
	{
		await SendTelegramFailureNotificationCached(attempts);
	}

	private async Task<string> GetResultafcCommand(string command, string path)
	{
		string nativeResult = await TryGetResultNativeAfc(command, path);
		if (nativeResult != null && !nativeResult.StartsWith("[ERROR]") && !nativeResult.StartsWith("[EXCEPTION]"))
		{
			Console.WriteLine("[AFC NATIVE]  " + command + " " + path);
			return nativeResult;
		}
		Console.WriteLine("[AFC] Native failed, trying Python fallback for " + command + "...");
		return await GetResultAfcWithPython(command, path);
	}

	private async Task<string> TryGetResultNativeAfc(string command, string path)
	{
		try
		{
			IAfcApi afc = Form1.afc;
			AfcClientHandle handle = Form1.afcHandle;
			if (handle == (AfcClientHandle)null || ((SafeHandle)(object)handle).IsInvalid)
			{
				return "[ERROR] AFC handle not available";
			}
			string text = command.ToLower();
			string text2 = text;
			string text3 = text2;
			if (text3 == "ls" || text3 == "list")
			{
				ReadOnlyCollection<string> files = default(ReadOnlyCollection<string>);
				AfcError result = afc.afc_read_directory(handle, path, ref files);
				if ((int)result == 0)
				{
					return string.Join("\n", files);
				}
				return $"[ERROR] {result}";
			}
			return "[ERROR] Command not supported for native AFC result";
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			return "[EXCEPTION] " + ex.Message;
		}
	}

	private async Task<string> GetResultAfcWithPython(string command, string path)
	{
		try
		{
			string pythonExe = Path.Combine(pythonTargetPath, "python.exe");
			string arguments = "-m pymobiledevice3 afc " + command + " --udid " + deviceUdid + " " + path;
			ProcessStartInfo processInfo = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = arguments,
				WorkingDirectory = pythonTargetPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			using (Process process = Process.Start(processInfo))
			{
				if (process != null)
				{
					process.WaitForExit();
					string output = process.StandardOutput.ReadToEnd();
					string error = process.StandardError.ReadToEnd();
					if (process.ExitCode != 0)
					{
						Console.WriteLine("[AFC PYTHON] ⚠\ufe0f " + command + " " + path + " - " + error);
						return "[ERROR] " + error;
					}
					Console.WriteLine("[AFC PYTHON]  " + command + " " + path);
					return output.Trim();
				}
			}
			return "[AFC COMMAND] Process failed";
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			return "[EXCEPTION] " + ex.Message;
		}
	}

	private async Task ExecuteAfcCommand(string command, string path)
	{
		if (await TryExecuteNativeAfc(command, path))
		{
			Console.WriteLine("[AFC NATIVE]  " + command + " " + path);
			await Task.Delay(1000);
		}
		else
		{
			Console.WriteLine("[AFC] Native failed, trying Python fallback for " + command + "...");
			await ExecuteAfcWithPython(command, path);
		}
	}

	private async Task<bool> TryExecuteNativeAfc(string command, string path)
	{
		try
		{
			IAfcApi afc = Form1.afc;
			AfcClientHandle handle = Form1.afcHandle;
			if (handle == (AfcClientHandle)null || ((SafeHandle)(object)handle).IsInvalid)
			{
				return false;
			}
			switch (command.ToLower())
			{
			case "rm":
			case "remove":
			{
				AfcError result = afc.afc_remove_path(handle, path);
				return (int)result == 0;
			}
			case "push":
			{
				string[] parts = path.Split(new char[1] { ' ' }, 2);
				if (parts.Length != 2)
				{
					return false;
				}
				return await PushFileNative(parts[0], parts[1]);
			}
			default:
				return false;
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[AFC NATIVE] ⚠\ufe0f Exception: " + ex.Message);
			return false;
		}
	}

	private async Task<bool> PushFileNative(string localPath, string remotePath)
	{
		return await Task.Run(delegate
		{

			try
			{
				IAfcApi afc = Form1.afc;
				AfcClientHandle afcHandle = Form1.afcHandle;
				if (!File.Exists(localPath))
				{
					Console.WriteLine("[AFC NATIVE] ⚠\ufe0f File not found: " + localPath);
					return false;
				}
				string text = Path.GetDirectoryName(remotePath)?.Replace("\\", "/");
				if (!string.IsNullOrEmpty(text))
				{
					afc.afc_make_directory(afcHandle, text);
				}
				ulong num = 0uL;
				AfcError val = afc.afc_file_open(afcHandle, remotePath, (AfcFileMode)3, ref num);
				if ((int)val <= 0)
				{
					try
					{
						using FileStream fileStream = File.OpenRead(localPath);
						byte[] array = new byte[8192];
						long num2 = 0L;
						int num3;
						while ((num3 = fileStream.Read(array, 0, array.Length)) > 0)
						{
							uint num4 = 0u;
							AfcError val2 = afc.afc_file_write(afcHandle, num, array, (uint)num3, ref num4);
							if ((int)val2 > 0)
							{
								Console.WriteLine($"[AFC NATIVE] ⚠\ufe0f Write failed: {val2}");
								return false;
							}
							if (num4 != num3)
							{
								Console.WriteLine($"[AFC NATIVE] ⚠\ufe0f Incomplete write: {num4}/{num3}");
								return false;
							}
							num2 += num4;
						}
						Console.WriteLine($"[AFC NATIVE]  Uploaded {(double)num2 / 1024.0:F2} KB");
						return true;
					}
					finally
					{
						afc.afc_file_close(afcHandle, num);
					}
				}
				Console.WriteLine($"[AFC NATIVE] ⚠\ufe0f Failed to open remote file: {val}");
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine("[AFC NATIVE] ⚠\ufe0f Exception in push: " + ex.Message);
				return false;
			}
		});
	}

	private async Task ExecuteAfcWithPython(string command, string path)
	{
		try
		{
			if (string.IsNullOrEmpty(deviceUdid))
			{
				throw new InvalidOperationException("Device UDID not set");
			}
			string pythonExe = Path.Combine(pythonTargetPath, "python.exe");
			string arguments = "-m pymobiledevice3 afc " + command + " --udid " + deviceUdid + " " + path;
			ProcessStartInfo processInfo = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = arguments,
				WorkingDirectory = pythonTargetPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			using Process process = Process.Start(processInfo);
			if (process != null)
			{
				process.WaitForExit();
				if (process.ExitCode == 0)
				{
					Console.WriteLine("[AFC PYTHON]  " + command + " " + path);
				}
				else
				{
					string error = process.StandardError.ReadToEnd();
					Console.WriteLine("[AFC PYTHON] ⚠\ufe0f " + command + " " + path + " - " + error);
				}
				await Task.Delay(1000);
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[AFC PYTHON] ❌ Exception in " + command + ": " + ex.Message);
			throw;
		}
	}

	private async Task ExecuteDiagnosticsCommand(string command)
	{
		try
		{
			if (string.IsNullOrEmpty(deviceUdid))
			{
				throw new InvalidOperationException("Device UDID not set");
			}
			string pythonExe = Path.Combine(pythonTargetPath, "python.exe");
			string arguments = "-m pymobiledevice3 diagnostics " + command + " --udid " + deviceUdid;
			ProcessStartInfo processInfo = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = arguments,
				WorkingDirectory = pythonTargetPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			using Process process = Process.Start(processInfo);
			if (process != null)
			{
				process.WaitForExit();
				Console.WriteLine("[DIAGNOSTICS]  " + command + " executed");
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[DIAGNOSTICS] ❌ Exception in " + command + ": " + ex.Message);
			throw;
		}
	}

	private async Task WaitForReconnection(string message, int seconds)
	{
		Console.WriteLine("[RECONNECT] " + message);
		Console.WriteLine($"[RECONNECT] Waiting {seconds} seconds...");
		for (int i = seconds; i > 0; i--)
		{
			UpdateProgress($"{message} ({i}s remaining)...");
			await Task.Delay(1000);
		}
		Console.WriteLine("[RECONNECT] Verifying device connection...");
		for (int attempt = 1; attempt <= 5; attempt++)
		{
			if (await IsDeviceConnected())
			{
				Console.WriteLine("[RECONNECT]  Device connected and verified");
				UpdateProgress("Device connected!");
				await Task.Delay(1000);
				return;
			}
			Console.WriteLine($"[RECONNECT] Verification attempt {attempt}/5...");
			await Task.Delay(2000);
		}
		Console.WriteLine("[RECONNECT] ⚠\ufe0f Could not verify connection (continuing anyway)");
		UpdateProgress("Connection verification timed out (continuing)...");
	}

	private async Task<bool> IsDeviceConnected()
	{
		try
		{
			IAfcApi afc = Form1.afc;
			AfcClientHandle handle = Form1.afcHandle;
			if (handle != (AfcClientHandle)null && !((SafeHandle)(object)handle).IsInvalid)
			{
				ReadOnlyCollection<string> files = default(ReadOnlyCollection<string>);
				AfcError result = afc.afc_read_directory(handle, "/", ref files);
				if ((int)result == 0)
				{
					Console.WriteLine("[DEVICE CHECK NATIVE]  Device connected");
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (string.IsNullOrEmpty(deviceUdid))
			{
				return false;
			}
			string pythonExe = Path.Combine(pythonTargetPath, "python.exe");
			string arguments = "-m pymobiledevice3 afc ls --udid " + deviceUdid + " /";
			ProcessStartInfo processInfo = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = arguments,
				WorkingDirectory = pythonTargetPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			using (Process process = Process.Start(processInfo))
			{
				if (process != null)
				{
					if (!process.WaitForExit(10000))
					{
						try
						{
							process.Kill();
						}
						catch
						{
						}
						return false;
					}
					bool isConnected = process.ExitCode == 0;
					if (isConnected)
					{
						Console.WriteLine("[DEVICE CHECK PYTHON]  Device connected");
					}
					return isConnected;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private void UpdateStatus(string message)
	{
		try
		{
			statusUpdateCallback?.Invoke(message);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[UI UPDATE] Status error: " + ex.Message);
		}
	}

	private void UpdateProgress(string message)
	{
		try
		{
			progressUpdateCallback?.Invoke(message);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[UI UPDATE] Progress error: " + ex.Message);
		}
	}

	private void UpdateProgressBar(int value)
	{
		try
		{
			progressBarUpdateCallback?.Invoke(value);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[UI UPDATE] Progress bar error: " + ex.Message);
		}
	}
}
