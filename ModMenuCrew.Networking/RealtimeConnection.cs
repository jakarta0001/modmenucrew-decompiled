using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InnerNet;
using ModMenuCrew.UI.Managers;
using ModMenuCrew.UI.Menus;
using UnityEngine;

namespace ModMenuCrew.Networking;

public static class RealtimeConnection
{
	private static ClientWebSocket _webSocket;

	private static CancellationTokenSource _cancellationTokenSource;

	private static bool _isConnected = false;

	private static bool _isAuthenticated = false;

	private static bool _shouldReconnect = true;

	private static volatile bool _isConnecting = false;

	private static int _reconnectAttempts = 0;

	private static readonly int MAX_RECONNECT_ATTEMPTS = 15;

	private static readonly int BASE_RECONNECT_DELAY_MS = 3000;

	private static long _lastConnectAttemptMs = 0L;

	private const long MIN_RECONNECT_INTERVAL_MS = 3000L;

	private const int KEEPALIVE_UPDATE_INTERVAL_MS = 45000;

	private static CancellationTokenSource _keepaliveCts = null;

	private static string _cachedKey = "";

	private static string _cachedToken = "";

	private static string _cachedHwid = "";

	private static string _wsUrl = "";

	private static byte[] _channelKey = null;

	private static int _channelEncryptedXor = 1513914000;

	private const int CHANNEL_SENTINEL = 1513914001;

	private static long _lastForceReconnectMs = 0L;

	private const long FORCE_RECONNECT_COOLDOWN_MS = 30000L;

	private static bool IsChannelEncrypted => (_channelEncryptedXor ^ 0x5A3C7E91) == 1513914001;

	internal static bool IsConnected
	{
		get
		{
			if (_isConnected)
			{
				return _isAuthenticated;
			}
			return false;
		}
	}

	private static void SetChannelEncrypted(bool value)
	{
		_channelEncryptedXor = ((!value) ? 1513914000 : 0);
	}

