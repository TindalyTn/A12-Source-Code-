#define DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Titan;

public class ApiClient : IDisposable
{
	private readonly HttpClient _httpClient;

	private readonly string _baseUrl;

	private readonly string _rsaPublicKey;

	private readonly string _rsaPrivateKey;

	private bool _disposed = false;

	public ApiClient(string baseUrl, string rsaPublicKey, string rsaPrivateKey)
	{

		_baseUrl = baseUrl.TrimEnd(new char[1] { '/' });
		_rsaPublicKey = rsaPublicKey;
		_rsaPrivateKey = rsaPrivateKey;
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromMinutes(5.0);
	}

	public async Task<ApiResponse<ReceiveDataResponse>> SendClientDataAsync(string serial, string ecid, string guid)
	{
		try
		{
			var clientData = new
			{
				ecid = ecid,
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				guid = guid
			};
			string jsonData = JsonConvert.SerializeObject((object)clientData);
			var requestData = new
			{
				serial = serial,
				encrypted_data = await EncryptRsaAsync(jsonData)
			};
			string requestJson = JsonConvert.SerializeObject((object)requestData);
			StringContent content = new StringContent(requestJson, Encoding.UTF8, "application/json");
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.Timeout = TimeSpan.FromMinutes(5.0);
				((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "iRealm-A12-Client/1.0");
				((HttpHeaders)httpClient.DefaultRequestHeaders).Add("Accept", "application/json");
				HttpResponseMessage response = await httpClient.PostAsync(_baseUrl + "/api_wrapper.php?endpoint=receive_client_data", (HttpContent)(object)content);
				string responseContent = await response.Content.ReadAsStringAsync();
				Debug.WriteLine($"API Response Status: {response.StatusCode}");
				Debug.WriteLine("API Response Content: " + responseContent);
				if (response.IsSuccessStatusCode)
				{
					try
					{
						ReceiveDataResponse result = JsonConvert.DeserializeObject<ReceiveDataResponse>(responseContent);
						return new ApiResponse<ReceiveDataResponse>
						{
							Success = true,
							Data = result
						};
					}
					catch (Exception ex2)
					{
						Debug.WriteLine("JSON deserialization error: " + ex2.Message);
						return new ApiResponse<ReceiveDataResponse>
						{
							Success = false,
							Error = "JSON deserialization failed: " + ex2.Message
						};
					}
				}
				return new ApiResponse<ReceiveDataResponse>
				{
					Success = false,
					Error = $"HTTP {response.StatusCode}: {responseContent}"
				};
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			return new ApiResponse<ReceiveDataResponse>
			{
				Success = false,
				Error = "Send client data failed: " + ex.Message
			};
		}
	}

	public async Task<ApiResponse<byte[]>> GenerateSqliteAsync(string serial, string ecid, string guid, string labelSN, string iOSVer, string labelType, string modeloffHello, string authToken = null)
	{
		try
		{
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			Console.WriteLine("[GENERATE SQLITE] Method called");
			Console.WriteLine($"[GENERATE SQLITE] Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			Console.WriteLine("[GENERATE SQLITE] Serial: " + (serial ?? "NULL"));
			Console.WriteLine("[GENERATE SQLITE] ECID: " + (ecid ?? "NULL") + " (client-side only)");
			Console.WriteLine("[GENERATE SQLITE] GUID: " + (guid ?? "NULL"));
			Console.WriteLine("[GENERATE SQLITE] ios: " + (iOSVer ?? "NULL"));
			Console.WriteLine("[GENERATE SQLITE] LabelSN: " + (labelSN ?? "NULL"));
			Console.WriteLine("[GENERATE SQLITE] LabelType: " + (labelType ?? "NULL"));
			Console.WriteLine("[GENERATE SQLITE] ModeloffHello: " + (modeloffHello ?? "NULL"));
			Console.WriteLine("[GENERATE SQLITE] AuthToken: " + (string.IsNullOrEmpty(authToken) ? "NULL" : "***PROVIDED***"));
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			if (string.IsNullOrEmpty(serial))
			{
				Console.WriteLine("[GENERATE SQLITE] ❌ ERROR: Serial is required");
				return new ApiResponse<byte[]>
				{
					Success = false,
					Error = "Serial is required"
				};
			}
			if (string.IsNullOrEmpty(guid) || guid == "{GUID}" || guid == "GUID" || guid.Contains("{"))
			{
				Console.WriteLine("[GENERATE SQLITE] ❌ ERROR: GUID is invalid or placeholder");
				return new ApiResponse<byte[]>
				{
					Success = false,
					Error = "GUID must be a real value, not a placeholder"
				};
			}
			if (string.IsNullOrEmpty(labelType))
			{
				Console.WriteLine("[GENERATE SQLITE] ❌ ERROR: LabelType is required");
				return new ApiResponse<byte[]>
				{
					Success = false,
					Error = "LabelType is required"
				};
			}
			var requestData = new { serial, guid, labelSN, iOSVer, labelType, modeloffHello };
			Console.WriteLine("[GENERATE SQLITE] ✅ Request payload created:");
			Console.WriteLine("[GENERATE SQLITE]    - serial: " + serial);
			Console.WriteLine("[GENERATE SQLITE]    - guid: " + guid);
			Console.WriteLine("[GENERATE SQLITE]    - labelSN: " + labelSN);
			Console.WriteLine("[GENERATE SQLITE]    - iOSVer: " + iOSVer);
			Console.WriteLine("[GENERATE SQLITE]    - labelType: " + labelType);
			Console.WriteLine("[GENERATE SQLITE]    - modeloffHello: " + modeloffHello);
			Console.WriteLine("[GENERATE SQLITE] ℹ\ufe0f  Server will use its own template (no assets sent)");
			string requestJson = JsonConvert.SerializeObject((object)requestData);
			StringContent content = new StringContent(requestJson, Encoding.UTF8, "application/json");
			Console.WriteLine("[GENERATE SQLITE] JSON payload: " + requestJson);
			Console.WriteLine("[GENERATE SQLITE] Sending POST request to: " + _baseUrl + "/api_wrapper.php?endpoint=sqlite_irealmversion");
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.Timeout = TimeSpan.FromMinutes(5.0);
				HttpResponseMessage response = await httpClient.PostAsync(_baseUrl + "/api_wrapper.php?endpoint=sqlite_irealmversion", (HttpContent)(object)content);
				MediaTypeHeaderValue contentType2 = response.Content.Headers.ContentType;
				string contentType = ((contentType2 != null) ? contentType2.MediaType : null) ?? "unknown";
				long? contentLength = response.Content.Headers.ContentLength;
				Console.WriteLine("[GENERATE SQLITE] ═══════════════════════════════════════");
				Console.WriteLine("[GENERATE SQLITE] Response received:");
				Console.WriteLine($"[GENERATE SQLITE]    - Status: {response.StatusCode}");
				Console.WriteLine("[GENERATE SQLITE]    - Content-Type: " + contentType);
				Console.WriteLine("[GENERATE SQLITE]    - Content-Length: " + (contentLength?.ToString() ?? "unknown"));
				Console.WriteLine("[GENERATE SQLITE] ═══════════════════════════════════════");
				if (!response.IsSuccessStatusCode)
				{
					string errorContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[GENERATE SQLITE] ❌ HTTP ERROR: {response.StatusCode}");
					Console.WriteLine("[GENERATE SQLITE] Error content: " + errorContent);
					try
					{
						string errorMessage = ((dynamic)JsonConvert.DeserializeObject<object>(errorContent))?.message?.ToString() ?? errorContent;
						return new ApiResponse<byte[]>
						{
							Success = false,
							Error = "Server error: " + errorMessage
						};
					}
					catch
					{
						return new ApiResponse<byte[]>
						{
							Success = false,
							Error = $"HTTP {response.StatusCode}: {errorContent}"
						};
					}
				}
				if (contentType.Contains("octet-stream") || contentType.Contains("application/x-sqlite") || contentType.Contains("application/vnd.sqlite3"))
				{
					Console.WriteLine("[GENERATE SQLITE] ✅ Binary SQLite file detected");
					byte[] fileData = await response.Content.ReadAsByteArrayAsync();
					Console.WriteLine($"[GENERATE SQLITE] File downloaded: {fileData.Length} bytes ({(double)fileData.Length / 1024.0:F2} KB)");
					if (fileData.Length > 16)
					{
						string header = Encoding.ASCII.GetString(fileData, 0, 15);
						if (header.StartsWith("SQLite format"))
						{
							Console.WriteLine("[GENERATE SQLITE] ✅ SQLite format verified: " + header);
							string fileText = Encoding.UTF8.GetString(fileData);
							bool hasPlaceholderRuta = fileText.Contains("{Ruta}");
							bool hasPlaceholderGuid = fileText.Contains("{GUID}");
							int labelTypeOccurrences = CountOccurrences(fileText, labelType);
							int guidOccurrences = CountOccurrences(fileText, guid);
							Console.WriteLine("[GENERATE SQLITE] ═══════════════════════════════════════");
							Console.WriteLine("[GENERATE SQLITE] Placeholder Replacement Verification:");
							Console.WriteLine($"[GENERATE SQLITE]    - {{Ruta}} found: {hasPlaceholderRuta}");
							Console.WriteLine($"[GENERATE SQLITE]    - {{GUID}} found: {hasPlaceholderGuid}");
							Console.WriteLine($"[GENERATE SQLITE]    - labelType ({labelType}) occurrences: {labelTypeOccurrences}");
							Console.WriteLine($"[GENERATE SQLITE]    - guid ({guid}) occurrences: {guidOccurrences}");
							if (hasPlaceholderRuta || hasPlaceholderGuid)
							{
								Console.WriteLine("[GENERATE SQLITE] ⚠\ufe0f⚠\ufe0f⚠\ufe0f WARNING: Placeholders NOT replaced!");
								Console.WriteLine("[GENERATE SQLITE] ⚠\ufe0f Database contains unreplaced placeholder strings");
								Console.WriteLine("[GENERATE SQLITE] ⚠\ufe0f This should not happen with server v9.0");
							}
							else if (labelTypeOccurrences > 0 && guidOccurrences > 0)
							{
								Console.WriteLine("[GENERATE SQLITE] ✅ Placeholders replaced successfully!");
								Console.WriteLine("[GENERATE SQLITE] ✅ Database contains real values");
							}
							else
							{
								Console.WriteLine("[GENERATE SQLITE] ⚠\ufe0f WARNING: Real values not found in database");
								Console.WriteLine("[GENERATE SQLITE] ⚠\ufe0f This may indicate a problem with the template");
							}
							Console.WriteLine("[GENERATE SQLITE] ═══════════════════════════════════════");
							Console.WriteLine("[GENERATE SQLITE] ✅✅✅ SUCCESS - SQLite file ready");
							Console.WriteLine($"[GENERATE SQLITE] File size: {fileData.Length} bytes");
							Console.WriteLine("[GENERATE SQLITE] Format: SQLite format 3");
							Console.WriteLine("[GENERATE SQLITE] Serial: " + serial);
							Console.WriteLine("═══════════════════════════════════════════════════════════");
							return new ApiResponse<byte[]>
							{
								Success = true,
								Data = fileData
							};
						}
						Console.WriteLine("[GENERATE SQLITE] ❌ Invalid SQLite header: " + header);
						return new ApiResponse<byte[]>
						{
							Success = false,
							Error = "Invalid SQLite file header: " + header
						};
					}
					Console.WriteLine($"[GENERATE SQLITE] ❌ File too small: {fileData.Length} bytes");
					return new ApiResponse<byte[]>
					{
						Success = false,
						Error = $"File too small to be valid SQLite: {fileData.Length} bytes"
					};
				}
				if (contentType.Contains("json"))
				{
					Console.WriteLine("[GENERATE SQLITE] ⚠\ufe0f JSON response detected (unexpected with v9.0)");
					string responseContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine("[GENERATE SQLITE] Response: " + responseContent);
					try
					{
						object jsonResponse = JsonConvert.DeserializeObject<object>(responseContent);
						if (((dynamic)jsonResponse)?.status?.ToString() == "error")
						{
							string errorMsg = ((dynamic)jsonResponse)?.message?.ToString() ?? "Unknown error";
							Console.WriteLine("[GENERATE SQLITE] ❌ Server error: " + errorMsg);
							return new ApiResponse<byte[]>
							{
								Success = false,
								Error = errorMsg
							};
						}
						Console.WriteLine("[GENERATE SQLITE] ❌ Unexpected JSON format");
						return new ApiResponse<byte[]>
						{
							Success = false,
							Error = "Unexpected JSON response: " + responseContent
						};
					}
					catch (Exception ex5)
					{
						Exception ex4 = ex5;
						Console.WriteLine("[GENERATE SQLITE] ❌ JSON parse error: " + ex4.Message);
						return new ApiResponse<byte[]>
						{
							Success = false,
							Error = "Invalid JSON response: " + ex4.Message
						};
					}
				}
				string responseContent2 = await response.Content.ReadAsStringAsync();
				string preview = responseContent2[..Math.Min(500, responseContent2.Length)];
				Console.WriteLine("[GENERATE SQLITE] ❌ Unexpected content-type: " + contentType);
				Console.WriteLine("[GENERATE SQLITE] Response preview: " + preview);
				return new ApiResponse<byte[]>
				{
					Success = false,
					Error = "Unexpected content-type: " + contentType + ". Preview: " + preview
				};
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (TaskCanceledException ex6)
		{
			TaskCanceledException ex3 = ex6;
			Console.WriteLine("[GENERATE SQLITE] ❌ TIMEOUT - Request took too long");
			Console.WriteLine("[GENERATE SQLITE] Error: " + ex3.Message);
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			return new ApiResponse<byte[]>
			{
				Success = false,
				Error = "Request timeout - Server took too long to respond. This may happen with high concurrent load."
			};
		}
		catch (HttpRequestException val)
		{
			HttpRequestException val2 = val;
			HttpRequestException ex2 = val2;
			Console.WriteLine("[GENERATE SQLITE] ❌ NETWORK ERROR");
			Console.WriteLine("[GENERATE SQLITE] Error: " + ((Exception)(object)ex2).Message);
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			return new ApiResponse<byte[]>
			{
				Success = false,
				Error = "Network error: " + ((Exception)(object)ex2).Message
			};
		}
		catch (Exception ex5)
		{
			Exception ex = ex5;
			Console.WriteLine("[GENERATE SQLITE] ❌❌❌ EXCEPTION ❌❌❌");
			Console.WriteLine("[GENERATE SQLITE] Type: " + ex.GetType().Name);
			Console.WriteLine("[GENERATE SQLITE] Error: " + ex.Message);
			Console.WriteLine("[GENERATE SQLITE] Stack trace:");
			Console.WriteLine(ex.StackTrace);
			Console.WriteLine("═══════════════════════════════════════════════════════════");
			return new ApiResponse<byte[]>
			{
				Success = false,
				Error = "Exception: " + ex.Message
			};
		}
	}

	public async Task<ApiResponse<AuthResponse>> AuthenticateAsync(string serial, string apiKey)
	{
		try
		{
			var requestData = new
			{
				serial = serial,
				api_key = apiKey
			};
			string requestJson = JsonConvert.SerializeObject((object)requestData);
			StringContent content = new StringContent(requestJson, Encoding.UTF8, "application/json");
			HttpClient httpClient = new HttpClient();
			try
			{
				httpClient.Timeout = TimeSpan.FromMinutes(5.0);
				((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "iRealm-A12-Client/1.0");
				((HttpHeaders)httpClient.DefaultRequestHeaders).Add("Accept", "application/json");
				HttpResponseMessage response = await httpClient.PostAsync(_baseUrl + "/api_wrapper.php?endpoint=auth", (HttpContent)(object)content);
				string responseContent = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					try
					{
						AuthResponse result = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
						return new ApiResponse<AuthResponse>
						{
							Success = true,
							Data = result
						};
					}
					catch (Exception ex2)
					{
						Debug.WriteLine("JSON deserialization error: " + ex2.Message);
						return new ApiResponse<AuthResponse>
						{
							Success = false,
							Error = "JSON deserialization failed: " + ex2.Message
						};
					}
				}
				return new ApiResponse<AuthResponse>
				{
					Success = false,
					Error = $"HTTP {response.StatusCode}: {responseContent}"
				};
			}
			finally
			{
				((IDisposable)httpClient)?.Dispose();
			}
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			return new ApiResponse<AuthResponse>
			{
				Success = false,
				Error = "Authentication failed: " + ex.Message
			};
		}
	}

	public async Task<ApiResponse<byte[]>> DownloadAndDecryptSqliteAsync(GenerateSqliteResponse sqliteResponse, string ecid)
	{
		try
		{
			byte[] fileData2 = Convert.FromBase64String(sqliteResponse.File);
			if (string.IsNullOrEmpty(sqliteResponse.File))
			{
				return new ApiResponse<byte[]>
				{
					Success = false,
					Error = "No file data received from server"
				};
			}
			if (sqliteResponse.CipherAlg == "NONE")
			{
				return new ApiResponse<byte[]>
				{
					Success = true,
					Data = fileData2
				};
			}
			if (sqliteResponse.CipherAlg == "AES-256-CBC")
			{
				byte[] decryptedAesKey = new byte[32];
				Array.Copy(Convert.FromBase64String(sqliteResponse.EncKey), decryptedAesKey, Math.Min(32, Convert.FromBase64String(sqliteResponse.EncKey).Length));
				byte[] iv = Convert.FromBase64String(sqliteResponse.Iv);
				fileData2 = await DecryptAesCbcAsync(fileData2, decryptedAesKey, iv);
				if (fileData2 == null)
				{
					return new ApiResponse<byte[]>
					{
						Success = false,
						Error = "File decryption failed"
					};
				}
				return new ApiResponse<byte[]>
				{
					Success = true,
					Data = fileData2
				};
			}
			return new ApiResponse<byte[]>
			{
				Success = false,
				Error = "Unsupported cipher algorithm: " + sqliteResponse.CipherAlg
			};
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			return new ApiResponse<byte[]>
			{
				Success = false,
				Error = "Download and decrypt failed: " + ex.Message
			};
		}
	}

	private int CountOccurrences(string text, string pattern)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
		{
			return 0;
		}
		int num = 0;
		int startIndex = 0;
		while ((startIndex = text.IndexOf(pattern, startIndex, StringComparison.Ordinal)) != -1)
		{
			num++;
			startIndex += pattern.Length;
		}
		return num;
	}

	private bool ContainsBinaryPattern(byte[] data, string pattern)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(pattern);
		for (int i = 0; i <= data.Length - bytes.Length; i++)
		{
			bool flag = true;
			for (int j = 0; j < bytes.Length; j++)
			{
				if (data[i + j] != bytes[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private string GenerateSessionKey(string serial, string clientIp)
	{
		using SHA256 sHA = SHA256.Create();
		string s = serial + ":" + clientIp + ":session_key";
		byte[] inArray = sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
		return Convert.ToBase64String(inArray);
	}

	private byte[] DecryptAesCbc(byte[] encryptedData, string key, byte[] iv)
	{
		using Aes aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		aes.Key = Convert.FromBase64String(key);
		aes.IV = iv;
		using ICryptoTransform transform = aes.CreateDecryptor();
		using MemoryStream stream = new MemoryStream(encryptedData);
		using CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
		using MemoryStream memoryStream = new MemoryStream();
		cryptoStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	private byte[] DecryptRsaAsync(byte[] encryptedData)
	{
		try
		{
			using RSA rSA = RSA.Create();
			RSAParameters rSAParameters = default(RSAParameters);
			rSAParameters.Modulus = Convert.FromBase64String(_rsaPrivateKey);
			rSAParameters.Exponent = Convert.FromBase64String("AQAB");
			RSAParameters parameters = rSAParameters;
			rSA.ImportParameters(parameters);
			return rSA.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);
		}
		catch (Exception ex)
		{
			throw new CryptographicException("RSA decryption failed: " + ex.Message, ex);
		}
	}

	private async Task<string> EncryptRsaAsync(string data)
	{
		return await Task.Run(delegate
		{
			try
			{
				byte[] bytes = Encoding.UTF8.GetBytes(data);
				return Convert.ToBase64String(bytes);
			}
			catch (Exception ex)
			{
				throw new CryptographicException("Data encoding failed: " + ex.Message, ex);
			}
		});
	}

	private async Task<string> DecryptRsaAsync(string encryptedData)
	{
		return await Task.FromResult(encryptedData);
	}

	private async Task<byte[]> DecryptAesCbcAsync(byte[] encryptedData, byte[] key, byte[] iv)
	{
		return await Task.Run(delegate
		{
			try
			{
				using Aes aes = Aes.Create();
				aes.Key = key;
				aes.IV = iv;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				using ICryptoTransform cryptoTransform = aes.CreateDecryptor();
				return cryptoTransform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
			}
			catch (Exception ex)
			{
				throw new CryptographicException("AES-CBC decryption failed: " + ex.Message, ex);
			}
		});
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		if (disposing)
		{
			HttpClient httpClient = _httpClient;
			if (httpClient != null)
			{
				((HttpMessageInvoker)httpClient).Dispose();
			}
		}
		_disposed = true;
	}
}
