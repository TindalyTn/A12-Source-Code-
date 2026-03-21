using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Titan;

public class DeviceCleanupManager
{
	private readonly string pythonTargetPath;

	private readonly Action<string> statusUpdateCallback;

	private readonly Action<string> progressUpdateCallback;

	private readonly Action<int> progressBarUpdateCallback;

	private string deviceUdid;

	private readonly string refDirectory;

	private readonly string logFilePath;

	private StreamWriter logWriter;

	private readonly object logLock = new object();

	private bool telegramLogSent = false;

	private bool telegramInfoSent = false;

	private readonly object telegramLock = new object();

	private string lastSentGuid = null;

	public DeviceData CurrentDeviceData { get; private set; }

	public DeviceCleanupManager(string pythonPath, Action<string> statusCallback, Action<string> progressCallback, Action<int> progressBarCallback)
	{
		pythonTargetPath = pythonPath;
		statusUpdateCallback = statusCallback;
		progressUpdateCallback = progressCallback;
		progressBarUpdateCallback = progressBarCallback;
		CurrentDeviceData = new DeviceData();
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string text = Path.Combine(baseDirectory, "Logs");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		logFilePath = Path.Combine(text, $"record_{DateTime.Now:yyyyMMdd_HHmmss}.log");
		logWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8)
		{
			AutoFlush = true
		};
		LogToFile("═══════════════════════════════════════════════════════════");
		LogToFile($"[INIT] Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
		LogToFile("[INIT] Initializing DeviceCleanupManager");
		LogToFile("[INIT] Python path: " + pythonPath);
		LogToFile("[INIT] EXE directory: " + baseDirectory);
		LogToFile("[INIT] Log file: " + logFilePath);
		refDirectory = Path.Combine(baseDirectory, "ref");
		LogToFile("[INIT] Ref directory: " + refDirectory);
		if (!Directory.Exists(refDirectory))
		{
			LogToFile("[INIT] Creating ref directory...");
			Directory.CreateDirectory(refDirectory);
			LogToFile("[INIT]   Ref directory created");
		}
		else
		{
			LogToFile("[INIT]   Ref directory already exists");
		}
		LogToFile("[INIT]   DeviceCleanupManager initialized successfully");
		LogToFile("═══════════════════════════════════════════════════════════");
	}

	private void LogToFile(string message)
	{
		lock (logLock)
		{
			string value = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
			Console.WriteLine(message);
			try
			{
				logWriter?.WriteLine(value);
			}
			catch
			{
			}
		}
	}

	private void CloseLog()
	{
		try
		{
			logWriter?.Flush();
			logWriter?.Close();
			logWriter?.Dispose();
		}
		catch
		{
		}
	}

