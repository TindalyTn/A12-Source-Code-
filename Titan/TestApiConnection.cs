using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Titan;

public static class TestApiConnection
{
	public static async Task<bool> TestConnection()
	{
		try
		{
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.Timeout = TimeSpan.FromSeconds(10.0);
				((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "iRealm-A12-Client/1.0");
				HttpResponseMessage response = await httpClient.GetAsync("https://server-backend-here.com/api_wrapper.php");
				string content = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"API Test - Status: {response.StatusCode}");
				Console.WriteLine("API Test - Content: " + content);
				return response.IsSuccessStatusCode;
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Console.WriteLine("API Test - Error: " + ex.Message);
			return false;
		}
	}
}
