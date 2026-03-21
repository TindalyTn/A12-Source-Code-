using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Core;
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Enums;
using Guna.UI2.WinForms.Suite;
using LibUsbDotNet.DeviceNotify;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Python.Runtime;
using Titan.Enumerates;
using Titan.Event;
using Titan.Properties;
using iMobileDevice;
using iMobileDevice.Afc;
using iMobileDevice.Lockdown;
using iMobileDevice.iDevice;

namespace Titan;

public class Form1 : Form
{
	private delegate void SetTextCallback(string text, Color color, string additionalText);

	public const int WM_NCLBUTTONDOWN = 161;

	public const int HT_CAPTION = 2;

	private readonly IiDeviceApi idevice = LibiMobileDevice.Instance.iDevice;

	private readonly ILockdownApi lockdown = LibiMobileDevice.Instance.Lockdown;

	public static IAfcApi afc = LibiMobileDevice.Instance.Afc;

	private iDeviceHandle deviceHandle;

	private LockdownClientHandle lockdownHandle;

	public static LockdownServiceDescriptorHandle lockdownServiceHandle;

	public static AfcClientHandle afcHandle;

	public static bool bool_1 = true;

	public static string ToolDir = Directory.GetCurrentDirectory();

	public static IDeviceNotifier UsbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();

	public static string UniqueDeviceIDGET;

	public static string UniqueChipIDGET;

	public iOSDeviceManager manager = new iOSDeviceManager();

	public iOSDevice currentiOSDevice;

	private static readonly HttpClient httpClient = new HttpClient();

	private static readonly HttpClient client = new HttpClient();

	private System.Threading.Timer timer2;

	private bool alertShown = false;

	private bool processKilled = false;

	private List<string> blacklist = new List<string>
	{
		"fiddler", "wireshark", "charles", "httpdebuggerui", "burp", "proxyman", "tcpview", "packetcapture", "networkminer", "tcpdump",
		"netsniffer", "mitmproxy", "ettercap", "zap", "dsniff", "iptrace", "tcpmonitor", "netcat", "aircrack-ng", "tshark",
		"netsleuth", "commview", "kismet", "airodump-ng", "p0f", "snoop", "xplico", "decodet", "hexinject", "postman",
		"snort", "openvas", "nessus", "nmap", "zenmap", "nikto", "recon-ng", "hping", "masscan", "pingplotter",
		"angryip", "intercepter-ng", "netspot", "dnspy", "dnspyex", "proximan", "ollydbg", "x64dbg", "immunitydebugger", "idapro",
		"hexrays", "hopper", "jeb", "ghidra", "binaryninja", "frida", "radare2", "gdb", "edb", "windbg",
		"debugger", "dtrace", "strace", "lldb", "cutter", "patchdiff2", "valgrind", "qiling", "rr", "peda",
		"kdbg", "paranoidfish", "unicorn", "bochs", "retdec", "rizin"
	};

	private DateTime startTime;

	private bool isDownloading = false;

	private string downloadError = null;

	private int lastProgress = 0;

	private readonly object uiUpdateLocker = new object();

	private bool librariesLoaded = false;

	private System.Timers.Timer autoReconnectionTimer;

	private bool isDeviceCurrentlyConnected = false;

	private DateTime? deviceDisconnectedAt = null;

	private const int RECONNECTION_TIMEOUT_SECONDS = 120;

	private const int TIMER_CHECK_INTERVAL = 5000;

	private string lastConnectedUdid = null;

	private string lastDeviceModel = "";

	private string lastDeviceType = "";

	private string lastDeviceSN = "";

	private string lastDeviceVersion = "";

	private string lastDeviceActivation = "";

	private string lastDeviceECID = "";

	private string lastDeviceRegion = "";

	private static readonly string pythonTargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iRealm", "python");

	private bool isPythonReady = false;

	private TaskCompletionSource<bool> afcReconnectionTcs;

	public static string ToolDirX = Application.StartupPath;

	public string OTADir = Path.Combine(ToolDirX, "OTA", "swp", "ad09186179f31a88dd6ee2c8f2d034025f54c82a");

	public string TMPPatchOTA = Path.Combine(ToolDirX, "OTA", "swp", "ad09186179f31a88dd6ee2c8f2d034025f54c82a");

	public static string RutaOTA = Path.Combine(ToolDirX, "OTA", "swp");

	private int totalProgress = 0;

	private bool isProcessRunning = false;

	private DeviceCleanupManager deviceCleanupManager;

	private DeviceFileManager deviceFileManager;

	private const string DefaultPublicKey = "u50R9IcbKtQHcwbo1UvJGnk4Mz8TTq89n+8Pv65YBgHiMYJl+aPS5/cBA++AhBbwdyXMqXfSkhnj/gO2DYmzNxHLioac2vJNgkS8ViVN71Df/wq9dXchiaQQkDHrjhec/L90frcg2n8oOFMCZRXddjpr7YJb1bRkBuG6Qw7s9MwUQIKoiDMjnFKz1V6u8IKNndkpbRGM7TywnV7JdQvEBp+Wj/rjN3KDE2MqzUbaXCamDF0/CModjz/TxTHtxMK3QifO+9qGV+8mm0+KB84EPyw7xFNnsUEcmLbOQIX+36KRw/Vi9jG33NROy+U4bM0mDeNvztrb+O7QLgcoWh0y5Q==";

	private const string DefaultPrivateKey = "u50R9IcbKtQHcwbo1UvJGnk4Mz8TTq89n+8Pv65YBgHiMYJl+aPS5/cBA++AhBbwdyXMqXfSkhnj/gO2DYmzNxHLioac2vJNgkS8ViVN71Df/wq9dXchiaQQkDHrjhec/L90frcg2n8oOFMCZRXddjpr7YJb1bRkBuG6Qw7s9MwUQIKoiDMjnFKz1V6u8IKNndkpbRGM7TywnV7JdQvEBp+Wj/rjN3KDE2MqzUbaXCamDF0/CModjz/TxTHtxMK3QifO+9qGV+8mm0+KB84EPyw7xFNnsUEcmLbOQIX+36KRw/Vi9jG33NROy+U4bM0mDeNvztrb+O7QLgcoWh0y5Q==";

	public const string BaseUrl = "https://server-backend-here.com/";

	public const string SqliteFileExtension = ".sqlitedb";

	private IContainer components = null;

	internal PictureBox pictureBoxModel;

	private PictureBox pictureBox3;

	private Label labelType;

	private Label labelVersion;

	private Label labelSN;

	private Label ModeloffHello;

	private Label label15;

	private Label label16;

	private Label label20;

	private Label label23;

	internal PictureBox pictureBoxDC;

	private Guna2CircleButton guna2CircleButton1;

	private Guna2CircleButton guna2CircleButton2;

	private Guna2Elipse guna2Elipse1;

	private Guna2Panel guna2Panel1;

	internal Guna2GradientButton guna2GradientButton3;

	internal Label labelInfoProgres;

	private Label label24;

	internal Guna2ProgressBar Guna2ProgressBar1;

	internal Guna2GradientButton ActivateButton;

	internal Guna2GradientButton guna2GradientButton2;

	private Label Status;

	private Label labelActivaction;

	internal Guna2GradientButton guna2GradientButton1;

	private Guna2CircleButton guna2CircleButton3;

	internal Label label2;

	private Label label1;

	private Label labelRegion;

	public static Form1 Instance { get; private set; }

	public string DeviceModel => ((Control)ModeloffHello).Text ?? "Unknown";

	public string iOSVer => currentiOSDevice?.ProductVersion ?? lastDeviceVersion ?? "Unknown";

	public string labelSNS => currentiOSDevice?.SerialNumber ?? lastDeviceSN ?? "Unknown";

	public string DeviceType => currentiOSDevice?.ProductType ?? lastDeviceType ?? "Unknown";

	public string DeviceRegion => currentiOSDevice?.RegionInfo ?? lastDeviceRegion ?? "Unknown";

	public DeviceData CurrentDeviceData { get; private set; }

	public static string RsaPublicKey => "u50R9IcbKtQHcwbo1UvJGnk4Mz8TTq89n+8Pv65YBgHiMYJl+aPS5/cBA++AhBbwdyXMqXfSkhnj/gO2DYmzNxHLioac2vJNgkS8ViVN71Df/wq9dXchiaQQkDHrjhec/L90frcg2n8oOFMCZRXddjpr7YJb1bRkBuG6Qw7s9MwUQIKoiDMjnFKz1V6u8IKNndkpbRGM7TywnV7JdQvEBp+Wj/rjN3KDE2MqzUbaXCamDF0/CModjz/TxTHtxMK3QifO+9qGV+8mm0+KB84EPyw7xFNnsUEcmLbOQIX+36KRw/Vi9jG33NROy+U4bM0mDeNvztrb+O7QLgcoWh0y5Q==";

	public static string RsaPrivateKey => "u50R9IcbKtQHcwbo1UvJGnk4Mz8TTq89n+8Pv65YBgHiMYJl+aPS5/cBA++AhBbwdyXMqXfSkhnj/gO2DYmzNxHLioac2vJNgkS8ViVN71Df/wq9dXchiaQQkDHrjhec/L90frcg2n8oOFMCZRXddjpr7YJb1bRkBuG6Qw7s9MwUQIKoiDMjnFKz1V6u8IKNndkpbRGM7TywnV7JdQvEBp+Wj/rjN3KDE2MqzUbaXCamDF0/CModjz/TxTHtxMK3QifO+9qGV+8mm0+KB84EPyw7xFNnsUEcmLbOQIX+36KRw/Vi9jG33NROy+U4bM0mDeNvztrb+O7QLgcoWh0y5Q==";

	[DllImport("user32.dll")]
	public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

	[DllImport("user32.dll")]
	public static extern bool ReleaseCapture();

	[DllImport("kernel32.dll")]
	private static extern bool IsDebuggerPresent();

	public Form1()
	{

		InitializeComponent();
		Instance = this;
		((Form)this).FormClosing += new FormClosingEventHandler(Form1_FormClosing);
	}

