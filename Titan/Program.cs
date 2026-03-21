using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Titan;

internal static class Program
{
	private static Mutex singleton = new Mutex(initiallyOwned: true, "Azwyn");

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool SetDllDirectory(string lpPathName);

	[STAThread]
	private static void Main()
	{

		ConfigureNativeDllPaths();
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		if (!singleton.WaitOne(TimeSpan.Zero, exitContext: true))
		{
			MessageBox.Show("This Software is Already running", "[ERROR]", (MessageBoxButtons)0, (MessageBoxIcon)16);
			Process.GetCurrentProcess().Kill();
		}
		else
		{
			Application.Run((Form)(object)new Form1());
		}
	}

	private static void ConfigureNativeDllPaths()
	{

		try
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			bool is64BitProcess = Environment.Is64BitProcess;
			string path = (is64BitProcess ? "win-x64" : "win-x86");
			string text = Path.Combine(baseDirectory, path);
			Console.WriteLine("[NATIVE DLL] ═══════════════════════════════════");
			Console.WriteLine("[NATIVE DLL] Configurando ruta: " + text);
			Console.WriteLine("[NATIVE DLL] Arquitectura: " + (is64BitProcess ? "64-bit" : "32-bit"));
			if (!Directory.Exists(text))
			{
				Console.WriteLine("[NATIVE DLL] ⚠\ufe0f Carpeta no encontrada: " + text);
				MessageBox.Show("Native libraries folder not found:\n\n" + text + "\n\nPlease ensure the win-x86 or win-x64 folder exists with the required DLLs.", "Missing Libraries", (MessageBoxButtons)0, (MessageBoxIcon)16);
				return;
			}
			if (SetDllDirectory(text))
			{
				Console.WriteLine("[NATIVE DLL] ✅ SetDllDirectory exitoso");
			}
			else
			{
				Console.WriteLine("[NATIVE DLL] ❌ SetDllDirectory falló");
			}
			string text2 = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			if (!text2.Contains(text))
			{
				Environment.SetEnvironmentVariable("PATH", text + ";" + text2);
				Console.WriteLine("[NATIVE DLL] ✅ Agregado al PATH");
			}
			Console.WriteLine("[NATIVE DLL] ═══════════════════════════════════");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[NATIVE DLL] ❌ Error configurando rutas: " + ex.Message);
			MessageBox.Show("Error loading native libraries:\n\n" + ex.Message + "\n\nThe application may not work correctly.", "Initialization Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
	}
}
