using System.Text.Json.Serialization;

namespace ModMenuCrew;

public class EncryptedPayload
{
	[JsonPropertyName("ciphertext")]
	[JsonPropertyOrder(1)]
	public string Ciphertext { get; set; }

	[JsonPropertyName("iv")]
	[JsonPropertyOrder(2)]
	public string Iv { get; set; }

	[JsonPropertyName("tag")]
	[JsonPropertyOrder(3)]
	public string Tag { get; set; }
}