	internal static async void Connect(string baseUrl, string key, string token)
	{
		if (!_isConnected)
		{
			if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				ServerData.TriggerSilentDenial();
				return;
			}
			_cachedKey = key;
			_cachedToken = token;
			_cachedHwid = ModKeyValidator.CachedHwid;
			_shouldReconnect = true;
			_reconnectAttempts = 0;
			_wsUrl = baseUrl.Replace("https://", "wss://").TrimEnd('/') + "/ws/realtime";
			await ConnectInternal();
		}
	}

	internal static void Disconnect()
	{
		_shouldReconnect = false;
		_isConnected = false;
		_isAuthenticated = false;
		_isConnecting = false;
		SetChannelEncrypted(value: false);
		_channelKey = null;
		try
		{
			_keepaliveCts?.Cancel();
		}
		catch
		{
		}
		_keepaliveCts = null;
		try
		{
			_cancellationTokenSource?.Cancel();
			ClientWebSocket webSocket = _webSocket;
			if (webSocket != null && webSocket.State == WebSocketState.Open)
			{
				_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None).Wait(1000);
			}
			_webSocket?.Dispose();
		}
		catch
		{
		}
		finally
		{
			_webSocket = null;
			_cancellationTokenSource = null;
		}
	}

	internal static void ForceReconnect()
	{
		long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (num - _lastForceReconnectMs < 30000)
		{
			return;
		}
		_lastForceReconnectMs = num;
		_isConnected = false;
		_isAuthenticated = false;
		_isConnecting = false;
		SetChannelEncrypted(value: false);
		_channelKey = null;
		_reconnectAttempts = 0;
		try
		{
			_keepaliveCts?.Cancel();
		}
		catch
		{
		}
		_keepaliveCts = null;
		try
		{
			_cancellationTokenSource?.Cancel();
			_webSocket?.Dispose();
		}
		catch
		{
		}
		finally
		{
			_webSocket = null;
			_cancellationTokenSource = null;
		}
		ScheduleReconnect();
	}

	internal static async void SendUpdate(object playerData, object uiState, object banMenuState, object banMenuUiState, object cheatsState = null, object cheatsUiState = null, object alivePlayers = null, object spoofingState = null, bool priority = false)
	{
		if (!IsConnected)
		{
			return;
		}
		try
		{
			string attestation_proof = ActionPermitSystem.ComputeAttestationProof();
			await SendMessage(new
			{
				type = "update",
				players = playerData,
				isHost = GetIsHost(),
				isInGame = GetIsInGame(),
				localPlayerId = GetLocalPlayerId(),
				uiState = uiState,
				banMenuState = banMenuState,
				banMenuUiState = banMenuUiState,
				cheatsState = cheatsState,
				cheatsUiState = cheatsUiState,
				alivePlayers = alivePlayers,
				spoofingState = spoofingState,
				isInMeeting = ((Object)(object)MeetingHud.Instance != (Object)null),
				currentRole = GetCurrentRole(),
				attestation_proof = attestation_proof,
				pri = (priority ? 1 : 0)
			});
		}
		catch (Exception)
		{
		}
	}

	internal static async void SendActionRequest(string requestId, string actionId, string permitToken, string clientChallenge)
	{
		if (!IsConnected)
		{
			return;
		}
		try
		{
			string attestation_proof = ActionPermitSystem.ComputeAttestationProof();
			await SendMessage(new
			{
				type = "action_request",
				requestId = requestId,
				actionId = actionId,
				permitToken = permitToken,
				clientChallenge = clientChallenge,
				attestation_proof = attestation_proof
			});
		}
		catch
		{
		}
	}

	private static string GetCurrentRole()
	{
		try
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			object obj;
			if (localPlayer == null)
			{
				obj = null;
			}
			else
			{
				NetworkedPlayerInfo data = localPlayer.Data;
				obj = ((data != null) ? data.Role : null);
			}
			RoleBehaviour val = (RoleBehaviour)obj;
			if ((Object)(object)val != (Object)null)
			{
				return ((object)val).GetType().Name;
			}
		}
		catch
		{
		}
		return "";
	}

	internal static string GetCurrentRoleForHeartbeat()
	{
		return GetCurrentRole();
	}

	private static async Task ConnectInternal()
	{
		if (_isConnecting || _isConnected)
		{
			return;
		}
		_isConnecting = true;
		try
		{
			_webSocket = new ClientWebSocket();
			_cancellationTokenSource = new CancellationTokenSource();
			_webSocket.Options.RemoteCertificateValidationCallback = delegate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
			{
				bool flag = CertificatePinner.ValidateServerCertificate(sender, cert, chain, errors);
				return (!flag && ModKeyValidator.IsRunningUnderWine && errors == SslPolicyErrors.RemoteCertificateChainErrors) || flag;
			};
			await _webSocket.ConnectAsync(new Uri(_wsUrl), _cancellationTokenSource.Token);
			_isConnected = true;
			string encryptedChannelKey = "";
			if (ModKeyValidator.IsRunningUnderWine)
			{
				_channelKey = null;
				SetChannelEncrypted(value: false);
			}
			else
			{
				_channelKey = new byte[32];
				SetChannelEncrypted(value: false);
				using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
				{
					randomNumberGenerator.GetBytes(_channelKey);
				}
				byte[] array = GhostUI.RsaEncrypt(_channelKey);
				encryptedChannelKey = ((array != null) ? Convert.ToBase64String(array) : "");
			}
			await SendMessageRaw(new
			{
				type = "auth",
				key = _cachedKey,
				token = _cachedToken,
				hwid = _cachedHwid,
				encryptedChannelKey = encryptedChannelKey
			});
			ReceiveLoop();
		}
		catch (Exception)
		{
			_isConnected = false;
			ScheduleReconnect();
		}
		finally
		{
			_isConnecting = false;
		}
	}

	private static async Task ReceiveLoop()
	{
		byte[] buffer = new byte[65536];
		MemoryStream messageStream = new MemoryStream(65536);
		try
		{
			while (true)
			{
				ClientWebSocket webSocket = _webSocket;
				if (webSocket == null || webSocket.State != WebSocketState.Open || _cancellationTokenSource.Token.IsCancellationRequested)
				{
					break;
				}
				WebSocketReceiveResult webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
				if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					break;
				}
				messageStream.Write(buffer, 0, webSocketReceiveResult.Count);
				if (!webSocketReceiveResult.EndOfMessage)
				{
					continue;
				}
				string text = Encoding.UTF8.GetString(messageStream.GetBuffer(), 0, (int)messageStream.Position);
				messageStream.Position = 0L;
				messageStream.SetLength(0L);
				string json;
				if (IsChannelEncrypted && _channelKey != null)
				{
					if (!text.StartsWith("E:"))
					{
						continue;
					}
					byte[] array = ChannelDecrypt(Convert.FromBase64String(text.Substring(2)));
					if (array == null)
					{
						continue;
					}
					json = Encoding.UTF8.GetString(array);
				}
				else
				{
					json = text;
				}
				ProcessMessage(json);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception)
		{
		}
		_isConnected = false;
		_isAuthenticated = false;
		SetChannelEncrypted(value: false);
		_channelKey = null;
		ScheduleReconnect();
	}

	private static void ProcessMessage(string json)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("type", out var value))
			{
				return;
			}
			switch (value.GetString())
			{
			case "auth_success":
				_isAuthenticated = true;
				_reconnectAttempts = 0;
				if (_channelKey != null)
				{
					SetChannelEncrypted(value: true);
				}
				try
				{
					if (rootElement.TryGetProperty("spki_pins", out var value3) && value3.ValueKind == JsonValueKind.Array)
					{
						List<string> list = new List<string>();
						foreach (JsonElement item in value3.EnumerateArray())
						{
							if (item.ValueKind == JsonValueKind.String)
							{
								list.Add(item.GetString());
							}
						}
						if (list.Count > 0)
						{
							CertificatePinner.UpdatePinsFromServer(list.ToArray());
						}
					}
				}
				catch
				{
				}
				try
				{
					PlayerPickMenu.ResetHashForReconnect();
				}
				catch
				{
				}
				TriggerInitialUpdate();
				StartKeepaliveLoop();
				break;
			case "auth_error":
				_reconnectAttempts = MAX_RECONNECT_ATTEMPTS;
				_isAuthenticated = false;
				try
				{
					_cancellationTokenSource?.Cancel();
					break;
				}
				catch
				{
					break;
				}
			case "bytecode_update":
				ProcessBytecodeUpdate(rootElement);
				break;
			case "action_approved":
			{
				JsonElement value4;
				string text = (rootElement.TryGetProperty("requestId", out value4) ? value4.GetString() : null);
				JsonElement value5;
				string serverNonce = (rootElement.TryGetProperty("serverNonce", out value5) ? value5.GetString() : null);
				if (!string.IsNullOrEmpty(text))
				{
					ActionPermitSystem.OnServerApproval(text, serverNonce);
				}
				break;
			}
			case "session_revoked":
			{
				ModKeyValidator.RequestResetFromRealtime((rootElement.TryGetProperty("reason", out var value2) && value2.ValueKind == JsonValueKind.String) ? value2.GetString() : "");
				Disconnect();
				break;
			}
			case "action_denied":
				break;
			}
		}
		catch
		{
		}
	}

	private static void ProcessBytecodeUpdate(JsonElement root)
	{
		try
		{
			JsonElement value;
			long num = (root.TryGetProperty("session_token", out value) ? value.GetInt64() : 0);
			if (root.TryGetProperty("action_map", out var value2) && value2.ValueKind == JsonValueKind.Object)
			{
				ServerData.UpdateActionIdMap(value2);
			}
			byte[] array = null;
			byte[] array2 = null;
			byte[] array3 = null;
			byte[] array4 = null;
			if (root.TryGetProperty("game_bytecode", out var value3) && value3.ValueKind == JsonValueKind.String)
			{
				string text = value3.GetString();
				if (!string.IsNullOrEmpty(text))
				{
					array = Convert.FromBase64String(text);
				}
			}
			if (root.TryGetProperty("lobby_bytecode", out var value4) && value4.ValueKind == JsonValueKind.String)
			{
				string text2 = value4.GetString();
				if (!string.IsNullOrEmpty(text2))
				{
					array2 = Convert.FromBase64String(text2);
				}
			}
			if (root.TryGetProperty("playerpick_bytecode", out var value5) && value5.ValueKind == JsonValueKind.String)
			{
				string text3 = value5.GetString();
				if (!string.IsNullOrEmpty(text3))
				{
					array3 = Convert.FromBase64String(text3);
				}
			}
			if (root.TryGetProperty("banmenu_bytecode", out var value6) && value6.ValueKind == JsonValueKind.String)
			{
				string text4 = value6.GetString();
				if (!string.IsNullOrEmpty(text4))
				{
					array4 = Convert.FromBase64String(text4);
				}
			}
			byte[] array5 = null;
			if (root.TryGetProperty("cheats_bytecode", out var value7) && value7.ValueKind == JsonValueKind.String)
			{
				string text5 = value7.GetString();
				if (!string.IsNullOrEmpty(text5))
				{
					array5 = Convert.FromBase64String(text5);
				}
			}
			byte[] array6 = null;
			if (root.TryGetProperty("spoofing_bytecode", out var value8) && value8.ValueKind == JsonValueKind.String)
			{
				string text6 = value8.GetString();
				if (!string.IsNullOrEmpty(text6))
				{
					array6 = Convert.FromBase64String(text6);
				}
			}
			if (array == null && array2 == null && array3 == null && array4 == null && array5 == null && array6 == null && num > 0)
			{
				long lastRealtimeUpdateMs = ServerData.LastRealtimeUpdateMs;
				long num2 = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if (lastRealtimeUpdateMs > 0 && num2 - lastRealtimeUpdateMs > 20000)
				{
					ActionPermitSystem.EnqueueMainThread(delegate
					{
						PlayerPickMenu.TriggerRealtimeUpdate(force: true);
					});
				}
			}
			else
			{
				ServerData.UpdateFromRealtime(array2, array, array3, array4, array5, array6, num);
			}
			if (root.TryGetProperty("attestation_seed", out var value9) && value9.ValueKind == JsonValueKind.String)
			{
				string text7 = value9.GetString();
				int[] array7 = null;
				if (root.TryGetProperty("attestation_methods", out var value10) && value10.ValueKind == JsonValueKind.Array)
				{
					List<int> list = new List<int>();
					foreach (JsonElement item in value10.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.Number)
						{
							list.Add(item.GetInt32());
						}
					}
					array7 = list.ToArray();
				}
				if (!string.IsNullOrEmpty(text7) && array7 != null && array7.Length != 0)
				{
					ActionPermitSystem.UpdateAttestation(text7, array7);
				}
			}
			if (root.TryGetProperty("session_proof", out var value11) && value11.ValueKind == JsonValueKind.String)
			{
				string text8 = value11.GetString();
				JsonElement value12;
				long num3 = ((root.TryGetProperty("proof_seed", out value12) && value12.ValueKind == JsonValueKind.Number) ? value12.GetInt64() : 0);
				JsonElement value13;
				long num4 = ((root.TryGetProperty("proof_expires", out value13) && value13.ValueKind == JsonValueKind.Number) ? value13.GetInt64() : 0);
				if (!string.IsNullOrEmpty(text8) && num3 > 0 && num4 > 0)
				{
					ModKeyValidator.UpdateProof(text8, num3, num4);
				}
			}
			if (root.TryGetProperty("render_key", out var value14) && value14.ValueKind == JsonValueKind.String)
			{
				string text9 = value14.GetString();
				JsonElement value15;
				long num5 = ((root.TryGetProperty("render_expires", out value15) && value15.ValueKind == JsonValueKind.Number) ? value15.GetInt64() : 0);
				JsonElement value16;
				long num6 = ((root.TryGetProperty("render_nonce", out value16) && value16.ValueKind == JsonValueKind.Number) ? value16.GetInt64() : 0);
				if (!string.IsNullOrEmpty(text9) && num5 > 0 && num6 > 0)
				{
					ServerGate.UpdateRenderPermission(text9, num5, num6);
				}
			}
		}
		catch
		{
		}
	}

	private static async Task SendMessage(object message)
	{
		ClientWebSocket webSocket = _webSocket;
		if (webSocket == null || webSocket.State != WebSocketState.Open)
		{
			return;
		}
		string s = JsonSerializer.Serialize(message);
		if (IsChannelEncrypted && _channelKey != null)
		{
			byte[] array = ChannelEncrypt(Encoding.UTF8.GetBytes(s));
			if (array != null)
			{
				string s2 = "E:" + Convert.ToBase64String(array);
				byte[] bytes = Encoding.UTF8.GetBytes(s2);
				await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, _cancellationTokenSource.Token);
			}
		}
		else
		{
			byte[] bytes2 = Encoding.UTF8.GetBytes(s);
			await _webSocket.SendAsync(new ArraySegment<byte>(bytes2), WebSocketMessageType.Text, endOfMessage: true, _cancellationTokenSource.Token);
		}
	}

	private static async Task SendMessageRaw(object message)
	{
		ClientWebSocket webSocket = _webSocket;
		if (webSocket != null && webSocket.State == WebSocketState.Open)
		{
			string s = JsonSerializer.Serialize(message);
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, _cancellationTokenSource.Token);
		}
	}

	private static byte[] ChannelEncrypt(byte[] plaintext)
	{
		try
		{
			byte[] array = new byte[12];
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(array);
			}
			byte[] array2 = new byte[plaintext.Length];
			byte[] array3 = new byte[16];
			using (AesGcm aesGcm = new AesGcm(_channelKey))
			{
				aesGcm.Encrypt(array, plaintext, array2, array3);
			}
			byte[] array4 = new byte[12 + array2.Length + 16];
			System.Array.Copy(array, 0, array4, 0, 12);
			System.Array.Copy(array2, 0, array4, 12, array2.Length);
			System.Array.Copy(array3, 0, array4, 12 + array2.Length, 16);
			return array4;
		}
		catch
		{
			return null;
		}
	}

	private static byte[] ChannelDecrypt(byte[] encrypted)
	{
		try
		{
			if (encrypted.Length < 28)
			{
				return null;
			}
			byte[] array = new byte[12];
			System.Array.Copy(encrypted, 0, array, 0, 12);
			int num = encrypted.Length - 12 - 16;
			byte[] array2 = new byte[num];
			System.Array.Copy(encrypted, 12, array2, 0, num);
			byte[] array3 = new byte[16];
			System.Array.Copy(encrypted, 12 + num, array3, 0, 16);
			byte[] array4 = new byte[num];
			using (AesGcm aesGcm = new AesGcm(_channelKey))
			{
				aesGcm.Decrypt(array, array2, array3, array4);
			}
			return array4;
		}
		catch
		{
			return null;
		}
	}

	private static void ScheduleReconnect()
	{
		if (!_shouldReconnect)
		{
			return;
		}
		long num = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastConnectAttemptMs;
		int val = (int)((num < 3000) ? (3000 - num) : 0);
		if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
		{
			Task.Delay(120000).ContinueWith(delegate(Task _)
			{
				if (_shouldReconnect && !_isConnected)
				{
					_reconnectAttempts = MAX_RECONNECT_ATTEMPTS - 3;
					_lastConnectAttemptMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
					_ = ConnectInternal();
				}
			});
			return;
		}
		_reconnectAttempts++;
		int num2 = Math.Max(BASE_RECONNECT_DELAY_MS * (int)Math.Pow(2.0, Math.Min(_reconnectAttempts - 1, 5)), val);
		Task.Delay(num2 + (int)((double)num2 * 0.25 * new System.Random().NextDouble())).ContinueWith(delegate(Task _)
		{
			if (_shouldReconnect && !_isConnected)
			{
				_lastConnectAttemptMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				_ = ConnectInternal();
			}
		});
	}

	internal static void ResetReconnectAttempts()
	{
		if (!_isConnected && _shouldReconnect && _reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
		{
			_reconnectAttempts = 0;
			ConnectInternal();
		}
	}

	private static void StartKeepaliveLoop()
	{
		try
		{
			_keepaliveCts?.Cancel();
		}
		catch
		{
		}
		_keepaliveCts = new CancellationTokenSource();
		CancellationToken token = _keepaliveCts.Token;
		Task.Run(async delegate
		{
			try
			{
				while (!token.IsCancellationRequested && _isConnected && _isAuthenticated)
				{
					await Task.Delay(45000, token);
					if (!_isConnected || !_isAuthenticated)
					{
						break;
					}
					TriggerInitialUpdate();
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch
			{
			}
		});
	}

	private static void TriggerInitialUpdate()
	{
		ActionPermitSystem.EnqueueMainThread(delegate
		{
			try
			{
				object[] playerData = PlayerPickMenu.CollectPlayerDataForServer();
				object uIState = PlayerPickMenu.GetUIState();
				object banMenuState = global::ModMenuCrew.UI.Menus.BanMenu.GetBanMenuState();
				object uIState2 = global::ModMenuCrew.UI.Menus.BanMenu.GetUIState();
				object cheatsState = CheatManager.GetCheatsState();
				object cheatsUiState = CheatManager.GetCheatsUiState();
				List<object> alivePlayersForServer = CheatManager.GetAlivePlayersForServer();
				object spoofingState = SpoofingMenu.GetSpoofingState();
				SendUpdate(playerData, uIState, banMenuState, uIState2, cheatsState, cheatsUiState, alivePlayersForServer, spoofingState);
			}
			catch
			{
			}
		});
	}

	private static bool GetIsHost()
	{
		try
		{
			AmongUsClient instance = AmongUsClient.Instance;
			return instance != null && ((InnerNetClient)instance).AmHost;
		}
		catch
		{
			return false;
		}
	}

	private static bool GetIsInGame()
	{
		try
		{
			return (Object)(object)ShipStatus.Instance != (Object)null;
		}
		catch
		{
			return false;
		}
	}

	private static int GetLocalPlayerId()
	{
		try
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			return (localPlayer != null) ? localPlayer.PlayerId : byte.MaxValue;
		}
		catch
		{
			return 255;
		}
	}
}