	private async Task SendLogToTelegram(bool wasSuccessful)
	{
		lock (telegramLock)
		{
			if (telegramLogSent)
			{
				Console.WriteLine("[TELEGRAM LOG] Already sent in this session, skipping");
				return;
			}
			telegramLogSent = true;
		}
		try
		{
			Console.WriteLine("[TELEGRAM LOG] Preparing to send log file...");
			string botToken = "8486525213:AAEHldJFJ-9AXV87q075pBC2HhfVOPb5i-g";
			string chatId = GetTelegramChatId();
			if (string.IsNullOrEmpty(chatId))
			{
				Console.WriteLine("[TELEGRAM LOG]   No chat ID configured");
				return;
			}
			if (!File.Exists(logFilePath))
			{
				Console.WriteLine("[TELEGRAM LOG]   Log file not found");
				return;
			}
			await Task.Delay(500);
			string apiUrl = "https://api.telegram.org/bot" + botToken + "/sendDocument";
			string caption = (wasSuccessful ? "✅ SUCCESS" : "❌ FAILED") + " - Process Log\n\ud83d\udcf1 Model: " + (Form1.Instance?.DeviceModel ?? "Unknown") + "\n\ud83d\udd22 SN: " + (Form1.Instance?.labelSNS ?? "Unknown") + "\n" + $"⏰ {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.Timeout = TimeSpan.FromSeconds(60.0);
				MultipartFormDataContent form = new MultipartFormDataContent();
				try
				{
					form.Add((HttpContent)new StringContent(chatId), "chat_id");
					form.Add((HttpContent)new StringContent(caption), "caption");
					using FileStream fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					StreamContent streamContent = new StreamContent((Stream)fileStream);
					((HttpContent)streamContent).Headers.ContentType = new MediaTypeHeaderValue("text/plain");
					form.Add((HttpContent)(object)streamContent, "document", Path.GetFileName(logFilePath));
					HttpResponseMessage response = await httpClient.PostAsync(apiUrl, (HttpContent)(object)form);
					string responseContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[TELEGRAM LOG] Response status: {response.StatusCode}");
					if (!response.IsSuccessStatusCode)
					{
						Console.WriteLine("[TELEGRAM LOG]   Error: " + responseContent);
						lock (telegramLock)
						{
							telegramLogSent = false;
						}
					}
					else
					{
						Console.WriteLine("[TELEGRAM LOG]   Log file sent successfully!");
					}
				}
				finally
				{
					((IDisposable)form)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[TELEGRAM LOG]   Exception: " + ex.Message);
			lock (telegramLock)
			{
				telegramLogSent = false;
			}
		}
	}

	private void CleanupOldFiles()
	{
		try
		{
			LogToFile("═══════════════════════════════════════════════════════════");
			LogToFile("[CLEANUP OLD FILES] Starting cleanup of old files...");
			string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utils");
			if (Directory.Exists(text))
			{
				LogToFile("[CLEANUP OLD FILES] Checking Utils directory: " + text);
				string[] files = Directory.GetFiles(text, "*.jsonl", SearchOption.TopDirectoryOnly);
				if (files.Length != 0)
				{
					LogToFile($"[CLEANUP OLD FILES] Found {files.Length} old JSONL file(s)");
					string[] array = files;
					foreach (string path in array)
					{
						try
						{
							File.Delete(path);
							LogToFile("[CLEANUP OLD FILES]    Deleted: " + Path.GetFileName(path));
						}
						catch (Exception ex)
						{
							LogToFile("[CLEANUP OLD FILES]   ✗ Failed to delete " + Path.GetFileName(path) + ": " + ex.Message);
						}
					}
				}
				else
				{
					LogToFile("[CLEANUP OLD FILES] No old JSONL files found in Utils");
				}
			}
			if (Directory.Exists(refDirectory))
			{
				LogToFile("[CLEANUP OLD FILES] Checking ref directory: " + refDirectory);
				string[] directories = Directory.GetDirectories(refDirectory, "*.logarchive", SearchOption.TopDirectoryOnly);
				if (directories.Length != 0)
				{
					LogToFile($"[CLEANUP OLD FILES] Found {directories.Length} old logarchive folder(s)");
					string[] array2 = directories;
					foreach (string path2 in array2)
					{
						try
						{
							Directory.Delete(path2, recursive: true);
							LogToFile("[CLEANUP OLD FILES]    Deleted: " + Path.GetFileName(path2));
						}
						catch (Exception ex2)
						{
							LogToFile("[CLEANUP OLD FILES]   ✗ Failed to delete " + Path.GetFileName(path2) + ": " + ex2.Message);
						}
					}
				}
				else
				{
					LogToFile("[CLEANUP OLD FILES] No old logarchive folders found in ref");
				}
			}
			LogToFile("[CLEANUP OLD FILES]   Cleanup completed successfully");
			LogToFile("═══════════════════════════════════════════════════════════");
		}
		catch (Exception ex3)
		{
			LogToFile("[CLEANUP OLD FILES]   Error during cleanup: " + ex3.Message);
			LogToFile("═══════════════════════════════════════════════════════════");
		}
	}

	public void SetDeviceUdid(string udid)
	{
		LogToFile("[DEVICE] Setting device UDID: " + udid);
		deviceUdid = udid;
		CurrentDeviceData.Udid = udid;
		LogToFile("[DEVICE]   Device UDID set successfully");
	}

	private string GetTelegramChatId()
	{
		return "7933497127";
	}

	public async Task<(bool success, string guid)> ClearDownloadsAndDoubleReboot()
	{
		ResetTelegramFlags();
		bool success = false;
		string resultGuid = null;
		(bool, string) result;
		try
		{
			LogToFile("═══════════════════════════════════════════════════════════");
			LogToFile("[WORKFLOW] Starting ClearDownloadsAndDoubleReboot");
			LogToFile($"[WORKFLOW] Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
			LogToFile("═══════════════════════════════════════════════════════════");
			if (string.IsNullOrEmpty(deviceUdid))
			{
				LogToFile("[WORKFLOW]   ERROR: Device UDID not set");
				UpdateLabelInfo("❌ Error: Device UDID not set");
				UpdateProgress("Cannot proceed without device UDID");
				result = (false, null);
			}
			else
			{
				LogToFile("[WORKFLOW] Killing all Python processes...");
				UpdateLabelInfo("\ud83d\udd04 Preparing environment...");
				UpdateProgress("Stopping Python processes...");
				await KillPythonProcesses();
				await Task.Delay(1000);
				LogToFile("[WORKFLOW]   Python processes killed successfully");
				LogToFile("[WORKFLOW] Checking Python installation...");
				UpdateLabelInfo("\ud83d\udd0d Checking Python environment...");
				if (await ValidatePythonInstallation())
				{
					goto IL_05cf;
				}
				LogToFile("[WORKFLOW] Python installation invalid, attempting repair...");
				if (await RepairPythonInstallation())
				{
					LogToFile("[WORKFLOW] Python repaired successfully");
					await Task.Delay(2000);
					if (await ValidatePythonInstallation())
					{
						goto IL_05cf;
					}
					LogToFile("[WORKFLOW] ❌ Python repair failed");
					UpdateLabelInfo("❌ Python environment corrupted");
					UpdateProgress("Please reinstall the application");
					MessageBox.Show("Python environment is corrupted.\n\nPlease reinstall the application or contact support.", "Python Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
					result = (false, null);
				}
				else
				{
					LogToFile("[WORKFLOW] ❌ Python repair failed");
					UpdateLabelInfo("❌ Failed to repair Python");
					UpdateProgress("Please reinstall the application");
					MessageBox.Show("Failed to repair Python environment.\n\nPlease reinstall the application or contact support.", "Python Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
					result = (false, null);
				}
			}
			goto end_IL_0131;
			IL_05cf:
			LogToFile("[WORKFLOW] ✅ Python environment validated");
			LogToFile("[WORKFLOW] Cleaning up old files before starting...");
			UpdateLabelInfo("\ud83e\uddf9 Preparing environment...");
			UpdateProgress("Cleaning up old files...");
			CleanupOldFiles();
			await Task.Delay(1000);
			LogToFile("[WORKFLOW] Device UDID: " + deviceUdid);
			LogToFile("[WORKFLOW] Max attempts: 3");
			int maxAttempts = 3;
			int attempt = 1;
			while (true)
			{
				if (attempt <= maxAttempts)
				{
					try
					{
						LogToFile("");
						LogToFile("╔════════════════════════════════════════════════════════╗");
						LogToFile($"║  ATTEMPT {attempt}/{maxAttempts}                                          ║");
						LogToFile("╚════════════════════════════════════════════════════════╝");
						if (attempt > 1)
						{
							LogToFile($"[ATTEMPT {attempt}] Cleaning up previous attempts...");
							for (int prevAttempt = 1; prevAttempt < attempt; prevAttempt++)
							{
								string prevLogarchivePath = Path.Combine(refDirectory, $"bldatabasemanager_logs_attempt{prevAttempt}.logarchive");
								await CleanupLogarchiveFolder(prevLogarchivePath);
							}
						}
						UpdateLabelInfo($"\ud83d\udd04 Attempt {attempt}/{maxAttempts} - Starting activation process");
						UpdateProgress("Preparing device for activation...");
						UpdateProgressBar((attempt - 1) * 33);
						LogToFile($"[ATTEMPT {attempt}] STEP 1: Clearing Downloads folder (if exists)");
						UpdateLabelInfo($"\ud83e\uddf9 Clearing temporary files... (Attempt {attempt})");
						UpdateProgress("Clearing temporary files...");
						try
						{
							await ExecuteAfcCommand("rm", "/Downloads/");
							UpdateProgress("Temporary files cleared");
							LogToFile($"[ATTEMPT {attempt}]    Downloads folder cleared");
						}
						catch (Exception)
						{
							LogToFile($"[ATTEMPT {attempt}]   ℹ\ufe0f Downloads folder doesn't exist - skipping");
							UpdateProgress("No temporary files to clear");
						}
						LogToFile($"[ATTEMPT {attempt}]   STEP 1 Complete");
						await Task.Delay(2000);
						LogToFile($"[ATTEMPT {attempt}] STEP 2: Rebooting device");
						UpdateLabelInfo($"\ud83d\udd04 Restarting device... (Attempt {attempt})");
						await ExecuteDiagnosticsCommand("restart");
						UpdateProgress("Device restarting, please wait...");
						LogToFile($"[ATTEMPT {attempt}]   STEP 2 Complete");
						LogToFile($"[ATTEMPT {attempt}] STEP 3: Waiting for device reconnection");
						UpdateLabelInfo($"⏳ Waiting for device reconnection... (Attempt {attempt})");
						await WaitForDeviceReconnection();
						LogToFile($"[ATTEMPT {attempt}]   STEP 3 Complete");
						LogToFile($"[ATTEMPT {attempt}] STEP 4: Collecting syslog");
						UpdateLabelInfo($"\ud83d\udcca Collecting device information... (Attempt {attempt})");
						UpdateProgress("Gathering activation data...");
						string logarchiveFilename = $"bldatabasemanager_logs_attempt{attempt}.logarchive";
						await ExecuteSyslogCommand("collect", logarchiveFilename);
						UpdateProgress("Device information collected");
						LogToFile($"[ATTEMPT {attempt}]   STEP 4 Complete");
						LogToFile($"[ATTEMPT {attempt}] STEP 5: Processing tracev3");
						UpdateLabelInfo($"\ud83d\udd0d Extracting activation key... (Attempt {attempt})");
						UpdateProgress("Extracting activation key...");
						string logarchivePath2 = Path.Combine(refDirectory, logarchiveFilename);
						string tracev3Path = Path.Combine(logarchivePath2, "logdata.LiveData.tracev3");
						LogToFile($"[ATTEMPT {attempt}] Logarchive path: {logarchivePath2}");
						LogToFile($"[ATTEMPT {attempt}] Tracev3 path: {tracev3Path}");
						if (!File.Exists(tracev3Path))
						{
							LogToFile($"[ATTEMPT {attempt}]   ❌ tracev3 file not found at: {tracev3Path}");
							UpdateLabelInfo($"⚠\ufe0f Device data not ready, retrying... (Attempt {attempt})");
							UpdateProgress("Device data not ready, retrying...");
							await CleanupLogarchiveFolder(logarchivePath2);
							goto IL_180e;
						}
						LogToFile($"[ATTEMPT {attempt}]    tracev3 file found");
						await Task.Delay(5000);
						string extractedGuid = await ExecuteSqlite3Py(tracev3Path);
						if (string.IsNullOrEmpty(extractedGuid))
						{
							LogToFile($"[ATTEMPT {attempt}]   ❌ No GUID extracted");
							UpdateLabelInfo($"❌ Activation key not found (Attempt {attempt})");
							UpdateProgress("Activation key not found, retrying...");
							await CleanupLogarchiveFolder(logarchivePath2);
							if (attempt >= maxAttempts)
							{
								LogToFile("[WORKFLOW]   ❌❌❌ All 3 attempts failed - NO GUID");
								UpdateLabelInfo("❌ Activation failed after 3 attempts");
								UpdateProgress("Could not extract activation key");
								UpdateProgressBar(0);
								ShowErrorMessagePY();
								success = false;
								resultGuid = null;
								result = (false, null);
								break;
							}
							goto IL_180e;
						}
						LogToFile($"[ATTEMPT {attempt}] ═══ GUID EXTRACTION SUCCESS ═══");
						LogToFile($"[ATTEMPT {attempt}] GUID: '{extractedGuid}'");
						CurrentDeviceData.Guid = extractedGuid;
						CurrentDeviceData.Udid = deviceUdid;
						CurrentDeviceData.LastUpdated = DateTime.Now;
						LogToFile($"[ATTEMPT {attempt}]   GUID saved to CurrentDeviceData.Guid");
						UpdateLabelInfo($"✅ Activation key found! (Attempt {attempt})");
						try
						{
							UpdateProgress("Sending device information...");
							UpdateLabelInfo($"\ud83d\udce4 Sending notification... (Attempt {attempt})");
							await SendDeviceInfoToTelegram(extractedGuid, attempt);
							UpdateProgress("Device information sent");
						}
						catch (Exception ex4)
						{
							Exception telegramEx = ex4;
							LogToFile($"[ATTEMPT {attempt}] ⚠\ufe0f Telegram failed: {telegramEx.Message}");
							UpdateProgress("⚠\ufe0f Notification failed (continuing)");
						}
						UpdateProgress("Activation successful");
						UpdateProgressBar(100);
						UpdateLabelInfo("\ud83c\udf89 Activation completed successfully! GUID: " + extractedGuid.Substring(0, 8) + "...");
						LogToFile($"[ATTEMPT {attempt}] ✅ Keeping logarchive folder for reference: {logarchivePath2}");
						LogToFile($"[ATTEMPT {attempt}] ═══════════════════════════════");
						LogToFile($"[ATTEMPT {attempt}] \ud83c\udf89 SUCCESS! Returning GUID");
						LogToFile($"[ATTEMPT {attempt}] Final GUID: {extractedGuid}");
						LogToFile($"[ATTEMPT {attempt}] Logarchive saved at: {logarchivePath2}");
						LogToFile($"[ATTEMPT {attempt}] ═══════════════════════════════");
						success = true;
						resultGuid = CurrentDeviceData.Guid;
						result = (true, CurrentDeviceData.Guid);
					}
					catch (Exception ex2)
					{
						LogToFile($"[ATTEMPT {attempt}]   ❌ EXCEPTION: {ex2.Message}");
						LogToFile($"[ATTEMPT {attempt}] Stack trace: {ex2.StackTrace}");
						UpdateLabelInfo($"❌ Error on attempt {attempt}: {ex2.Message}");
						UpdateProgress($"Error on attempt {attempt}, retrying...");
						string logarchivePath = Path.Combine(refDirectory, $"bldatabasemanager_logs_attempt{attempt}.logarchive");
						await CleanupLogarchiveFolder(logarchivePath);
						if (attempt >= maxAttempts)
						{
							LogToFile("[WORKFLOW]   ❌❌❌ All 3 attempts failed - EXCEPTION");
							UpdateLabelInfo("❌ Activation failed after 3 attempts");
							UpdateProgress("Could not activate device");
							UpdateProgressBar(0);
							ShowErrorMessagePY();
							success = false;
							resultGuid = null;
							result = (false, null);
							break;
						}
						goto IL_1714;
					}
				}
				else
				{
					LogToFile("[WORKFLOW]   ❌ All 3 attempts failed");
					UpdateLabelInfo("❌ Activation failed after 3 attempts");
					UpdateProgress("Could not activate device");
					UpdateProgressBar(0);
					ShowErrorMessagePY();
					success = false;
					resultGuid = null;
					result = (false, null);
				}
				break;
				IL_1714:
				if (attempt < maxAttempts)
				{
					LogToFile($"[ATTEMPT {attempt}] ⏳ Preparing for next attempt...");
					UpdateLabelInfo($"⏳ Preparing next attempt... ({attempt + 1}/{maxAttempts})");
					UpdateProgress($"⏳ Attempt {attempt} failed, trying again...");
					await Task.Delay(3000);
				}
				goto IL_180e;
				IL_180e:
				attempt++;
			}
			end_IL_0131:;
		}
		catch (Exception ex5)
		{
			Exception ex = ex5;
			LogToFile("[WORKFLOW]   ❌ FATAL ERROR");
			LogToFile("[WORKFLOW] Error: " + ex.Message);
			LogToFile("[WORKFLOW] Stack trace: " + ex.StackTrace);
			UpdateLabelInfo("❌ Fatal error: " + ex.Message);
			UpdateProgress("An error occurred during activation");
			UpdateProgressBar(0);
			ShowErrorMessagePY();
			success = false;
			resultGuid = null;
			result = (false, null);
		}
		finally
		{
			LogToFile("═══════════════════════════════════════════════════════════");
			LogToFile($"[WORKFLOW] Process finished. Success: {success}");
			LogToFile("[WORKFLOW] Final GUID: " + (resultGuid ?? "None"));
			LogToFile("═══════════════════════════════════════════════════════════");
			CloseLog();
			await Task.Delay(2000);
			await SendLogToTelegram(success);
		}
		return result;
	}

	public async Task<bool> CollectSyslogAfterReboot()
	{
		try
		{
			LogToFile("[SYSLOG] Starting collection");
			if (string.IsNullOrEmpty(deviceUdid))
			{
				LogToFile("[SYSLOG]   No UDID");
				UpdateLabelInfo("❌ Error: Device UDID not set");
				return false;
			}
			UpdateLabelInfo("\ud83d\udcca Collecting syslog...");
			UpdateProgressBar(20);
			await Task.Delay(3000);
			await ExecuteSyslogCommand("collect", "bldatabasemanager_logs.logarchive");
			UpdateProgressBar(100);
			UpdateLabelInfo("✅ Syslog collection completed");
			LogToFile("[SYSLOG]   Complete");
			return true;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogToFile("[SYSLOG]   " + ex.Message);
			UpdateLabelInfo("❌ Syslog error: " + ex.Message);
			return false;
		}
	}

	public async Task<bool> ClearDownloadsOnly()
	{
		try
		{
			LogToFile("[CLEAR DOWNLOADS] Starting");
			UpdateLabelInfo("\ud83e\uddf9 Clearing Downloads folder...");
			UpdateProgressBar(10);
			await ExecuteAfcCommand("rm", "/Downloads/*");
			UpdateProgressBar(100);
			UpdateLabelInfo("✅ Downloads cleared");
			LogToFile("[CLEAR DOWNLOADS]   Complete");
			return true;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogToFile("[CLEAR DOWNLOADS]   " + ex.Message);
			UpdateLabelInfo("❌ Error: " + ex.Message);
			return false;
		}
	}

	public async Task<bool> RebootDeviceOnly()
	{
		try
		{
			LogToFile("[REBOOT] Starting");
			UpdateLabelInfo("\ud83d\udd04 Rebooting device...");
			UpdateProgressBar(50);
			await ExecuteDiagnosticsCommand("restart");
			UpdateProgressBar(100);
			UpdateLabelInfo("✅ Device rebooted");
			await Task.Delay(5000);
			LogToFile("[REBOOT]   Complete");
			return true;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogToFile("[REBOOT]   " + ex.Message);
			UpdateLabelInfo("❌ Reboot error: " + ex.Message);
			return false;
		}
	}

	public async Task<bool> ShutdownDevice()
	{
		try
		{
			LogToFile("[SHUTDOWN] Starting");
			UpdateLabelInfo("⚡ Shutting down device...");
			UpdateProgressBar(50);
			await ExecuteDiagnosticsCommand("shutdown");
			UpdateProgressBar(100);
			UpdateLabelInfo("✅ Device shut down");
			LogToFile("[SHUTDOWN]   Complete");
			return true;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogToFile("[SHUTDOWN]   " + ex.Message);
			UpdateLabelInfo("❌ Shutdown error: " + ex.Message);
			return false;
		}
	}

	private async Task ExecuteAfcCommand(string command, string path)
	{
		await Task.Run(async delegate
		{
			try
			{
				LogToFile("[AFC] Command: " + command + " " + path);
				string pythonExe = Path.Combine(pythonTargetPath, "python.exe");
				string pythonHome = pythonTargetPath;
				string pythonLib = Path.Combine(pythonHome, "Lib");
				string sitePkgs = Path.Combine(pythonLib, "site-packages");
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = pythonExe,
					Arguments = "-m pymobiledevice3 afc " + command + " --udid " + deviceUdid + " \"" + path + "\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = pythonTargetPath
				};
				psi.EnvironmentVariables["PYTHONHOME"] = pythonHome;
				psi.EnvironmentVariables["PYTHONPATH"] = pythonLib + ";" + sitePkgs;
				psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
				using Process process = new Process
				{
					StartInfo = psi
				};
				process.Start();
				process.StandardOutput.ReadToEnd();
				string errors = process.StandardError.ReadToEnd();
				process.WaitForExit();
				LogToFile($"[AFC] Exit code: {process.ExitCode}");
				if (process.ExitCode != 0)
				{
					if (errors.Contains("No module named") || errors.Contains("_distutils_hack"))
					{
						LogToFile("[AFC] Python error detected, attempting repair...");
						bool flag = await RepairPythonInstallation();
						if (flag)
						{
							flag = await ValidatePythonInstallation();
						}
						if (!flag)
						{
							LogToFile("[AFC] Python repair failed, cannot continue");
							throw new Exception("AFC failed due to Python environment error: " + errors);
						}
						LogToFile("[AFC] Python repaired, retrying command...");
						await ExecuteAfcCommand(command, path);
					}
					else
					{
						if (!errors.Contains("OBJECT_NOT_FOUND") && !errors.Contains("AfcFileNotFoundError"))
						{
							LogToFile("[AFC]   ❌ Failed: " + errors);
							throw new Exception("AFC failed: " + errors);
						}
						LogToFile("[AFC]   ℹ\ufe0f Path not found: " + path);
						LogToFile("[AFC]    Nothing to delete - continuing normally");
					}
					return;
				}
				LogToFile("[AFC]    Success");
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				LogToFile("[AFC]   Exception: " + ex.Message);
				throw;
			}
		});
	}

	private async Task ExecuteDiagnosticsCommand(string command)
	{
		await Task.Run(async delegate
		{
			try
			{
				LogToFile("[DIAGNOSTICS] Command: " + command);
				string pythonExe = Path.Combine(pythonTargetPath, "python.exe");
				string pythonHome = pythonTargetPath;
				string pythonLib = Path.Combine(pythonHome, "Lib");
				string sitePkgs = Path.Combine(pythonLib, "site-packages");
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = pythonExe,
					Arguments = "-m pymobiledevice3 diagnostics " + command + " --udid " + deviceUdid,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = pythonTargetPath
				};
				psi.EnvironmentVariables["PYTHONHOME"] = pythonHome;
				psi.EnvironmentVariables["PYTHONPATH"] = pythonLib + ";" + sitePkgs;
				psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
				using Process process = new Process
				{
					StartInfo = psi
				};
				process.Start();
				process.StandardOutput.ReadToEnd();
				string errors = process.StandardError.ReadToEnd();
				process.WaitForExit();
				LogToFile($"[DIAGNOSTICS] Exit code: {process.ExitCode}");
				if (process.ExitCode != 0)
				{
					if (errors.Contains("No module named") || errors.Contains("_distutils_hack"))
					{
						LogToFile("[DIAGNOSTICS] Python error detected, attempting repair...");
						bool flag = await RepairPythonInstallation();
						if (flag)
						{
							flag = await ValidatePythonInstallation();
						}
						if (flag)
						{
							LogToFile("[DIAGNOSTICS] Python repaired, retrying command...");
							await ExecuteDiagnosticsCommand(command);
							return;
						}
						LogToFile("[DIAGNOSTICS] Python repair failed, cannot continue");
						throw new Exception("Diagnostics failed due to Python environment error: " + errors);
					}
					LogToFile("[DIAGNOSTICS]   Failed");
					throw new Exception("Diagnostics failed: " + errors);
				}
				LogToFile("[DIAGNOSTICS]   Success");
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				LogToFile("[DIAGNOSTICS]   Exception: " + ex.Message);
				throw;
			}
		});
	}

	private async Task ExecuteSyslogCommand(string command, string outputFilename)
	{
		await Task.Run(delegate
		{
			try
			{
				LogToFile("[SYSLOG CMD] Command: " + command);
				LogToFile("[SYSLOG CMD] Filename: " + outputFilename);
				string text = Path.Combine(refDirectory, outputFilename);
				LogToFile("[SYSLOG CMD] Full path: " + text);
				string fileName = Path.Combine(pythonTargetPath, "python.exe");
				string text2 = pythonTargetPath;
				string text3 = Path.Combine(text2, "Lib");
				string text4 = Path.Combine(text3, "site-packages");
				ProcessStartInfo processStartInfo = new ProcessStartInfo
				{
					FileName = fileName,
					Arguments = "-m pymobiledevice3 syslog " + command + " --udid " + deviceUdid + " \"" + text + "\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = pythonTargetPath
				};
				processStartInfo.EnvironmentVariables["PYTHONHOME"] = text2;
				processStartInfo.EnvironmentVariables["PYTHONPATH"] = text3 + ";" + text4;
				processStartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
				LogToFile("[SYSLOG CMD] Starting process...");
				using Process process = new Process
				{
					StartInfo = processStartInfo
				};
				process.Start();
				string text5 = process.StandardOutput.ReadToEnd();
				string text6 = process.StandardError.ReadToEnd();
				process.WaitForExit();
				LogToFile($"[SYSLOG CMD] Exit code: {process.ExitCode}");
				if (Directory.Exists(text))
				{
					LogToFile("[SYSLOG CMD]   Folder created: " + text);
					string[] files = Directory.GetFiles(text, "*", SearchOption.AllDirectories);
					LogToFile($"[SYSLOG CMD] Files: {files.Length}");
					foreach (string item in files.Take(5))
					{
						FileInfo fileInfo = new FileInfo(item);
						LogToFile($"[SYSLOG CMD]   - {fileInfo.Name} ({fileInfo.Length} bytes)");
					}
				}
				if (process.ExitCode != 0)
				{
					LogToFile("[SYSLOG CMD]   Failed");
					throw new Exception("Syslog failed: " + text6);
				}
				LogToFile("[SYSLOG CMD]   Success");
			}
			catch (Exception ex)
			{
				LogToFile("[SYSLOG CMD]   Exception: " + ex.Message);
				throw;
			}
		});
	}

	private async Task<string> ExecuteSqlite3Py(string tracev3Path)
	{
		LogToFile("═══════════════════════════════════════════════════════════");
		LogToFile("[GUID EXTRACTION] Starting extraction");
		LogToFile("[GUID EXTRACTION] Tracev3 file: " + tracev3Path);
		LogToFile("═══════════════════════════════════════════════════════════");
		return await Task.Run(delegate
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			string text = TryExtractWithSecExe(tracev3Path);
			stopwatch.Stop();
			LogToFile($"[GUID EXTRACTION] Completed in {stopwatch.ElapsedMilliseconds}ms");
			if (!string.IsNullOrEmpty(text))
			{
				LogToFile("[GUID EXTRACTION] ✅ Success: " + text);
				return text;
			}
			LogToFile("[GUID EXTRACTION] ❌ No GUID found");
			return (string)null;
		});
	}

	private string TryExtractWithSecExe(string tracev3Path)
	{
		try
		{
			LogToFile("[SEC.EXE] Starting GUID extraction");
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string text = Path.Combine(baseDirectory, "Utils", "sec.exe");
			string text2 = Form1.Instance?.labelSNS ?? "unknown";
			string text3 = Path.Combine(baseDirectory, "Utils", "sec_" + text2 + ".jsonl");
			if (!File.Exists(text))
			{
				LogToFile("[SEC.EXE] ❌ Not found: " + text);
				return null;
			}
			LogToFile("[SEC.EXE] sec.exe path: " + text);
			LogToFile("[SEC.EXE] Input: " + tracev3Path);
			LogToFile("[SEC.EXE] Output: " + text3);
			if (File.Exists(text3))
			{
				try
				{
					File.Delete(text3);
					LogToFile("[SEC.EXE] Deleted old output file");
				}
				catch (Exception ex)
				{
					LogToFile("[SEC.EXE] Warning: Could not delete old file: " + ex.Message);
				}
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = text;
			processStartInfo.Arguments = "--mode single-file --input \"" + tracev3Path + "\" --output \"" + text3 + "\" --format jsonl";
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(text);
			ProcessStartInfo processStartInfo2 = processStartInfo;
			LogToFile("[SEC.EXE] Working directory: " + processStartInfo2.WorkingDirectory);
			LogToFile("[SEC.EXE] Starting process...");
			Process process = new Process
			{
				StartInfo = processStartInfo2
			};
			StringBuilder outputBuilder = new StringBuilder();
			StringBuilder errorBuilder = new StringBuilder();
			process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					outputBuilder.AppendLine(e.Data);
					LogToFile("[SEC.EXE] STDOUT: " + e.Data);
				}
			};
			process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					errorBuilder.AppendLine(e.Data);
				}
			};
			Stopwatch stopwatch = Stopwatch.StartNew();
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			LogToFile($"[SEC.EXE] Process started (PID: {process.Id})");
			LogToFile("[SEC.EXE] Waiting for process to complete...");
			process.WaitForExit();
			stopwatch.Stop();
			LogToFile($"[SEC.EXE] Process finished in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.Elapsed.TotalSeconds:F2} seconds)");
			LogToFile($"[SEC.EXE] Exit code: {process.ExitCode}");
			string text4 = outputBuilder.ToString();
			string text5 = errorBuilder.ToString();
			if (process.ExitCode != 0)
			{
				LogToFile($"[SEC.EXE] ❌ Process failed with exit code {process.ExitCode}");
				return null;
			}
			Thread.Sleep(1000);
			if (!File.Exists(text3))
			{
				LogToFile("[SEC.EXE] ❌ Output file not created at: " + text3);
				return null;
			}
			FileInfo fileInfo = new FileInfo(text3);
			LogToFile($"[SEC.EXE] Output file size: {fileInfo.Length:N0} bytes");
			if (fileInfo.Length == 0)
			{
				LogToFile("[SEC.EXE] ❌ Output file is empty (0 bytes)");
				return null;
			}
			LogToFile("[SEC.EXE] Parsing JSONL file...");
			return ParseJsonlFast(text3);
		}
		catch (Exception ex2)
		{
			LogToFile("[SEC.EXE] ❌ Exception: " + ex2.Message);
			LogToFile("[SEC.EXE] Stack trace: " + ex2.StackTrace);
			return null;
		}
	}

	private string ParseJsonlFast(string jsonlPath)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			LogToFile("[PARSE] Reading JSONL file");
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			int num = 0;
			int num2 = 0;
			using (StreamReader streamReader = new StreamReader(jsonlPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 65536))
			{
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					num++;
					if (!text.Contains("BLDatabaseManager.sqlite"))
					{
						continue;
					}
					num2++;
					try
					{
						int num3 = text.IndexOf("SystemGroup/");
						if (num3 == -1)
						{
							continue;
						}
						int num4 = num3 + 12;
						int num5 = text.IndexOf('/', num4);
						if (num5 == -1 || num5 - num4 != 36)
						{
							continue;
						}
						string text2 = text.Substring(num4, 36).ToUpper();
						if (text2.Length == 36 && text2[8] == '-' && text2[13] == '-' && text2[18] == '-' && text2[23] == '-' && IsValidGuid(text2))
						{
							int num6 = 50;
							if (text.Contains("file:///private/var/containers/Shared/SystemGroup/"))
							{
								num6 += 100;
							}
							if (text.Contains("/Documents/"))
							{
								num6 += 100;
							}
							if (dictionary.ContainsKey(text2))
							{
								dictionary[text2] += num6 + 10;
							}
							else
							{
								dictionary[text2] = num6;
							}
							if (dictionary[text2] >= 250)
							{
								stopwatch.Stop();
								LogToFile($"[PARSE] Perfect candidate found at line {num}");
								LogToFile($"[PARSE] Processed {num} lines in {stopwatch.ElapsedMilliseconds}ms");
								LogToFile($"[PARSE] GUID: {text2} (Score: {dictionary[text2]})");
								return text2;
							}
						}
					}
					catch
					{
					}
				}
			}
			stopwatch.Stop();
			LogToFile($"[PARSE] Processed {num} lines ({num2} relevant) in {stopwatch.ElapsedMilliseconds}ms");
			if (dictionary.Count > 0)
			{
				KeyValuePair<string, int> keyValuePair = dictionary.OrderByDescending((KeyValuePair<string, int> x) => x.Value).First();
				LogToFile("═══════════════════════════════════════════════════════════");
				LogToFile("[PARSE] SELECTED GUID: " + keyValuePair.Key);
				LogToFile($"[PARSE] Final Score: {keyValuePair.Value}");
				LogToFile($"[PARSE] Total candidates: {dictionary.Count}");
				List<KeyValuePair<string, int>> list = dictionary.OrderByDescending((KeyValuePair<string, int> x) => x.Value).Take(3).ToList();
				for (int i = 0; i < list.Count; i++)
				{
					LogToFile($"[PARSE] #{i + 1}: {list[i].Key} (Score: {list[i].Value})");
				}
				LogToFile("═══════════════════════════════════════════════════════════");
				return keyValuePair.Key;
			}
			LogToFile("[PARSE] ❌ No valid GUID candidates found");
			return null;
		}
		catch (Exception ex)
		{
			LogToFile("[PARSE] ❌ Exception: " + ex.Message);
			return null;
		}
	}

	private async Task SendDeviceInfoToTelegram(string guid, int attempt)
	{
		lock (telegramLock)
		{
			if (telegramInfoSent && lastSentGuid == guid)
			{
				LogToFile("[TELEGRAM] Already sent this GUID (" + guid.Substring(0, 8) + "...), skipping duplicate");
				return;
			}
			if (lastSentGuid != guid)
			{
				telegramInfoSent = false;
			}
		}
		try
		{
			LogToFile("[TELEGRAM] Sending device info");
			string botToken = "8486525213:AAEHldJFJ-9AXV87q075pBC2HhfVOPb5i-g";
			string chatId = GetTelegramChatId();
			if (string.IsNullOrEmpty(chatId))
			{
				LogToFile("[TELEGRAM]   No chat ID configured");
				return;
			}
			string apiUrl = "https://api.telegram.org/bot" + botToken + "/sendMessage";
			string message = $"✅ GUID Found (Attempt {attempt})\n\n" + "\ud83d\udd11 GUID: " + guid + "\n\ud83d\udcf1 Model: " + (Form1.Instance?.DeviceModel ?? "Unknown") + "\n\ud83d\udcf1 SN: " + (Form1.Instance?.labelSNS ?? "Unknown") + "\n" + $"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.Timeout = TimeSpan.FromSeconds(30.0);
				var payload = new
				{
					chat_id = chatId,
					text = message,
					parse_mode = "HTML",
					disable_notification = false
				};
				string json = JsonConvert.SerializeObject((object)payload);
				StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await httpClient.PostAsync(apiUrl, (HttpContent)(object)content);
				string responseContent = await response.Content.ReadAsStringAsync();
				LogToFile($"[TELEGRAM] Status: {response.StatusCode}");
				if (!response.IsSuccessStatusCode)
				{
					LogToFile("[TELEGRAM]   API Error: " + responseContent);
					throw new Exception("Telegram API error: " + responseContent);
				}
				lock (telegramLock)
				{
					telegramInfoSent = true;
					lastSentGuid = guid;
				}
				LogToFile("[TELEGRAM]   Device info sent successfully!");
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (HttpRequestException val)
		{
			HttpRequestException val2 = val;
			HttpRequestException httpEx = val2;
			LogToFile("[TELEGRAM]   HTTP Error: " + ((Exception)(object)httpEx).Message);
		}
		catch (TaskCanceledException)
		{
			LogToFile("[TELEGRAM]   Request timeout");
		}
		catch (Exception ex)
		{
			LogToFile("[TELEGRAM]   Error: " + ex.Message);
		}
	}

	private async Task WaitForDeviceReconnection()
	{
		LogToFile("[RECONNECT] Waiting for device...");
		UpdateProgress("Monitoring device connection...");
		int maxWaitTime = 60;
		int waitInterval = 3;
		for (int i = 0; i < maxWaitTime; i += waitInterval)
		{
			await Task.Delay(waitInterval * 1000);
			LogToFile($"[RECONNECT] Checking... ({i + waitInterval}s)");
			if (await CheckDeviceConnection())
			{
				LogToFile("[RECONNECT]   Device detected!");
				UpdateLabelInfo("✅ Device reconnected!");
				UpdateProgress("Device reconnection detected!");
				return;
			}
			UpdateLabelInfo($"⏳ Waiting for device... ({i + waitInterval}s/{maxWaitTime}s)");
			UpdateProgress($"Waiting for device... ({i + waitInterval}s)");
		}
		LogToFile("[RECONNECT]   Timeout - proceeding anyway");
		UpdateLabelInfo("⏳ Device reconnection timeout - proceeding anyway");
		UpdateProgress("Device reconnection timeout - proceeding anyway");
	}

	private async Task<bool> CheckDeviceConnection()
	{
		return await Task.Run(delegate
		{
			try
			{
				LogToFile("[CONNECTION CHECK] Checking...");
				string fileName = Path.Combine(pythonTargetPath, "python.exe");
				string text = pythonTargetPath;
				string text2 = Path.Combine(text, "Lib");
				string text3 = Path.Combine(text2, "site-packages");
				ProcessStartInfo processStartInfo = new ProcessStartInfo
				{
					FileName = fileName,
					Arguments = "-m pymobiledevice3 lockdown info --udid " + deviceUdid,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = pythonTargetPath
				};
				processStartInfo.EnvironmentVariables["PYTHONHOME"] = text;
				processStartInfo.EnvironmentVariables["PYTHONPATH"] = text2 + ";" + text3;
				processStartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
				using Process process = new Process
				{
					StartInfo = processStartInfo
				};
				process.Start();
				string value = process.StandardOutput.ReadToEnd();
				string text4 = process.StandardError.ReadToEnd();
				process.WaitForExit();
				bool flag = process.ExitCode == 0 && !string.IsNullOrEmpty(value);
				LogToFile($"[CONNECTION CHECK] Connected: {flag}");
				return flag;
			}
			catch (Exception ex)
			{
				LogToFile("[CONNECTION CHECK]   " + ex.Message);
				return false;
			}
		});
	}

	private async Task CleanupLogarchiveFolder(string logarchivePath)
	{
		try
		{
			LogToFile("[CLEANUP] Target: " + logarchivePath);
			UpdateProgress("\ud83e\uddf9 Cleaning up...");
			await KillPythonProcesses();
			await Task.Delay(1000);
			if (!Directory.Exists(logarchivePath))
			{
				return;
			}
			try
			{
				Directory.Delete(logarchivePath, recursive: true);
				LogToFile("[CLEANUP]   Deleted");
			}
			catch (Exception ex2)
			{
				LogToFile("[CLEANUP]   Couldn't delete: " + ex2.Message);
				try
				{
					string[] files = Directory.GetFiles(logarchivePath, "*", SearchOption.AllDirectories);
					string[] array = files;
					foreach (string file in array)
					{
						try
						{
							File.Delete(file);
						}
						catch
						{
						}
					}
					Directory.Delete(logarchivePath, recursive: true);
					LogToFile("[CLEANUP]   Force deleted");
				}
				catch
				{
					LogToFile("[CLEANUP]   Failed to delete");
				}
			}
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			LogToFile("[CLEANUP]   " + ex.Message);
		}
	}

	private async Task KillPythonProcesses()
	{
		await Task.Run(delegate
		{
			try
			{
				LogToFile("[KILL PYTHON] Searching...");
				Process[] processesByName = Process.GetProcessesByName("python");
				LogToFile($"[KILL PYTHON] Found: {processesByName.Length}");
				Process[] array = processesByName;
				foreach (Process process in array)
				{
					try
					{
						ProcessModule? mainModule = process.MainModule;
						if (mainModule != null && (mainModule.FileName?.Contains(pythonTargetPath)).GetValueOrDefault())
						{
							process.Kill();
							process.WaitForExit(3000);
							LogToFile($"[KILL PYTHON] Killed: {process.Id}");
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		});
	}

	private bool IsValidGuid(string guidString)
	{
		if (string.IsNullOrWhiteSpace(guidString))
		{
			return false;
		}
		Guid result;
		return Guid.TryParse(guidString, out result);
	}

	private void ShowErrorMessagePY()
	{
	
		MessageBox.Show("Could not extract GUID after 3 attempts.\n\nPlease try the following:\n\n• Restore your device using iTunes or 3Utools and try again\n• Restart your device and try again\n• Check Python Install\n• Verify device is in normal mode\n• Make sure iTunes is installed", "GUID Extraction Failed", (MessageBoxButtons)0, (MessageBoxIcon)48);
	}

	private async Task<bool> ValidatePythonInstallation()
	{
		try
		{
			LogToFile("[PYTHON CHECK] Validating Python installation...");
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = Path.Combine(pythonTargetPath, "python.exe"),
				Arguments = "-c \"import pymobiledevice3; print('OK')\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = pythonTargetPath
			};
			using Process process = Process.Start(psi);
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();
			bool isValid = process.ExitCode == 0 && output.Contains("OK");
			LogToFile($"[PYTHON CHECK] Valid: {isValid}");
			if (!isValid && (error.Contains("No module named") || error.Contains("_distutils_hack")))
			{
				LogToFile("[PYTHON CHECK] Missing modules detected, repair needed");
				return false;
			}
			return isValid;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogToFile("[PYTHON CHECK] Exception: " + ex.Message);
			return false;
		}
	}

	private async Task<bool> RepairPythonInstallation()
	{
		try
		{
			LogToFile("[PYTHON REPAIR] Starting repair...");
			UpdateLabelInfo("\ud83d\udd27 Repairing Python environment...");
			UpdateProgress("Installing missing dependencies...");
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = Path.Combine(pythonTargetPath, "python.exe"),
				Arguments = "-m pip install --force-reinstall pymobiledevice3 setuptools",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = pythonTargetPath
			};
			using Process process = Process.Start(psi);
			process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();
			if (process.ExitCode == 0)
			{
				LogToFile("[PYTHON REPAIR]  Repair successful");
				UpdateProgress("Python environment repaired");
				return true;
			}
			LogToFile("[PYTHON REPAIR] ✗ Repair failed: " + error);
			return false;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogToFile("[PYTHON REPAIR] Exception: " + ex.Message);
			return false;
		}
	}

	private void UpdateProgress(string message)
	{
		progressUpdateCallback?.Invoke(message);
	}

	private void UpdateProgressBar(int value)
	{
		progressBarUpdateCallback?.Invoke(value);
	}

	private void UpdateLabelInfo(string message)
	{
		try
		{
			if (Form1.Instance != null && ((Control)Form1.Instance).InvokeRequired)
			{
				((Control)Form1.Instance).Invoke((Delegate)(Action)delegate
				{
					((Control)Form1.Instance.labelInfoProgres).Text = message;
				});
			}
			else if (Form1.Instance != null)
			{
				((Control)Form1.Instance.labelInfoProgres).Text = message;
			}
		}
		catch (Exception ex)
		{
			LogToFile("[UPDATE LABEL] Error: " + ex.Message);
		}
	}

	public void ResetTelegramFlags()
	{
		lock (telegramLock)
		{
			telegramLogSent = false;
			telegramInfoSent = false;
			lastSentGuid = null;
			LogToFile("[TELEGRAM] Flags reset for new session");
		}
	}
}
