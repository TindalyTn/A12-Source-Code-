using Newtonsoft.Json;

namespace Titan;

public class GenerateSqliteResponse
{
	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("cipher_alg")]
	public string CipherAlg { get; set; }

	[JsonProperty("iv")]
	public string Iv { get; set; }

	[JsonProperty("tag")]
	public string Tag { get; set; }

	[JsonProperty("enc_key")]
	public string EncKey { get; set; }

	[JsonProperty("session_iv")]
	public string SessionIv { get; set; }

	[JsonProperty("session_token")]
	public string SessionToken { get; set; }

	[JsonProperty("file")]
	public string File { get; set; }

	[JsonProperty("file_size")]
	public int FileSize { get; set; }

	[JsonProperty("timestamp")]
	public long Timestamp { get; set; }

	[JsonProperty("nonce")]
	public string Nonce { get; set; }
}