	private async void Form1_Load(object sender, EventArgs e)
	{
		((Form)this).FormBorderStyle = (FormBorderStyle)0;
		DropShadow dropShadow = new DropShadow();
		dropShadow.ApplyShadowsAndRoundedCorners((Form)(object)this, 7);
		DropShadow.MakeFormDraggable((Form)(object)this);
		if (!(await CheckAndInstallDependencies()))
		{
			Application.Exit();
			return;
		}
		if (!(await SetupEmbeddedPython()))
		{
			MessageBox.Show("Failed to initialize Python environment.\r\n\r\nThe application will close.", "Initialization Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
			Application.Exit();
			return;
		}
		InitializeFormSettings();
		InitializeEventHandlers();
		InitializeSecurityTimer();
		StartDeviceListener();
		CheckVersionAsync();
		((Control)pictureBoxModel).SendToBack();
		InitializeAutoReconnection();
		InitializeDeviceManagers();
	}

	private async Task<bool> CheckAndInstallDependencies()
	{
		bool isX64 = Environment.Is64BitOperatingSystem;
		string[] registryPaths = new string[2] { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall" };
		bool vcX64Installed = false;
		bool vcX86Installed = false;
		string[] array = registryPaths;
		foreach (string path2 in array)
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(path2);
			if (registryKey == null)
			{
				continue;
			}
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string subKeyName2 in subKeyNames)
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey(subKeyName2);
				string displayName2 = registryKey2?.GetValue("DisplayName")?.ToString();
				if (!string.IsNullOrEmpty(displayName2) && displayName2.Contains("Microsoft Visual C++ 2015"))
				{
					if (displayName2.Contains("(x64)"))
					{
						vcX64Installed = true;
					}
					if (displayName2.Contains("(x86)"))
					{
						vcX86Installed = true;
					}
				}
			}
		}
		if (isX64 && !vcX64Installed)
		{
			string vcPath2 = Path.Combine(Application.StartupPath, "Fix_Folder", "VC_redist.x64.exe");
			if (File.Exists(vcPath2))
			{
				await Task.Run(delegate
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = vcPath2,
						Arguments = "/install /quiet /norestart",
						UseShellExecute = true,
						Verb = "runas"
					})?.WaitForExit();
				});
			}
		}
		if (!vcX86Installed)
		{
			string vcPath = Path.Combine(Application.StartupPath, "Fix_Folder", "VC_redist.x86.exe");
			if (File.Exists(vcPath))
			{
				await Task.Run(delegate
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = vcPath,
						Arguments = "/install /quiet /norestart",
						UseShellExecute = true,
						Verb = "runas"
					})?.WaitForExit();
				});
			}
		}
		bool amdInstalled = false;
		string[] array2 = registryPaths;
		foreach (string path in array2)
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
			{
				if (key != null)
				{
					string[] subKeyNames2 = key.GetSubKeyNames();
					foreach (string subKeyName in subKeyNames2)
					{
						using RegistryKey subKey = key.OpenSubKey(subKeyName);
						string displayName = subKey?.GetValue("DisplayName")?.ToString();
						if (!string.IsNullOrEmpty(displayName) && displayName.Contains("Apple Mobile Device Support"))
						{
							amdInstalled = true;
							break;
						}
					}
				}
			}
			if (amdInstalled)
			{
				break;
			}
		}
		if (!amdInstalled)
		{
			string amdPath = Path.Combine(Application.StartupPath, "Fix_Folder", isX64 ? "AppleMobileDeviceSupportx64.msi" : "AppleMobileDeviceSupportx86.msi");
			if (File.Exists(amdPath))
			{
				MessageBox.Show("Apple Mobile Device Support not detected. Installing now...", "Installing Component", (MessageBoxButtons)0, (MessageBoxIcon)64);
				await Task.Run(delegate
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "msiexec.exe",
						Arguments = "/i \"" + amdPath + "\" /quiet /norestart",
						UseShellExecute = true,
						Verb = "runas"
					})?.WaitForExit();
				});
				MessageBox.Show("Installation complete. Please restart the application.", "Installation Complete", (MessageBoxButtons)0, (MessageBoxIcon)64);
				return false;
			}
			MessageBox.Show("Apple Mobile Device Support is not installed and installer not found.\n\nPlease place " + (isX64 ? "AppleMobileDeviceSupportx64.msi" : "AppleMobileDeviceSupportx86.msi") + " in Fix_Folder", "Component Required", (MessageBoxButtons)0, (MessageBoxIcon)48);
			return false;
		}
		return true;
	}

	private void InitializeAutoReconnection()
	{
		Console.WriteLine("[AUTO-RECONNECT] ═══════════════════════════════════════");
		Console.WriteLine("[AUTO-RECONNECT] Initializing monitoring module...");
		autoReconnectionTimer = new System.Timers.Timer
		{
			Interval = 5000.0,
			AutoReset = true
		};
		autoReconnectionTimer.Elapsed += AutoReconnectionTimer_Tick;
		Console.WriteLine("[AUTO-RECONNECT] Timer created but not started yet");
		Console.WriteLine("[AUTO-RECONNECT] Waiting for device manager initialization...");
		Console.WriteLine("[AUTO-RECONNECT] ═══════════════════════════════════════");
	}

	private void AutoReconnectionTimer_Tick(object sender, ElapsedEventArgs e)
	{
		try
		{
			if (!deviceDisconnectedAt.HasValue)
			{
				return;
			}
			double totalSeconds = (DateTime.Now - deviceDisconnectedAt.Value).TotalSeconds;
			if (totalSeconds > 120.0)
			{
				isDeviceCurrentlyConnected = false;
				deviceDisconnectedAt = null;
				autoReconnectionTimer.Stop();
				((Control)this).Invoke((Delegate)(Action)delegate
				{
					InsertLabelText("  Device disconnected - timeout exceeded", Color.Red);
				});
			}
			else if ((int)totalSeconds % 10 == 0)
			{
				Console.WriteLine($"[AUTO-RECONNECT] ⏱\ufe0f Waiting... ({(int)totalSeconds}s / {120}s)");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[AUTO-RECONNECT ERROR] " + ex.Message);
		}
	}

	private void HandleNoDeviceInTimer()
	{
		if (!isDeviceCurrentlyConnected)
		{
			return;
		}
		if (!deviceDisconnectedAt.HasValue)
		{
			deviceDisconnectedAt = DateTime.Now;
			Console.WriteLine("[AUTO-RECONNECT] Device disconnected, waiting for reconnection...");
			((Control)this).Invoke((Delegate)(Action)delegate
			{
				InsertLabelText("Device disconnected, waiting for reconnection...", Color.Orange);
			});
			return;
		}
		double totalSeconds = (DateTime.Now - deviceDisconnectedAt.Value).TotalSeconds;
		if (totalSeconds > 120.0)
		{
			Console.WriteLine($"[AUTO-RECONNECT] Timeout exceeded ({120}s), marking as permanently disconnected");
			isDeviceCurrentlyConnected = false;
			deviceDisconnectedAt = null;
			((Control)this).Invoke((Delegate)(Action)delegate
			{
				InsertLabelText("Device disconnected - timeout exceeded", Color.Red);
			});
		}
		else if ((int)totalSeconds % 10 == 0)
		{
			Console.WriteLine($"[AUTO-RECONNECT] Waiting for reconnection... ({(int)totalSeconds}s / {120}s)");
		}
	}

	private void HandleDeviceFoundInTimer(string udid)
	{
		deviceDisconnectedAt = null;
		if (isDeviceCurrentlyConnected)
		{
			return;
		}
		Console.WriteLine("[AUTO-RECONNECT] Device detected/reconnected: " + udid);
		Task.Run(async delegate
		{
			try
			{
				Console.WriteLine("[AUTO-RECONNECT] Attempting to reconnect lockdown/AFC services...");
				await Task.Delay(2000);
				if (ReconnectLockdown())
				{
					Console.WriteLine("[AUTO-RECONNECT] Lockdown/AFC services reconnected successfully");
					isDeviceCurrentlyConnected = true;
					((Control)this).Invoke((Delegate)(Action)delegate
					{
						InsertLabelText("Device reconnected successfully!", Color.Green);
					});
				}
				else
				{
					Console.WriteLine("[AUTO-RECONNECT] Failed to reconnect lockdown/AFC services");
				}
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				Console.WriteLine("[AUTO-RECONNECT] Reconnection error: " + ex.Message);
			}
		});
	}

	private void InitializeFormSettings()
	{

		((Form)this).FormBorderStyle = (FormBorderStyle)0;
		((Control)this).MouseDown += new MouseEventHandler(Form1_MouseDown);
		((Control)this).DoubleBuffered = true;
	}

	private void InitializeEventHandlers()
	{
		manager.CommonConnectEvent += CommonConnectDevice;
		manager.RecoveryConnectEvent += RecoveryConnectDevice;
		manager.ListenErrorEvent += ListenError;
	}

	private void InitializeSecurityTimer()
	{
		timer2 = new System.Threading.Timer(DetectEavesdroppingApps, null, 0, 2000);
	}

	private void StartDeviceListener()
	{
		Thread thread = new Thread(manager.StartListen);
		thread.IsBackground = true;
		thread.Start();
	}

	private void Form1_MouseDown(object sender, MouseEventArgs e)
	{

		if ((int)e.Button == 1048576)
		{
			ReleaseCapture();
			SendMessage(((Control)this).Handle, 161, 2, 0);
		}
	}

	private void panel1_MouseDown(object sender, MouseEventArgs e)
	{

		if ((int)e.Button == 1048576)
		{
			ReleaseCapture();
			SendMessage(((Control)this).Handle, 161, 2, 0);
		}
	}

	private void Form1_FormClosing(object sender, FormClosingEventArgs e)
	{
		CloseExitAPP("idevicebackup");
		CloseExitAPP("idevicebackup2");
		CloseExitAPP("ideviceinfo");
		CloseExitAPP("python");
		if (autoReconnectionTimer != null)
		{
			autoReconnectionTimer.Stop();
			autoReconnectionTimer.Dispose();
			Console.WriteLine("[AUTO-RECONNECT] Timer stopped and disposed");
		}
	}

	private bool IsAdministrator()
	{
		WindowsIdentity current = WindowsIdentity.GetCurrent();
		WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
		return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
	}

	private bool IsDebugging()
	{
		return false;
	}

	private async Task<bool> SetupEmbeddedPython()
	{
		try
		{
			Console.WriteLine("=======================================");
			Console.WriteLine("[Python Setup] Starting Python initialization...");
			UpdatelabelInfoProgres("Initializing Python...");
			string pythonDllPath = Path.Combine(pythonTargetPath, "python311.dll");
			string python311ZipPath = Path.Combine(pythonTargetPath, "python311.zip");
			if (Directory.Exists(pythonTargetPath))
			{
				bool needsReinstall = false;
				string reason = "";
				if (!File.Exists(pythonDllPath) || !File.Exists(python311ZipPath) || !File.Exists(Path.Combine(pythonTargetPath, "python.exe")))
				{
					needsReinstall = true;
					reason = "missing critical files";
				}
				else
				{
					long folderSizeMB = GetDirectorySize(pythonTargetPath) / 1048576;
					Console.WriteLine($"[Python Setup] Existing installation size: {folderSizeMB} MB");
					if (folderSizeMB < 140 || folderSizeMB > 280)
					{
						needsReinstall = true;
						reason = $"invalid size ({folderSizeMB} MB)";
					}
				}
				if (needsReinstall)
				{
					Console.WriteLine("[Python Setup] WARNING: Existing installation corrupted: " + reason);
					Console.WriteLine("[Python Setup] Cleaning and reinstalling...");
					UpdatelabelInfoProgres("Corrupted installation detected, reinstalling...");
					try
					{
						Directory.Delete(pythonTargetPath, recursive: true);
						await Task.Delay(500);
					}
					catch (Exception ex2)
					{
						Exception cleanEx = ex2;
						Console.WriteLine("[Python Setup] WARNING: Error cleaning: " + cleanEx.Message);
					}
				}
				else
				{
					Console.WriteLine("[Python Setup] SUCCESS: Existing installation is valid");
					UpdatelabelInfoProgres("Valid Python installation found");
				}
			}
			if (!Directory.Exists(pythonTargetPath) || !File.Exists(pythonDllPath) || !File.Exists(python311ZipPath))
			{
				Console.WriteLine("[Python Setup] Starting extraction process...");
				if (!(await ExtractEmbeddedPython()))
				{
					ShowManualSetupMessage();
					return false;
				}
				long finalSizeMB = GetDirectorySize(pythonTargetPath) / 1048576;
				if (finalSizeMB < 140 || finalSizeMB > 280)
				{
					Console.WriteLine($"[Python Setup] ERROR: Final validation failed: {finalSizeMB} MB");
					ShowManualSetupMessage();
					return false;
				}
			}
			Console.WriteLine("[Python Setup] Starting configuration...");
			UpdatelabelInfoProgres("Configuring Python...");
			if (!ConfigurePythonEnvironment(pythonDllPath))
			{
				Console.WriteLine("[Python Setup] Configuration failed, attempting reinstall...");
				try
				{
					if (Directory.Exists(pythonTargetPath))
					{
						Directory.Delete(pythonTargetPath, recursive: true);
					}
					await Task.Delay(500);
					if (!(await ExtractEmbeddedPython()))
					{
						ShowManualSetupMessage();
						return false;
					}
					if (!ConfigurePythonEnvironment(pythonDllPath))
					{
						ShowManualSetupMessage();
						return false;
					}
					Console.WriteLine("[Python Setup] SUCCESS: Reinstall successful");
				}
				catch
				{
					ShowManualSetupMessage();
					return false;
				}
			}
			Console.WriteLine("[Python Setup] Starting verification...");
			UpdatelabelInfoProgres("Verifying Python...");
			if (!VerifyPythonInitialization())
			{
				ShowManualSetupMessage();
				return false;
			}
			long installedSizeMB = GetDirectorySize(pythonTargetPath) / 1048576;
			Console.WriteLine($"[Python Setup] SUCCESS: Python is ready! Installation size: {installedSizeMB} MB");
			Console.WriteLine("=======================================");
			UpdatelabelInfoProgres($"Python ready! ({installedSizeMB} MB)");
			isPythonReady = true;
			return true;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[Python Setup] FATAL ERROR: " + ex.Message);
			ShowManualSetupMessage();
			return false;
		}
	}

	private void ShowManualSetupMessage()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string text = Path.Combine(baseDirectory, "Utils", "python.zip");
		string text2 = "Python initialization failed\n\nMANUAL SETUP:\n\n1. Extract python.zip to:\n   " + pythonTargetPath + "\n\n2. Verify these files exist:\n   - python311.dll\n   - python311.zip\n   - python.exe\n\n3. Restart the application\n\nSource file: " + text + "\n\nThe app will continue without Python-dependent features.\n\nTarget path copied to clipboard";
		try
		{
			Clipboard.SetText(pythonTargetPath);
		}
		catch
		{
		}
		MessageBox.Show(text2, "Python Setup Required", (MessageBoxButtons)0, (MessageBoxIcon)64);
	}

	private async Task<bool> ExtractEmbeddedPython()
	{
		return await Task.Run(async delegate
		{
			string pythonZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utils", "python.zip");
			if (!File.Exists(pythonZipPath))
			{
				Console.WriteLine("[Python Extract] ERROR: python.zip not found at: " + pythonZipPath);
				return false;
			}
			for (int attempt = 1; attempt <= 2; attempt++)
			{
				try
				{
					Console.WriteLine($"[Python Extract] Extraction attempt {attempt}/2...");
					if (Directory.Exists(pythonTargetPath))
					{
						try
						{
							Directory.Delete(pythonTargetPath, recursive: true);
						}
						catch
						{
						}
						await Task.Delay(500);
					}
					Directory.CreateDirectory(pythonTargetPath);
					using (FileStream fs = new FileStream(pythonZipPath, FileMode.Open, FileAccess.Read))
					{
						using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read);
						int total = archive.Entries.Count;
						int count = 0;
						foreach (ZipArchiveEntry entry in archive.Entries)
						{
							try
							{
								string destPath = Path.Combine(pythonTargetPath, entry.FullName.Replace("/", "\\"));
								if (entry.FullName.EndsWith("/"))
								{
									Directory.CreateDirectory(destPath);
								}
								else
								{
									string dirPath = Path.GetDirectoryName(destPath);
									if (!Directory.Exists(dirPath))
									{
										Directory.CreateDirectory(dirPath);
									}
									entry.ExtractToFile(destPath, overwrite: true);
								}
								count++;
								if (count % 500 == 0)
								{
									int pct = count * 100 / total;
									((Control)this).Invoke((Delegate)(Action)delegate
									{
										UpdatelabelInfoProgres($"Extracting... {pct}% (Attempt {attempt})");
									});
								}
							}
							catch (Exception ex2)
							{
								Console.WriteLine("[Python Extract] WARNING: Failed: " + entry.FullName + " - " + ex2.Message);
							}
						}
					}
					Console.WriteLine("[Python Extract] Waiting for filesystem sync...");
					await Task.Delay(3000);
					long folderSizeMB = GetDirectorySize(pythonTargetPath) / 1048576;
					Console.WriteLine($"[Python Extract] Folder size: {folderSizeMB} MB");
					if (folderSizeMB < 140 || folderSizeMB > 280)
					{
						Console.WriteLine($"[Python Extract] ERROR: Invalid size: {folderSizeMB} MB (expected 140-280 MB)");
						if (attempt != 1)
						{
							Console.WriteLine("[Python Extract] ERROR: Failed after 2 attempts");
							try
							{
								if (Directory.Exists(pythonTargetPath))
								{
									Directory.Delete(pythonTargetPath, recursive: true);
								}
							}
							catch
							{
							}
							return false;
						}
						Console.WriteLine("[Python Extract] Retrying extraction...");
						((Control)this).Invoke((Delegate)(Action)delegate
						{
							UpdatelabelInfoProgres("Extraction failed, retrying...");
						});
						await Task.Delay(1000);
					}
					else
					{
						if (File.Exists(Path.Combine(pythonTargetPath, "python311.dll")) && File.Exists(Path.Combine(pythonTargetPath, "python311.zip")) && File.Exists(Path.Combine(pythonTargetPath, "python.exe")))
						{
							Console.WriteLine($"[Python Extract] SUCCESS: Extraction successful: {folderSizeMB} MB (Attempt {attempt})");
							((Control)this).Invoke((Delegate)(Action)delegate
							{
								UpdatelabelInfoProgres($"Extraction complete: {folderSizeMB} MB");
							});
							return true;
						}
						Console.WriteLine("[Python Extract] ERROR: Critical files missing");
						if (attempt != 1)
						{
							Console.WriteLine("[Python Extract] ERROR: Critical files still missing after retry");
							return false;
						}
						Console.WriteLine("[Python Extract] Retrying extraction...");
						((Control)this).Invoke((Delegate)(Action)delegate
						{
							UpdatelabelInfoProgres("Critical files missing, retrying...");
						});
						await Task.Delay(1000);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[Python Extract] ERROR attempt {attempt}: {ex.Message}");
					if (attempt != 1)
					{
						Console.WriteLine("[Python Extract] ERROR: Failed after 2 attempts");
						return false;
					}
					Console.WriteLine("[Python Extract] Retrying due to exception...");
					((Control)this).Invoke((Delegate)(Action)delegate
					{
						UpdatelabelInfoProgres("Extraction error, retrying...");
					});
					await Task.Delay(2000);
				}
			}
			return false;
		});
	}

	private long GetDirectorySize(string path)
	{
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum((FileInfo file) => file.Length);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[GetDirectorySize] Error: " + ex.Message);
			return 0L;
		}
	}

	private bool ConfigurePythonEnvironment(string pythonDllPath)
	{
		try
		{
			string text = Path.Combine(pythonTargetPath, "python311.zip");
			if (!File.Exists(text) || !File.Exists(pythonDllPath))
			{
				Console.WriteLine("[Python Config] ERROR: Critical files missing");
				return false;
			}
			string text2 = Path.Combine(pythonTargetPath, "Lib");
			string text3 = Path.Combine(text2, "site-packages");
			Directory.CreateDirectory(text2);
			Directory.CreateDirectory(text3);
			Environment.SetEnvironmentVariable("PYTHONHOME", pythonTargetPath);
			Environment.SetEnvironmentVariable("PYTHONPATH", text + ";" + text2 + ";" + text3);
			Environment.SetEnvironmentVariable("PYTHONIOENCODING", "utf-8");
			Environment.SetEnvironmentVariable("PYTHONDONTWRITEBYTECODE", "1");
			Runtime.PythonDLL = pythonDllPath;
			if (!PythonEngine.IsInitialized)
			{
				PythonEngine.Initialize();
			}
			Console.WriteLine("[Python Config] SUCCESS: Configuration successful");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[Python Config] ERROR: " + ex.Message);
			return false;
		}
	}

	private bool VerifyPythonInitialization()
	{
		try
		{
			if (!PythonEngine.IsInitialized)
			{
				return false;
			}
			GILState val = Py.GIL();
			try
			{
				object obj = Py.Import("sys");
				Py.Import("os");
				Py.Import("json");
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			Console.WriteLine("[Python Verify] SUCCESS: Verification successful");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[Python Verify] ERROR: " + ex.Message);
			return false;
		}
	}

	private void UpdatelabelInfoProgres(string text)
	{
		if (((Control)labelInfoProgres).InvokeRequired)
		{
			((Control)labelInfoProgres).Invoke((Delegate)new Action<string>(UpdatelabelInfoProgres), new object[1] { text });
		}
		else
		{
			((Control)labelInfoProgres).Text = text;
		}
	}

	private void DetectEavesdroppingApps(object state)
	{

		if (alertShown)
		{
			return;
		}
		Process[] processes = Process.GetProcesses();
		Process[] array = processes;
		foreach (Process process in array)
		{
			string item = process.ProcessName.ToLower();
			if (blacklist.Contains(item))
			{
				HandleSecurityViolation("Blacklisted process detected");
				return;
			}
		}
		if (IsDebugging())
		{
			MessageBox.Show("Debugger detected. The application will close.", "Security Alert", (MessageBoxButtons)0, (MessageBoxIcon)64);
			HandleSecurityViolation("Debugger detected");
		}
		else if (IsProxyEnabled())
		{
			MessageBox.Show("Proxy detected. Please disable proxy settings.", "Security Alert", (MessageBoxButtons)0, (MessageBoxIcon)64);
			HandleSecurityViolation("Proxy detected");
		}
	}

	private void HandleSecurityViolation(string reason)
	{
		alertShown = true;
		string toolip = GetToolip();
		string macAddress = GetMacAddress();
		string localIPAddress = GetLocalIPAddress();
		string machineName = Environment.MachineName;
		Application.Exit();
	}

	private bool IsProxyEnabled()
	{
		object value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable", null);
		object value2 = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer", null);
		if (value != null && (int)value == 1 && value2 != null)
		{
			string text = value2.ToString().ToLower();
			if (text.Contains("fiddler") || text.Contains("proxyman"))
			{
				return true;
			}
		}
		if (IsFiddlerOrProxymanRunning())
		{
			return true;
		}
		if (IsProxyInBrowser())
		{
			return true;
		}
		return false;
	}

	private bool IsFiddlerOrProxymanRunning()
	{
		Process[] processes = Process.GetProcesses();
		Process[] array = processes;
		foreach (Process process in array)
		{
			string text = process.ProcessName.ToLower();
			if (text.Contains("fiddler") || text.Contains("proxyman"))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsProxyInBrowser()
	{
		string chromeProxySettings = GetChromeProxySettings();
		if (!string.IsNullOrEmpty(chromeProxySettings) && chromeProxySettings.Contains("proxy") && (chromeProxySettings.Contains("fiddler") || chromeProxySettings.Contains("proxyman")))
		{
			return true;
		}
		string firefoxProxySettings = GetFirefoxProxySettings();
		if (!string.IsNullOrEmpty(firefoxProxySettings) && (firefoxProxySettings.Contains("fiddler") || firefoxProxySettings.Contains("proxyman")))
		{
			return true;
		}
		return false;
	}

	private string GetChromeProxySettings()
	{
		using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Google\\Chrome\\BLBeacon"))
		{
			if (registryKey == null)
			{
				return "Google Chrome is not installed or no proxy settings found.";
			}
		}
		object value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Google\\Chrome\\BLBeacon", "browser", null);
		if (value != null)
		{
			return value.ToString();
		}
		return "No proxy settings found in Chrome.";
	}

	private string GetFirefoxProxySettings()
	{
		string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mozilla\\Firefox\\Profiles\\";
		if (!Directory.Exists(path))
		{
			return "Firefox profile directory not found.";
		}
		string text = Directory.GetFiles(path, "prefs.js", SearchOption.AllDirectories).FirstOrDefault();
		if (text != null && File.Exists(text))
		{
			string[] array = File.ReadAllLines(text);
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text2.Contains("network.proxy") && (text2.Contains("fiddler") || text2.Contains("proxyman")))
				{
					return text2;
				}
			}
		}
		return string.Empty;
	}

	private string GetMacAddress()
	{
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		string text = "";
		NetworkInterface[] array = allNetworkInterfaces;
		foreach (NetworkInterface networkInterface in array)
		{
			if (networkInterface.OperationalStatus == OperationalStatus.Up)
			{
				text = text + networkInterface.GetPhysicalAddress().ToString() + "\n";
			}
		}
		return text.Trim();
	}

	private string GetLocalIPAddress()
	{
		string hostName = Dns.GetHostName();
		string result = "";
		IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);
		foreach (IPAddress iPAddress in hostAddresses)
		{
			if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
			{
				result = iPAddress.ToString();
				break;
			}
		}
		return result;
	}

	private string GetToolip()
	{
		try
		{
			string requestUriString = "https://api.ipify.org/";
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
			httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			httpWebRequest.Timeout = -1;
			string result;
			using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
				result = streamReader.ReadToEnd();
			}
			return result;
		}
		catch
		{
			return "ERROR";
		}
	}

	private async void CheckVersionAsync()
	{
		string versionUrl = H1010X9191("aHR0cHM6Ly9lc3BpbmdhcmRhcmlhbmV2ZXMuY29tL2Jpbi9jb21tb24vdmVyc2lvbi50eHQ=");
		try
		{
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
				string serverVersionString = (await httpClient.GetStringAsync(versionUrl)).Trim();
				Version serverVersion = new Version(serverVersionString);
				Version toolVersion = new Version("1.4");
				int comparisonResult = serverVersion.CompareTo(toolVersion);
				if (comparisonResult != 0)
				{
					string message = ((comparisonResult > 0) ? ("The tool is outdated. Please update to version " + serverVersionString + ".") : ("The server is under maintenance. Please check for updates to the tool. Server version: " + serverVersionString + "."));
					((Control)this).Invoke((Delegate)(MethodInvoker)delegate
					{
						//IL_0019: Unknown result type (might be due to invalid IL or missing references)
						//IL_001e: Unknown result type (might be due to invalid IL or missing references)
						DialogResult val3 = MessageBox.Show(message + " Do you want to update the tool?", "Notification", (MessageBoxButtons)4, (MessageBoxIcon)64);
					});
					Application.ExitThread();
					Environment.Exit(0);
				}
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (HttpRequestException val)
		{
			HttpRequestException val2 = val;
			HttpRequestException ex2 = val2;
			HandleVersionCheckError("An error occurred while checking the tool version: " + ((Exception)(object)ex2).Message, "Network Error");
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			HandleVersionCheckError("An error occurred while checking the tool version: " + ex.Message, "General Error");
		}
	}

	private void HandleVersionCheckError(string message, string title)
	{

		((Control)this).Invoke((Delegate)(MethodInvoker)delegate
		{

			MessageBox.Show(message, title, (MessageBoxButtons)0, (MessageBoxIcon)16);
			Application.ExitThread();
			Environment.Exit(0);
		});
	}

	private void ListenError(object sender, ListenErrorEventHandlerEventArgs args)
	{

		if (args.ErrorType != 0)
		{
			return;
		}
		string errorMessage = args.ErrorMessage;
		bool is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
		string[] array = new string[2] { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall" };
		bool flag = false;
		bool flag2 = false;
		string[] array2 = array;
		foreach (string name in array2)
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
			if (registryKey == null)
			{
				continue;
			}
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string name2 in subKeyNames)
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey(name2);
				string text = registryKey2?.GetValue("DisplayName")?.ToString();
				if (!string.IsNullOrEmpty(text) && text.Contains("Microsoft Visual C++ 2015"))
				{
					if (text.Contains("(x64)"))
					{
						flag = true;
					}
					if (text.Contains("(x86)"))
					{
						flag2 = true;
					}
				}
			}
		}
		if (is64BitOperatingSystem && !flag)
		{
			string text2 = Path.Combine(Application.StartupPath, "Fix_Folder", "VC_redist.x64.exe");
			if (File.Exists(text2))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = text2,
					Arguments = "/install /quiet /norestart",
					UseShellExecute = true,
					Verb = "runas"
				})?.WaitForExit();
			}
		}
		if (!flag2)
		{
			string text3 = Path.Combine(Application.StartupPath, "Fix_Folder", "VC_redist.x86.exe");
			if (File.Exists(text3))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = text3,
					Arguments = "/install /quiet /norestart",
					UseShellExecute = true,
					Verb = "runas"
				})?.WaitForExit();
			}
		}
		string text4 = Path.Combine(Application.StartupPath, "Fix_Folder", is64BitOperatingSystem ? "AppleMobileDeviceSupportx64.msi" : "AppleMobileDeviceSupportx86.msi");
		if (File.Exists(text4))
		{
			MessageBox.Show("Apple Mobile Device Support not detected. Installing now...", "Installing Component", (MessageBoxButtons)0, (MessageBoxIcon)64);
			Process.Start(new ProcessStartInfo
			{
				FileName = "msiexec.exe",
				Arguments = "/i \"" + text4 + "\" /quiet /norestart",
				UseShellExecute = true,
				Verb = "runas"
			})?.WaitForExit();
			MessageBox.Show("Installation complete. Please restart the application.", "Installation Complete", (MessageBoxButtons)0, (MessageBoxIcon)64);
		}
		else
		{
			MessageBox.Show("Apple Mobile Device Support is not installed and installer not found.\n\nPlease place " + (is64BitOperatingSystem ? "AppleMobileDeviceSupportx64.msi" : "AppleMobileDeviceSupportx86.msi") + " in Fix_Folder", "Component Required", (MessageBoxButtons)0, (MessageBoxIcon)48);
		}
		Application.Exit();
	}

	private async void CommonConnectDevice(object sender, DeviceCommonConnectEventArgs args)
	{
		if (args.Message == ConnectNotificationMessage.Connected)
		{
			currentiOSDevice = args.Device;
			isDeviceCurrentlyConnected = true;
			deviceDisconnectedAt = null;
			Console.WriteLine("[AUTO-RECONNECT] Device connected: " + args.Device.UniqueDeviceID);
			Console.WriteLine("[AUTO-RECONNECT] Waiting for device stabilization...");
			await Task.Delay(3000);
			if (!(await InitializeAfcWithRetry(3)))
			{
				Console.WriteLine("[AUTO-RECONNECT] AFC initialization failed, but continuing...");
			}
			((Control)this).Invoke((Delegate)(Action)async delegate
			{
				await HandleDeviceConnected();
			});
		}
		else if (args.Message == ConnectNotificationMessage.Disconnected)
		{
			deviceDisconnectedAt = DateTime.Now;
			Console.WriteLine($"[AUTO-RECONNECT] Device disconnected at {DateTime.Now:HH:mm:ss}");
			((Control)this).Invoke((Delegate)(Action)delegate
			{
				HandleDeviceDisconnected();
			});
		}
	}

	private async Task<bool> InitializeAfcWithRetry(int maxAttempts)
	{
		ReadOnlyCollection<string> rootFiles = default(ReadOnlyCollection<string>);
		for (int attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				Console.WriteLine($"[AFC] Initialization attempt {attempt}/{maxAttempts}");
				string udid = currentiOSDevice.UniqueDeviceID;
				CleanupAfcHandles();
				iDeviceErrorExtensions.ThrowOnError(idevice.idevice_new(ref deviceHandle, udid));
				LockdownErrorExtensions.ThrowOnError(lockdown.lockdownd_client_new_with_handshake(deviceHandle, ref lockdownHandle, "Titan"));
				LockdownErrorExtensions.ThrowOnError(lockdown.lockdownd_start_service(lockdownHandle, "com.apple.afc", ref lockdownServiceHandle));
				AfcErrorExtensions.ThrowOnError(lockdownHandle.Api.Afc.afc_client_new(deviceHandle, lockdownServiceHandle, ref afcHandle));
				AfcError testResult = afc.afc_read_directory(afcHandle, "/", ref rootFiles);
				if ((int)testResult == 0)
				{
					Console.WriteLine($"[AFC] Initialized successfully on attempt {attempt}");
					if (afcReconnectionTcs != null && !afcReconnectionTcs.Task.IsCompleted)
					{
						afcReconnectionTcs.SetResult(result: true);
					}
					return true;
				}
				Console.WriteLine($"[AFC] Verification failed: {testResult}");
				CleanupAfcHandles();
				rootFiles = null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AFC] Attempt {attempt} failed: {ex.Message}");
				CleanupAfcHandles();
			}
			if (attempt < maxAttempts)
			{
				await Task.Delay(2000);
			}
		}
		Console.WriteLine($"[AFC] Failed after {maxAttempts} attempts");
		if (afcReconnectionTcs != null && !afcReconnectionTcs.Task.IsCompleted)
		{
			afcReconnectionTcs.SetResult(result: false);
		}
		return false;
	}

	private void CleanupAfcHandles()
	{
		try
		{
			if (afcHandle != (AfcClientHandle)null && !((SafeHandle)(object)afcHandle).IsInvalid)
			{
				((SafeHandle)(object)afcHandle).Dispose();
				afcHandle = null;
			}
			if (lockdownServiceHandle != (LockdownServiceDescriptorHandle)null && !((SafeHandle)(object)lockdownServiceHandle).IsInvalid)
			{
				((SafeHandle)(object)lockdownServiceHandle).Dispose();
				lockdownServiceHandle = null;
			}
			if (lockdownHandle != (LockdownClientHandle)null && !((SafeHandle)(object)lockdownHandle).IsInvalid)
			{
				((SafeHandle)(object)lockdownHandle).Dispose();
				lockdownHandle = null;
			}
			if (deviceHandle != (iDeviceHandle)null && !((SafeHandle)(object)deviceHandle).IsInvalid)
			{
				((SafeHandle)(object)deviceHandle).Dispose();
				deviceHandle = null;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[AFC] Cleanup error: " + ex.Message);
		}
	}

	public async Task<bool> WaitForAfcReconnection(int timeoutSeconds = 90)
	{
		Console.WriteLine($"[AFC WAIT] Waiting for reconnection (timeout: {timeoutSeconds}s)");
		afcReconnectionTcs = new TaskCompletionSource<bool>();
		Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
		if (await Task.WhenAny(new Task[2] { afcReconnectionTcs.Task, timeoutTask }) == timeoutTask)
		{
			Console.WriteLine("[AFC WAIT] Timeout reached");
			return false;
		}
		bool result = await afcReconnectionTcs.Task;
		Console.WriteLine($"[AFC WAIT] Reconnection result: {result}");
		return result;
	}

	private async Task HandleDeviceConnected()
	{
		((Control)pictureBoxDC).SendToBack();
		((Control)pictureBoxDC).Visible = false;
		int maxRetries = 3;
		bool infoLoaded = false;
		for (int attempt = 1; attempt <= maxRetries; attempt++)
		{
			if (infoLoaded)
			{
				break;
			}
			try
			{
				Console.WriteLine($"[DEVICE INFO] Loading attempt {attempt}/{maxRetries}");
				if (attempt != 1)
				{
					await Task.Delay(1000);
				}
				else
				{
					await Task.Delay(2000);
				}
				if (currentiOSDevice != null && !string.IsNullOrEmpty(currentiOSDevice.ProductType) && !string.IsNullOrEmpty(currentiOSDevice.SerialNumber))
				{
					if (!string.IsNullOrEmpty(lastConnectedUdid) && currentiOSDevice.UniqueDeviceID == lastConnectedUdid)
					{
						Console.WriteLine("[RECONNECT] Same device: " + lastConnectedUdid);
						RestoreDeviceData();
					}
					else
					{
						Console.WriteLine("[RECONNECT] New device detected");
						float zoomFactor = 2f;
						await LoadImageWithZoomAsync(zoomFactor);
						UpdateDeviceModel();
						UpdateDeviceInfo();
						lastConnectedUdid = currentiOSDevice.UniqueDeviceID;
					}
					infoLoaded = true;
					Console.WriteLine($"[DEVICE INFO] Loaded successfully on attempt {attempt}");
				}
				else
				{
					Console.WriteLine($"[DEVICE INFO] Device data incomplete on attempt {attempt}");
					await RefreshDeviceInfo();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DEVICE INFO] Attempt {attempt} failed: {ex.Message}");
				if (attempt == maxRetries && !string.IsNullOrEmpty(lastDeviceModel))
				{
					RestoreDeviceData();
					infoLoaded = true;
				}
			}
		}
		if (!infoLoaded)
		{
			Console.WriteLine("[DEVICE INFO] Failed to load device information after all attempts");
			InsertLabelText("Unable to load device info. Please reconnect.", Color.Orange);
		}
		await ShowElementsAsync();
	}

	private async Task RefreshDeviceInfo()
	{
		try
		{
			if (currentiOSDevice == null)
			{
				Console.WriteLine("[REFRESH] Device is null, waiting...");
				await Task.Delay(1000);
				return;
			}
			_ = currentiOSDevice.UniqueDeviceID;
			if (lockdownHandle != (LockdownClientHandle)null && !((SafeHandle)(object)lockdownHandle).IsInvalid)
			{
				Console.WriteLine("[REFRESH] Refreshing device properties via lockdown");
			}
			await Task.Delay(500);
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[REFRESH] Error: " + ex.Message);
		}
	}

	private void RestoreDeviceData()
	{
		try
		{
			Console.WriteLine("[RESTORE] Restoring saved device data...");
			((Control)ModeloffHello).Text = ((!string.IsNullOrEmpty(lastDeviceModel)) ? lastDeviceModel : "Unknown");
			((Control)labelType).Text = ((!string.IsNullOrEmpty(lastDeviceType)) ? lastDeviceType : "Unknown");
			((Control)labelSN).Text = ((!string.IsNullOrEmpty(lastDeviceSN)) ? lastDeviceSN : "Unknown");
			((Control)labelVersion).Text = ((!string.IsNullOrEmpty(lastDeviceVersion)) ? lastDeviceVersion : "Unknown");
			((Control)labelActivaction).Text = ((!string.IsNullOrEmpty(lastDeviceActivation)) ? lastDeviceActivation : "Unknown");
			((Control)labelRegion).Text = ((!string.IsNullOrEmpty(lastDeviceRegion)) ? lastDeviceRegion : "Unknown");
			Console.WriteLine("[RESTORE] Data restored - Model: " + ((Control)ModeloffHello).Text + ", SN: " + ((Control)labelSN).Text);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[RESTORE] Error: " + ex.Message);
		}
	}

	private void HandleDeviceDisconnected()
	{
		if (currentiOSDevice != null)
		{
			lastConnectedUdid = currentiOSDevice.UniqueDeviceID;
			lastDeviceModel = ((Control)ModeloffHello).Text;
			lastDeviceType = ((Control)labelType).Text;
			lastDeviceSN = ((Control)labelSN).Text;
			lastDeviceVersion = ((Control)labelVersion).Text;
			lastDeviceActivation = ((Control)labelActivaction).Text;
			lastDeviceECID = currentiOSDevice.UniqueChipID;
			lastDeviceRegion = ((Control)labelRegion).Text;
			Console.WriteLine("[DISCONNECT] Saved device data for UDID: " + lastConnectedUdid);
			Console.WriteLine("[DISCONNECT] Model: " + lastDeviceModel + ", SN: " + lastDeviceSN);
		}
		((Control)pictureBoxDC).BringToFront();
		((Control)pictureBoxDC).Visible = true;
		((Control)this).Show();
	}

	private void UpdateDeviceModel()
	{
		switch (currentiOSDevice.ProductType)
		{
		case "iPhone6,1":
		case "iPhone6,2":
			((Control)ModeloffHello).Text = "iPhone 5S";
			break;
		case "iPhone7,2":
			((Control)ModeloffHello).Text = "iPhone 6";
			break;
		case "iPhone7,1":
			((Control)ModeloffHello).Text = "iPhone 6 Plus";
			break;
		case "iPhone8,1":
			((Control)ModeloffHello).Text = "iPhone 6S";
			break;
		case "iPhone8,2":
			((Control)ModeloffHello).Text = "iPhone 6S Plus";
			break;
		case "iPhone8,4":
			((Control)ModeloffHello).Text = "iPhone SE";
			break;
		case "iPhone9,1":
		case "iPhone9,3":
			((Control)ModeloffHello).Text = "iPhone 7";
			break;
		case "iPhone9,2":
		case "iPhone9,4":
			((Control)ModeloffHello).Text = "iPhone 7 Plus";
			break;
		case "iPhone10,1":
		case "iPhone10,4":
			((Control)ModeloffHello).Text = "iPhone 8";
			break;
		case "iPhone10,2":
		case "iPhone10,5":
			((Control)ModeloffHello).Text = "iPhone 8 Plus";
			break;
		case "iPhone10,3":
		case "iPhone10,6":
			((Control)ModeloffHello).Text = "iPhone X";
			break;
		case "iPhone11,2":
			((Control)ModeloffHello).Text = "iPhone Xs";
			break;
		case "iPhone11,4":
		case "iPhone11,6":
			((Control)ModeloffHello).Text = "iPhone Xs Max";
			break;
		case "iPhone11,8":
			((Control)ModeloffHello).Text = "iPhone Xr";
			break;
		case "iPhone12,1":
			((Control)ModeloffHello).Text = "iPhone 11";
			break;
		case "iPhone12,3":
			((Control)ModeloffHello).Text = "iPhone 11 Pro";
			break;
		case "iPhone12,5":
			((Control)ModeloffHello).Text = "iPhone 11 Pro Max";
			break;
		case "iPhone12,8":
			((Control)ModeloffHello).Text = "iPhone SE 2";
			break;
		case "iPhone13,1":
			((Control)ModeloffHello).Text = "iPhone 12 mini";
			break;
		case "iPhone13,2":
			((Control)ModeloffHello).Text = "iPhone 12";
			break;
		case "iPhone13,3":
			((Control)ModeloffHello).Text = "iPhone 12 Pro";
			break;
		case "iPhone13,4":
			((Control)ModeloffHello).Text = "iPhone 12 Pro Max";
			break;
		case "iPhone14,4":
			((Control)ModeloffHello).Text = "iPhone 13 mini";
			break;
		case "iPhone14,5":
			((Control)ModeloffHello).Text = "iPhone 13";
			break;
		case "iPhone14,2":
			((Control)ModeloffHello).Text = "iPhone 13 Pro";
			break;
		case "iPhone14,3":
			((Control)ModeloffHello).Text = "iPhone 13 Pro Max";
			break;
		case "iPhone14,6":
			((Control)ModeloffHello).Text = "iPhone SE 3";
			break;
		case "iPhone14,7":
			((Control)ModeloffHello).Text = "iPhone 14";
			break;
		case "iPhone14,8":
			((Control)ModeloffHello).Text = "iPhone 14 Plus";
			break;
		case "iPhone15,2":
			((Control)ModeloffHello).Text = "iPhone 14 Pro";
			break;
		case "iPhone15,3":
			((Control)ModeloffHello).Text = "iPhone 14 Pro Max";
			break;
		case "iPhone15,4":
			((Control)ModeloffHello).Text = "iPhone 15";
			break;
		case "iPhone15,5":
			((Control)ModeloffHello).Text = "iPhone 15 Plus";
			break;
		case "iPhone16,1":
			((Control)ModeloffHello).Text = "iPhone 15 Pro";
			break;
		case "iPhone16,2":
			((Control)ModeloffHello).Text = "iPhone 15 Pro Max";
			break;
		case "iPhone17,3":
			((Control)ModeloffHello).Text = "iPhone 16";
			break;
		case "iPhone17,4":
			((Control)ModeloffHello).Text = "iPhone 16 Plus";
			break;
		case "iPhone17,1":
			((Control)ModeloffHello).Text = "iPhone 16 Pro";
			break;
		case "iPhone17,2":
			((Control)ModeloffHello).Text = "iPhone 16 Pro Max";
			break;
		case "iPhone17,5":
			((Control)ModeloffHello).Text = "iPhone 16 e";
			break;
		case "iPhone18,1":
			((Control)ModeloffHello).Text = "iPhone 17 Pro";
			break;
		case "iPhone18,2":
			((Control)ModeloffHello).Text = "iPhone 17 Pro Max";
			break;
		case "iPhone18,3":
			((Control)ModeloffHello).Text = "iPhone 17";
			break;
		case "iPhone18,4":
			((Control)ModeloffHello).Text = "iPhone Air";
			break;
		case "iPad2,1":
		case "iPad2,2":
		case "iPad2,3":
		case "iPad2,4":
			((Control)ModeloffHello).Text = "iPad 2";
			break;
		case "iPad3,1":
		case "iPad3,2":
		case "iPad3,3":
			((Control)ModeloffHello).Text = "iPad 3";
			break;
		case "iPad3,4":
		case "iPad3,5":
		case "iPad3,6":
			((Control)ModeloffHello).Text = "iPad 4";
			break;
		case "iPad6,11":
		case "iPad6,12":
			((Control)ModeloffHello).Text = "iPad 5";
			break;
		case "iPad7,5":
		case "iPad7,6":
			((Control)ModeloffHello).Text = "iPad 6";
			break;
		case "iPad7,11":
		case "iPad7,12":
			((Control)ModeloffHello).Text = "iPad 7";
			break;
		case "iPad11,6":
		case "iPad11,7":
			((Control)ModeloffHello).Text = "iPad 8";
			break;
		case "iPad12,1":
		case "iPad12,2":
			((Control)ModeloffHello).Text = "iPad 9";
			break;
		case "iPad13,18":
		case "iPad13,19":
			((Control)ModeloffHello).Text = "iPad 10";
			break;
		case "iPad4,1":
		case "iPad4,2":
		case "iPad4,3":
			((Control)ModeloffHello).Text = "iPad Air";
			break;
		case "iPad5,3":
		case "iPad5,4":
			((Control)ModeloffHello).Text = "iPad Air 2";
			break;
		case "iPad11,3":
		case "iPad11,4":
			((Control)ModeloffHello).Text = "iPad Air 3";
			break;
		case "iPad13,1":
		case "iPad13,2":
			((Control)ModeloffHello).Text = "iPad Air 4";
			break;
		case "iPad13,16":
		case "iPad13,17":
			((Control)ModeloffHello).Text = "iPad Air 5";
			break;
		case "iPad14,8":
		case "iPad14,9":
			((Control)ModeloffHello).Text = "iPad Air 11-inch (M2)";
			break;
		case "iPad14,10":
		case "iPad14,11":
			((Control)ModeloffHello).Text = "iPad Air 13-inch (M2)";
			break;
		case "iPad2,5":
		case "iPad2,6":
		case "iPad2,7":
			((Control)ModeloffHello).Text = "iPad Mini";
			break;
		case "iPad4,4":
		case "iPad4,5":
		case "iPad4,6":
			((Control)ModeloffHello).Text = "iPad Mini 2";
			break;
		case "iPad4,7":
		case "iPad4,8":
		case "iPad4,9":
			((Control)ModeloffHello).Text = "iPad Mini 3";
			break;
		case "iPad5,1":
		case "iPad5,2":
			((Control)ModeloffHello).Text = "iPad Mini 4";
			break;
		case "iPad11,1":
		case "iPad11,2":
			((Control)ModeloffHello).Text = "iPad Mini 5";
			break;
		case "iPad14,1":
		case "iPad14,2":
			((Control)ModeloffHello).Text = "iPad Mini 6";
			break;
		case "iPad6,3":
		case "iPad6,4":
			((Control)ModeloffHello).Text = "iPad Pro 9.7-inch";
			break;
		case "iPad7,3":
		case "iPad7,4":
			((Control)ModeloffHello).Text = "iPad Pro 10.5-inch";
			break;
		case "iPad8,1":
		case "iPad8,2":
		case "iPad8,3":
		case "iPad8,4":
			((Control)ModeloffHello).Text = "iPad Pro 11-inch";
			break;
		case "iPad8,9":
		case "iPad8,10":
			((Control)ModeloffHello).Text = "iPad Pro 11-inch 2";
			break;
		case "iPad13,4":
		case "iPad13,5":
		case "iPad13,6":
		case "iPad13,7":
			((Control)ModeloffHello).Text = "iPad Pro 11-inch 3";
			break;
		case "iPad14,3":
		case "iPad14,4":
			((Control)ModeloffHello).Text = "iPad Pro 11-inch (M2)";
			break;
		case "iPad16,3":
		case "iPad16,4":
			((Control)ModeloffHello).Text = "iPad Pro 11-inch (M4)";
			break;
		case "iPad6,7":
		case "iPad6,8":
			((Control)ModeloffHello).Text = "iPad Pro 12.9-inch";
			break;
		case "iPad7,1":
		case "iPad7,2":
			((Control)ModeloffHello).Text = "iPad Pro 12.9-inch 2";
			break;
		case "iPad8,5":
		case "iPad8,6":
		case "iPad8,7":
		case "iPad8,8":
			((Control)ModeloffHello).Text = "iPad Pro 12.9-inch 3";
			break;
		case "iPad8,11":
		case "iPad8,12":
			((Control)ModeloffHello).Text = "iPad Pro 12.9-inch 4";
			break;
		case "iPad13,8":
		case "iPad13,9":
		case "iPad13,10":
		case "iPad13,11":
			((Control)ModeloffHello).Text = "iPad Pro 12.9-inch 5";
			break;
		case "iPad14,5":
		case "iPad14,6":
			((Control)ModeloffHello).Text = "iPad Pro 12.9-inch (M2)";
			break;
		case "iPad16,5":
		case "iPad16,6":
			((Control)ModeloffHello).Text = "iPad Pro 13-inch (M4)";
			break;
		case "iPod4,1":
			((Control)ModeloffHello).Text = "iPod Touch 4";
			break;
		case "iPod5,1":
			((Control)ModeloffHello).Text = "iPod Touch 5";
			break;
		case "iPod7,1":
			((Control)ModeloffHello).Text = "iPod Touch 6";
			break;
		case "iPod9,1":
			((Control)ModeloffHello).Text = "iPod Touch 7";
			break;
		default:
			((Control)ModeloffHello).Text = "Unknown Model";
			pictureBoxModel.Image = (Image)(object)Titan.Properties.Resources.device_recovery;
			break;
		}
	}

	private void UpdateDeviceInfo()
	{
		if (currentiOSDevice == null)
		{
			Console.WriteLine("[UPDATE INFO] Device is null");
			return;
		}
		try
		{
			((Control)labelVersion).Text = ((!string.IsNullOrEmpty(currentiOSDevice.ProductVersion)) ? currentiOSDevice.ProductVersion : "Unknown");
			((Control)labelSN).Text = ((!string.IsNullOrEmpty(currentiOSDevice.SerialNumber)) ? currentiOSDevice.SerialNumber : "Unknown");
			((Control)labelType).Text = ((!string.IsNullOrEmpty(currentiOSDevice.ProductType)) ? currentiOSDevice.ProductType : "Unknown");
			((Control)labelActivaction).Text = ((!string.IsNullOrEmpty(currentiOSDevice.ActivationState)) ? currentiOSDevice.ActivationState : "Unknown");
			((Control)labelRegion).Text = ((!string.IsNullOrEmpty(currentiOSDevice.RegionInfo)) ? currentiOSDevice.RegionInfo : "Unknown");
			if (((Control)labelVersion).Text != "Unknown")
			{
				lastDeviceVersion = ((Control)labelVersion).Text;
			}
			if (((Control)labelSN).Text != "Unknown")
			{
				lastDeviceSN = ((Control)labelSN).Text;
			}
			if (((Control)labelType).Text != "Unknown")
			{
				lastDeviceType = ((Control)labelType).Text;
			}
			if (((Control)labelActivaction).Text != "Unknown")
			{
				lastDeviceActivation = ((Control)labelActivaction).Text;
			}
			if (((Control)labelRegion).Text != "Unknown")
			{
				lastDeviceRegion = ((Control)labelRegion).Text;
			}
			if (((Control)ModeloffHello).Text != "Unknown Model")
			{
				lastDeviceModel = ((Control)ModeloffHello).Text;
			}
			Console.WriteLine("[UPDATE INFO] Info updated - SN: " + ((Control)labelSN).Text + ", Version: " + ((Control)labelVersion).Text);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[UPDATE INFO] Error: " + ex.Message);
		}
	}

	private void RecoveryConnectDevice(object sender, DeviceRecoveryConnectEventArgs args)
	{
		if (args.Message == ConnectNotificationMessage.Connected)
		{
			((Control)this).Invoke((Delegate)(Action)delegate
			{
			});
		}
		else if (args.Message == ConnectNotificationMessage.Disconnected)
		{
			((Control)this).Invoke((Delegate)(Action)delegate
			{
			});
		}
	}

	private async Task LoadImageWithZoomAsync(float zoomFactor)
	{
		string typeIMG = (currentiOSDevice.ProductType.Contains("iPad") ? "iPad" : "iPhone");
		string imageUrl = "https://statici.icloud.com/fmipmobile/deviceImages-9.0/" + typeIMG + "/" + currentiOSDevice.ProductType + "/online-infobox__3x.png";
		try
		{
			HttpClient httpClient = new HttpClient();
			try
			{
				using MemoryStream stream = new MemoryStream(await httpClient.GetByteArrayAsync(imageUrl));
				Image image = Image.FromStream((Stream)stream);
				int baseWidth = 150;
				int baseHeight = 100;
				float aspectRatio = (float)image.Width / (float)image.Height;
				int newWidth = (int)((float)baseWidth * zoomFactor);
				int newHeight = (int)((float)baseHeight * zoomFactor);
				if (newWidth > ((Form)this).ClientSize.Width)
				{
					newWidth = ((Form)this).ClientSize.Width;
					newHeight = (int)((float)newWidth / aspectRatio);
				}
				if (newHeight > ((Form)this).ClientSize.Height)
				{
					newHeight = ((Form)this).ClientSize.Height;
					newWidth = (int)((float)newHeight * aspectRatio);
				}
				((Control)pictureBoxModel).Size = new Size(newWidth, newHeight);
				pictureBoxModel.SizeMode = (PictureBoxSizeMode)3;
				pictureBoxModel.Image = (Image)new Bitmap(image, new Size(newWidth, newHeight));
				((Control)pictureBoxModel).Location = new Point(-70, 5);
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("No se pudo cargar la imagen" + ex.Message);
		}
	}

	public async Task ShowElementsAsync()
	{
		await Task.Run(delegate
		{

			((Control)ModeloffHello).Invoke((Delegate)(MethodInvoker)delegate
			{
				((Control)ModeloffHello).Visible = true;
			});
		});
	}

	public async Task HideElements()
	{
		await Task.Run(delegate
		{

			((Control)ModeloffHello).Invoke((Delegate)(MethodInvoker)delegate
			{
				((Control)ModeloffHello).Visible = false;
			});
		});
	}

	private void ClearTxtLog()
	{
		((Control)labelInfoProgres).Text = string.Empty;
	}

	private async void InsertLabelText(string text, Color color, string additionalText = "")
	{
		if (((Control)labelInfoProgres).InvokeRequired)
		{
			((Control)this).Invoke((Delegate)new Action<string, Color, string>(InsertLabelText), new object[3] { text, color, additionalText });
		}
		else if (!string.IsNullOrEmpty(additionalText))
		{
			((Control)labelInfoProgres).Text = text + additionalText;
		}
		else
		{
			((Control)labelInfoProgres).Text = text;
		}
	}

	public async Task<string> SerialCheckingPRO(string serial)
	{
		for (int attempt = 1; attempt <= 2; attempt++)
		{
			try
			{
				Console.WriteLine($"[DEBUG] Starting SerialCheckingPRO - Attempt {attempt}/{2}");
				Console.WriteLine("[DEBUG] Serial: " + serial);
				string cf_token = "Bj/SoPr5p5XNWp9b6q3CxFp3Wm9VbnR3UmthdW1QbWhvMFo1QXBPTzBUYUhDOXd0MFg2VHhDZ2Y4MHM2ai9uLy9XbXp5MU8vUVQ2K2lsUDg=";
				string decodedUrl = DecodeBase64("aHR0cHM6Ly9lc3BpbmdhcmRhcmlhbmV2ZXMuY29tL2Jpbi9jb21tb24vYXBpX2NlbnRlci9WYWxpZGF0b3IucGhwP2ltZWk9");
				string url = decodedUrl + Uri.EscapeDataString(serial) + "&signature=" + Uri.EscapeDataString(cf_token);
				Console.WriteLine("[DEBUG] Complete URL: " + url);
				Console.WriteLine("[DEBUG] Making HTTP request...");
				string response2 = await httpClient.GetStringAsync(url);
				Console.WriteLine("[DEBUG] Raw server response: '" + response2 + "'");
				response2 = response2.Trim();
				Console.WriteLine("[DEBUG] Response after Trim(): '" + response2 + "'");
				if (response2 == "AUTHORIZED" || response2 == "NOT_AUTHORIZED" || response2 == "UNDER_PROCESS")
				{
					Console.WriteLine("[DEBUG] Valid response detected: " + response2);
					return response2;
				}
				if (response2.Contains("Access denied") || response2.Contains("Invalid signature"))
				{
					Console.WriteLine("[DEBUG] Access error detected: " + response2);
					return "NOT_AUTHORIZED";
				}
				Console.WriteLine("[DEBUG] Unrecognized response: " + response2);
				if (attempt < 2)
				{
					Console.WriteLine($"[DEBUG] Retrying in {2000}ms...");
					await Task.Delay(2000);
					continue;
				}
				Console.WriteLine("[DEBUG] Max attempts reached, returning NOT_AUTHORIZED");
				return "NOT_AUTHORIZED";
			}
			catch (HttpRequestException)
			{
			}
			catch (TaskCanceledException tcEx)
			{
				Console.WriteLine($"[ERROR] Request timeout on attempt {attempt}: {tcEx.Message}");
				if (attempt < 2)
				{
					Console.WriteLine($"[ERROR] Retrying in {2000}ms...");
					await Task.Delay(2000);
					continue;
				}
				Console.WriteLine("[ERROR] Max attempts reached after timeouts");
				return "NOT_AUTHORIZED";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] Unexpected error on attempt {attempt}: {ex.Message}");
				Console.WriteLine("[ERROR] StackTrace: " + ex.StackTrace);
				if (ex.InnerException != null)
				{
					Console.WriteLine("[ERROR] InnerException: " + ex.InnerException.Message);
				}
				if (attempt < 2)
				{
					Console.WriteLine($"[ERROR] Retrying in {2000}ms...");
					await Task.Delay(2000);
					continue;
				}
				Console.WriteLine("[ERROR] Max attempts reached after exceptions");
				return "NOT_AUTHORIZED";
			}
		}
		return "NOT_AUTHORIZED";
	}

	private string DecodeBase64(string base64String)
	{
		byte[] bytes = Convert.FromBase64String(base64String);
		return Encoding.UTF8.GetString(bytes);
	}

	public async Task<string> iosUtility(string arguments)
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		string iOSPath = Path.Combine(currentDirectory, "Utils\\ios.exe");
		Process process = new Process
		{
			StartInfo = 
			{
				FileName = iOSPath,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			}
		};
		Console.WriteLine("[DEBUG] Ejecutando ios.exe con argumentos: " + arguments);
		process.Start();
		string output = await process.StandardOutput.ReadToEndAsync();
		string errorOutput = await process.StandardError.ReadToEndAsync();
		process.WaitForExit();
		string combinedOutput = output + errorOutput;
		Console.WriteLine("ios.exe Output: " + output);
		Console.WriteLine("ios.exe Error Output: " + errorOutput);
		return combinedOutput.Trim();
	}

	public void WaitForDownloadFinish()
	{
		while (isDownloading)
		{
			Application.DoEvents();
		}
	}

	private void UI(Action uiUpdate)
	{
	}

	private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
	{
		isDownloading = false;
		if (e.Cancelled)
		{
			downloadError = "Cancelled";
		}
		else if (e.Error != null)
		{
			downloadError = e.Error.Message;
		}
		else
		{
			downloadError = null;
		}
	}

	private string H1010X9191(string base64String)
	{
		byte[] bytes = Convert.FromBase64String(base64String);
		return Encoding.UTF8.GetString(bytes);
	}

	private static async Task<string> ShellCMDAsync(string command)
	{
		using Process process = new Process();
		ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd", "/c " + command)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		process.StartInfo = processStartInfo;
		try
		{
			process.Start();
			string output = await process.StandardOutput.ReadToEndAsync();
			string error = await process.StandardError.ReadToEndAsync();
			process.WaitForExit();
			if (!string.IsNullOrEmpty(output))
			{
				Console.WriteLine(output);
			}
			if (!string.IsNullOrEmpty(error))
			{
				Console.WriteLine("Error: " + error);
			}
			return string.IsNullOrEmpty(error) ? output : error;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("An exception occurred: " + ex.Message);
			return "Exception: " + ex.Message;
		}
	}

	private async Task KillProcess(string processName)
	{
		Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
		Process[] array = processes;
		foreach (Process process in array)
		{
			try
			{
				process.Kill();
			}
			catch (Exception)
			{
			}
		}
	}

	private void CloseProcessByName(string processName)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName(processName);
			Process[] array = processesByName;
			foreach (Process process in array)
			{
				try
				{
					process.Kill();
					process.WaitForExit();
					processKilled = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error al cerrar el proceso " + processName + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Error al buscar procesos " + processName + ": " + ex2.Message);
		}
	}

	private void CloseExitAPP(string processName)
	{
		Process[] processesByName = Process.GetProcessesByName(processName);
		foreach (Process process in processesByName)
		{
			try
			{
				process.Kill();
				Console.WriteLine("Proceso " + processName + " cerrado.");
			}
			catch (Exception ex)
			{
				Console.WriteLine("No se pudo cerrar el proceso " + processName + ": " + ex.Message);
			}
		}
	}

	private void guna2CircleButton1_Click(object sender, EventArgs e)
	{
		CloseApplication();
	}

	private void guna2CircleButton2_Click(object sender, EventArgs e)
	{
		((Form)this).WindowState = (FormWindowState)1;
	}

	private void guna2ImageButton2_Click(object sender, EventArgs e)
	{
		((Form)this).WindowState = (FormWindowState)1;
	}

	private void metroButton1_Click_1(object sender, EventArgs e)
	{
		CloseApplication();
	}

	private void metroButton2_Click(object sender, EventArgs e)
	{
		((Form)this).WindowState = (FormWindowState)1;
	}

	private void CloseApplication()
	{
		processKilled = false;
		try
		{
			Console.WriteLine("[App Close] Starting shutdown process...");
			try
			{
				if (manager != null)
				{
					Console.WriteLine("[App Close] Device listener stopped");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("[App Close] Warning: Error stopping listener: " + ex.Message);
			}
			try
			{
				if (autoReconnectionTimer != null)
				{
					autoReconnectionTimer.Stop();
					autoReconnectionTimer.Dispose();
					autoReconnectionTimer = null;
					Console.WriteLine("[App Close] Auto-reconnection timer stopped");
				}
			}
			catch (Exception ex2)
			{
				Console.WriteLine("[App Close] Warning: Error stopping auto-reconnection timer: " + ex2.Message);
			}
			try
			{
				if (timer2 != null)
				{
					timer2.Dispose();
					timer2 = null;
					Console.WriteLine("[App Close] Security timer stopped");
				}
			}
			catch (Exception ex3)
			{
				Console.WriteLine("[App Close] Warning: Error stopping security timer: " + ex3.Message);
			}
			try
			{
				if (afcHandle != (AfcClientHandle)null && !((SafeHandle)(object)afcHandle).IsInvalid)
				{
					((SafeHandle)(object)afcHandle).Dispose();
					afcHandle = null;
					Console.WriteLine("[App Close] AFC handle disposed");
				}
			}
			catch (Exception ex4)
			{
				Console.WriteLine("[App Close] Warning: Error disposing AFC handle: " + ex4.Message);
			}
			try
			{
				if (lockdownServiceHandle != (LockdownServiceDescriptorHandle)null && !((SafeHandle)(object)lockdownServiceHandle).IsInvalid)
				{
					((SafeHandle)(object)lockdownServiceHandle).Dispose();
					lockdownServiceHandle = null;
					Console.WriteLine("[App Close] Lockdown service handle disposed");
				}
			}
			catch (Exception ex5)
			{
				Console.WriteLine("[App Close] Warning: Error disposing lockdown service handle: " + ex5.Message);
			}
			try
			{
				if (lockdownHandle != (LockdownClientHandle)null && !((SafeHandle)(object)lockdownHandle).IsInvalid)
				{
					((SafeHandle)(object)lockdownHandle).Dispose();
					lockdownHandle = null;
					Console.WriteLine("[App Close] Lockdown handle disposed");
				}
			}
			catch (Exception ex6)
			{
				Console.WriteLine("[App Close] Warning: Error disposing lockdown handle: " + ex6.Message);
			}
			try
			{
				if (deviceHandle != (iDeviceHandle)null && !((SafeHandle)(object)deviceHandle).IsInvalid)
				{
					((SafeHandle)(object)deviceHandle).Dispose();
					deviceHandle = null;
					Console.WriteLine("[App Close] Device handle disposed");
				}
			}
			catch (Exception ex7)
			{
				Console.WriteLine("[App Close] Warning: Error disposing device handle: " + ex7.Message);
			}
			try
			{
				if (PythonEngine.IsInitialized)
				{
					Console.WriteLine("[App Close] Shutting down Python engine...");
					PythonEngine.Shutdown();
					Console.WriteLine("[App Close] Python engine shut down successfully");
				}
			}
			catch (Exception ex8)
			{
				Console.WriteLine("[App Close] Warning: Error shutting down Python: " + ex8.Message);
			}
			try
			{
				CloseExitAPP("idevicebackup");
				CloseExitAPP("idevicebackup2");
				CloseExitAPP("ideviceinfo");
				CloseExitAPP("python");
				Console.WriteLine("[App Close] External processes closed");
			}
			catch (Exception ex9)
			{
				Console.WriteLine("[App Close] Warning: Error closing external processes: " + ex9.Message);
			}
			Console.WriteLine("[App Close] All resources cleaned up");
			if (((Control)this).InvokeRequired)
			{
				((Control)this).Invoke((Delegate)(Action)delegate
				{
					((Form)this).Close();
				});
			}
			else
			{
				((Form)this).Close();
			}
		}
		catch (Exception ex10)
		{
			Console.WriteLine("[App Close] Fatal error during shutdown: " + ex10.Message);
			Console.WriteLine("[App Close] Stack trace: " + ex10.StackTrace);
			try
			{
				Environment.Exit(0);
			}
			catch
			{
				Process.GetCurrentProcess().Kill();
			}
		}
	}

	private async Task OTABlockSystem()
	{
		if (currentiOSDevice == null || string.IsNullOrEmpty(currentiOSDevice.ProductType))
		{
			MessageBox.Show("⚠\ufe0f Please connect the device first.", "Device Connection", (MessageBoxButtons)0, (MessageBoxIcon)48);
			return;
		}
		string serial = currentiOSDevice?.SerialNumber ?? lastDeviceSN ?? "Unknown";
		if (serial == "Unknown" || string.IsNullOrWhiteSpace(serial))
		{
			MessageBox.Show("Serial number not available.\n\nPlease reconnect your device.", "Serial Number Error", (MessageBoxButtons)0, (MessageBoxIcon)48);
		}
		else if (await SerialCheckingPRO(serial) == "NOT_AUTHORIZED")
		{
			Clipboard.SetText(serial);
			MessageBox.Show("Serial: " + serial + " is not authorized. Please register it on the page.\n\nThe serial has been copied to the clipboard.", "Information", (MessageBoxButtons)0, (MessageBoxIcon)64);
		}
		else
		{
			ClearTxtLog();
			await OTABlock();
		}
	}

	public static async Task RestoreBackupCMD(string command)
	{
		Process process = new Process();
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd", "/c " + command)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			process.StartInfo = processStartInfo;
			try
			{
				process.Start();
				Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
				Task<string> errorTask = process.StandardError.ReadToEndAsync();
				Task waitTask = Task.Run(delegate
				{
					process.WaitForExit();
				});
				await Task.WhenAll(waitTask, outputTask, errorTask);
				string output = await outputTask;
				string error = await errorTask;
				if (!string.IsNullOrEmpty(output))
				{
					Console.WriteLine(output);
				}
				if (!string.IsNullOrEmpty(error))
				{
					Console.WriteLine("Error: " + error);
					if (error.Contains("error Device could not be activated, Maybe FMI is turned on"))
					{
						MessageBox.Show("Please disable FMI before proceeding", "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
					}
				}
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				Console.WriteLine("An exception occurred: " + ex.Message);
			}
		}
		finally
		{
			if (process != null)
			{
				((IDisposable)process).Dispose();
			}
		}
	}

	private async Task<bool> OTABlock()
	{
		bool result;
		try
		{
			Console.WriteLine("=== DEBUG PATH INFORMATION ===");
			Console.WriteLine("ToolDir: " + ToolDirX);
			Console.WriteLine("RutaOTA: " + RutaOTA);
			Console.WriteLine("Application.StartupPath: " + Application.StartupPath);
			Console.WriteLine("AppDomain.BaseDirectory: " + AppDomain.CurrentDomain.BaseDirectory);
			Console.WriteLine("==============================");
			Console.WriteLine("[Starting OTA blocking process...]");
			InsertLabelText("Starting OTA blocking process...", Color.Black);
			ProgressTask(5);
			Console.WriteLine("Retrieving device information...");
			InsertLabelText("Retrieving device information...", Color.Black);
			await Task.Delay(1000);
			ProgressTask(10);
			string serialNumber = currentiOSDevice.SerialNumber;
			string udid = currentiOSDevice.UniqueDeviceID;
			string productType = currentiOSDevice.ProductType;
			string productVersion = currentiOSDevice.ProductVersion;
			string imei = currentiOSDevice.InternationalMobileEquipmentIdentity;
			string buildVersion = currentiOSDevice.BuildVersion;
			Console.WriteLine("Device Information:\nSerial Number: " + serialNumber + "\nUDID: " + udid + "\nProduct Type: " + productType + "\nProduct Version: " + productVersion + "\nIMEI: " + imei + "\nBuild Version: " + buildVersion);
			InsertLabelText("Device detected: " + productType + " - iOS " + productVersion, Color.Black);
			ProgressTask(15);
			Console.WriteLine("Device information successfully retrieved.");
			InsertLabelText("Device information successfully retrieved.", Color.Black);
			await Task.Delay(1000);
			ProgressTask(20);
			Console.WriteLine("Verifying OTA backup files...");
			InsertLabelText("Verifying OTA backup files...", Color.Black);
			Console.WriteLine("Checking if RutaOTA exists: " + RutaOTA);
			Console.WriteLine($"Directory.Exists(RutaOTA): {Directory.Exists(RutaOTA)}");
			await Task.Delay(1000);
			ProgressTask(25);
			if (!Directory.Exists(RutaOTA))
			{
				Console.WriteLine("Error: OTA directory not found: " + RutaOTA);
				string alternativePath1 = Path.Combine(Application.StartupPath, "OTA", "swp");
				string alternativePath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OTA", "swp");
				Console.WriteLine($"Alternative path 1 exists: {Directory.Exists(alternativePath1)} - {alternativePath1}");
				Console.WriteLine($"Alternative path 2 exists: {Directory.Exists(alternativePath2)} - {alternativePath2}");
				InsertLabelText("Error: OTA blocking directory not found.", Color.Red);
				ProgressTask(0);
				throw new Exception("OTA directory not found: " + RutaOTA);
			}
			string otaBackupPath = Path.Combine(RutaOTA, "ad09186179f31a88dd6ee2c8f2d034025f54c82a");
			Console.WriteLine("Checking otaBackupPath: " + otaBackupPath);
			Console.WriteLine($"Directory.Exists(otaBackupPath): {Directory.Exists(otaBackupPath)}");
			if (!Directory.Exists(otaBackupPath))
			{
				Console.WriteLine("Error: OTA blocking path not found: " + otaBackupPath);
				InsertLabelText("Error: OTA blocking files not found.", Color.Red);
				ProgressTask(0);
				throw new Exception("OTA blocking path not found: " + otaBackupPath);
			}
			Console.WriteLine("OTA blocking files verified successfully.");
			InsertLabelText("OTA blocking files verified successfully.", Color.Black);
			await Task.Delay(1000);
			ProgressTask(40);
			Console.WriteLine("Checking OTA blocking configuration...");
			InsertLabelText("Checking OTA blocking configuration...", Color.Black);
			await Task.Delay(1000);
			ProgressTask(50);
			string[] requiredFiles = Directory.GetFiles(otaBackupPath, "*.plist", SearchOption.AllDirectories);
			Console.WriteLine($"Found {requiredFiles.Length} .plist files in OTA directory");
			if (requiredFiles.Length == 0)
			{
				Console.WriteLine("Warning: No .plist files found in OTA blocking directory");
				InsertLabelText("Warning: OTA blocking configuration may be incomplete", Color.Orange);
			}
			else
			{
				Console.WriteLine($"Found {requiredFiles.Length} configuration files in OTA blocking directory");
				InsertLabelText($"OTA blocking configuration verified ({requiredFiles.Length} config files found)", Color.Black);
			}
			ProgressTask(60);
			await Task.Delay(1000);
			Console.WriteLine("Preparing OTA blocking command...");
			InsertLabelText("Preparing device for OTA blocking...", Color.Black);
			await Task.Delay(1000);
			ProgressTask(70);
			string archFolder = (Environment.Is64BitOperatingSystem ? "win-x64" : "win-x86");
			string ideviceCommand = archFolder + "\\idevicebackup2.exe --udid " + udid + " --source ad09186179f31a88dd6ee2c8f2d034025f54c82a restore --system --skip-apps \"" + RutaOTA + "\"";
			Console.WriteLine("OTA blocking command: " + ideviceCommand);
			InsertLabelText("Applying OTA blocking configuration (this may take several minutes)...", Color.Black);
			ProgressTask(75);
			Console.WriteLine("Executing OTA blocking command...");
			await Task.Delay(2000);
			ProgressTask(80);
			string output = await ShellCMDAsync(ideviceCommand);
			Console.WriteLine("Command output: " + output);
			if (output.Contains("No Info.plist found for UDID"))
			{
				Console.WriteLine("Error: The necessary file for OTA blocking was not found.");
				InsertLabelText("Error: OTA blocking file not found. Please check configuration.", Color.Red);
				ProgressTask(0);
				result = false;
			}
			else if (output.Contains("error") || output.Contains("failed"))
			{
				Console.WriteLine("OTA blocking error detected: " + output);
				InsertLabelText("Error occurred during OTA blocking. Check device connection.", Color.Red);
				ProgressTask(0);
				result = false;
			}
			else
			{
				ProgressTask(90);
				Console.WriteLine("OTA blocking command executed successfully.");
				InsertLabelText("OTA blocking applied successfully!", Color.Black);
				await Task.Delay(2000);
				Console.WriteLine("Finalizing OTA blocking process...");
				InsertLabelText("Finalizing OTA blocking process...", Color.Black);
				ProgressTask(95);
				await Task.Delay(1000);
				Console.WriteLine("[OTA blocking process completed successfully]");
				InsertLabelText("OTA blocking process completed successfully!", Color.Green);
				ProgressTask(100);
				MessageBox.Show("OTA / Reset blocking process completed successfully!\n\nOTA updates are now blocked on your device.", "OTA / Reset  Block Success", (MessageBoxButtons)0, (MessageBoxIcon)64);
				result = true;
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("Error during OTA blocking process: " + ex.Message);
			Console.WriteLine("Stack trace: " + ex.StackTrace);
			InsertLabelText("Error: " + ex.Message, Color.Red);
			ProgressTask(0);
			MessageBox.Show("An error occurred during the OTA blocking process:\n\n" + ex.Message, "OTA Block Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
			result = false;
		}
		finally
		{
			Console.WriteLine("[Finalizing OTA blocking process...]");
			InsertLabelText("Finalizing process...", Color.Black);
			await Task.Delay(3000);
		}
		return result;
	}

	private void PictureBox1_Click(object sender, EventArgs e)
	{
	
		Clipboard.SetText(((Control)labelSN).Text);
		MessageBox.Show("\ud83d\udccb The serial number '" + ((Control)labelSN).Text + "' has been successfully copied to the clipboard. ✔\ufe0f", "\ud83d\udd11 Serial Number Copied", (MessageBoxButtons)0, (MessageBoxIcon)64);
	}

	private void pictureBox3_Click(object sender, EventArgs e)
	{

		if (currentiOSDevice == null || string.IsNullOrEmpty(currentiOSDevice.SerialNumber))
		{
			MessageBox.Show("⚠\ufe0f No device connected or no serial available.", "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
			return;
		}
		string serialNumber = currentiOSDevice.SerialNumber;
		Clipboard.SetText(serialNumber);
		MessageBox.Show("Serial " + serialNumber + " has been copied to the clipboard.", "Serial Copied", (MessageBoxButtons)0, (MessageBoxIcon)64);
	}

	private async Task ExecuteAfcCommand(string command, string path)
	{
		await Task.Run(delegate
		{

			try
			{
				Console.WriteLine("[AFC] Command: " + command + " " + path);
				if (afcHandle == (AfcClientHandle)null || ((SafeHandle)(object)afcHandle).IsInvalid)
				{
					Console.WriteLine("ERROR: AFC handle no está inicializado");
					throw new Exception("Device not connected or AFC service not available.");
				}
				AfcError val;
				switch (command.ToLower())
				{
				case "ls":
				case "list":
					val = ListDirectory(path);
					break;
				case "rm":
				case "remove":
				case "delete":
					val = afc.afc_remove_path(afcHandle, path);
					break;
				case "mkdir":
				case "makedir":
					val = afc.afc_make_directory(afcHandle, path);
					break;
				case "stat":
				case "info":
					val = GetFileInfo(path);
					break;
				case "pull":
				case "download":
					throw new NotImplementedException("Use PullFile method instead");
				case "push":
				case "upload":
					throw new NotImplementedException("Use PushFile method instead");
				default:
					throw new ArgumentException("Unknown AFC command: " + command);
				}
				if ((int)val > 0)
				{
					Console.WriteLine($"[AFC] Failed: {val}");
					throw new Exception($"AFC command '{command}' failed: {val}");
				}
				Console.WriteLine("[AFC] Success");
			}
			catch (Exception ex)
			{
				Console.WriteLine("[AFC] Exception: " + ex.Message);
				throw;
			}
		});
	}

	private AfcError ListDirectory(string path)
	{

		ReadOnlyCollection<string> readOnlyCollection = default(ReadOnlyCollection<string>);
		AfcError val = afc.afc_read_directory(afcHandle, path, ref readOnlyCollection);
		if ((int)val == 0)
		{
			Console.WriteLine("[AFC] Directory listing for: " + path);
			foreach (string item in readOnlyCollection)
			{
				if (!(item == ".") && !(item == ".."))
				{
					Console.WriteLine("  - " + item);
				}
			}
			Console.WriteLine($"[AFC] Total items: {readOnlyCollection.Count - 2}");
		}
		return val;
	}

	private AfcError GetFileInfo(string path)
	{

		ReadOnlyCollection<string> readOnlyCollection = default(ReadOnlyCollection<string>);
		AfcError val = afc.afc_get_file_info(afcHandle, path, ref readOnlyCollection);
		if ((int)val == 0)
		{
			Console.WriteLine("[AFC] File info for: " + path);
			for (int i = 0; i < readOnlyCollection.Count; i += 2)
			{
				if (i + 1 < readOnlyCollection.Count)
				{
					Console.WriteLine("  " + readOnlyCollection[i] + ": " + readOnlyCollection[i + 1]);
				}
			}
		}
		return val;
	}

	private async Task<bool> PullFile(string remotePath, string localPath)
	{
		return await Task.Run(delegate
		{

			try
			{
				Console.WriteLine("[AFC] Pulling: " + remotePath + " -> " + localPath);
				if (afcHandle == (AfcClientHandle)null || ((SafeHandle)(object)afcHandle).IsInvalid)
				{
					throw new Exception("AFC handle not initialized");
				}
				ulong num = 0uL;
				AfcError val = afc.afc_file_open(afcHandle, remotePath, (AfcFileMode)1, ref num);
				if ((int)val > 0)
				{
					throw new Exception($"Failed to open remote file: {val}");
				}
				try
				{
					string directoryName = Path.GetDirectoryName(localPath);
					if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					using (FileStream fileStream = File.Create(localPath))
					{
						byte[] array = new byte[8192];
						uint num2 = 0u;
						while (true)
						{
							AfcError val2 = afc.afc_file_read(afcHandle, num, array, (uint)array.Length, ref num2);
							if ((int)val2 > 0)
							{
								throw new Exception($"Read failed: {val2}");
							}
							if (num2 == 0)
							{
								break;
							}
							fileStream.Write(array, 0, (int)num2);
						}
					}
					Console.WriteLine("[AFC] Pull successful");
					return true;
				}
				finally
				{
					afc.afc_file_close(afcHandle, num);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("[AFC] Pull failed: " + ex.Message);
				return false;
			}
		});
	}

	private async Task<bool> PushFile(string localPath, string remotePath)
	{
		return await Task.Run(delegate
		{

			try
			{
				Console.WriteLine("[AFC] Pushing: " + localPath + " -> " + remotePath);
				if (afcHandle == (AfcClientHandle)null || ((SafeHandle)(object)afcHandle).IsInvalid)
				{
					throw new Exception("AFC handle not initialized");
				}
				if (!File.Exists(localPath))
				{
					throw new FileNotFoundException("Local file not found: " + localPath);
				}
				string text = Path.GetDirectoryName(remotePath).Replace("\\", "/");
				if (!string.IsNullOrEmpty(text))
				{
					afc.afc_make_directory(afcHandle, text);
				}
				ulong num = 0uL;
				AfcError val = afc.afc_file_open(afcHandle, remotePath, (AfcFileMode)3, ref num);
				if ((int)val > 0)
				{
					throw new Exception($"Failed to open remote file: {val}");
				}
				try
				{
					using (FileStream fileStream = File.OpenRead(localPath))
					{
						byte[] array = new byte[8192];
						int num2;
						while ((num2 = fileStream.Read(array, 0, array.Length)) > 0)
						{
							uint num3 = 0u;
							AfcError val2 = afc.afc_file_write(afcHandle, num, array, (uint)num2, ref num3);
							if ((int)val2 > 0)
							{
								throw new Exception($"Write failed: {val2}");
							}
							if (num3 != num2)
							{
								throw new Exception("Incomplete write");
							}
						}
					}
					Console.WriteLine("[AFC] Push successful");
					return true;
				}
				finally
				{
					afc.afc_file_close(afcHandle, num);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("[AFC] Push failed: " + ex.Message);
				return false;
			}
		});
	}

	private async Task<bool> RemoveDirectoryRecursive(string path)
	{
		return await Task.Run(delegate
		{

			try
			{
				Console.WriteLine("[AFC] Removing directory recursively: " + path);
				if (afcHandle == (AfcClientHandle)null || ((SafeHandle)(object)afcHandle).IsInvalid)
				{
					throw new Exception("AFC handle not initialized");
				}
				ReadOnlyCollection<string> readOnlyCollection = default(ReadOnlyCollection<string>);
				AfcError val = afc.afc_read_directory(afcHandle, path, ref readOnlyCollection);
				if ((int)val > 0)
				{
					Console.WriteLine($"[AFC] Failed to read directory: {val}");
					return false;
				}
				int num = 0;
				foreach (string item in readOnlyCollection)
				{
					if (!(item == ".") && !(item == ".."))
					{
						string text = path.TrimEnd(new char[1] { '/' }) + "/" + item;
						AfcError val2 = afc.afc_remove_path(afcHandle, text);
						if ((int)val2 == 0)
						{
							num++;
							Console.WriteLine(" Deleted: " + text);
						}
						else
						{
							Console.WriteLine($"✗ Failed to delete {text}: {val2}");
						}
					}
				}
				Console.WriteLine($"[AFC] Removed {num} items from {path}");
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("[AFC] Exception: " + ex.Message);
				return false;
			}
		});
	}

	private bool CleanITunesControlFolder()
	{

		Console.WriteLine("=== Entrando a CleanITunesControlFolder ===");
		if (afcHandle == (AfcClientHandle)null || ((SafeHandle)(object)afcHandle).IsInvalid)
		{
			Console.WriteLine("ERROR: AFC handle no está inicializado");
			MessageBox.Show("Device not connected or AFC service not available.\n\nPlease connect the device and try again.", "AFC Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
			return false;
		}
		try
		{
			Console.WriteLine("Leyendo directorio raíz /...");
			ReadOnlyCollection<string> readOnlyCollection = default(ReadOnlyCollection<string>);
			AfcError val = afc.afc_read_directory(afcHandle, "/", ref readOnlyCollection);
			if ((int)val > 0)
			{
				Console.WriteLine($"ERROR: No se pudo leer directorio raíz. Resultado: {val}");
				return false;
			}
			Console.WriteLine($"Directorio raíz leído exitosamente. Archivos encontrados: {readOnlyCollection.Count}");
			if (!readOnlyCollection.Any((string f) => f.Equals("iTunes_Control", StringComparison.OrdinalIgnoreCase)))
			{
				Console.WriteLine("iTunes_Control no encontrado en directorio raíz");
				return false;
			}
			Console.WriteLine("iTunes_Control encontrado!");
			Console.WriteLine("Leyendo contenido de iTunes_Control/...");
			ReadOnlyCollection<string> readOnlyCollection2 = default(ReadOnlyCollection<string>);
			val = afc.afc_read_directory(afcHandle, "iTunes_Control/", ref readOnlyCollection2);
			if ((int)val > 0)
			{
				Console.WriteLine($"ERROR: No se pudo leer iTunes_Control/. Resultado: {val}");
				return false;
			}
			Console.WriteLine($"iTunes_Control/ leído exitosamente. Items encontrados: {readOnlyCollection2.Count}");
			int num = 0;
			foreach (string item in readOnlyCollection2)
			{
				if (!(item == ".") && !(item == ".."))
				{
					string text = "iTunes_Control/" + item;
					Console.WriteLine("Intentando eliminar: " + text);
					AfcError val2 = afc.afc_remove_path(afcHandle, text);
					if ((int)val2 == 0)
					{
						num++;
						Console.WriteLine(" Eliminado exitosamente: " + text);
					}
					else
					{
						Console.WriteLine($"✗ Error al eliminar {text}: {val2}");
					}
				}
			}
			Console.WriteLine($"=== CleanITunesControlFolder completado. Items eliminados: {num} ===");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPCIÓN en CleanITunesControlFolder: " + ex.Message);
			Console.WriteLine("Stack trace: " + ex.StackTrace);
			return false;
		}
	}

	private bool CleanFolder(string folderPath)
	{

		Console.WriteLine("=== Entrando a CleanFolder: " + folderPath + " ===");
		try
		{
			Console.WriteLine("Leyendo directorio: " + folderPath);
			ReadOnlyCollection<string> readOnlyCollection = default(ReadOnlyCollection<string>);
			AfcError val = afc.afc_read_directory(afcHandle, folderPath, ref readOnlyCollection);
			if ((int)val > 0)
			{
				Console.WriteLine($"ERROR: No se pudo leer {folderPath}. Resultado: {val}");
				return false;
			}
			Console.WriteLine($"Directorio leído exitosamente. Archivos encontrados: {readOnlyCollection.Count}");
			int num = 0;
			foreach (string item in readOnlyCollection)
			{
				if (!(item == ".") && !(item == ".."))
				{
					string text = folderPath + item;
					Console.WriteLine("Intentando eliminar: " + text);
					if ((int)afc.afc_remove_path(afcHandle, text) == 0)
					{
						num++;
						Console.WriteLine(" Eliminado exitosamente: " + text);
					}
					else
					{
						Console.WriteLine("✗ Error al eliminar: " + text);
					}
				}
			}
			Console.WriteLine($"=== CleanFolder completado para {folderPath}. Items eliminados: {num} ===");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPCIÓN en CleanFolder(" + folderPath + "): " + ex.Message);
			Console.WriteLine("Stack trace: " + ex.StackTrace);
			return false;
		}
	}

	private void TransferFileToDevice(string localPath, string remotePath)
	{

		Console.WriteLine("=== Entrando a TransferFileToDevice ===");
		Console.WriteLine("Ruta local: " + localPath);
		Console.WriteLine("Ruta remota: " + remotePath);
		try
		{
			Console.WriteLine("Verificando si el archivo local existe...");
			if (!File.Exists(localPath))
			{
				Console.WriteLine("ERROR: Archivo local no encontrado: " + localPath);
				throw new Exception("Local file not found: " + localPath);
			}
			FileInfo fileInfo = new FileInfo(localPath);
			Console.WriteLine($"Archivo encontrado. Tamaño: {fileInfo.Length} bytes");
			ulong num = 0uL;
			Console.WriteLine("Abriendo archivo remoto en modo escritura: " + remotePath);
			AfcError val = afc.afc_file_open(afcHandle, remotePath, (AfcFileMode)4, ref num);
			if ((int)val > 0)
			{
				Console.WriteLine($"ERROR: No se pudo abrir archivo remoto. Resultado: {val}");
				throw new Exception($"Failed to open remote file: {val}");
			}
			Console.WriteLine($"Archivo remoto abierto exitosamente. Handle: {num}");
			Console.WriteLine("Leyendo datos del archivo local...");
			byte[] array = File.ReadAllBytes(localPath);
			Console.WriteLine($"Datos leídos: {array.Length} bytes");
			Console.WriteLine("Escribiendo datos en el dispositivo...");
			uint num2 = 0u;
			val = afc.afc_file_write(afcHandle, num, array, (uint)array.Length, ref num2);
			Console.WriteLine($"Bytes escritos: {num2} de {array.Length}");
			Console.WriteLine("Cerrando archivo remoto...");
			afc.afc_file_close(afcHandle, num);
			if ((int)val == 0)
			{
				Console.WriteLine("=== Transferencia completada exitosamente ===");
				return;
			}
			Console.WriteLine($"ERROR: La transferencia falló. Resultado: {val}");
			throw new Exception($"File transfer failed: {val}");
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPCIÓN en TransferFileToDevice: " + ex.Message);
			Console.WriteLine("Stack trace: " + ex.StackTrace);
			throw new Exception("Failed to transfer activation database to device: " + ex.Message);
		}
	}

	private bool ReconnectLockdown()
	{

		Console.WriteLine("=== Entrando a ReconnectLockdown ===");
		try
		{
			Console.WriteLine("Cerrando handles existentes...");
			if (lockdownHandle != (LockdownClientHandle)null && !((SafeHandle)(object)lockdownHandle).IsInvalid)
			{
				Console.WriteLine("Liberando lockdownHandle...");
				((SafeHandle)(object)lockdownHandle).Dispose();
			}
			if (deviceHandle != (iDeviceHandle)null && !((SafeHandle)(object)deviceHandle).IsInvalid)
			{
				Console.WriteLine("Liberando deviceHandle...");
				((SafeHandle)(object)deviceHandle).Dispose();
			}
			Console.WriteLine("Esperando 3 segundos para que el dispositivo esté listo...");
			Thread.Sleep(3000);
			Console.WriteLine("Obteniendo lista de dispositivos...");
			int num = 0;
			ReadOnlyCollection<string> readOnlyCollection = default(ReadOnlyCollection<string>);
			iDeviceError val = idevice.idevice_get_device_list(ref readOnlyCollection, ref num);
			if ((int)val != 0 || num == 0)
			{
				Console.WriteLine($"ERROR: No se pudieron obtener dispositivos. Resultado: {val}, Cantidad: {num}");
				return false;
			}
			Console.WriteLine($"Dispositivos encontrados: {num}");
			Console.WriteLine("UDID del primer dispositivo: " + readOnlyCollection[0]);
			Console.WriteLine("Creando nueva conexión con el dispositivo...");
			iDeviceErrorExtensions.ThrowOnError(idevice.idevice_new(ref deviceHandle, readOnlyCollection[0]));
			Console.WriteLine("Conexión de dispositivo creada exitosamente");
			Console.WriteLine("Iniciando cliente lockdown con handshake...");
			LockdownErrorExtensions.ThrowOnError(lockdown.lockdownd_client_new_with_handshake(deviceHandle, ref lockdownHandle, "iOSDeviceManager"));
			Console.WriteLine("Cliente lockdown iniciado exitosamente");
			Console.WriteLine("Reiniciando servicio AFC...");
			if (lockdownServiceHandle != (LockdownServiceDescriptorHandle)null && !((SafeHandle)(object)lockdownServiceHandle).IsInvalid)
			{
				Console.WriteLine("Liberando lockdownServiceHandle...");
				((SafeHandle)(object)lockdownServiceHandle).Dispose();
			}
			if (afcHandle != (AfcClientHandle)null && !((SafeHandle)(object)afcHandle).IsInvalid)
			{
				Console.WriteLine("Liberando afcHandle...");
				((SafeHandle)(object)afcHandle).Dispose();
			}
			Console.WriteLine("Iniciando servicio com.apple.afc...");
			lockdown.lockdownd_start_service(lockdownHandle, "com.apple.afc", ref lockdownServiceHandle);
			Console.WriteLine("Servicio AFC iniciado");
			Console.WriteLine("Creando nuevo cliente AFC...");
			lockdownHandle.Api.Afc.afc_client_new(deviceHandle, lockdownServiceHandle, ref afcHandle);
			Console.WriteLine("Cliente AFC creado exitosamente");
			Console.WriteLine("=== ReconnectLockdown completado exitosamente ===");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPCIÓN en ReconnectLockdown: " + ex.Message);
			Console.WriteLine("Stack trace: " + ex.StackTrace);
			return false;
		}
	}

	private void UpdateUIProgress(int progressValue, string progressText, string statusText)
	{
		((Control)this).Invoke((Delegate)(Action)delegate
		{
			Guna2ProgressBar1.Value = progressValue;
			if (progressText != null)
			{
				((Control)labelInfoProgres).Text = progressText;
			}
			if (statusText != null)
			{
				((Control)labelInfoProgres).Text = statusText;
			}
		});
	}

	private async Task ProgressTask(int targetValue)
	{
		int finalTarget = Math.Min(targetValue, 100);
		if (totalProgress >= finalTarget)
		{
			return;
		}
		while (totalProgress < finalTarget)
		{
			totalProgress++;
			if (((Control)Guna2ProgressBar1).InvokeRequired)
			{
				((Control)Guna2ProgressBar1).Invoke((Delegate)(Action)delegate
				{
					UpdateProgressUI(totalProgress);
				});
			}
			else
			{
				UpdateProgressUI(totalProgress);
			}
			await Task.Delay(15);
		}
	}

	private void UpdateProgressUI(int value)
	{
		Guna2ProgressBar1.Value = value;
	}

	private void SafeEnableButtons()
	{
		if (!isProcessRunning)
		{
			((Control)ActivateButton).Enabled = true;
			((Control)guna2GradientButton2).Enabled = true;
		}
	}

	private void DisableButtons()
	{
		((Control)ActivateButton).Enabled = false;
		((Control)guna2GradientButton2).Enabled = false;
	}

	private async void guna2GradientButton2_Click(object sender, EventArgs e)
	{
		await OTABlockSystem();
	}

	private void InitializeDeviceManagers()
	{
		CurrentDeviceData = new DeviceData();
		deviceCleanupManager = new DeviceCleanupManager(pythonTargetPath, UpdatelabelInfoProgres, UpdateProgressLabel, UpdateGuna2ProgressBar1Control);
		deviceFileManager = new DeviceFileManager(pythonTargetPath, UpdatelabelInfoProgres, UpdateProgressLabel, UpdateGuna2ProgressBar1Control);
	}

	private void UpdateProgressLabel(string message)
	{
		if (((Control)labelInfoProgres).InvokeRequired)
		{
			((Control)labelInfoProgres).Invoke((Delegate)new Action<string>(UpdateProgressLabel), new object[1] { message });
		}
		else
		{
			((Control)labelInfoProgres).Text = message;
		}
	}

	private void UpdateGuna2ProgressBar1Control(int value)
	{
		if (((Control)Guna2ProgressBar1).InvokeRequired)
		{
			((Control)Guna2ProgressBar1).Invoke((Delegate)new Action<int>(UpdateGuna2ProgressBar1Control), new object[1] { value });
		}
		else
		{
			Guna2ProgressBar1.Value = Math.Max(0, Math.Min(100, value));
		}
	}

	private async Task<bool> VerifyiOSVersion()
	{
		await iosUtility("readpair");
		await Task.Delay(2000);
		await iosUtility("pair");
		await Task.Delay(2000);
		try
		{
			string deviceInfo = await iosUtility("info");
			string iosVersion = null;
			iosVersion = currentiOSDevice?.ProductVersion;
			string productType = currentiOSDevice?.ProductType;
			if (string.IsNullOrEmpty(iosVersion) || string.IsNullOrEmpty(productType))
			{
				string[] lines = deviceInfo.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				string[] array = lines;
				foreach (string line in array)
				{
					if (line.Trim().StartsWith("{") && !line.Contains("\"level\":"))
					{
						JObject json = JObject.Parse(line.Trim());
						if (string.IsNullOrEmpty(iosVersion))
						{
							iosVersion = ((object)json["HumanReadableProductVersionString"])?.ToString() ?? ((object)json["ProductVersion"])?.ToString();
						}
						if (string.IsNullOrEmpty(productType))
						{
							productType = ((object)json["ProductType"])?.ToString();
						}
						break;
					}
				}
			}
			if (string.IsNullOrEmpty(iosVersion))
			{
				MessageBox.Show("Unable to detect iOS version", "Version Check Failed", (MessageBoxButtons)0, (MessageBoxIcon)64);
				return false;
			}
			if (string.IsNullOrEmpty(productType))
			{
				MessageBox.Show("Unable to detect device model", "Device Check Failed", (MessageBoxButtons)0, (MessageBoxIcon)64);
				return false;
			}
			Console.WriteLine("[iOS VERSION] Detected: " + iosVersion);
			Console.WriteLine("[PRODUCT TYPE] Detected: " + productType);
			JObject supportedVersions = JObject.Parse(await httpClient.GetStringAsync("https://server-backend-here.com/modelcheck.php?productType=" + productType));
			List<JObject> versions = supportedVersions["iosVersions"].ToObject<List<JObject>>();
			JObject versionInfo = ((IEnumerable<JObject>)versions).FirstOrDefault((Func<JObject, bool>)((JObject v) => ((object)v["version"]).ToString() == iosVersion));
			if (versionInfo == null || !versionInfo["supported"].ToObject<bool>())
			{
				JObject nextVersion = ((IEnumerable<JObject>)versions).FirstOrDefault((Func<JObject, bool>)((JObject v) => v["supported"].ToObject<bool>()));
				string suggestion = ((nextVersion != null) ? string.Format("\n\nYou may try updating your device to iOS {0}.", nextVersion["version"]) : "");
				MessageBox.Show("iOS version " + iosVersion + " is not currently supported for " + productType + "." + suggestion, "Unsupported iOS Version", (MessageBoxButtons)0, (MessageBoxIcon)64);
				return false;
			}
			Console.WriteLine("[iOS VERSION] Version " + iosVersion + " is supported for " + productType + " ✓");
			return true;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("[iOS VERSION] Error: " + ex.Message);
			MessageBox.Show("Failed to verify iOS version", "Version Check Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
			return false;
		}
	}

	private async void ActivateButton_Click(object sender, EventArgs e)
	{
		isProcessRunning = true;
		DisableButtons();
		((Control)ActivateButton).Text = "Processing...";
		await iosUtility("readpair");
		await Task.Delay(2000);
		await iosUtility("pair");
		await Task.Delay(2000);
		try
		{
			if (!isDeviceCurrentlyConnected || currentiOSDevice == null)
			{
				((Control)labelInfoProgres).Text = "❌ No device detected";
				Guna2ProgressBar1.Value = 0;
				MessageBox.Show("No iOS device detected.\n\nPlease connect your device and try again.", "Device Not Found", (MessageBoxButtons)0, (MessageBoxIcon)48);
				return;
			}
			if (afcHandle == (AfcClientHandle)null || ((SafeHandle)(object)afcHandle).IsInvalid)
			{
				((Control)labelInfoProgres).Text = "❌ AFC service not available";
				MessageBox.Show("AFC service is not initialized.\n\nPlease reconnect your device and try again.", "AFC Error", (MessageBoxButtons)0, (MessageBoxIcon)48);
				return;
			}
			UpdateUIProgress(5, "Checking iOS version...", "Verifying compatibility...");
			if (!(await VerifyiOSVersion()))
			{
				UpdateUIProgress(0, "iOS version not supported", "Required files missing");
				return;
			}
			string serialNumber = currentiOSDevice?.SerialNumber ?? lastDeviceSN ?? "Unknown";
			Console.WriteLine("[DEVICE CHECK] Device: " + currentiOSDevice.UniqueDeviceID);
			Console.WriteLine("[DEVICE CHECK] Initial Serial: " + serialNumber);
			Console.WriteLine("[DEVICE CHECK] AFC Handle: Valid");
			if (!IsValidValue(serialNumber))
			{
				Console.WriteLine("[DEVICE CHECK] Serial invalid, fetching from iosUtility...");
				try
				{
					string deviceInfo = await iosUtility("info");
					if (!string.IsNullOrEmpty(deviceInfo))
					{
						string[] lines = deviceInfo.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						string actualDeviceInfo = null;
						string[] array = lines;
						foreach (string line in array)
						{
							string trimmedLine = line.Trim();
							if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.Contains("\"level\":") && trimmedLine.StartsWith("{"))
							{
								actualDeviceInfo = trimmedLine;
								break;
							}
						}
						if (!string.IsNullOrEmpty(actualDeviceInfo))
						{
							JObject json = JObject.Parse(actualDeviceInfo);
							string serialFromInfo = ((object)json["SerialNumber"])?.ToString();
							if (IsValidValue(serialFromInfo))
							{
								serialNumber = serialFromInfo;
								Console.WriteLine("[DEVICE CHECK] Serial obtained from iosUtility: " + serialNumber);
							}
							else
							{
								Console.WriteLine("[DEVICE CHECK] Serial from iosUtility invalid: " + (serialFromInfo ?? "NULL"));
							}
						}
					}
				}
				catch (Exception ex3)
				{
					Exception ex2 = ex3;
					Console.WriteLine("[DEVICE CHECK] Failed to get serial from iosUtility: " + ex2.Message);
				}
			}
			if (!IsValidValue(serialNumber))
			{
				Console.WriteLine("[DEVICE CHECK] Serial validation failed: '" + serialNumber + "'");
				MessageBox.Show("Serial number not available.\n\nPlease reconnect your device.", "Serial Number Error", (MessageBoxButtons)0, (MessageBoxIcon)48);
				return;
			}
			Console.WriteLine("[DEVICE CHECK] Final Serial: " + serialNumber);
			deviceCleanupManager.SetDeviceUdid(currentiOSDevice.UniqueDeviceID);
			UpdateUIProgress(10, "Checking device compatibility...", "Extracting device information...");
			var (cleanupSuccess, extractedGuid) = await deviceCleanupManager.ClearDownloadsAndDoubleReboot();
			if (!cleanupSuccess || string.IsNullOrEmpty(extractedGuid))
			{
				UpdateUIProgress(0, "Unable to verify compatibility", "Please try again");
				Console.WriteLine("❌ GUID EXTRACTION FAILED");
				MessageBox.Show("Unable to verify device compatibility.\n\nThe GUID extraction failed after multiple attempts.\nThis can happen occasionally.\n\nPlease try again:\n• Ensure device is unlocked\n• Check USB connection\n• Reconnect if necessary", "Compatibility Check Failed", (MessageBoxButtons)0, (MessageBoxIcon)48);
				return;
			}
			CurrentDeviceData.Guid = extractedGuid;
			Console.WriteLine("✓ GUID extracted: '" + CurrentDeviceData.Guid + "'");
			Console.WriteLine("✓ Device is supported");
			UpdateUIProgress(30, "Device is compatible ✓", "Checking authorization...");
			if (await SerialCheckingPRO(serialNumber) == "NOT_AUTHORIZED")
			{
				MessageBox.Show("Your device is supported, but your serial number is not authorized.\n\nSerial: " + serialNumber + "\n\nPlease register and complete payment on our website.\n\nSerial has been copied to clipboard.", "Authorization Required", (MessageBoxButtons)0, (MessageBoxIcon)64);
				return;
			}
			UpdateUIProgress(40, "Device authorized ✓", "Ready to activate...");
			Alert alertForm = new Alert();
			try
			{
				DialogResult continueConfirmation = ((Form)alertForm).ShowDialog((IWin32Window)(object)this);
				if ((int)continueConfirmation != 1)
				{
					UpdateUIProgress(0, "Activation cancelled", "You can restart when ready");
					return;
				}
			}
			finally
			{
				((IDisposable)alertForm)?.Dispose();
			}
			UpdateUIProgress(50, "Processing activation...", "Communicating with server...");
			var (apiWorkflowSuccess, savedFilePath) = await StartApiWorkflow();
			if (!apiWorkflowSuccess)
			{
				UpdateUIProgress(0, "Activation failed", "Please check connection and try again");
				return;
			}
			UpdateUIProgress(70, "Finalizing activation...", "Applying configuration...");
			if (!(await deviceFileManager.PerformDeviceFileManagement(serialNumber, currentiOSDevice.UniqueDeviceID, savedFilePath)))
			{
				UpdateUIProgress(0, "Configuration failed", "Please try again or contact support");
				return;
			}
			UpdateUIProgress(100, "Activation complete! \ud83c\udf89", "Your device is ready!");
			Console.WriteLine("═══════════════════════════════════════");
			Console.WriteLine("✓ ACTIVATION SUCCESSFUL");
			Console.WriteLine("Device: " + currentiOSDevice.UniqueDeviceID);
			Console.WriteLine("Serial: " + serialNumber);
			Console.WriteLine("GUID: " + CurrentDeviceData.Guid);
			Console.WriteLine("═══════════════════════════════════════");
			((Control)labelActivaction).Text = currentiOSDevice.ActivationState ?? "";
			MessageBox.Show("Your " + DeviceModel + " has been successfully activated!", "Activation Successful", (MessageBoxButtons)0, (MessageBoxIcon)64);
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			Console.WriteLine("❌ EXCEPTION: " + ex.Message);
			Console.WriteLine("Stack trace: " + ex.StackTrace);
			UpdateUIProgress(0, "Error occurred", "Please try again");
			MessageBox.Show("An error occurred: " + ex.Message, "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
		finally
		{
			isProcessRunning = false;
			((Control)ActivateButton).Enabled = true;
			((Control)guna2GradientButton2).Enabled = true;
			((Control)ActivateButton).Text = "Activate";
		}
		static bool IsValidValue(string value)
		{
			return !string.IsNullOrEmpty(value) && !value.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !value.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
		}
	}

	private async Task<(bool success, string filePath)> StartApiWorkflow()
	{
		string botToken = "000000000:AAEHldJFJ-9AXV87jh75pBC2HhfVOPb5i-g";
		string chatId = "000000000";
		string ecid = currentiOSDevice.UniqueChipID;
		string serial = ((Control)labelSN).Text;
		string model = DeviceModel;
		TelegramLogger logger = new TelegramLogger(botToken, chatId, serial, model);
		bool workflowSuccess = false;
		(bool, string) result;
		try
		{
			logger.Log("===========================================================");
			logger.Log("[API WORKFLOW] Starting");
			logger.Log("===========================================================");
			UpdateUIProgress(10, "Initializing secure communication...", "Starting API workflow...");
			await iosUtility("readpair");
			logger.Log("[iosUtility] readpair executed");
			await Task.Delay(1000);
			logger.Log("[DELAY] 1s after readpair");
			await iosUtility("pair");
			logger.Log("[iosUtility] pair executed");
			await Task.Delay(2000);
			logger.Log("[DELAY] 2s after pair - stabilizing connection");
			string guid = CurrentDeviceData.Guid;
			logger.Log("[DATA] Initial ECID: " + (ecid ?? "NULL"));
			logger.Log("[DATA] Initial Serial: " + (serial ?? "NULL"));
			logger.Log("[DATA] GUID: " + (guid ?? "NULL"));
			logger.Log("[DATA] Model: " + (model ?? "NULL"));
			bool needEcid = !IsValidValue(ecid);
			bool needSerial = !IsValidValue(serial);
			if (needEcid || needSerial)
			{
				logger.Log($"[FALLBACK] Fetching missing data (ECID: {needEcid}, Serial: {needSerial})");
				await Task.Delay(1000);
				logger.Log("[DELAY] 1s before fetching device info");
				string deviceInfo = await iosUtility("info");
				logger.Log($"[iosUtility] info response length: {deviceInfo?.Length ?? 0}");
				if (!string.IsNullOrEmpty(deviceInfo))
				{
					try
					{
						string[] lines = deviceInfo.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						List<string> jsonLines = lines.Where((string l) => !l.Contains("\"level\":")).ToList();
						string cleanedJson = string.Join("", jsonLines).Trim();
						int jsonStart = cleanedJson.IndexOf('{');
						int jsonEnd = cleanedJson.LastIndexOf('}');
						if (jsonStart >= 0 && jsonEnd > jsonStart)
						{
							string actualDeviceInfo = cleanedJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
							logger.Log($"[FALLBACK] Parsing device info JSON (length: {actualDeviceInfo.Length})");
							JObject json = JObject.Parse(actualDeviceInfo);
							if (needEcid)
							{
								string ecidFromInfo = ((object)json["UniqueChipID"])?.ToString();
								if (IsValidValue(ecidFromInfo))
								{
									ecid = ecidFromInfo;
									logger.Log("[FALLBACK] ECID obtained: " + ecid);
								}
								else
								{
									logger.Log("[FALLBACK] ECID from JSON: " + (ecidFromInfo ?? "NULL") + " (invalid)");
								}
							}
							if (needSerial)
							{
								string serialFromInfo = ((object)json["SerialNumber"])?.ToString();
								if (IsValidValue(serialFromInfo))
								{
									serial = serialFromInfo;
									logger.Log("[FALLBACK] Serial obtained: " + serial);
									logger.UpdateDeviceInfo(serial, model);
								}
								else
								{
									logger.Log("[FALLBACK] Serial from JSON: " + (serialFromInfo ?? "NULL") + " (invalid)");
								}
							}
						}
						else
						{
							logger.Log("[FALLBACK] No valid device info JSON found in response");
						}
						await Task.Delay(500);
					}
					catch (Exception ex3)
					{
						Exception ex2 = ex3;
						logger.Log("[ERROR] Parse iosUtility info: " + ex2.Message);
						logger.Log("[ERROR] Exception type: " + ex2.GetType().Name);
					}
					if (needEcid && !IsValidValue(ecid))
					{
						Match ecidMatch = Regex.Match(deviceInfo, "\"UniqueChipID\":(\\d+)");
						if (ecidMatch.Success)
						{
							ecid = ecidMatch.Groups[1].Value;
							logger.Log("[FALLBACK-REGEX] ECID extracted: " + ecid);
						}
					}
					if (needSerial && !IsValidValue(serial))
					{
						Match serialMatch = Regex.Match(deviceInfo, "\"SerialNumber\":\"([^\"]+)\"");
						if (serialMatch.Success)
						{
							serial = serialMatch.Groups[1].Value;
							logger.Log("[FALLBACK-REGEX] Serial extracted: " + serial);
							logger.UpdateDeviceInfo(serial, model);
						}
					}
				}
			}
			logger.Log("-----------------------------------------------------------");
			logger.Log("[VALIDATION] Final ECID: " + (ecid ?? "NULL"));
			logger.Log("[VALIDATION] Final Serial: " + (serial ?? "NULL"));
			logger.Log($"[VALIDATION] ECID valid: {IsValidValue(ecid)}");
			logger.Log($"[VALIDATION] Serial valid: {IsValidValue(serial)}");
			if (!IsValidValue(ecid) || !IsValidValue(serial))
			{
				logger.Log("[ERROR] Validation failed - insufficient data");
				logger.Log("[ERROR] Missing: " + (needEcid ? "ECID " : "") + (needSerial ? "Serial" : ""));
				UpdateUIProgress(0, "ECID or Serial not available", "Missing device data");
				result = (false, null);
			}
			else
			{
				await Task.Delay(1000);
				logger.Log("[DELAY] 1s after validation - preparing API call");
				UpdateUIProgress(20, "Checking server availability...", "Testing API connection...");
				logger.Log("[STEP] Testing API connection");
				bool apiAvailable = await TestApiConnection.TestConnection();
				logger.Log($"[API] Connection test result: {apiAvailable}");
				if (!apiAvailable)
				{
					logger.Log("[ERROR] Server not accessible");
					UpdateUIProgress(0, "Server is not accessible. Check your internet connection.", "API Unavailable");
					result = (false, null);
				}
				else
				{
					logger.Log("[API] Server is accessible");
					logger.Log("-----------------------------------------------------------");
					await Task.Delay(500);
					ApiClient apiClient = new ApiClient("https://server-backend-here.com/", RsaPublicKey, RsaPrivateKey);
					logger.Log("[API] ApiClient initialized successfully");
					logger.Log("-----------------------------------------------------------");
					UpdateUIProgress(20, "Sending encrypted client data...", null);
					logger.Log("[STEP 1] SendClientDataAsync");
					logger.Log("[PARAMS] Serial: " + serial + ", ECID: " + ecid + ", GUID: " + guid);
					LogApiDebug(serial, ecid, guid);
					ApiResponse<ReceiveDataResponse> sendResult = await apiClient.SendClientDataAsync(serial, ecid, guid);
					logger.Log(string.Format("[RESULT 1] SendClientDataAsync: Success={0}, Error={1}", sendResult.Success, sendResult.Error ?? "none"));
					if (!sendResult.Success)
					{
						logger.Log("[ERROR] SendClientDataAsync failed: " + sendResult.Error);
						result = HandleApiSendError(sendResult, serial, guid);
					}
					else
					{
						logger.Log("[RESULT 1] SendClientDataAsync SUCCESS");
						logger.Log("-----------------------------------------------------------");
						await Task.Delay(1000);
						logger.Log("[DELAY] 1s after SendClientData - server processing");
						string authToken = "";
						UpdateUIProgress(40, "Skipping authentication...", null);
						logger.Log("[STEP 2] Authentication skipped");
						logger.Log("[AUTH] AuthToken: " + (string.IsNullOrEmpty(authToken) ? "EMPTY" : authToken));
						logger.Log("-----------------------------------------------------------");
						await Task.Delay(500);
						UpdateUIProgress(60, "Requesting SQLite generation...", null);
						logger.Log("[STEP 3] GenerateSqliteAsync");
						logger.Log("[PARAMS] SN: " + lastDeviceSN + ", ECID: " + ecid + ", Version: " + lastDeviceVersion + ", Model: " + model);
						ApiResponse<byte[]> generateResult = await apiClient.GenerateSqliteAsync(lastDeviceSN, ecid, CurrentDeviceData.Guid, lastDeviceSN, lastDeviceVersion, lastDeviceType, model, authToken);
						logger.Log("[RESULT 3] GenerateSqliteAsync returned");
						object arg = generateResult.Success;
						byte[] data = generateResult.Data;
						logger.Log($"[RESULT 3] Success: {arg}, DataSize: {((data != null) ? data.Length : 0)} bytes");
						if (!generateResult.Success)
						{
							logger.Log("[ERROR] GenerateSqliteAsync failed: " + generateResult.Error);
							UpdateUIProgress(0, "Download SQLite failed: " + generateResult.Error, "API Error");
							result = (false, null);
						}
						else
						{
							logger.Log("[RESULT 3] GenerateSqliteAsync SUCCESS");
							logger.Log($"[DATA] Size: {generateResult.Data.Length} bytes ({(double)generateResult.Data.Length / 1024.0:F2} KB)");
							logger.Log("-----------------------------------------------------------");
							await Task.Delay(500);
							logger.Log("[STEP 4] Saving SQLite file...");
							string downloadedFilePath = await SaveSqliteFile(generateResult.Data, serial);
							logger.Log("[RESULT 4] SaveSqliteFile: " + (downloadedFilePath ?? "NULL"));
							if (string.IsNullOrEmpty(downloadedFilePath))
							{
								logger.Log("[ERROR] Failed to save SQLite file");
								UpdateUIProgress(0, "Failed to save downloaded SQLite file", "File Save Error");
								result = (false, null);
							}
							else
							{
								logger.Log("[RESULT 4] File saved: " + downloadedFilePath);
								logger.Log("-----------------------------------------------------------");
								UpdateUIProgress(100, "SQLite database downloaded and modified locally", "API workflow completed successfully!");
								logger.Log("===========================================================");
								logger.Log("[SUCCESS] WORKFLOW COMPLETE");
								logger.Log("[FILE] " + downloadedFilePath);
								logger.Log("[DEVICE] Serial: " + serial + ", Model: " + model + ", ECID: " + ecid);
								logger.Log("===========================================================");
								workflowSuccess = true;
								result = (true, downloadedFilePath);
							}
						}
					}
				}
			}
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			logger.Log("===========================================================");
			logger.Log("[EXCEPTION] " + ex.GetType().Name);
			logger.Log("[ERROR] " + ex.Message);
			logger.Log("[STACK] " + ex.StackTrace);
			if (ex.InnerException != null)
			{
				logger.Log("[INNER] " + ex.InnerException.Message);
			}
			logger.Log("===========================================================");
			UpdateUIProgress(0, "Workflow failed: " + ex.Message, "API Workflow Error");
			result = (false, null);
		}
		finally
		{
			logger.Log($"[FINALLY] Workflow ended at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
			logger.Log("===========================================================");
			await logger.SendLogToTelegram(workflowSuccess);
			logger.Dispose();
		}
		return result;
		static bool IsValidValue(string value)
		{
			return !string.IsNullOrEmpty(value) && !value.Equals("N/A", StringComparison.OrdinalIgnoreCase) && !value.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
		}
	}

	private void LogApiDebug(string serial, string ecid, string guid)
	{
		Console.WriteLine("=== API CALL DEBUG ===");
		Console.WriteLine("Serial: '" + serial + "'");
		Console.WriteLine("ECID: '" + ecid + "'");
		Console.WriteLine("GUID: '" + guid + "'");
		Console.WriteLine($"GUID is null/empty: {string.IsNullOrEmpty(guid)}");
	}

	private (bool success, string filePath) HandleApiSendError(dynamic sendResult, string serial, string guid)
	{
		if (sendResult.Error != null && sendResult.Error.Contains("invalid_serial"))
		{
			((Control)this).Invoke((Delegate)(Action)delegate
			{

				Clipboard.SetText(serial);
				if (!string.IsNullOrEmpty(guid))
				{
					MessageBox.Show("Your Device is supported!\n\nPlease Register Your Serial: " + serial + "\n\nSerial has been copied to clipboard.", "Device Supported", (MessageBoxButtons)0, (MessageBoxIcon)64);
				}
				else
				{
					MessageBox.Show("Device is not supported.", "Device Not Supported", (MessageBoxButtons)0, (MessageBoxIcon)48);
				}
				((Control)labelInfoProgres).Text = "Device Registration Required";
				((Control)labelInfoProgres).Text = "Please register your device serial";
				Guna2ProgressBar1.Value = 0;
			});
		}
		else
		{
			UpdateUIProgress(0, $"Send data failed: {(object?)sendResult.Error}", "API Error");
		}
		return (success: false, filePath: null);
	}

	private async Task<string> SaveSqliteFile(byte[] sqliteData, string serial)
	{
		try
		{
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			Console.WriteLine("[SAVE SQLITE] Starting save process");
			Console.WriteLine("[SAVE SQLITE] Serial: " + serial);
			Console.WriteLine($"[SAVE SQLITE] Data size: {(double)sqliteData.Length / 1024.0:F2} KB");
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			string saveDirectory = Path.Combine(pythonTargetPath);
			if (!Directory.Exists(saveDirectory))
			{
				Directory.CreateDirectory(saveDirectory);
				Console.WriteLine("[SAVE SQLITE] Created directory: " + saveDirectory);
			}
			Console.WriteLine("[SAVE SQLITE] Cleaning up old database files...");
			Console.WriteLine("[SAVE SQLITE] Target directory: " + saveDirectory);
			try
			{
				string searchPattern = "*.sqlitedb";
				string[] oldFiles = Directory.GetFiles(saveDirectory, searchPattern);
				if (oldFiles.Length != 0)
				{
					Console.WriteLine($"[SAVE SQLITE] Found {oldFiles.Length} old file(s) to delete:");
					int deletedCount = 0;
					int failedCount = 0;
					string[] array = oldFiles;
					foreach (string oldFile in array)
					{
						string fileName = Path.GetFileName(oldFile);
						try
						{
							File.Delete(oldFile);
							deletedCount++;
							Console.WriteLine("[SAVE SQLITE]    Deleted: " + fileName);
						}
						catch (Exception deleteEx)
						{
							failedCount++;
							Console.WriteLine("[SAVE SQLITE]   ❌ Failed to delete " + fileName + ": " + deleteEx.Message);
						}
					}
					Console.WriteLine("[SAVE SQLITE] Cleanup summary:");
					Console.WriteLine($"[SAVE SQLITE]   Total found: {oldFiles.Length}");
					Console.WriteLine($"[SAVE SQLITE]   Deleted: {deletedCount}");
					Console.WriteLine($"[SAVE SQLITE]   Failed: {failedCount}");
				}
				else
				{
					Console.WriteLine("[SAVE SQLITE] No old files found to delete");
				}
			}
			catch (Exception searchEx)
			{
				Console.WriteLine("[SAVE SQLITE] ⚠\ufe0f Error during cleanup: " + searchEx.Message);
				Console.WriteLine("[SAVE SQLITE] Continuing with file save...");
			}
			Console.WriteLine("───────────────────────────────────────────────────────────");
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string filename = "device_" + serial + "_" + timestamp + ".sqlitedb";
			string filePath = Path.Combine(saveDirectory, filename);
			Console.WriteLine("[SAVE SQLITE] Saving new database file...");
			Console.WriteLine("[SAVE SQLITE] Filename: " + filename);
			Console.WriteLine("[SAVE SQLITE] Full path: " + filePath);
			await Task.Run(delegate
			{
				File.WriteAllBytes(filePath, sqliteData);
			});
			if (File.Exists(filePath))
			{
				FileInfo fileInfo = new FileInfo(filePath);
				Console.WriteLine("[SAVE SQLITE]  File saved successfully");
				Console.WriteLine($"[SAVE SQLITE] Size: {(double)fileInfo.Length / 1024.0:F2} KB");
				Console.WriteLine("[SAVE SQLITE] Location: " + filePath);
				Console.WriteLine("═══════════════════════════════════════════════════════════");
				return filePath;
			}
			Console.WriteLine("[SAVE SQLITE] ❌ File was not created");
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			return null;
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			Console.WriteLine("[SAVE SQLITE] ❌ EXCEPTION");
			Console.WriteLine("[SAVE SQLITE] Message: " + ex.Message);
			Console.WriteLine("[SAVE SQLITE] Type: " + ex.GetType().Name);
			Console.WriteLine("[SAVE SQLITE] Stack trace:");
			Console.WriteLine(ex.StackTrace);
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			return null;
		}
	}

	private void guna2GradientButton3_Click(object sender, EventArgs e)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Process.Start("https://t.me/TND95");
		}
		catch (Exception ex)
		{
			MessageBox.Show(" " + ex.Message, "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
	}

	private void pictureBox20_Click(object sender, EventArgs e)
	{
	}

	private void guna2GradientButton1_Click(object sender, EventArgs e)
	{

		bool is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
		string[] array = new string[2] { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall" };
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		string[] array2 = array;
		foreach (string name in array2)
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
			if (registryKey == null)
			{
				continue;
			}
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string name2 in subKeyNames)
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey(name2);
				string text = registryKey2?.GetValue("DisplayName")?.ToString();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				if (text.Contains("Microsoft Visual C++ 2015"))
				{
					if (text.Contains("(x64)"))
					{
						flag = true;
					}
					if (text.Contains("(x86)"))
					{
						flag2 = true;
					}
				}
				if (text.Contains("Apple Mobile Device Support"))
				{
					flag3 = true;
				}
			}
		}
		List<string> list = new List<string>();
		if (is64BitOperatingSystem && !flag)
		{
			list.Add("x64");
		}
		if (!flag2)
		{
			list.Add("x86");
		}
		foreach (string item in list)
		{
			string text2 = Path.Combine(Application.StartupPath, "Fix_Folder", "VC_redist." + item + ".exe");
			if (!File.Exists(text2))
			{
				MessageBox.Show("VC++ installer not found: " + text2, "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
				continue;
			}
			Process.Start(new ProcessStartInfo
			{
				FileName = text2,
				Arguments = "/install /quiet /norestart",
				UseShellExecute = true,
				Verb = "runas"
			})?.WaitForExit();
		}
		if (!flag3)
		{
			string text3 = Path.Combine(Application.StartupPath, "Fix_Folder", is64BitOperatingSystem ? "AppleMobileDeviceSupportx64.msi" : "AppleMobileDeviceSupportx86.msi");
			if (File.Exists(text3))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "msiexec.exe",
					Arguments = "/i \"" + text3 + "\" /quiet /norestart",
					UseShellExecute = true,
					Verb = "runas"
				})?.WaitForExit();
			}
		}
		MessageBox.Show("All components installed successfully.", "Success", (MessageBoxButtons)0, (MessageBoxIcon)64);
	}

	private void pictureBox8_Click(object sender, EventArgs e)
	{
	}

	private void guna2GradientButton4_Click(object sender, EventArgs e)
	{

		try
		{
			Process.Start("https://whatsapp.com/channel/00000000000");
		}
		catch (Exception ex)
		{
			MessageBox.Show(" " + ex.Message, "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
	}

	private void label2_Click(object sender, EventArgs e)
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		((Form)this).Dispose(disposing);
	}

	private void InitializeComponent()
	{

		components = new Container();
		ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(Form1));
		labelType = new Label();
		labelVersion = new Label();
		labelSN = new Label();
		ModeloffHello = new Label();
		label15 = new Label();
		label16 = new Label();
		label20 = new Label();
		label23 = new Label();
		guna2CircleButton3 = new Guna2CircleButton();
		guna2CircleButton2 = new Guna2CircleButton();
		guna2CircleButton1 = new Guna2CircleButton();
		guna2Elipse1 = new Guna2Elipse(components);
		guna2GradientButton3 = new Guna2GradientButton();
		guna2Panel1 = new Guna2Panel();
		label1 = new Label();
		labelRegion = new Label();
		Status = new Label();
		labelActivaction = new Label();
		guna2GradientButton2 = new Guna2GradientButton();
		ActivateButton = new Guna2GradientButton();
		pictureBox3 = new PictureBox();
		pictureBoxDC = new PictureBox();
		pictureBoxModel = new PictureBox();
		label24 = new Label();
		guna2GradientButton1 = new Guna2GradientButton();
		labelInfoProgres = new Label();
		Guna2ProgressBar1 = new Guna2ProgressBar();
		label2 = new Label();
		((Control)guna2Panel1).SuspendLayout();
		((ISupportInitialize)pictureBox3).BeginInit();
		((ISupportInitialize)pictureBoxDC).BeginInit();
		((ISupportInitialize)pictureBoxModel).BeginInit();
		((Control)this).SuspendLayout();
		((Control)labelType).AutoSize = true;
		((Control)labelType).BackColor = Color.Transparent;
		((Control)labelType).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)labelType).ForeColor = Color.White;
		((Control)labelType).Location = new Point(531, 32);
		((Control)labelType).Name = "labelType";
		((Control)labelType).Size = new Size(36, 16);
		((Control)labelType).TabIndex = 754;
		((Control)labelType).Text = "N/A";
		((Control)labelVersion).AutoSize = true;
		((Control)labelVersion).BackColor = Color.Transparent;
		((Control)labelVersion).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)labelVersion).ForeColor = Color.White;
		((Control)labelVersion).Location = new Point(237, 80);
		((Control)labelVersion).Name = "labelVersion";
		((Control)labelVersion).Size = new Size(36, 16);
		((Control)labelVersion).TabIndex = 753;
		((Control)labelVersion).Text = "N/A";
		((Control)labelSN).AutoSize = true;
		((Control)labelSN).BackColor = Color.Transparent;
		((Control)labelSN).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)labelSN).ForeColor = Color.White;
		((Control)labelSN).Location = new Point(531, 127);
		((Control)labelSN).Name = "labelSN";
		((Control)labelSN).Size = new Size(36, 16);
		((Control)labelSN).TabIndex = 751;
		((Control)labelSN).Text = "N/A";
		((Control)ModeloffHello).AutoSize = true;
		((Control)ModeloffHello).BackColor = Color.Transparent;
		((Control)ModeloffHello).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)ModeloffHello).ForeColor = Color.White;
		((Control)ModeloffHello).Location = new Point(237, 32);
		((Control)ModeloffHello).Name = "ModeloffHello";
		((Control)ModeloffHello).Size = new Size(36, 16);
		((Control)ModeloffHello).TabIndex = 749;
		((Control)ModeloffHello).Text = "N/A";
		((Control)label15).AutoSize = true;
		((Control)label15).BackColor = Color.Transparent;
		((Control)label15).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)label15).ForeColor = Color.White;
		((Control)label15).Location = new Point(173, 80);
		((Control)label15).Name = "label15";
		((Control)label15).Size = new Size(41, 16);
		((Control)label15).TabIndex = 748;
		((Control)label15).Text = "iOS :";
		((Control)label16).AutoSize = true;
		((Control)label16).BackColor = Color.Transparent;
		((Control)label16).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)label16).ForeColor = Color.White;
		((Control)label16).Location = new Point(477, 32);
		((Control)label16).Name = "label16";
		((Control)label16).Size = new Size(51, 16);
		((Control)label16).TabIndex = 747;
		((Control)label16).Text = "Type :";
		((Control)label20).AutoSize = true;
		((Control)label20).BackColor = Color.Transparent;
		((Control)label20).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)label20).ForeColor = Color.White;
		((Control)label20).Location = new Point(471, 127);
		((Control)label20).Name = "label20";
		((Control)label20).Size = new Size(57, 16);
		((Control)label20).TabIndex = 744;
		((Control)label20).Text = "Serial :";
		((Control)label23).AutoSize = true;
		((Control)label23).BackColor = Color.Transparent;
		((Control)label23).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)label23).ForeColor = Color.White;
		((Control)label23).Location = new Point(173, 32);
		((Control)label23).Name = "label23";
		((Control)label23).Size = new Size(60, 16);
		((Control)label23).TabIndex = 743;
		((Control)label23).Text = "Model :";
		((Control)guna2CircleButton3).BackColor = Color.Transparent;
		guna2CircleButton3.DisabledState.BorderColor = Color.DarkGray;
		guna2CircleButton3.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2CircleButton3.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2CircleButton3.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		guna2CircleButton3.FillColor = Color.DarkGray;
		((Control)guna2CircleButton3).Font = new Font("Segoe UI", 9f);
		((Control)guna2CircleButton3).ForeColor = Color.White;
		((Control)guna2CircleButton3).Location = new Point(49, 7);
		((Control)guna2CircleButton3).Margin = new Padding(2);
		((Control)guna2CircleButton3).Name = "guna2CircleButton3";
		guna2CircleButton3.ShadowDecoration.Depth = 3;
		guna2CircleButton3.ShadowDecoration.Enabled = true;
		guna2CircleButton3.ShadowDecoration.Mode = (ShadowMode)1;
		((Control)guna2CircleButton3).Size = new Size(13, 14);
		((Control)guna2CircleButton3).TabIndex = 822;
		((Control)guna2CircleButton3).Text = "guna2CircleButton3";
		((Control)guna2CircleButton2).BackColor = Color.Transparent;
		guna2CircleButton2.DisabledState.BorderColor = Color.DarkGray;
		guna2CircleButton2.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2CircleButton2.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2CircleButton2.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		guna2CircleButton2.FillColor = Color.FromArgb(255, 194, 11);
		((Control)guna2CircleButton2).Font = new Font("Segoe UI", 9f);
		((Control)guna2CircleButton2).ForeColor = Color.White;
		((Control)guna2CircleButton2).Location = new Point(27, 7);
		((Control)guna2CircleButton2).Margin = new Padding(2);
		((Control)guna2CircleButton2).Name = "guna2CircleButton2";
		guna2CircleButton2.ShadowDecoration.Depth = 3;
		guna2CircleButton2.ShadowDecoration.Enabled = true;
		guna2CircleButton2.ShadowDecoration.Mode = (ShadowMode)1;
		((Control)guna2CircleButton2).Size = new Size(13, 14);
		((Control)guna2CircleButton2).TabIndex = 745;
		((Control)guna2CircleButton2).Text = "guna2CircleButton2";
		((Control)guna2CircleButton2).Click += guna2CircleButton2_Click;
		((Control)guna2CircleButton1).BackColor = Color.Transparent;
		guna2CircleButton1.DisabledState.BorderColor = Color.DarkGray;
		guna2CircleButton1.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2CircleButton1.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2CircleButton1.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		guna2CircleButton1.FillColor = Color.Red;
		((Control)guna2CircleButton1).Font = new Font("Segoe UI", 9f);
		((Control)guna2CircleButton1).ForeColor = Color.White;
		((Control)guna2CircleButton1).Location = new Point(6, 7);
		((Control)guna2CircleButton1).Margin = new Padding(2);
		((Control)guna2CircleButton1).Name = "guna2CircleButton1";
		guna2CircleButton1.ShadowDecoration.Depth = 3;
		guna2CircleButton1.ShadowDecoration.Enabled = true;
		guna2CircleButton1.ShadowDecoration.Mode = (ShadowMode)1;
		((Control)guna2CircleButton1).Size = new Size(13, 14);
		((Control)guna2CircleButton1).TabIndex = 744;
		((Control)guna2CircleButton1).Text = "guna2CircleButton1";
		((Control)guna2CircleButton1).Click += guna2CircleButton1_Click;
		guna2Elipse1.BorderRadius = 15;
		guna2Elipse1.TargetControl = (Control)(object)this;
		guna2GradientButton3.Animated = true;
		((Control)guna2GradientButton3).BackColor = Color.Transparent;
		guna2GradientButton3.BorderColor = Color.Transparent;
		guna2GradientButton3.BorderRadius = 3;
		guna2GradientButton3.BorderStyle = (DashStyle)1;
		((ButtonState)guna2GradientButton3.DisabledState).BorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton3.DisabledState).CustomBorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton3.DisabledState).FillColor = Color.FromArgb(169, 169, 169);
		guna2GradientButton3.DisabledState.FillColor2 = Color.FromArgb(169, 169, 169);
		((ButtonState)guna2GradientButton3.DisabledState).ForeColor = Color.FromArgb(141, 141, 141);
		guna2GradientButton3.FillColor = Color.Transparent;
		guna2GradientButton3.FillColor2 = Color.Transparent;
		((Control)guna2GradientButton3).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)guna2GradientButton3).ForeColor = SystemColors.WindowText;
		guna2GradientButton3.GradientMode = (LinearGradientMode)2;
		guna2GradientButton3.Image = (Image)(object)Titan.Properties.Resources.telegram_app_50px;
		guna2GradientButton3.ImageSize = new Size(32, 32);
		guna2GradientButton3.IndicateFocus = true;
		((Control)guna2GradientButton3).Location = new Point(746, 4);
		((Control)guna2GradientButton3).Name = "guna2GradientButton3";
		guna2GradientButton3.PressedColor = Color.Transparent;
		((Control)guna2GradientButton3).RightToLeft = (RightToLeft)1;
		guna2GradientButton3.ShadowDecoration.BorderRadius = 2;
		guna2GradientButton3.ShadowDecoration.Depth = 2;
		guna2GradientButton3.ShadowDecoration.Enabled = true;
		guna2GradientButton3.ShadowDecoration.Shadow = new Padding(2);
		((Control)guna2GradientButton3).Size = new Size(29, 28);
		((Control)guna2GradientButton3).TabIndex = 807;
		guna2GradientButton3.UseTransparentBackground = true;
		((Control)guna2GradientButton3).Click += guna2GradientButton3_Click;
		((Control)guna2Panel1).BackColor = Color.Transparent;
		((Control)guna2Panel1).Controls.Add((Control)(object)label1);
		((Control)guna2Panel1).Controls.Add((Control)(object)labelRegion);
		((Control)guna2Panel1).Controls.Add((Control)(object)Status);
		((Control)guna2Panel1).Controls.Add((Control)(object)labelActivaction);
		((Control)guna2Panel1).Controls.Add((Control)(object)label16);
		((Control)guna2Panel1).Controls.Add((Control)(object)guna2GradientButton2);
		((Control)guna2Panel1).Controls.Add((Control)(object)ModeloffHello);
		((Control)guna2Panel1).Controls.Add((Control)(object)label20);
		((Control)guna2Panel1).Controls.Add((Control)(object)label15);
		((Control)guna2Panel1).Controls.Add((Control)(object)ActivateButton);
		((Control)guna2Panel1).Controls.Add((Control)(object)labelType);
		((Control)guna2Panel1).Controls.Add((Control)(object)pictureBox3);
		((Control)guna2Panel1).Controls.Add((Control)(object)label23);
		((Control)guna2Panel1).Controls.Add((Control)(object)labelSN);
		((Control)guna2Panel1).Controls.Add((Control)(object)labelVersion);
		((Control)guna2Panel1).Controls.Add((Control)(object)pictureBoxDC);
		((Control)guna2Panel1).Controls.Add((Control)(object)pictureBoxModel);
		((Control)guna2Panel1).Location = new Point(7, 96);
		((Control)guna2Panel1).Name = "guna2Panel1";
		((Control)guna2Panel1).Size = new Size(669, 210);
		((Control)guna2Panel1).TabIndex = 788;
		guna2Panel1.UseTransparentBackground = true;
		((Control)label1).AutoSize = true;
		((Control)label1).BackColor = Color.Transparent;
		((Control)label1).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)label1).ForeColor = Color.White;
		((Control)label1).Location = new Point(462, 78);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(66, 16);
		((Control)label1).TabIndex = 821;
		((Control)label1).Text = "Region :";
		((Control)labelRegion).AutoSize = true;
		((Control)labelRegion).BackColor = Color.Transparent;
		((Control)labelRegion).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)labelRegion).ForeColor = Color.White;
		((Control)labelRegion).Location = new Point(531, 78);
		((Control)labelRegion).Name = "labelRegion";
		((Control)labelRegion).Size = new Size(36, 16);
		((Control)labelRegion).TabIndex = 822;
		((Control)labelRegion).Text = "N/A";
		((Control)Status).AutoSize = true;
		((Control)Status).BackColor = Color.Transparent;
		((Control)Status).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)Status).ForeColor = Color.White;
		((Control)Status).Location = new Point(174, 127);
		((Control)Status).Name = "Status";
		((Control)Status).Size = new Size(62, 16);
		((Control)Status).TabIndex = 817;
		((Control)Status).Text = "Status :";
		((Control)labelActivaction).AutoSize = true;
		((Control)labelActivaction).BackColor = Color.Transparent;
		((Control)labelActivaction).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)labelActivaction).ForeColor = Color.White;
		((Control)labelActivaction).Location = new Point(237, 127);
		((Control)labelActivaction).Name = "labelActivaction";
		((Control)labelActivaction).Size = new Size(36, 16);
		((Control)labelActivaction).TabIndex = 818;
		((Control)labelActivaction).Text = "N/A";
		guna2GradientButton2.Animated = true;
		((Control)guna2GradientButton2).BackColor = Color.Transparent;
		guna2GradientButton2.BorderColor = Color.Transparent;
		guna2GradientButton2.BorderRadius = 3;
		guna2GradientButton2.BorderStyle = (DashStyle)1;
		((ButtonState)guna2GradientButton2.DisabledState).BorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton2.DisabledState).CustomBorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton2.DisabledState).FillColor = Color.FromArgb(169, 169, 169);
		guna2GradientButton2.DisabledState.FillColor2 = Color.FromArgb(169, 169, 169);
		((ButtonState)guna2GradientButton2.DisabledState).ForeColor = Color.FromArgb(141, 141, 141);
		guna2GradientButton2.FillColor = Color.FromArgb(55, 55, 55);
		guna2GradientButton2.FillColor2 = Color.FromArgb(55, 55, 55);
		((Control)guna2GradientButton2).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)guna2GradientButton2).ForeColor = Color.GhostWhite;
		guna2GradientButton2.GradientMode = (LinearGradientMode)2;
		guna2GradientButton2.Image = (Image)(object)Titan.Properties.Resources.icons8_unlock_32;
		guna2GradientButton2.ImageAlign = (HorizontalAlignment)0;
		guna2GradientButton2.ImageSize = new Size(25, 25);
		guna2GradientButton2.IndicateFocus = true;
		((Control)guna2GradientButton2).Location = new Point(443, 158);
		((Control)guna2GradientButton2).Name = "guna2GradientButton2";
		guna2GradientButton2.PressedColor = Color.Transparent;
		guna2GradientButton2.ShadowDecoration.BorderRadius = 4;
		guna2GradientButton2.ShadowDecoration.Depth = 6;
		guna2GradientButton2.ShadowDecoration.Enabled = true;
		((Control)guna2GradientButton2).Size = new Size(212, 33);
		((Control)guna2GradientButton2).TabIndex = 820;
		((Control)guna2GradientButton2).Text = "Block OTA / Reset";
		guna2GradientButton2.UseTransparentBackground = true;
		((Control)guna2GradientButton2).Click += guna2GradientButton2_Click;
		ActivateButton.Animated = true;
		((Control)ActivateButton).BackColor = Color.Transparent;
		ActivateButton.BorderColor = Color.Transparent;
		ActivateButton.BorderRadius = 3;
		ActivateButton.BorderStyle = (DashStyle)1;
		((ButtonState)ActivateButton.DisabledState).BorderColor = Color.DarkGray;
		((ButtonState)ActivateButton.DisabledState).CustomBorderColor = Color.DarkGray;
		((ButtonState)ActivateButton.DisabledState).FillColor = Color.FromArgb(169, 169, 169);
		ActivateButton.DisabledState.FillColor2 = Color.FromArgb(169, 169, 169);
		((ButtonState)ActivateButton.DisabledState).ForeColor = Color.FromArgb(141, 141, 141);
		ActivateButton.FillColor = Color.FromArgb(55, 55, 55);
		ActivateButton.FillColor2 = Color.FromArgb(55, 55, 55);
		((Control)ActivateButton).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)ActivateButton).ForeColor = Color.GhostWhite;
		ActivateButton.GradientMode = (LinearGradientMode)2;
		ActivateButton.Image = (Image)(object)Titan.Properties.Resources.icons8_unlock_32;
		ActivateButton.ImageAlign = (HorizontalAlignment)0;
		ActivateButton.ImageSize = new Size(25, 25);
		ActivateButton.IndicateFocus = true;
		((Control)ActivateButton).Location = new Point(171, 158);
		((Control)ActivateButton).Name = "ActivateButton";
		ActivateButton.PressedColor = Color.Transparent;
		ActivateButton.ShadowDecoration.BorderRadius = 4;
		ActivateButton.ShadowDecoration.Depth = 6;
		ActivateButton.ShadowDecoration.Enabled = true;
		((Control)ActivateButton).Size = new Size(212, 33);
		((Control)ActivateButton).TabIndex = 817;
		((Control)ActivateButton).Text = " Activated iDevice";
		ActivateButton.UseTransparentBackground = true;
		((Control)ActivateButton).Click += ActivateButton_Click;
		((Control)pictureBox3).BackColor = Color.Transparent;
		((Control)pictureBox3).Cursor = Cursors.Hand;
		pictureBox3.Image = (Image)(object)Titan.Properties.Resources.ImplicitTransition;
		((Control)pictureBox3).Location = new Point(450, 126);
		((Control)pictureBox3).Name = "pictureBox3";
		((Control)pictureBox3).Size = new Size(20, 20);
		pictureBox3.SizeMode = (PictureBoxSizeMode)4;
		pictureBox3.TabIndex = 759;
		pictureBox3.TabStop = false;
		((Control)pictureBox3).Click += pictureBox3_Click;
		((Control)pictureBoxDC).BackColor = Color.Transparent;
		pictureBoxDC.Image = (Image)(object)Titan.Properties.Resources.device_recovery;
		((Control)pictureBoxDC).Location = new Point(-7, 1);
		((Control)pictureBoxDC).Name = "pictureBoxDC";
		((Control)pictureBoxDC).Size = new Size(167, 209);
		pictureBoxDC.SizeMode = (PictureBoxSizeMode)4;
		pictureBoxDC.TabIndex = 777;
		pictureBoxDC.TabStop = false;
		((Control)pictureBoxModel).BackColor = Color.Transparent;
		pictureBoxModel.Image = (Image)(object)Titan.Properties.Resources.device_recovery3;
		((Control)pictureBoxModel).Location = new Point(-9, 0);
		((Control)pictureBoxModel).Name = "pictureBoxModel";
		((Control)pictureBoxModel).Size = new Size(167, 209);
		pictureBoxModel.SizeMode = (PictureBoxSizeMode)4;
		pictureBoxModel.TabIndex = 674;
		pictureBoxModel.TabStop = false;
		((Control)label24).AutoSize = true;
		((Control)label24).BackColor = Color.Transparent;
		((Control)label24).Font = new Font("Segoe UI Semibold", 8f, (FontStyle)1);
		((Control)label24).ForeColor = Color.White;
		((Control)label24).Location = new Point(346, 104);
		((Control)label24).Name = "label24";
		((Control)label24).Size = new Size(123, 13);
		((Control)label24).TabIndex = 814;
		((Control)label24).Text = "iDEVICE INFORMATION";
		guna2GradientButton1.Animated = true;
		((Control)guna2GradientButton1).BackColor = Color.Transparent;
		guna2GradientButton1.BorderColor = Color.Transparent;
		guna2GradientButton1.BorderRadius = 3;
		guna2GradientButton1.BorderStyle = (DashStyle)1;
		((ButtonState)guna2GradientButton1.DisabledState).BorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton1.DisabledState).CustomBorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton1.DisabledState).FillColor = Color.FromArgb(169, 169, 169);
		guna2GradientButton1.DisabledState.FillColor2 = Color.FromArgb(169, 169, 169);
		((ButtonState)guna2GradientButton1.DisabledState).ForeColor = Color.FromArgb(141, 141, 141);
		guna2GradientButton1.FillColor = Color.Transparent;
		guna2GradientButton1.FillColor2 = Color.Transparent;
		((Control)guna2GradientButton1).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)guna2GradientButton1).ForeColor = SystemColors.WindowText;
		guna2GradientButton1.GradientMode = (LinearGradientMode)2;
		guna2GradientButton1.Image = (Image)(object)Titan.Properties.Resources.tips_unlock3;
		guna2GradientButton1.ImageSize = new Size(40, 40);
		guna2GradientButton1.IndicateFocus = true;
		((Control)guna2GradientButton1).Location = new Point(744, 41);
		((Control)guna2GradientButton1).Name = "guna2GradientButton1";
		guna2GradientButton1.PressedColor = Color.Transparent;
		((Control)guna2GradientButton1).RightToLeft = (RightToLeft)1;
		guna2GradientButton1.ShadowDecoration.BorderRadius = 2;
		guna2GradientButton1.ShadowDecoration.Depth = 2;
		guna2GradientButton1.ShadowDecoration.Enabled = true;
		guna2GradientButton1.ShadowDecoration.Shadow = new Padding(2);
		((Control)guna2GradientButton1).Size = new Size(31, 32);
		((Control)guna2GradientButton1).TabIndex = 820;
		guna2GradientButton1.UseTransparentBackground = true;
		((Control)guna2GradientButton1).Click += guna2GradientButton1_Click;
		((Control)labelInfoProgres).BackColor = Color.Transparent;
		((Control)labelInfoProgres).Font = new Font("Segoe UI", 9f, (FontStyle)1, (GraphicsUnit)3, (byte)0);
		((Control)labelInfoProgres).ForeColor = Color.White;
		((Control)labelInfoProgres).Location = new Point(0, 348);
		((Control)labelInfoProgres).Name = "labelInfoProgres";
		((Control)labelInfoProgres).Size = new Size(784, 18);
		((Control)labelInfoProgres).TabIndex = 811;
		((Control)labelInfoProgres).Text = "...";
		labelInfoProgres.TextAlign = (ContentAlignment)32;
		((Control)Guna2ProgressBar1).BackColor = Color.Transparent;
		Guna2ProgressBar1.BorderRadius = 5;
		Guna2ProgressBar1.FillColor = Color.Transparent;
		((Control)Guna2ProgressBar1).ForeColor = Color.Transparent;
		((Control)Guna2ProgressBar1).Location = new Point(-3, 324);
		Guna2ProgressBar1.Minimum = 10;
		((Control)Guna2ProgressBar1).Name = "Guna2ProgressBar1";
		Guna2ProgressBar1.ProgressColor = Color.SpringGreen;
		Guna2ProgressBar1.ProgressColor2 = Color.RoyalBlue;
		Guna2ProgressBar1.ShadowDecoration.BorderRadius = 4;
		Guna2ProgressBar1.ShadowDecoration.Depth = 6;
		Guna2ProgressBar1.ShadowDecoration.Enabled = true;
		((Control)Guna2ProgressBar1).Size = new Size(789, 10);
		((Control)Guna2ProgressBar1).TabIndex = 816;
		Guna2ProgressBar1.TextRenderingHint = (TextRenderingHint)0;
		Guna2ProgressBar1.Value = 100;
		((Control)label2).BackColor = Color.Transparent;
		((Control)label2).Font = new Font("Segoe UI", 11f, (FontStyle)1);
		((Control)label2).ForeColor = Color.White;
		((Control)label2).Location = new Point(213, 3);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(328, 29);
		((Control)label2).TabIndex = 823;
		((Control)label2).Text = "iCloud Bypass A12+";
		label2.TextAlign = (ContentAlignment)32;
		((Control)label2).Click += label2_Click;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Control)this).BackColor = Color.FromArgb(32, 32, 32);
		((Control)this).BackgroundImage = (Image)componentResourceManager.GetObject("$this.BackgroundImage");
		((Form)this).ClientSize = new Size(783, 372);
		((Control)this).Controls.Add((Control)(object)label2);
		((Control)this).Controls.Add((Control)(object)guna2CircleButton3);
		((Control)this).Controls.Add((Control)(object)label24);
		((Control)this).Controls.Add((Control)(object)guna2GradientButton1);
		((Control)this).Controls.Add((Control)(object)guna2GradientButton3);
		((Control)this).Controls.Add((Control)(object)guna2CircleButton2);
		((Control)this).Controls.Add((Control)(object)guna2CircleButton1);
		((Control)this).Controls.Add((Control)(object)Guna2ProgressBar1);
		((Control)this).Controls.Add((Control)(object)guna2Panel1);
		((Control)this).Controls.Add((Control)(object)labelInfoProgres);
		((Control)this).ForeColor = Color.White;
		((Form)this).FormBorderStyle = (FormBorderStyle)0;
		((Form)this).Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
		((Form)this).MaximizeBox = false;
		((Form)this).MinimizeBox = false;
		((Control)this).Name = "Form1";
		((Control)this).Text = "TND95 A12+";
		((Form)this).Load += Form1_Load;
		((Control)guna2Panel1).ResumeLayout(false);
		((Control)guna2Panel1).PerformLayout();
		((ISupportInitialize)pictureBox3).EndInit();
		((ISupportInitialize)pictureBoxDC).EndInit();
		((ISupportInitialize)pictureBoxModel).EndInit();
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}
}
