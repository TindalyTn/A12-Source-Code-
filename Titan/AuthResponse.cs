namespace Titan;

public class AuthResponse
{
	public string Status { get; set; }

	public string Message { get; set; }

	public string AuthToken { get; set; }

	public int ExpiresIn { get; set; }

	public long Timestamp { get; set; }
}
