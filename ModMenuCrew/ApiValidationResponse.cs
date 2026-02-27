using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModMenuCrew;

public class ApiValidationResponse
{
	[JsonPropertyName("status")]
	public string Status { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; }

	[JsonPropertyName("download_token")]
	public string Download_Token { get; set; }

	[JsonPropertyName("username")]
	public string Username { get; set; }

	[JsonPropertyName("session_token")]
	public long? SessionToken { get; set; }

	[JsonPropertyName("new_token")]
	public long? NewToken { get; set; }

	[JsonPropertyName("key_type")]
	public string KeyType { get; set; }

	[JsonPropertyName("is_premium")]
	public bool? IsPremium { get; set; }

	[JsonPropertyName("expires_at")]
	public long? ExpiresAt { get; set; }

	[JsonPropertyName("reason")]
	public string Reason { get; set; }

	[JsonPropertyName("hint")]
	public string Hint { get; set; }

	[JsonPropertyName("signature")]
	public string Signature { get; set; }

	[JsonPropertyName("session_proof")]
	public string SessionProof { get; set; }

	[JsonPropertyName("proof_seed")]
	public long? ProofSeed { get; set; }

	[JsonPropertyName("proof_expires")]
	public long? ProofExpires { get; set; }

	[JsonPropertyName("render_key")]
	public string RenderKey { get; set; }

	[JsonPropertyName("render_expires")]
	public long? RenderExpires { get; set; }

	[JsonPropertyName("render_nonce")]
	public long? RenderNonce { get; set; }

	[JsonPropertyName("ui_definition")]
	public JsonElement? UiDefinition { get; set; }

	[JsonPropertyName("session_decrypt_key")]
	public string SessionDecryptKey { get; set; }

	[JsonPropertyName("ui_payload")]
	public EncryptedPayload UiPayload { get; set; }

	[JsonPropertyName("session_key")]
	public string SessionKey { get; set; }

	[JsonPropertyName("payload_signature")]
	public string PayloadSignature { get; set; }

	[JsonPropertyName("signature_rsa")]
	public string SignatureRsa { get; set; }

	[JsonPropertyName("force_update")]
	public bool? ForceUpdate { get; set; }

	[JsonPropertyName("download_url")]
	public string DownloadUrl { get; set; }

	[JsonPropertyName("min_version")]
	public string MinVersion { get; set; }

	[JsonPropertyName("discord_authenticated")]
	public bool? DiscordAuthenticated { get; set; }

	[JsonPropertyName("expected_tick")]
	public long? ExpectedTick { get; set; }

	[JsonPropertyName("tick_tolerance")]
	public long? TickTolerance { get; set; }

	[JsonPropertyName("heartbeat_nonce")]
	public string HeartbeatNonce { get; set; }

	[JsonPropertyName("attestation_seed")]
	public string AttestationSeed { get; set; }

	[JsonPropertyName("attestation_methods")]
	public int[] AttestationMethods { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
