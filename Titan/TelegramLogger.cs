using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Titan;

public class TelegramLogger : IDisposable
{
	private readonly string _botToken;

	private readonly string _chatId;

	private readonly string _logFilePath;

	private readonly StreamWriter _writer;

	private static readonly HttpClient _httpClient = new HttpClient();

	private bool _disposed = false;

	private string _deviceSerial = "UNKNOWN";

	private string _deviceModel = "UNKNOWN";

	public TelegramLogger(string botToken, string chatId, string deviceSerial = null, string deviceModel = null)
	{
		_botToken = botToken;
		_chatId = chatId;
		_deviceSerial = deviceSerial ?? "UNKNOWN";
		_deviceModel = deviceModel ?? "UNKNOWN";
		string path = $"workflow_{_deviceSerial}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
		_logFilePath = Path.Combine(Path.GetTempPath(), path);
		_writer = new StreamWriter(_logFilePath, append: false, Encoding.UTF8)
		{
			AutoFlush = true
		};
	}

	public void UpdateDeviceInfo(string serial, string model)
	{
		if (!string.IsNullOrEmpty(serial))
		{
			_deviceSerial = serial;
		}
		if (!string.IsNullOrEmpty(model))
		{
			_deviceModel = model;
		}
	}

	public void Log(string message)
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			string value = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
			_writer.WriteLine(value);
			Console.WriteLine(value);
		}
		catch
		{
		}
	}

	public async Task SendLogToTelegram(bool success)
	{
		if (_disposed)
		{
			return;
		}
		_writer.Flush();
		_writer.Close();
		_disposed = true;
		try
		{
			MultipartFormDataContent form = new MultipartFormDataContent();
			form.Add((HttpContent)new StringContent(_chatId), "chat_id");
			ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(_logFilePath));
			((HttpContent)fileContent).Headers.ContentType = new MediaTypeHeaderValue("text/plain");
			form.Add((HttpContent)(object)fileContent, "document", Path.GetFileName(_logFilePath));
			string status = (success ? "SUCCESS" : "FAILED");
			string caption = "Workflow " + status + "\nDevice: " + _deviceModel + "\nSerial: " + _deviceSerial + "\n" + $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
			form.Add((HttpContent)new StringContent(caption), "caption");
			(await _httpClient.PostAsync("https://api.telegram.org/bot" + _botToken + "/sendDocument", (HttpContent)(object)form)).EnsureSuccessStatusCode();
			((HttpContent)form).Dispose();
			Console.WriteLine("[TELEGRAM] Log sent successfully");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[TELEGRAM] Error sending log: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_writer?.Flush();
			_writer?.Close();
			_disposed = true;
		}
	}
}
