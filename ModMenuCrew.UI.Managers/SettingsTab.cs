using System;
using AmongUs.GameOptions;
using BepInEx.Configuration;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ModMenuCrew.Features;
using ModMenuCrew.Patches;
using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew.UI.Managers;

public class SettingsTab
{
	private bool showVisual = true;

	private bool showInterfaceSize = true;

	private bool showStreamer;

	private bool showLobby = true;

	private bool showVision = true;

	private bool showRadar = true;

	private bool showGameplay = true;

	private bool showNetwork = true;

	private bool showRoles;

	private bool showKeybinds = true;

	private bool isBindingKey;

	private string activeKeybind;

	private Vector2 scrollPosition = Vector2.zero;

	private static GUIStyle _keybindActiveStyle;

	public void DrawSettingsTab()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (ServerData.IsTabEnabled("settings"))
		{
			scrollPosition = GuiStyles.BeginEliteScrollView(scrollPosition, GUILayout.ExpandHeight(true));
			GuiStyles.BeginEliteCard();
			DrawConfigButtons();
			GuiStyles.EndEliteCard();
			GUILayout.Space(8f);
			DrawNetworkSection();
			GUILayout.Space(8f);
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			DrawVisualSection();
			GUILayout.Space(8f);
			DrawInterfaceSizeSection();
			GUILayout.Space(8f);
			DrawLobbySection();
			GUILayout.Space(8f);
			DrawRadarSection();
			GUILayout.Space(8f);
			DrawKeybindsSection();
			GUILayout.EndVertical();
			GUILayout.Space(8f);
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
			DrawStreamerSection();
			GUILayout.Space(8f);
			DrawVisionSection();
			GUILayout.Space(8f);
			DrawGameplaySection();
			GUILayout.Space(8f);
			DrawRolesSection();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GuiStyles.EndEliteScrollView();
		}
	}

	private void DrawConfigButtons()
	{
		GUILayout.Label("âš™\ufe0f SETTINGS", GuiStyles.HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(8f);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (GuiStyles.DrawEliteButton("\ud83d\udcbe Save"))
		{
			ConfigFile config = LobbyHarmonyPatches.Config;
			if (config != null)
			{
				config.Save();
			}
			ShowNotification("Settings saved!");
		}
		GUILayout.Space(4f);
		if (GuiStyles.DrawEliteButton("\ud83d\udcc2 Reload"))
		{
			ConfigFile config2 = LobbyHarmonyPatches.Config;
			if (config2 != null)
			{
				config2.Reload();
			}
			ShowNotification("Settings reloaded!");
		}
		GUILayout.Space(4f);
		if (GuiStyles.DrawEliteButton("\ud83d\udd04 Reset"))
		{
			ResetAllToDefaults();
			ShowNotification("Settings reset to defaults!");
		}
		GUILayout.EndHorizontal();
	}

	private void DrawVisualSection()
	{
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Invalid comparison between Unknown and I4
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Invalid comparison between Unknown and I4
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		showVisual = DrawCollapsibleHeader("\ud83c\udfa8 VISUAL", showVisual);
		if (!showVisual)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (LobbyHarmonyPatches.cfgRgbLobbyCode != null)
		{
			bool flag = GuiStyles.DrawCrewToggle(LobbyHarmonyPatches.cfgRgbLobbyCode.Value, "\ud83c\udf08 RGB Lobby Code", "Rainbow effect on lobby code");
			if (flag != LobbyHarmonyPatches.cfgRgbLobbyCode.Value)
			{
				LobbyHarmonyPatches.cfgRgbLobbyCode.Value = flag;
			}
		}
		if (LobbyHarmonyPatches.cfgDisableLobbyMusic != null)
		{
			bool flag2 = GuiStyles.DrawCrewToggle(!LobbyHarmonyPatches.cfgDisableLobbyMusic.Value, "\ud83c\udfb5 Lobby Music", "Toggle lobby music");
			if (flag2 != !LobbyHarmonyPatches.cfgDisableLobbyMusic.Value)
			{
				LobbyHarmonyPatches.cfgDisableLobbyMusic.Value = !flag2;
			}
		}
		if (CheatConfig.MenuOpacity != null)
		{
			float num = GuiStyles.DrawCrewSlider(CheatConfig.MenuOpacity.Value, 0.3f, 1f, "\ud83d\udd32 Menu Opacity", "F2", "%", showMinMax: true);
			if (Mathf.Abs(num - CheatConfig.MenuOpacity.Value) > 0.01f)
			{
				CheatConfig.MenuOpacity.Value = num;
			}
		}
		if (CheatConfig.MenuToggleKey != null)
		{
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label("âŒ¨\ufe0f Menu Key", GuiStyles.LabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(100f) });
			if (GUILayout.Button(isBindingKey ? "Press any key..." : ((object)CheatConfig.MenuToggleKey.Value/*cast due to .constrained prefix*/).ToString(), GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) }))
			{
				isBindingKey = !isBindingKey;
			}
			GUILayout.EndHorizontal();
			if (isBindingKey)
			{
				Event current = Event.current;
				if (current.isKey && (int)current.type == 4 && (int)current.keyCode != 0)
				{
					if ((int)current.keyCode != 27)
					{
						CheatConfig.MenuToggleKey.Value = current.keyCode;
						CheatConfig.Save();
					}
					isBindingKey = false;
				}
			}
		}
		if (CheatConfig.HideMMCStar != null)
		{
			bool flag3 = GuiStyles.DrawCrewToggle(CheatConfig.HideMMCStar.Value, "â˜… Hide MMC Star", "Hide the star prefix on names");
			if (flag3 != CheatConfig.HideMMCStar.Value)
			{
				CheatConfig.HideMMCStar.Value = flag3;
			}
		}
		GUILayout.EndVertical();
	}

	private void DrawInterfaceSizeSection()
	{
		showInterfaceSize = DrawCollapsibleHeader("\ud83d\udcd0 INTERFACE SIZE", showInterfaceSize);
		if (!showInterfaceSize)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		GUILayout.Label("<color=#FFAA00>Use when resizer doesn't work in fullscreen</color>", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(4f);
		if (CheatConfig.MenuWidth != null)
		{
			float num = GuiStyles.DrawCrewSlider(CheatConfig.MenuWidth.Value, 400f, 1200f, "â†”\ufe0f Width", "F0", "px");
			if (Mathf.Abs(num - CheatConfig.MenuWidth.Value) > 1f)
			{
				CheatConfig.MenuWidth.Value = num;
				ApplyInterfaceSize();
			}
		}
		if (CheatConfig.MenuHeight != null)
		{
			float num2 = GuiStyles.DrawCrewSlider(CheatConfig.MenuHeight.Value, 400f, 1000f, "â†•\ufe0f Height", "F0", "px");
			if (Mathf.Abs(num2 - CheatConfig.MenuHeight.Value) > 1f)
			{
				CheatConfig.MenuHeight.Value = num2;
				ApplyInterfaceSize();
			}
		}
		GUILayout.Space(8f);
		GUILayout.Label("Quick Presets:", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(4f);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (GUILayout.Button("Small (400x450)", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			SetInterfaceSize(400f, 450f);
		}
		if (GUILayout.Button("Default (500x600)", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			SetInterfaceSize(500f, 600f);
		}
		if (GUILayout.Button("Medium (600x700)", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			SetInterfaceSize(600f, 700f);
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(4f);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (GUILayout.Button("Large (800x800)", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			SetInterfaceSize(800f, 800f);
		}
		if (GUILayout.Button("Wide (900x600)", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			SetInterfaceSize(900f, 600f);
		}
		if (GUILayout.Button("Tall (500x900)", GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(30f) }))
		{
			SetInterfaceSize(500f, 900f);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private void SetInterfaceSize(float width, float height)
	{
		if (CheatConfig.MenuWidth != null)
		{
			CheatConfig.MenuWidth.Value = width;
		}
		if (CheatConfig.MenuHeight != null)
		{
			CheatConfig.MenuHeight.Value = height;
		}
		ApplyInterfaceSize();
		ShowNotification($"Interface size: {width}x{height}");
	}

	private void ApplyInterfaceSize()
	{
		GhostUI.SetWindowSize(CheatConfig.MenuWidth?.Value ?? 500f, CheatConfig.MenuHeight?.Value ?? 600f);
	}

	private void DrawStreamerSection()
	{
		showStreamer = DrawCollapsibleHeader("\ud83d\udcfa STREAMER", showStreamer);
		if (!showStreamer)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (LobbyHarmonyPatches.cfgStreamerMode != null)
		{
			bool flag = GuiStyles.DrawCrewToggle(LobbyHarmonyPatches.cfgStreamerMode.Value, "\ud83c\udfad Streamer Mode", "Hide lobby code");
			if (flag != LobbyHarmonyPatches.cfgStreamerMode.Value)
			{
				LobbyHarmonyPatches.cfgStreamerMode.Value = flag;
			}
		}
		ConfigEntry<bool> cfgStreamerMode = LobbyHarmonyPatches.cfgStreamerMode;
		if (cfgStreamerMode != null && cfgStreamerMode.Value && LobbyHarmonyPatches.cfgCustomCode != null)
		{
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label("\ud83d\udcdd Code:", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) });
			string text = GUILayout.TextField(LobbyHarmonyPatches.cfgCustomCode.Value ?? "SECRET", GuiStyles.TextFieldStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(150f) });
			if (text != LobbyHarmonyPatches.cfgCustomCode.Value)
			{
				LobbyHarmonyPatches.cfgCustomCode.Value = text;
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
	}

	private void DrawLobbySection()
	{
		showLobby = DrawCollapsibleHeader("\ud83c\udfe0 LOBBY", showLobby);
		if (!showLobby)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (LobbyHarmonyPatches.cfgShowLobbyTimer != null)
		{
			bool flag = GuiStyles.DrawCrewToggle(LobbyHarmonyPatches.cfgShowLobbyTimer.Value, "â±\ufe0f Lobby Timer", "Show countdown timer");
			if (flag != LobbyHarmonyPatches.cfgShowLobbyTimer.Value)
			{
				LobbyHarmonyPatches.cfgShowLobbyTimer.Value = flag;
			}
		}
		if (LobbyHarmonyPatches.cfgAutoExtendTimer != null)
		{
			bool flag2 = GuiStyles.DrawCrewToggle(LobbyHarmonyPatches.cfgAutoExtendTimer.Value, "\ud83d\udd04 Auto-Extend", "Auto extend timer (Host only)");
			if (flag2 != LobbyHarmonyPatches.cfgAutoExtendTimer.Value)
			{
				LobbyHarmonyPatches.cfgAutoExtendTimer.Value = flag2;
			}
		}
		if (LobbyHarmonyPatches.cfgShowLobbyInfo != null)
		{
			bool flag3 = GuiStyles.DrawCrewToggle(LobbyHarmonyPatches.cfgShowLobbyInfo.Value, "\ud83d\udccb Extra Info", "Extra info panel in lobby list");
			if (flag3 != LobbyHarmonyPatches.cfgShowLobbyInfo.Value)
			{
				LobbyHarmonyPatches.cfgShowLobbyInfo.Value = flag3;
			}
		}
		if (CheatConfig.ShowPlayerInfo != null)
		{
			bool flag4 = GuiStyles.DrawCrewToggle(CheatConfig.ShowPlayerInfo.Value, "\ud83d\udda5\ufe0f Player Info", "Show platform, level & ID in player list");
			if (flag4 != CheatConfig.ShowPlayerInfo.Value)
			{
				CheatConfig.ShowPlayerInfo.Value = flag4;
			}
		}
		GUILayout.EndVertical();
	}

	private void DrawVisionSection()
	{
		showVision = DrawCollapsibleHeader("\ud83d\udc41\ufe0f VISION & MOVEMENT", showVision);
		if (!showVision)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (CheatConfig.SeeGhosts != null)
		{
			bool flag = GuiStyles.DrawCrewToggle(CheatConfig.SeeGhosts.Value, "\ud83d\udc7b See Ghosts", "See ghosts while alive");
			if (flag != CheatConfig.SeeGhosts.Value)
			{
				CheatConfig.SeeGhosts.Value = flag;
			}
		}
		if (CheatConfig.SeeDeadChat != null)
		{
			bool flag2 = GuiStyles.DrawCrewToggle(CheatConfig.SeeDeadChat.Value, "\ud83d\udcac Dead Chat", "Read dead player messages");
			if (flag2 != CheatConfig.SeeDeadChat.Value)
			{
				CheatConfig.SeeDeadChat.Value = flag2;
			}
		}
		if (CheatConfig.FreeCamEnabled != null)
		{
			bool flag3 = GuiStyles.DrawCrewToggle(CheatConfig.FreeCamEnabled.Value, "\ud83d\udcf7 FreeCam", "Free camera (WASD + Q/E)");
			if (flag3 != CheatConfig.FreeCamEnabled.Value)
			{
				CheatConfig.FreeCamEnabled.Value = flag3;
			}
		}
		if (CheatConfig.NoClipSmoothEnabled != null)
		{
			bool flag4 = GuiStyles.DrawCrewToggle(CheatConfig.NoClipSmoothEnabled.Value, "\ud83d\udeb6 NoClip", "Walk through walls");
			if (flag4 != CheatConfig.NoClipSmoothEnabled.Value)
			{
				CheatConfig.NoClipSmoothEnabled.Value = flag4;
			}
		}
		if (CheatConfig.VisionMultiplier > 0.1f)
		{
			float num = GuiStyles.DrawCrewSlider(CheatConfig.VisionMultiplier, 0.5f, 15f, "\ud83d\udc41\ufe0f Vision Range", "F1", "x", showMinMax: true);
			if (Mathf.Abs(num - CheatConfig.VisionMultiplier) > 0.01f)
			{
				CheatConfig.VisionMultiplier = num;
			}
		}
		GUILayout.EndVertical();
	}

	private void DrawRadarSection()
	{
		showRadar = DrawCollapsibleHeader("\ud83d\udce1 RADAR & ESP", showRadar);
		if (!showRadar)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (CheatConfig.RadarEnabled != null)
		{
			bool flag = GuiStyles.DrawCrewToggle(CheatConfig.RadarEnabled.Value, "\ud83d\udce1 Radar", "Show player positions");
			if (flag != CheatConfig.RadarEnabled.Value)
			{
				CheatConfig.RadarEnabled.Value = flag;
			}
		}
		if (CheatConfig.TracersEnabled != null)
		{
			bool flag2 = GuiStyles.DrawCrewToggle(CheatConfig.TracersEnabled.Value, "\ud83d\udccd Tracers", "Lines showing player positions");
			if (flag2 != CheatConfig.TracersEnabled.Value)
			{
				CheatConfig.TracersEnabled.Value = flag2;
			}
		}
		ConfigEntry<bool> radarEnabled = CheatConfig.RadarEnabled;
		if (radarEnabled != null && radarEnabled.Value && CheatConfig.RadarScale != null)
		{
			float num = GuiStyles.DrawCrewSlider(CheatConfig.RadarScale.Value, 0.03f, 0.15f, "\ud83d\udd0d Radar Zoom", "F2");
			if (Mathf.Abs(num - CheatConfig.RadarScale.Value) > 0.001f)
			{
				CheatConfig.RadarScale.Value = num;
			}
		}
		ConfigEntry<bool> radarEnabled2 = CheatConfig.RadarEnabled;
		if (radarEnabled2 != null && radarEnabled2.Value && CheatConfig.RadarShowMapImage != null)
		{
			bool flag3 = GuiStyles.DrawCrewToggle(CheatConfig.RadarShowMapImage.Value, "\ud83d\uddfa\ufe0f Map Background", "Show Skeld map image (scroll to zoom)");
			if (flag3 != CheatConfig.RadarShowMapImage.Value)
			{
				CheatConfig.RadarShowMapImage.Value = flag3;
			}
		}
		GUILayout.EndVertical();
	}

	private void DrawGameplaySection()
	{
		showGameplay = DrawCollapsibleHeader("\ud83c\udfae GAMEPLAY", showGameplay);
		if (!showGameplay)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (CheatConfig.AllowVenting != null)
		{
			bool flag = GuiStyles.DrawCrewToggle(CheatConfig.AllowVenting.Value, "\ud83d\udeaa Allow Venting", "All roles can use vents");
			if (flag != CheatConfig.AllowVenting.Value)
			{
				CheatConfig.AllowVenting.Value = flag;
			}
		}
		if (CheatConfig.TeleportWithCursor != null)
		{
			bool flag2 = GuiStyles.DrawCrewToggle(CheatConfig.TeleportWithCursor.Value, "\ud83c\udfaf Teleport Cursor", "Click to teleport");
			if (flag2 != CheatConfig.TeleportWithCursor.Value)
			{
				CheatConfig.TeleportWithCursor.Value = flag2;
			}
		}
		if (CheatConfig.NoKillCooldown != null)
		{
			bool flag3 = GuiStyles.DrawCrewToggle(CheatConfig.NoKillCooldown.Value, "\ud83d\udd2b No Kill Cooldown", "Kill without waiting");
			if (flag3 != CheatConfig.NoKillCooldown.Value)
			{
				CheatConfig.NoKillCooldown.Value = flag3;
			}
		}
		if (CheatConfig.ForceImpostor != null)
		{
			bool flag4 = GuiStyles.DrawCrewToggle(CheatConfig.ForceImpostor.Value, "\ud83d\udc7f Force Impostor", "Always be impostor (Host only)");
			if (flag4 != CheatConfig.ForceImpostor.Value)
			{
				CheatConfig.ForceImpostor.Value = flag4;
				ImpostorForcer.SetRoleOverrideEnabled(flag4);
				if (flag4)
				{
					ImpostorForcer.SetSelectedRoleForHost((RoleTypes)1);
				}
			}
		}
		if (CheatConfig.DisableGameEnd != null)
		{
			bool flag5 = GuiStyles.DrawCrewToggle(CheatConfig.DisableGameEnd.Value, "\ud83d\uded1 Block Game End", "Prevent game from ending");
			if (flag5 != CheatConfig.DisableGameEnd.Value)
			{
				CheatConfig.DisableGameEnd.Value = flag5;
			}
		}
		if (CheatConfig.CrewmateSabotage != null)
		{
			bool flag6 = GuiStyles.DrawCrewToggle(CheatConfig.CrewmateSabotage.Value, "\ud83d\uddfa\ufe0f Crew Sabotage", "Open sabotage map as crewmate");
			if (flag6 != CheatConfig.CrewmateSabotage.Value)
			{
				CheatConfig.CrewmateSabotage.Value = flag6;
			}
		}
		if (CheatConfig.MultiSabotage != null)
		{
			bool flag7 = GuiStyles.DrawCrewToggle(CheatConfig.MultiSabotage.Value, "\ud83d\udca3 Multi Sabotage", "Allow multiple sabotages at once");
			if (flag7 != CheatConfig.MultiSabotage.Value)
			{
				CheatConfig.MultiSabotage.Value = flag7;
			}
		}
		GUILayout.EndVertical();
	}

	private void DrawNetworkSection()
	{
		showNetwork = DrawCollapsibleHeader("\ud83d\udee1\ufe0f NETWORK PROTECTION", showNetwork);
		if (!showNetwork)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		if (CheatConfig.Nf4 != null)
		{
			string label = "\ud83c\udf44 Anti Spore Overload";
			string tooltip = "Block spore RPC spam (Fungle crash protection)";
			bool flag = GuiStyles.DrawCrewToggle(CheatConfig.Nf4.Value, label, tooltip);
			if (flag != CheatConfig.Nf4.Value)
			{
				CheatConfig.Nf4.Value = flag;
			}
		}
		else
		{
			GUILayout.Label("Network Filter setting is unavailable.", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		}
		GUILayout.EndVertical();
	}

	private void DrawRolesSection()
	{
		showRoles = DrawCollapsibleHeader("\ud83c\udfad ROLE CHEATS", showRoles);
		if (!showRoles)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		GUILayout.Label("\ud83d\udd27 Engineer", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (CheatConfig.EndlessVentTime != null)
		{
			bool flag = GUILayout.Toggle(CheatConfig.EndlessVentTime.Value, "âˆž Vent Time", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag != CheatConfig.EndlessVentTime.Value)
			{
				CheatConfig.EndlessVentTime.Value = flag;
			}
		}
		if (CheatConfig.NoVentCooldown != null)
		{
			bool flag2 = GUILayout.Toggle(CheatConfig.NoVentCooldown.Value, "âš¡ No CD", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag2 != CheatConfig.NoVentCooldown.Value)
			{
				CheatConfig.NoVentCooldown.Value = flag2;
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		GUILayout.Label("\ud83c\udfad Shapeshifter", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (CheatConfig.EndlessShapeshiftDuration != null)
		{
			bool flag3 = GUILayout.Toggle(CheatConfig.EndlessShapeshiftDuration.Value, "âˆž Duration", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag3 != CheatConfig.EndlessShapeshiftDuration.Value)
			{
				CheatConfig.EndlessShapeshiftDuration.Value = flag3;
			}
		}
		if (CheatConfig.NoShapeshiftCooldown != null)
		{
			bool flag4 = GUILayout.Toggle(CheatConfig.NoShapeshiftCooldown.Value, "âš¡ No CD", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag4 != CheatConfig.NoShapeshiftCooldown.Value)
			{
				CheatConfig.NoShapeshiftCooldown.Value = flag4;
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		GUILayout.Label("\ud83d\udd2c Scientist", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (CheatConfig.EndlessBattery != null)
		{
			bool flag5 = GUILayout.Toggle(CheatConfig.EndlessBattery.Value, "\ud83d\udd0b Battery", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag5 != CheatConfig.EndlessBattery.Value)
			{
				CheatConfig.EndlessBattery.Value = flag5;
			}
		}
		if (CheatConfig.NoVitalsCooldown != null)
		{
			bool flag6 = GUILayout.Toggle(CheatConfig.NoVitalsCooldown.Value, "âš¡ No CD", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag6 != CheatConfig.NoVitalsCooldown.Value)
			{
				CheatConfig.NoVitalsCooldown.Value = flag6;
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		GUILayout.Label("\ud83d\udccd Tracker", GuiStyles.SubHeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		if (CheatConfig.EndlessTracking != null)
		{
			bool flag7 = GUILayout.Toggle(CheatConfig.EndlessTracking.Value, "âˆž Tracking", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag7 != CheatConfig.EndlessTracking.Value)
			{
				CheatConfig.EndlessTracking.Value = flag7;
			}
		}
		if (CheatConfig.NoTrackingCooldown != null)
		{
			bool flag8 = GUILayout.Toggle(CheatConfig.NoTrackingCooldown.Value, "âš¡ No CD", GuiStyles.ToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			if (flag8 != CheatConfig.NoTrackingCooldown.Value)
			{
				CheatConfig.NoTrackingCooldown.Value = flag8;
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private void DrawKeybindsSection()
	{
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Invalid comparison between Unknown and I4
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		showKeybinds = DrawCollapsibleHeader("âŒ¨\ufe0f KEYBINDS", showKeybinds);
		if (!showKeybinds)
		{
			return;
		}
		GUILayout.BeginVertical(GuiStyles.SectionStyle, System.Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		GUILayout.Label("Click to bind, ESC to cancel, Backspace to clear", GuiStyles.LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Space(4f);
		Event current = Event.current;
		if (activeKeybind != null && current.isKey && (int)current.type == 4)
		{
			if ((int)current.keyCode == 27)
			{
				activeKeybind = null;
			}
			else if ((int)current.keyCode == 8)
			{
				SetKeybind(activeKeybind, (KeyCode)0);
				activeKeybind = null;
			}
			else if ((int)current.keyCode != 0)
			{
				SetKeybind(activeKeybind, current.keyCode);
				activeKeybind = null;
			}
			current.Use();
		}
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		ConfigEntry<KeyCode> keybindRadar = CheatConfig.KeybindRadar;
		DrawKeybindRow("\ud83d\udce1 Radar", "radar", (KeyCode)((keybindRadar != null) ? ((int)keybindRadar.Value) : 0));
		ConfigEntry<KeyCode> keybindFreeCam = CheatConfig.KeybindFreeCam;
		DrawKeybindRow("\ud83d\udcf7 FreeCam", "freecam", (KeyCode)((keybindFreeCam != null) ? ((int)keybindFreeCam.Value) : 0));
		ConfigEntry<KeyCode> keybindNoClip = CheatConfig.KeybindNoClip;
		DrawKeybindRow("\ud83d\udeb6 NoClip", "noclip", (KeyCode)((keybindNoClip != null) ? ((int)keybindNoClip.Value) : 0));
		GUILayout.EndVertical();
		GUILayout.Space(10f);
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		ConfigEntry<KeyCode> keybindTracers = CheatConfig.KeybindTracers;
		DrawKeybindRow("\ud83d\udccd Tracers", "tracers", (KeyCode)((keybindTracers != null) ? ((int)keybindTracers.Value) : 0));
		ConfigEntry<KeyCode> keybindSeeGhosts = CheatConfig.KeybindSeeGhosts;
		DrawKeybindRow("\ud83d\udc7b Ghosts", "ghosts", (KeyCode)((keybindSeeGhosts != null) ? ((int)keybindSeeGhosts.Value) : 0));
		ConfigEntry<KeyCode> keybindNoKillCooldown = CheatConfig.KeybindNoKillCooldown;
		DrawKeybindRow("\ud83d\udd2b No Kill CD", "nokillcd", (KeyCode)((keybindNoKillCooldown != null) ? ((int)keybindNoKillCooldown.Value) : 0));
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private unsafe void DrawKeybindRow(string label, string id, KeyCode currentKey)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Label(label, GuiStyles.LabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(85f) });
		bool num = activeKeybind == id;
		string text = (num ? "[...]" : (((int)currentKey == 0) ? "None" : ((object)(*(KeyCode*)(&currentKey))/*cast due to .constrained prefix*/).ToString()));
		if (num)
		{
			if (_keybindActiveStyle == null)
			{
				_keybindActiveStyle = new GUIStyle(GuiStyles.ButtonStyle);
				_keybindActiveStyle.normal.textColor = GuiStyles.Theme.Visor;
			}
			if (GUILayout.Button(text, _keybindActiveStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(70f),
				GUILayout.Height(24f)
			}))
			{
				activeKeybind = null;
			}
		}
		else if (GUILayout.Button(text, GuiStyles.ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(70f),
			GUILayout.Height(24f)
		}))
		{
			activeKeybind = id;
		}
		GUILayout.EndHorizontal();
	}

	private void SetKeybind(string id, KeyCode key)
	{
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		switch (id)
		{
		case "radar":
			if (CheatConfig.KeybindRadar != null)
			{
				CheatConfig.KeybindRadar.Value = key;
			}
			break;
		case "freecam":
			if (CheatConfig.KeybindFreeCam != null)
			{
				CheatConfig.KeybindFreeCam.Value = key;
			}
			break;
		case "noclip":
			if (CheatConfig.KeybindNoClip != null)
			{
				CheatConfig.KeybindNoClip.Value = key;
			}
			break;
		case "tracers":
			if (CheatConfig.KeybindTracers != null)
			{
				CheatConfig.KeybindTracers.Value = key;
			}
			break;
		case "ghosts":
			if (CheatConfig.KeybindSeeGhosts != null)
			{
				CheatConfig.KeybindSeeGhosts.Value = key;
			}
			break;
		case "nokillcd":
			if (CheatConfig.KeybindNoKillCooldown != null)
			{
				CheatConfig.KeybindNoKillCooldown.Value = key;
			}
			break;
		}
		CheatConfig.Save();
		ShowNotification($"Keybind set: {key}");
	}

	private void ResetAllToDefaults()
	{
		if (LobbyHarmonyPatches.Config == null)
		{
			return;
		}
		foreach (ConfigDefinition key in LobbyHarmonyPatches.Config.Keys)
		{
			LobbyHarmonyPatches.Config[key].BoxedValue = LobbyHarmonyPatches.Config[key].DefaultValue;
		}
		LobbyHarmonyPatches.Config.Save();
	}

	private void ShowNotification(string message)
	{
		NotifyUtils.Info(message);
	}

	private bool DrawCollapsibleHeader(string title, bool expanded)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		GuiStyles.DrawEliteSeparator();
		string text = (expanded ? "â–¼" : "â–¶");
		GUILayout.BeginHorizontal(GuiStyles.HeaderBackgroundStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(26f) });
		GUILayout.Label(text + " " + title, GuiStyles.HeaderStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		GUILayout.EndHorizontal();
		Rect lastRect = GUILayoutUtility.GetLastRect();
		if ((int)Event.current.type == 0 && lastRect.Contains(Event.current.mousePosition))
		{
			expanded = !expanded;
			Event.current.Use();
		}
		return expanded;
	}
}



