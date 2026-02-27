using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ModMenuCrew.UI.Styles;

public static class GuiStyles
{
	public static class Spacing
	{
		public const float XS = 2f;

		public const float SM = 4f;

		public const float MD = 8f;

		public const float LG = 12f;

		public const float XL = 16f;

		public const float XXL = 24f;

		public static float Scale => Mathf.Clamp(Mathf.Pow((float)Screen.height / 1080f, 0.7f), 0.85f, 2.2f);

		public static int Scaled(float value)
		{
			return Mathf.RoundToInt(value * Scale);
		}
	}

	public static class Layout
	{
		public const float LABEL_WIDTH = 120f;

		public const float SLIDER_HEIGHT = 24f;

		public const float BUTTON_HEIGHT = 32f;

		public const float TOGGLE_HEIGHT = 28f;

		public const float SECTION_PADDING = 12f;

		public const float SIDEBAR_WIDTH = 160f;

		public static float ScaledLabelWidth => 120f * Spacing.Scale;

		public static float ScaledButtonHeight => 32f * Spacing.Scale;

		public static float ScaledSidebarWidth => 160f * Spacing.Scale;
	}

	public static class Animation
	{
		private static float _lastHoverTime;

		private static string _lastHoveredId;

		public static float GetHoverScale(string id, Rect rect)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			if (rect.Contains(Event.current.mousePosition))
			{
				if (_lastHoveredId != id)
				{
					_lastHoveredId = id;
					_lastHoverTime = Time.realtimeSinceStartup;
				}
				float num = Time.realtimeSinceStartup - _lastHoverTime;
				return 1f + Mathf.Min(0.03f, num * 0.15f);
			}
			return 1f;
		}

		public static float Pulse(float speed = 2f, float min = 0.85f, float max = 1f)
		{
			return Mathf.Lerp(min, max, (Mathf.Sin(Time.realtimeSinceStartup * speed) + 1f) * 0.5f);
		}

		public static float PingPong(float speed = 1f)
		{
			return Mathf.PingPong(Time.realtimeSinceStartup * speed, 1f);
		}

		public static Color PulseColor(Color baseColor, float intensity = 0.15f, float speed = 2f)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			float num = Pulse(speed, 1f - intensity);
			return new Color(baseColor.r * num, baseColor.g * num, baseColor.b * num, baseColor.a);
		}
	}

	public static class Theme
	{
		public static readonly Color BgDarkA = new Color(0.02f, 0.02f, 0.02f, 0.96f);

		public static readonly Color BgDarkB = new Color(0.04f, 0.04f, 0.04f, 0.96f);

		public static readonly Color BgSection = new Color(0.06f, 0.06f, 0.06f, 0.92f);

		public static readonly Color BgCard = new Color(0.08f, 0.08f, 0.08f, 0.85f);

		public static readonly Color Primary = new Color(1f, 0.051f, 0.051f, 1f);

		public static readonly Color Secondary = new Color(0.08f, 0.08f, 0.08f, 1f);

		public static readonly Color Accent = new Color(1f, 0.051f, 0.051f, 1f);

		public static readonly Color AccentSoft = new Color(1f, 0.051f, 0.051f, 0.15f);

		public static readonly Color AccentDim = new Color(1f, 0.051f, 0.051f, 0.05f);

		public static readonly Color AccentHover = new Color(1f, 0.3f, 0.3f, 1f);

		public static readonly Color AccentActive = new Color(0.8f, 0f, 0f, 1f);

		public static readonly Color Visor = new Color(0f, 0.8f, 1f, 1f);

		public static readonly Color Gold = new Color(1f, 0.85f, 0f, 1f);

		public static readonly Color Success = new Color(0f, 0.9f, 0.4f, 1f);

		public static readonly Color Error = new Color(1f, 0.2f, 0.2f, 1f);

		public static readonly Color Warning = new Color(1f, 0.7f, 0f, 1f);

		public static readonly Color TextPrimary = new Color(1f, 1f, 1f, 1f);

		public static readonly Color TextMuted = new Color(0.9f, 0.9f, 0.9f, 1f);

		public static readonly Color TextInactive = new Color(0.65f, 0.65f, 0.7f, 1f);

		public static readonly Color TextDisabled = new Color(0.45f, 0.45f, 0.5f, 0.8f);

		public static readonly Color TextAccent = new Color(1f, 0.4f, 0.4f, 1f);

		public static readonly Color Glow = new Color(1f, 0.051f, 0.051f, 0.12f);

		public static readonly Color GlowStrong = new Color(1f, 0.1f, 0.1f, 0.25f);

		public static readonly Color GlowCyan = new Color(0f, 0.8f, 1f, 0.15f);

		public static readonly Color HeaderTop = BgDarkB;

		public static readonly Color HeaderBottom = BgDarkB;

		public static readonly Color FreeBorder = new Color(0.15f, 0.15f, 0.15f, 1f);
	}

	public static class VisualEffects
	{
		private struct Particle
		{
			public Vector2 position;

			public Vector2 velocity;

			public float size;

			public float alpha;

			public float life;

			public float maxLife;

			public Color color;
		}

		private static List<Particle> _particles = new List<Particle>();

		private static float _lastUpdate;

		private const int MAX_PARTICLES = 30;

		private static Texture2D _particleTex;

		public static void UpdateAndDraw(Rect windowRect)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_0168: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0209: Unknown result type (might be due to invalid IL or missing references)
			//IL_021e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0233: Unknown result type (might be due to invalid IL or missing references)
			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
			//IL_0254: Unknown result type (might be due to invalid IL or missing references)
			if ((int)Event.current.type != 7)
			{
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			float num = realtimeSinceStartup - _lastUpdate;
			_lastUpdate = realtimeSinceStartup;
			if (num > 0.1f)
			{
				num = 0.016f;
			}
			if (_particles.Count < 30 && UnityEngine.Random.value < 0.1f)
			{
				float num2 = UnityEngine.Random.Range(windowRect.x, windowRect.xMax);
				float yMax = windowRect.yMax;
				_particles.Add(new Particle
				{
					position = new Vector2(num2, yMax),
					velocity = new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-20f, -60f)),
					size = UnityEngine.Random.Range(2f, 5f),
					alpha = 0f,
					life = 0f,
					maxLife = UnityEngine.Random.Range(3f, 6f),
					color = ((UnityEngine.Random.value > 0.5f) ? Theme.Accent : Theme.Secondary)
				});
			}
			DrawParticle(Vector2.zero, 0f, Color.clear);
			for (int num3 = _particles.Count - 1; num3 >= 0; num3--)
			{
				Particle value = _particles[num3];
				value.life += num;
				ref Vector2 position = ref value.position;
				position += value.velocity * num;
				float num4 = 0.5f;
				if (value.life < num4)
				{
					value.alpha = value.life / num4;
				}
				else
				{
					value.alpha = 1f - (value.life - num4) / (value.maxLife - num4);
				}
				if (value.alpha > 0f)
				{
					Color color = value.color;
					color.a = value.alpha * 0.4f;
					if (value.position.x > windowRect.x && value.position.x < windowRect.xMax && value.position.y > windowRect.y && value.position.y < windowRect.yMax)
					{
						DrawParticle(value.position, value.size, color);
					}
				}
				_particles[num3] = value;
				if (value.life >= value.maxLife)
				{
					_particles.RemoveAt(num3);
				}
			}
		}

		private static void DrawParticle(Vector2 pos, float size, Color color)
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_particleTex == (Object)null)
			{
				_particleTex = MakeCircleTexture(16, Color.white, new Color(1f, 1f, 1f, 0f));
			}
			if (!(color.a <= 0f))
			{
				Color color2 = GUI.color;
				GUI.color = color;
				GUI.DrawTexture(new Rect(pos.x - size / 2f, pos.y - size / 2f, size, size), (Texture)(object)_particleTex);
				GUI.color = color2;
			}
		}
	}

	private static readonly Dictionary<float, GUILayoutOption> _widthCache = new Dictionary<float, GUILayoutOption>(32);

	private static readonly Dictionary<float, GUILayoutOption> _heightCache = new Dictionary<float, GUILayoutOption>(32);

	private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

	private static Texture2D _cachedPixelTexture;

	private static Texture2D _cachedPixelDarkTexture;

	private static Texture2D _cachedPixelAccentTexture;

	private static Texture2D _cachedPixelErrorTexture;

	private static Texture2D _cachedSuccessTexture;

	private static Texture2D _cachedErrorTexture;

	private static GUIStyle _cachedSuccessIndicatorStyle;

	private static GUIStyle _cachedErrorIndicatorStyle;

	private static GUIStyle _cachedAnimatedHeaderStyle;

	private static int _cyberNoiseSeed = 42;

	private static GUIStyle _outlineLabelStyle;

	private static Texture2D _cachedHoverGlowTexture;

	private static Texture2D _gradientTexture;

	private static GUIStyle _headerStyle;

	private static GUIStyle _subHeaderStyle;

	private static GUIStyle _buttonStyle;

	private static GUIStyle _toggleStyle;

	private static GUIStyle _sliderStyle;

	private static GUIStyle _labelStyle;

	private static GUIStyle _tabStyle;

	private static GUIStyle _selectedTabStyle;

	private static GUIStyle _containerStyle;

	private static GUIStyle _sectionStyle;

	private static GUIStyle _errorStyle;

	private static GUIStyle _iconStyle;

	private static GUIStyle _tooltipStyle;

	private static GUIStyle _statusIndicatorStyle;

	private static GUIStyle _glowStyle;

	private static GUIStyle _shadowStyle;

	private static GUIStyle _highlightStyle;

	private static GUIStyle _separatorStyle;

	private static GUIStyle _betterToggleStyle;

	private static GUIStyle _windowStyle;

	private static GUIStyle _headerBackgroundStyle;

	private static GUIStyle _titleLabelStyle;

	private static GUIStyle _titleBarButtonStyle;

	private static GUIStyle _textFieldStyle;

	private static GUIStyle _crewToggleStyle;

	private static GUIStyle _premiumBadgeStyle;

	private static GUIStyle _freeBadgeStyle;

	private static GUIStyle _timeRemainingStyle;

	private static GUIStyle _visorAccentStyle;

	private static GUIStyle _primaryButtonStyle;

	private static GUIStyle _hostButtonStyle;

	private static GUIStyle _sidebarStyle;

	private static GUIStyle _sidebarButtonStyle;

	private static GUIStyle _sidebarButtonActiveStyle;

	private static GUIStyle _sidebarHeaderStyle;

	private static GUIStyle _sidebarFooterStyle;

	private static GUIStyle _dashboardCardStyle;

	private static GUIStyle _dashboardStatStyle;

	private static GUIStyle _itemStyle;

	private static GUIStyle _selectedItemStyle;

	private static GUIStyle _listButtonStyle;

	private static GUIStyle _statusPillStyle;

	private static GUIStyle _sliderThumbStyle;

	private static Texture2D _sliderThumbTexture;

	private static Texture2D _sliderThumbHoverTexture;

	private static Texture2D _separatorTexture;

	private static GUIStyle _crewLogoStyleRed;

	private static GUIStyle _crewLogoStyleCyan;

	private static GUIStyle _sliderLabelStyle;

	private static GUIStyle _sliderValueStyle;

	private static GUIStyle _sliderMinMaxStyle;

	private static GUIStyle _eliteStatusActiveStyle;

	private static GUIStyle _eliteStatusInactiveStyle;

	public static GUIStyle SidebarStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			if (_sidebarStyle == null)
			{
				_sidebarStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(0, 0, 0, 0),
					margin = CreateRectOffset(0, 0, 0, 0)
				};
				_sidebarStyle.normal.background = MakeVerticalGradientTexture(256, 256, Theme.BgDarkB, Theme.BgDarkA);
			}
			else
			{
				GUIStyleState normal = _sidebarStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_sidebarStyle.normal.background = MakeVerticalGradientTexture(256, 256, Theme.BgDarkB, Theme.BgDarkA);
				}
			}
			return _sidebarStyle;
		}
	}

	public static GUIStyle SeparatorStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected O, but got Unknown
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			if (_separatorStyle == null)
			{
				_separatorStyle = new GUIStyle(GUI.skin.box)
				{
					fixedHeight = 2f,
					margin = CreateRectOffset(10, 10, 5, 5),
					stretchWidth = true
				};
				_separatorStyle.normal.background = MakeLaserTexture(128, 2, Theme.Primary);
			}
			return _separatorStyle;
		}
	}

	public static GUIStyle SidebarButtonStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected O, but got Unknown
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			if (_sidebarButtonStyle == null)
			{
				_sidebarButtonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 13,
					alignment = (TextAnchor)3,
					fixedHeight = 48f,
					padding = CreateRectOffset(0, 8, 8, 8),
					margin = CreateRectOffset(0, 0, 0, 0),
					border = CreateRectOffset(0, 0, 0, 0)
				};
				_sidebarButtonStyle.normal.background = MakeTexture(2, 2, Color.clear);
				_sidebarButtonStyle.normal.textColor = Theme.TextMuted;
				_sidebarButtonStyle.hover.background = MakeTexture(2, 2, new Color(Theme.Primary.r, Theme.Primary.g, Theme.Primary.b, 0.15f));
				_sidebarButtonStyle.hover.textColor = Theme.TextPrimary;
				_sidebarButtonStyle.active.background = MakeTexture(2, 2, new Color(Theme.Primary.r, Theme.Primary.g, Theme.Primary.b, 0.25f));
				_sidebarButtonStyle.active.textColor = Color.white;
				_sidebarButtonStyle.richText = true;
			}
			return _sidebarButtonStyle;
		}
	}

	public static GUIStyle SidebarButtonActiveStyle
	{
		get
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Expected O, but got Unknown
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			if (_sidebarButtonActiveStyle == null)
			{
				GUIStyle val = new GUIStyle(SidebarButtonStyle);
				val.normal.textColor = new Color(1f, 0.2f, 0.2f, 1f);
				_sidebarButtonActiveStyle = val;
				int num = 128;
				int num2 = 48;
				Texture2D val2 = new Texture2D(num, num2);
				Color val3 = default(Color);
				val3._002Ector(Theme.Primary.r, Theme.Primary.g, Theme.Primary.b, 0.15f);
				Color val4 = default(Color);
				val4._002Ector(1f, 0.1f, 0.1f, 1f);
				for (int i = 0; i < num2; i++)
				{
					for (int j = 0; j < num; j++)
					{
						if (j < 3)
						{
							val2.SetPixel(j, i, val4);
							continue;
						}
						float num3 = 1f - (float)j / ((float)num * 0.6f);
						num3 = Mathf.Clamp01(num3);
						Color val5 = val3;
						val5.a *= num3;
						val2.SetPixel(j, i, val5);
					}
				}
				val2.Apply();
				((Object)val2).hideFlags = (HideFlags)61;
				_sidebarButtonActiveStyle.normal.background = val2;
				_sidebarButtonActiveStyle.hover.background = val2;
				_sidebarButtonActiveStyle.active.background = val2;
			}
			return _sidebarButtonActiveStyle;
		}
	}

	public static GUIStyle StatusPillStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected O, but got Unknown
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			if (_statusPillStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					alignment = (TextAnchor)4,
					fontSize = 10,
					fontStyle = (FontStyle)1
				};
				val.normal.textColor = Theme.TextPrimary;
				val.padding = CreateRectOffset(8, 8, 2, 2);
				val.margin = CreateRectOffset(0, 0, 0, 0);
				val.fixedHeight = 20f;
				_statusPillStyle = val;
				_statusPillStyle.normal.background = MakeGlowFrameTexture(32, 20, new Color(0.1f, 0.1f, 0.1f, 0.8f), Theme.Success, 2);
			}
			return _statusPillStyle;
		}
	}

	public static GUIStyle SidebarHeaderStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Expected O, but got Unknown
			if (_sidebarHeaderStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 16,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(0, 0, 15, 5)
				};
				val.normal.textColor = Theme.TextPrimary;
				_sidebarHeaderStyle = val;
				_sidebarHeaderStyle.richText = true;
			}
			return _sidebarHeaderStyle;
		}
	}

	public static GUIStyle SidebarFooterStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Expected O, but got Unknown
			if (_sidebarFooterStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 10,
					alignment = (TextAnchor)7,
					padding = CreateRectOffset(10, 10, 4, 10)
				};
				val.normal.textColor = Theme.TextMuted;
				_sidebarFooterStyle = val;
			}
			return _sidebarFooterStyle;
		}
	}

	public static GUIStyle DashboardCardStyle
	{
		get
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Expected O, but got Unknown
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			if (_dashboardCardStyle == null)
			{
				_dashboardCardStyle = new GUIStyle(SectionStyle);
				_dashboardCardStyle.normal.background = MakeGlowFrameTexture(32, 32, Theme.BgSection, Theme.AccentDim, 1);
			}
			else
			{
				GUIStyleState normal = _dashboardCardStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_dashboardCardStyle.normal.background = MakeGlowFrameTexture(32, 32, Theme.BgSection, Theme.AccentDim, 1);
				}
			}
			return _dashboardCardStyle;
		}
	}

	public static GUIStyle HeaderStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Expected O, but got Unknown
			if (_headerStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 16,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)3
				};
				val.normal.textColor = Theme.Accent;
				val.padding = CreateRectOffset(8, 8, 6, 6);
				val.margin = CreateRectOffset(4, 4, 2, 4);
				_headerStyle = val;
				_headerStyle.richText = true;
			}
			return _headerStyle;
		}
	}

	public static GUIStyle SubHeaderStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Expected O, but got Unknown
			if (_subHeaderStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 13,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)3
				};
				val.normal.textColor = new Color(0.9f, 0.7f, 0.7f, 1f);
				val.padding = CreateRectOffset(6, 6, 4, 4);
				val.margin = CreateRectOffset(4, 4, 2, 2);
				_subHeaderStyle = val;
				_subHeaderStyle.richText = true;
			}
			return _subHeaderStyle;
		}
	}

	public static GUIStyle ButtonStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Expected O, but got Unknown
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_0244: Unknown result type (might be due to invalid IL or missing references)
			//IL_0245: Unknown result type (might be due to invalid IL or missing references)
			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
			//IL_028d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
			if (_buttonStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.button)
				{
					fontSize = 13,
					alignment = (TextAnchor)4
				};
				val.normal.textColor = Theme.TextPrimary;
				val.padding = CreateRectOffset(8, 8, 5, 5);
				val.margin = CreateRectOffset(2, 2, 2, 2);
				val.fixedHeight = 28f;
				_buttonStyle = val;
				Color val2 = default(Color);
				val2._002Ector(0.22f, 0.22f, 0.24f, 1f);
				Color border = default(Color);
				border._002Ector(0.5f, 0.2f, 0.2f, 1f);
				_buttonStyle.normal.background = MakeFrameTexture(16, 64, val2, val2, border, 1);
				_buttonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
				Color val3 = default(Color);
				val3._002Ector(0.28f, 0.18f, 0.18f, 1f);
				_buttonStyle.hover.background = MakeFrameTexture(16, 64, val3, val3, Theme.Accent, 2);
				_buttonStyle.hover.textColor = Color.white;
				_buttonStyle.active.background = MakeTexture(16, 64, new Color(0.5f, 0.1f, 0.1f, 1f));
				_buttonStyle.active.textColor = Color.white;
				_buttonStyle.focused.background = _buttonStyle.hover.background;
				_buttonStyle.richText = true;
			}
			else
			{
				GUIStyleState normal = _buttonStyle.normal;
				if (!((Object)(object)((normal != null) ? normal.background : null) == (Object)null))
				{
					GUIStyleState hover = _buttonStyle.hover;
					if (!((Object)(object)((hover != null) ? hover.background : null) == (Object)null))
					{
						GUIStyleState active = _buttonStyle.active;
						if (!((Object)(object)((active != null) ? active.background : null) == (Object)null))
						{
							goto IL_02f1;
						}
					}
				}
				Color val4 = default(Color);
				val4._002Ector(0.22f, 0.22f, 0.24f, 1f);
				Color border2 = default(Color);
				border2._002Ector(0.5f, 0.2f, 0.2f, 1f);
				_buttonStyle.normal.background = MakeFrameTexture(16, 64, val4, val4, border2, 1);
				_buttonStyle.hover.background = MakeFrameTexture(16, 64, new Color(0.28f, 0.18f, 0.18f, 1f), new Color(0.28f, 0.18f, 0.18f, 1f), Theme.Accent, 2);
				_buttonStyle.active.background = MakeTexture(16, 64, new Color(0.5f, 0.1f, 0.1f, 1f));
				_buttonStyle.focused.background = _buttonStyle.hover.background;
			}
			goto IL_02f1;
			IL_02f1:
			return _buttonStyle;
		}
	}

	public static GUIStyle ToggleStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Expected O, but got Unknown
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0207: Unknown result type (might be due to invalid IL or missing references)
			//IL_0220: Unknown result type (might be due to invalid IL or missing references)
			//IL_0275: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
			if (_toggleStyle == null)
			{
				_toggleStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 12,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(6, 6, 4, 4),
					margin = CreateRectOffset(2, 2, 2, 2),
					fixedHeight = 26f,
					stretchWidth = true
				};
				Color color = default(Color);
				color._002Ector(0.12f, 0.12f, 0.14f, 1f);
				Color textColor = default(Color);
				textColor._002Ector(0.65f, 0.65f, 0.7f, 1f);
				Color color2 = default(Color);
				color2._002Ector(0.1f, 0.55f, 0.18f, 1f);
				Color textColor2 = default(Color);
				textColor2._002Ector(0.5f, 1f, 0.6f, 1f);
				_toggleStyle.normal.background = MakeTexture(2, 2, color);
				_toggleStyle.normal.textColor = textColor;
				_toggleStyle.onNormal.background = MakeTexture(2, 2, color2);
				_toggleStyle.onNormal.textColor = textColor2;
				_toggleStyle.hover.background = MakeTexture(2, 2, new Color(0.18f, 0.18f, 0.2f, 1f));
				_toggleStyle.hover.textColor = Theme.TextPrimary;
				_toggleStyle.onHover.background = MakeTexture(2, 2, new Color(0.15f, 0.6f, 0.2f, 1f));
				_toggleStyle.onHover.textColor = Color.white;
				_toggleStyle.active.background = MakeTexture(2, 2, new Color(0.08f, 0.08f, 0.1f, 1f));
				_toggleStyle.active.textColor = Theme.TextPrimary;
				_toggleStyle.onActive.background = MakeTexture(2, 2, new Color(0.2f, 0.7f, 0.25f, 1f));
				_toggleStyle.onActive.textColor = Color.white;
				_toggleStyle.richText = true;
			}
			else
			{
				GUIStyleState normal = _toggleStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_toggleStyle.normal.background = MakeTexture(2, 2, new Color(0.12f, 0.12f, 0.14f, 1f));
					_toggleStyle.onNormal.background = MakeTexture(2, 2, new Color(0.1f, 0.5f, 0.15f, 1f));
				}
			}
			return _toggleStyle;
		}
	}

	public static GUIStyle SliderStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			if (_sliderStyle == null)
			{
				_sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
				{
					margin = CreateRectOffset(4, 4, 4, 4),
					padding = CreateRectOffset(0, 0, 0, 0),
					fixedHeight = 8f,
					stretchWidth = true
				};
				_sliderStyle.normal.background = MakeRoundedTexture(100, 8, Theme.BgDarkA, Theme.AccentDim, 1);
			}
			return _sliderStyle;
		}
	}

	public static GUIStyle SliderThumbStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected O, but got Unknown
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			if (_sliderThumbStyle == null)
			{
				_sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
				{
					fixedWidth = 18f,
					fixedHeight = 18f,
					margin = CreateRectOffset(0, 0, 0, 0),
					padding = CreateRectOffset(0, 0, 0, 0)
				};
				if ((Object)(object)_sliderThumbTexture == (Object)null)
				{
					_sliderThumbTexture = MakeCircleTexture(18, Theme.Accent, Theme.Primary);
				}
				if ((Object)(object)_sliderThumbHoverTexture == (Object)null)
				{
					_sliderThumbHoverTexture = MakeCircleTexture(18, Theme.AccentHover, Theme.Gold);
				}
				_sliderThumbStyle.normal.background = _sliderThumbTexture;
				_sliderThumbStyle.hover.background = _sliderThumbHoverTexture;
				_sliderThumbStyle.active.background = _sliderThumbHoverTexture;
				_sliderThumbStyle.focused.background = _sliderThumbHoverTexture;
			}
			return _sliderThumbStyle;
		}
	}

	public static GUIStyle LabelStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Expected O, but got Unknown
			if (_labelStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 14
				};
				val.normal.textColor = Theme.TextPrimary;
				val.padding = CreateRectOffset(10, 10, 6, 6);
				val.margin = CreateRectOffset(6, 6, 3, 5);
				_labelStyle = val;
				_labelStyle.richText = true;
			}
			return _labelStyle;
		}
	}

	public static GUIStyle TabStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Expected O, but got Unknown
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0230: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_024e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0280: Unknown result type (might be due to invalid IL or missing references)
			//IL_0299: Unknown result type (might be due to invalid IL or missing references)
			//IL_029e: Unknown result type (might be due to invalid IL or missing references)
			if (_tabStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.button)
				{
					fontSize = 13,
					padding = CreateRectOffset(10, 10, 5, 5),
					margin = CreateRectOffset(3, 3, 2, 2),
					fixedHeight = 28f
				};
				val.normal.textColor = Theme.TextMuted;
				_tabStyle = val;
				_tabStyle.normal.background = MakeFrameTexture(16, 48, new Color(0.09f, 0.09f, 0.11f, 0.95f), new Color(0.07f, 0.07f, 0.09f, 0.95f), Theme.AccentDim, 1);
				_tabStyle.hover.background = MakeFrameTexture(16, 48, new Color(0.11f, 0.11f, 0.14f, 0.95f), new Color(0.09f, 0.09f, 0.12f, 0.95f), Theme.AccentHover, 1);
				_tabStyle.active.background = MakeFrameTexture(16, 48, new Color(0.12f, 0.03f, 0.06f, 0.95f), new Color(0.1f, 0.02f, 0.05f, 0.95f), Theme.AccentActive, 1);
				_tabStyle.richText = true;
			}
			else
			{
				GUIStyleState normal = _tabStyle.normal;
				if (!((Object)(object)((normal != null) ? normal.background : null) == (Object)null))
				{
					GUIStyleState hover = _tabStyle.hover;
					if (!((Object)(object)((hover != null) ? hover.background : null) == (Object)null))
					{
						GUIStyleState active = _tabStyle.active;
						if (!((Object)(object)((active != null) ? active.background : null) == (Object)null))
						{
							goto IL_02ae;
						}
					}
				}
				_tabStyle.normal.background = MakeFrameTexture(16, 48, new Color(0.09f, 0.09f, 0.11f, 0.95f), new Color(0.07f, 0.07f, 0.09f, 0.95f), Theme.AccentDim, 1);
				_tabStyle.hover.background = MakeFrameTexture(16, 48, new Color(0.11f, 0.11f, 0.14f, 0.95f), new Color(0.09f, 0.09f, 0.12f, 0.95f), Theme.AccentHover, 1);
				_tabStyle.active.background = MakeFrameTexture(16, 48, new Color(0.12f, 0.03f, 0.06f, 0.95f), new Color(0.1f, 0.02f, 0.05f, 0.95f), Theme.AccentActive, 1);
			}
			goto IL_02ae;
			IL_02ae:
			return _tabStyle;
		}
	}

	public static GUIStyle SelectedTabStyle
	{
		get
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			if (_selectedTabStyle == null)
			{
				GUIStyle val = new GUIStyle(TabStyle);
				val.normal.textColor = Theme.Accent;
				_selectedTabStyle = val;
				_selectedTabStyle.normal.background = MakeFrameTexture(16, 48, new Color(0.13f, 0.04f, 0.08f, 0.95f), new Color(0.11f, 0.03f, 0.07f, 0.95f), Theme.Accent, 1);
			}
			return _selectedTabStyle;
		}
	}

	public static GUIStyle ErrorStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Expected O, but got Unknown
			if (_errorStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 14
				};
				val.normal.textColor = Theme.Error;
				val.padding = CreateRectOffset(12, 12, 10, 10);
				val.wordWrap = true;
				_errorStyle = val;
				_errorStyle.richText = true;
			}
			return _errorStyle;
		}
	}

	public static GUIStyle ContainerStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			if (_containerStyle == null)
			{
				_containerStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(8, 8, 8, 8),
					margin = CreateRectOffset(4, 4, 4, 4)
				};
				_containerStyle.normal.background = MakeTexture(2, 2, new Color(0.07f, 0.07f, 0.09f, 0.8f));
			}
			else
			{
				GUIStyleState normal = _containerStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_containerStyle.normal.background = MakeTexture(2, 2, new Color(0.07f, 0.07f, 0.09f, 0.8f));
				}
			}
			return _containerStyle;
		}
	}

	public static GUIStyle SectionStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Expected O, but got Unknown
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			if (_sectionStyle == null)
			{
				_sectionStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(12, 12, 12, 12),
					margin = CreateRectOffset(8, 8, 8, 8)
				};
				_sectionStyle.normal.background = MakeGlowFrameTexture(32, 32, Theme.BgDarkB, Theme.AccentDim, 1);
			}
			else
			{
				GUIStyleState normal = _sectionStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_sectionStyle.normal.background = MakeGlowFrameTexture(32, 32, Theme.BgDarkB, Theme.AccentDim, 1);
				}
			}
			return _sectionStyle;
		}
	}

	public static GUIStyle IconStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected O, but got Unknown
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			if (_iconStyle == null)
			{
				_iconStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(3, 3, 3, 3),
					margin = CreateRectOffset(3, 3, 3, 3),
					fixedWidth = 28f,
					fixedHeight = 28f
				};
				_iconStyle.normal.background = MakeFrameTexture(8, 8, new Color(0.11f, 0.11f, 0.13f, 0.95f), new Color(0.09f, 0.09f, 0.11f, 0.95f), Theme.AccentDim, 1);
			}
			else
			{
				GUIStyleState normal = _iconStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_iconStyle.normal.background = MakeFrameTexture(8, 8, new Color(0.11f, 0.11f, 0.13f, 0.95f), new Color(0.09f, 0.09f, 0.11f, 0.95f), Theme.AccentDim, 1);
				}
			}
			return _iconStyle;
		}
	}

	public static GUIStyle TooltipStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Expected O, but got Unknown
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			if (_tooltipStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.box)
				{
					fontSize = 12
				};
				val.normal.textColor = Theme.TextPrimary;
				val.normal.background = MakeFrameTexture(16, 48, new Color(0.07f, 0.07f, 0.09f, 0.98f), new Color(0.05f, 0.05f, 0.07f, 0.98f), Theme.Accent, 1);
				val.padding = CreateRectOffset(8, 8, 8, 8);
				val.margin = CreateRectOffset(6, 6, 6, 6);
				val.wordWrap = true;
				_tooltipStyle = val;
				_tooltipStyle.richText = true;
			}
			else
			{
				GUIStyleState normal = _tooltipStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_tooltipStyle.normal.background = MakeFrameTexture(16, 48, new Color(0.07f, 0.07f, 0.09f, 0.98f), new Color(0.05f, 0.05f, 0.07f, 0.98f), Theme.Accent, 1);
				}
			}
			return _tooltipStyle;
		}
	}

	public static GUIStyle StatusIndicatorStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected O, but got Unknown
			if (_statusIndicatorStyle == null)
			{
				_statusIndicatorStyle = new GUIStyle(GUI.skin.box)
				{
					fixedWidth = 12f,
					fixedHeight = 12f,
					margin = CreateRectOffset(6, 6, 4, 4)
				};
			}
			return _statusIndicatorStyle;
		}
	}

	public static GUIStyle GlowStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			if (_glowStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.box);
				val.normal.background = MakeTexture(2, 2, new Color(1f, 0f, 0.2f, 0.15f));
				val.margin = CreateRectOffset(3, 3, 3, 3);
				_glowStyle = val;
			}
			return _glowStyle;
		}
	}

	public static GUIStyle ShadowStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			if (_shadowStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.box);
				val.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.6f));
				val.margin = CreateRectOffset(3, 3, 3, 3);
				_shadowStyle = val;
			}
			return _shadowStyle;
		}
	}

	public static GUIStyle HighlightStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			if (_highlightStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.box);
				val.normal.background = MakeTexture(2, 2, new Color(1f, 1f, 1f, 0.12f));
				val.margin = CreateRectOffset(3, 3, 3, 3);
				_highlightStyle = val;
			}
			return _highlightStyle;
		}
	}

	public static GUIStyle BetterToggleStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Expected O, but got Unknown
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_015e: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_0204: Unknown result type (might be due to invalid IL or missing references)
			//IL_0230: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_0262: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0308: Unknown result type (might be due to invalid IL or missing references)
			//IL_0334: Unknown result type (might be due to invalid IL or missing references)
			//IL_034d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0366: Unknown result type (might be due to invalid IL or missing references)
			//IL_0394: Unknown result type (might be due to invalid IL or missing references)
			if (_betterToggleStyle == null)
			{
				_betterToggleStyle = new GUIStyle(GUI.skin.toggle)
				{
					fontSize = 15,
					fontStyle = (FontStyle)0,
					alignment = (TextAnchor)3,
					padding = CreateRectOffset(32, 18, 11, 11),
					margin = CreateRectOffset(10, 10, 6, 6),
					fixedHeight = 38f,
					stretchWidth = true
				};
				Color innerTop = default(Color);
				innerTop._002Ector(0.12f, 0.12f, 0.15f, 0.95f);
				Color innerBottom = default(Color);
				innerBottom._002Ector(0.08f, 0.08f, 0.1f, 0.95f);
				Color border = default(Color);
				border._002Ector(0.35f, 0.35f, 0.4f, 1f);
				Color innerTop2 = default(Color);
				innerTop2._002Ector(0.08f, 0.4f, 0.15f, 0.95f);
				Color innerBottom2 = default(Color);
				innerBottom2._002Ector(0.04f, 0.28f, 0.1f, 0.95f);
				Color border2 = default(Color);
				border2._002Ector(0.25f, 0.95f, 0.4f, 1f);
				_betterToggleStyle.normal.background = MakeFrameTexture(16, 64, innerTop, innerBottom, border, 2);
				_betterToggleStyle.normal.textColor = new Color(0.55f, 0.55f, 0.6f, 1f);
				_betterToggleStyle.onNormal.background = MakeFrameTexture(16, 64, innerTop2, innerBottom2, border2, 2);
				_betterToggleStyle.onNormal.textColor = new Color(0.9f, 1f, 0.9f, 1f);
				_betterToggleStyle.hover.background = MakeFrameTexture(16, 64, new Color(0.15f, 0.15f, 0.18f, 0.95f), new Color(0.11f, 0.11f, 0.14f, 0.95f), new Color(0.5f, 0.5f, 0.55f, 1f), 2);
				_betterToggleStyle.hover.textColor = Theme.TextPrimary;
				_betterToggleStyle.onHover.background = MakeFrameTexture(16, 64, new Color(0.1f, 0.5f, 0.18f, 0.95f), new Color(0.06f, 0.38f, 0.12f, 0.95f), new Color(0.35f, 1f, 0.5f, 1f), 2);
				_betterToggleStyle.onHover.textColor = new Color(0.95f, 1f, 0.95f, 1f);
				_betterToggleStyle.active.background = MakeFrameTexture(16, 64, new Color(0.1f, 0.1f, 0.12f, 0.95f), new Color(0.07f, 0.07f, 0.09f, 0.95f), new Color(0.4f, 0.4f, 0.45f, 1f), 2);
				_betterToggleStyle.active.textColor = Theme.TextMuted;
				_betterToggleStyle.onActive.background = MakeFrameTexture(16, 64, new Color(0.06f, 0.35f, 0.12f, 0.95f), new Color(0.03f, 0.22f, 0.08f, 0.95f), new Color(0.2f, 0.8f, 0.35f, 1f), 2);
				_betterToggleStyle.onActive.textColor = new Color(0.85f, 1f, 0.85f, 1f);
			}
			return _betterToggleStyle;
		}
	}

	public static GUIStyle WindowStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Expected O, but got Unknown
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			if (_windowStyle == null)
			{
				_windowStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(16, 16, 16, 16),
					margin = CreateRectOffset(0, 0, 0, 0)
				};
				_windowStyle.normal.background = MakeGlowFrameTexture(64, 64, Theme.BgDarkA, Theme.Glow, 1);
			}
			else
			{
				GUIStyleState normal = _windowStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					_windowStyle.normal.background = MakeGlowFrameTexture(64, 64, Theme.BgDarkA, Theme.Glow, 1);
				}
			}
			return _windowStyle;
		}
	}

	public static GUIStyle HeaderBackgroundStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			if (_headerBackgroundStyle == null)
			{
				_headerBackgroundStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(0, 0, 0, 0),
					margin = CreateRectOffset(0, 0, 0, 0)
				};
				Color headerTop = Theme.HeaderTop;
				Color headerBottom = Theme.HeaderBottom;
				_headerBackgroundStyle.normal.background = MakeVerticalGradientTexture(2, 32, headerTop, headerBottom);
			}
			else
			{
				GUIStyleState normal = _headerBackgroundStyle.normal;
				if ((Object)(object)((normal != null) ? normal.background : null) == (Object)null)
				{
					Color headerTop2 = Theme.HeaderTop;
					Color headerBottom2 = Theme.HeaderBottom;
					_headerBackgroundStyle.normal.background = MakeVerticalGradientTexture(2, 32, headerTop2, headerBottom2);
				}
			}
			return _headerBackgroundStyle;
		}
	}

	public static GUIStyle TitleLabelStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			if (_titleLabelStyle == null)
			{
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 15,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4
				};
				val.normal.textColor = Theme.TextPrimary;
				_titleLabelStyle = val;
			}
			return _titleLabelStyle;
		}
	}

	public static GUIStyle TitleBarButtonStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Expected O, but got Unknown
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0203: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_024d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0252: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			if (_titleBarButtonStyle == null)
			{
				_titleBarButtonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 13,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(2, 2, 2, 2),
					margin = CreateRectOffset(4, 4, 4, 4),
					fixedWidth = 24f,
					fixedHeight = 20f
				};
				_titleBarButtonStyle.normal.textColor = Theme.TextPrimary;
				_titleBarButtonStyle.normal.background = MakeFrameTexture(8, 32, new Color(0.13f, 0.13f, 0.15f, 0.95f), new Color(0.11f, 0.11f, 0.13f, 0.95f), Theme.AccentDim, 1);
				_titleBarButtonStyle.hover.background = MakeFrameTexture(8, 32, new Color(0.15f, 0.05f, 0.1f, 0.95f), new Color(0.13f, 0.04f, 0.08f, 0.95f), Theme.AccentHover, 1);
				_titleBarButtonStyle.active.background = MakeFrameTexture(8, 32, new Color(0.17f, 0.06f, 0.12f, 0.95f), new Color(0.15f, 0.05f, 0.1f, 0.95f), Theme.AccentActive, 1);
			}
			else
			{
				GUIStyleState normal = _titleBarButtonStyle.normal;
				if (!((Object)(object)((normal != null) ? normal.background : null) == (Object)null))
				{
					GUIStyleState hover = _titleBarButtonStyle.hover;
					if (!((Object)(object)((hover != null) ? hover.background : null) == (Object)null))
					{
						GUIStyleState active = _titleBarButtonStyle.active;
						if (!((Object)(object)((active != null) ? active.background : null) == (Object)null))
						{
							goto IL_02b1;
						}
					}
				}
				_titleBarButtonStyle.normal.background = MakeFrameTexture(8, 32, new Color(0.13f, 0.13f, 0.15f, 0.95f), new Color(0.11f, 0.11f, 0.13f, 0.95f), Theme.AccentDim, 1);
				_titleBarButtonStyle.hover.background = MakeFrameTexture(8, 32, new Color(0.15f, 0.05f, 0.1f, 0.95f), new Color(0.13f, 0.04f, 0.08f, 0.95f), Theme.AccentHover, 1);
				_titleBarButtonStyle.active.background = MakeFrameTexture(8, 32, new Color(0.17f, 0.06f, 0.12f, 0.95f), new Color(0.15f, 0.05f, 0.1f, 0.95f), Theme.AccentActive, 1);
			}
			goto IL_02b1;
			IL_02b1:
			return _titleBarButtonStyle;
		}
	}

	public static GUIStyle TextFieldStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Expected O, but got Unknown
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0230: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_024e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0280: Unknown result type (might be due to invalid IL or missing references)
			//IL_0299: Unknown result type (might be due to invalid IL or missing references)
			//IL_029e: Unknown result type (might be due to invalid IL or missing references)
			if (_textFieldStyle == null)
			{
				_textFieldStyle = new GUIStyle(GUI.skin.textField)
				{
					fontSize = 14,
					alignment = (TextAnchor)3,
					padding = CreateRectOffset(10, 10, 8, 8),
					margin = CreateRectOffset(6, 6, 6, 8)
				};
				_textFieldStyle.normal.textColor = Theme.TextPrimary;
				_textFieldStyle.normal.background = MakeFrameTexture(16, 48, new Color(0.08f, 0.08f, 0.1f, 0.95f), new Color(0.06f, 0.06f, 0.08f, 0.95f), Theme.AccentDim, 1);
				_textFieldStyle.hover.background = MakeFrameTexture(16, 48, new Color(0.1f, 0.1f, 0.12f, 0.95f), new Color(0.08f, 0.08f, 0.1f, 0.95f), Theme.AccentHover, 1);
				_textFieldStyle.focused.background = MakeFrameTexture(16, 48, new Color(0.11f, 0.02f, 0.05f, 0.95f), new Color(0.09f, 0.01f, 0.04f, 0.95f), Theme.Accent, 1);
				_textFieldStyle.richText = true;
			}
			else
			{
				GUIStyleState normal = _textFieldStyle.normal;
				if (!((Object)(object)((normal != null) ? normal.background : null) == (Object)null))
				{
					GUIStyleState hover = _textFieldStyle.hover;
					if (!((Object)(object)((hover != null) ? hover.background : null) == (Object)null))
					{
						GUIStyleState focused = _textFieldStyle.focused;
						if (!((Object)(object)((focused != null) ? focused.background : null) == (Object)null))
						{
							goto IL_02ae;
						}
					}
				}
				_textFieldStyle.normal.background = MakeFrameTexture(16, 48, new Color(0.08f, 0.08f, 0.1f, 0.95f), new Color(0.06f, 0.06f, 0.08f, 0.95f), Theme.AccentDim, 1);
				_textFieldStyle.hover.background = MakeFrameTexture(16, 48, new Color(0.1f, 0.1f, 0.12f, 0.95f), new Color(0.08f, 0.08f, 0.1f, 0.95f), Theme.AccentHover, 1);
				_textFieldStyle.focused.background = MakeFrameTexture(16, 48, new Color(0.11f, 0.02f, 0.05f, 0.95f), new Color(0.09f, 0.01f, 0.04f, 0.95f), Theme.Accent, 1);
			}
			goto IL_02ae;
			IL_02ae:
			return _textFieldStyle;
		}
	}

	public static GUIStyle CrewToggleStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Expected O, but got Unknown
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_018a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0208: Unknown result type (might be due to invalid IL or missing references)
			if (_crewToggleStyle == null)
			{
				_crewToggleStyle = new GUIStyle(GUI.skin.toggle)
				{
					fontSize = 14,
					fontStyle = (FontStyle)0,
					alignment = (TextAnchor)3,
					padding = CreateRectOffset(30, 14, 8, 8),
					margin = CreateRectOffset(8, 8, 4, 4),
					fixedHeight = 34f,
					stretchWidth = true
				};
				Color val = default(Color);
				val._002Ector(0.08f, 0.08f, 0.12f, 0.95f);
				Color border = default(Color);
				border._002Ector(0.3f, 0.3f, 0.38f, 1f);
				_crewToggleStyle.normal.background = MakeFrameTexture(16, 34, val, val, border, 2);
				_crewToggleStyle.normal.textColor = Theme.TextMuted;
				Color innerTop = default(Color);
				innerTop._002Ector(0.15f, 0.03f, 0.06f, 0.95f);
				Color innerBottom = default(Color);
				innerBottom._002Ector(0.1f, 0.02f, 0.04f, 0.95f);
				_crewToggleStyle.onNormal.background = MakeFrameTexture(16, 34, innerTop, innerBottom, Theme.Visor, 2);
				_crewToggleStyle.onNormal.textColor = Theme.TextPrimary;
				_crewToggleStyle.hover.background = MakeFrameTexture(16, 34, new Color(0.12f, 0.12f, 0.16f, 0.95f), new Color(0.1f, 0.1f, 0.14f, 0.95f), new Color(0.5f, 0.5f, 0.6f, 1f), 2);
				_crewToggleStyle.hover.textColor = Theme.TextPrimary;
				_crewToggleStyle.onHover.background = MakeFrameTexture(16, 34, new Color(0.18f, 0.04f, 0.08f, 0.95f), new Color(0.13f, 0.03f, 0.06f, 0.95f), Theme.Visor, 2);
				_crewToggleStyle.onHover.textColor = Theme.TextPrimary;
				_crewToggleStyle.richText = true;
			}
			return _crewToggleStyle;
		}
	}

	public static GUIStyle PremiumBadgeStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Expected O, but got Unknown
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			if (_premiumBadgeStyle == null)
			{
				_premiumBadgeStyle = new GUIStyle(GUI.skin.box)
				{
					fontSize = 10,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(6, 6, 2, 2),
					margin = CreateRectOffset(4, 4, 2, 2),
					fixedHeight = 18f
				};
				_premiumBadgeStyle.normal.textColor = new Color(0.1f, 0.08f, 0f, 1f);
				_premiumBadgeStyle.normal.background = MakeFrameTexture(12, 18, Theme.Gold, new Color(0.85f, 0.65f, 0f, 1f), new Color(1f, 0.9f, 0.4f, 1f), 1);
			}
			return _premiumBadgeStyle;
		}
	}

	public static GUIStyle FreeBadgeStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Expected O, but got Unknown
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			if (_freeBadgeStyle == null)
			{
				_freeBadgeStyle = new GUIStyle(GUI.skin.box)
				{
					fontSize = 10,
					fontStyle = (FontStyle)0,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(6, 6, 2, 2),
					margin = CreateRectOffset(4, 4, 2, 2),
					fixedHeight = 18f
				};
				_freeBadgeStyle.normal.textColor = Theme.TextMuted;
				_freeBadgeStyle.normal.background = MakeFrameTexture(12, 18, new Color(0.15f, 0.15f, 0.18f, 0.95f), new Color(0.12f, 0.12f, 0.15f, 0.95f), Theme.FreeBorder, 1);
			}
			return _freeBadgeStyle;
		}
	}

	public static GUIStyle TimeRemainingStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Expected O, but got Unknown
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			if (_timeRemainingStyle == null)
			{
				_timeRemainingStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 11,
					fontStyle = (FontStyle)0,
					alignment = (TextAnchor)5,
					padding = CreateRectOffset(4, 8, 2, 2),
					margin = CreateRectOffset(2, 2, 0, 0)
				};
				_timeRemainingStyle.normal.textColor = Theme.Gold;
				_timeRemainingStyle.richText = true;
			}
			return _timeRemainingStyle;
		}
	}

	public static GUIStyle VisorAccentStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected O, but got Unknown
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			if (_visorAccentStyle == null)
			{
				_visorAccentStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 14,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)3,
					padding = CreateRectOffset(8, 8, 4, 4)
				};
				_visorAccentStyle.normal.textColor = Theme.Visor;
				_visorAccentStyle.richText = true;
			}
			return _visorAccentStyle;
		}
	}

	public static GUIStyle PrimaryButtonStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Expected O, but got Unknown
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_018a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
			if (_primaryButtonStyle == null)
			{
				_primaryButtonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 16,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(14, 14, 8, 8),
					margin = CreateRectOffset(6, 6, 6, 6),
					fixedHeight = 38f
				};
				_primaryButtonStyle.normal.background = MakeFrameTexture(16, 64, new Color(0.85f, 0.08f, 0.22f, 0.95f), new Color(0.65f, 0.06f, 0.18f, 0.95f), new Color(1f, 0.2f, 0.35f, 1f), 2);
				_primaryButtonStyle.normal.textColor = Color.white;
				_primaryButtonStyle.hover.background = MakeFrameTexture(16, 64, new Color(1f, 0.12f, 0.28f, 0.98f), new Color(0.78f, 0.08f, 0.22f, 0.98f), new Color(1f, 0.4f, 0.5f, 1f), 2);
				_primaryButtonStyle.hover.textColor = Color.white;
				_primaryButtonStyle.active.background = MakeFrameTexture(16, 64, new Color(0.55f, 0.04f, 0.14f, 0.98f), new Color(0.45f, 0.03f, 0.12f, 0.98f), new Color(0.8f, 0.15f, 0.25f, 1f), 2);
				_primaryButtonStyle.active.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
				_primaryButtonStyle.focused.background = _primaryButtonStyle.hover.background;
				_primaryButtonStyle.richText = true;
			}
			return _primaryButtonStyle;
		}
	}

	public static GUIStyle HostButtonStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Expected O, but got Unknown
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			if (_hostButtonStyle == null)
			{
				_hostButtonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 14,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					padding = CreateRectOffset(10, 10, 5, 5),
					margin = CreateRectOffset(4, 4, 3, 3),
					fixedHeight = 30f
				};
				_hostButtonStyle.normal.background = MakeFrameTexture(16, 64, new Color(0f, 0.7f, 0.8f, 0.25f), new Color(0f, 0.5f, 0.6f, 0.25f), Theme.Visor, 1);
				_hostButtonStyle.normal.textColor = Theme.Visor;
				_hostButtonStyle.hover.background = MakeFrameTexture(16, 64, new Color(0f, 0.8f, 0.9f, 0.35f), new Color(0f, 0.6f, 0.7f, 0.35f), new Color(0.2f, 1f, 1f, 1f), 2);
				_hostButtonStyle.hover.textColor = Color.white;
				_hostButtonStyle.active.background = MakeFrameTexture(16, 64, new Color(0f, 0.4f, 0.5f, 0.4f), new Color(0f, 0.3f, 0.4f, 0.4f), Theme.Visor, 1);
				_hostButtonStyle.active.textColor = Theme.Visor;
				_hostButtonStyle.richText = true;
			}
			return _hostButtonStyle;
		}
	}

	public static GUIStyle ItemStyle
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			if (_itemStyle == null)
			{
				_itemStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(4, 4, 2, 2),
					margin = CreateRectOffset(2, 2, 1, 1)
				};
				_itemStyle.normal.background = MakeTexture(2, 2, new Color(0.08f, 0.08f, 0.1f, 0.8f));
			}
			return _itemStyle;
		}
	}

	public static GUIStyle SelectedItemStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			if (_selectedItemStyle == null)
			{
				_selectedItemStyle = new GUIStyle(GUI.skin.box)
				{
					padding = CreateRectOffset(4, 4, 2, 2),
					margin = CreateRectOffset(2, 2, 1, 1)
				};
				_selectedItemStyle.normal.background = MakeFrameTexture(16, 28, new Color(0.12f, 0.03f, 0.06f, 0.95f), new Color(0.1f, 0.02f, 0.05f, 0.95f), Theme.Accent, 2);
			}
			return _selectedItemStyle;
		}
	}

	public static GUIStyle ListButtonStyle
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_015e: Unknown result type (might be due to invalid IL or missing references)
			if (_listButtonStyle == null)
			{
				_listButtonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 12,
					alignment = (TextAnchor)3,
					padding = CreateRectOffset(6, 6, 3, 3),
					margin = CreateRectOffset(1, 1, 0, 0),
					fixedHeight = 22f
				};
				_listButtonStyle.normal.textColor = Color.white;
				_listButtonStyle.fontStyle = (FontStyle)1;
				_listButtonStyle.normal.background = MakeTexture(new Color(0.1f, 0.1f, 0.12f, 0.6f));
				_listButtonStyle.hover.background = MakeFrameTexture(8, 22, new Color(0.14f, 0.14f, 0.17f, 0.8f), new Color(0.12f, 0.12f, 0.15f, 0.8f), Theme.AccentSoft, 1);
				_listButtonStyle.hover.textColor = Color.white;
				_listButtonStyle.active.background = MakeFrameTexture(8, 22, new Color(0.16f, 0.04f, 0.08f, 0.9f), new Color(0.12f, 0.03f, 0.06f, 0.9f), Theme.Accent, 1);
				_listButtonStyle.active.textColor = Color.white;
				_listButtonStyle.richText = true;
			}
			return _listButtonStyle;
		}
	}

	public static GUILayoutOption CachedWidth(float w)
	{
		if (!_widthCache.TryGetValue(w, out var value))
		{
			value = GUILayout.Width(w);
			_widthCache[w] = value;
		}
		return value;
	}

	public static GUILayoutOption CachedHeight(float h)
	{
		if (!_heightCache.TryGetValue(h, out var value))
		{
			value = GUILayout.Height(h);
			_heightCache[h] = value;
		}
		return value;
	}

	public static void ClearCache()
	{
		foreach (Texture2D value in _textureCache.Values)
		{
			if ((Object)(object)value != (Object)null)
			{
				Object.Destroy((Object)(object)value);
			}
		}
		_textureCache.Clear();
		if (InteropFix.Cast((Object)(object)_cachedPixelTexture))
		{
			Object.Destroy((Object)(object)_cachedPixelTexture);
		}
		if (InteropFix.Cast((Object)(object)_cachedPixelDarkTexture))
		{
			Object.Destroy((Object)(object)_cachedPixelDarkTexture);
		}
		if (InteropFix.Cast((Object)(object)_cachedPixelAccentTexture))
		{
			Object.Destroy((Object)(object)_cachedPixelAccentTexture);
		}
		if (InteropFix.Cast((Object)(object)_cachedPixelErrorTexture))
		{
			Object.Destroy((Object)(object)_cachedPixelErrorTexture);
		}
		if (InteropFix.Cast((Object)(object)_cachedSuccessTexture))
		{
			Object.Destroy((Object)(object)_cachedSuccessTexture);
		}
		if (InteropFix.Cast((Object)(object)_cachedErrorTexture))
		{
			Object.Destroy((Object)(object)_cachedErrorTexture);
		}
		_cachedPixelTexture = null;
		_cachedPixelDarkTexture = null;
		_cachedPixelAccentTexture = null;
		_cachedPixelErrorTexture = null;
		_cachedSuccessTexture = null;
		_cachedErrorTexture = null;
		_cachedSuccessIndicatorStyle = null;
		_cachedErrorIndicatorStyle = null;
		_cachedAnimatedHeaderStyle = null;
	}

	private unsafe static Texture2D MakeVerticalGradientTexture(int width, int height, Color top, Color bottom)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		string key = $"grad_{width}_{height}_{((object)(*(Color*)(&top))/*cast due to .constrained prefix*/).GetHashCode()}_{((object)(*(Color*)(&bottom))/*cast due to .constrained prefix*/).GetHashCode()}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		if (width < 1)
		{
			width = 1;
		}
		if (height < 2)
		{
			height = 2;
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Texture)val).filterMode = (FilterMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			float num = (float)i / (float)(height - 1);
			Color val2 = Color.Lerp(top, bottom, num);
			for (int j = 0; j < width; j++)
			{
				array[i * width + j] = val2;
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	private unsafe static Texture2D MakeFrameTexture(int width, int height, Color innerTop, Color innerBottom, Color border, int borderThickness)
	{
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		string key = $"frame_{width}_{height}_{((object)(*(Color*)(&innerTop))/*cast due to .constrained prefix*/).GetHashCode()}_{((object)(*(Color*)(&innerBottom))/*cast due to .constrained prefix*/).GetHashCode()}_{((object)(*(Color*)(&border))/*cast due to .constrained prefix*/).GetHashCode()}_{borderThickness}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		if (width < borderThickness * 2 + 1)
		{
			width = borderThickness * 2 + 1;
		}
		if (height < borderThickness * 2 + 2)
		{
			height = borderThickness * 2 + 2;
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Texture)val).filterMode = (FilterMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			float num = (float)i / (float)(height - 1);
			Color val2 = Color.Lerp(innerTop, innerBottom, num);
			for (int j = 0; j < width; j++)
			{
				bool flag = j < borderThickness || j >= width - borderThickness || i < borderThickness || i >= height - borderThickness;
				array[i * width + j] = (flag ? border : val2);
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	public unsafe static Texture2D MakeTexture(int width, int height, Color color)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		string key = $"solid_{width}_{height}_{((object)(*(Color*)(&color))/*cast due to .constrained prefix*/).GetHashCode()}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Texture)val).filterMode = (FilterMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	private unsafe static Texture2D MakeTexture(Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if (color == Theme.BgDarkA)
		{
			if ((Object)(object)_cachedPixelTexture == (Object)null)
			{
				_cachedPixelTexture = CreatePixelTexture(color);
			}
			return _cachedPixelTexture;
		}
		if (color == Theme.BgDarkB)
		{
			if ((Object)(object)_cachedPixelDarkTexture == (Object)null)
			{
				_cachedPixelDarkTexture = CreatePixelTexture(color);
			}
			return _cachedPixelDarkTexture;
		}
		if (color == Theme.Accent)
		{
			if ((Object)(object)_cachedPixelAccentTexture == (Object)null)
			{
				_cachedPixelAccentTexture = CreatePixelTexture(color);
			}
			return _cachedPixelAccentTexture;
		}
		if (color == Theme.Error)
		{
			if ((Object)(object)_cachedPixelErrorTexture == (Object)null)
			{
				_cachedPixelErrorTexture = CreatePixelTexture(color);
			}
			return _cachedPixelErrorTexture;
		}
		string key = $"pixel_{((object)(*(Color*)(&color))/*cast due to .constrained prefix*/).GetHashCode()}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		Texture2D val = CreatePixelTexture(color);
		_textureCache[key] = val;
		return val;
	}

	private static Texture2D CreatePixelTexture(Color color)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		Texture2D val = new Texture2D(1, 1, (TextureFormat)4, false);
		val.SetPixel(0, 0, color);
		((Texture)val).filterMode = (FilterMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		val.Apply();
		return val;
	}

	public unsafe static Texture2D MakeCyberTexture(int w, int h, Color baseCol)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		string key = $"cyber_{w}_{h}_{((object)(*(Color*)(&baseCol))/*cast due to .constrained prefix*/).GetHashCode()}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		if (w < 4)
		{
			w = 4;
		}
		if (h < 4)
		{
			h = 4;
		}
		Texture2D val = new Texture2D(w, h, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Texture)val).filterMode = (FilterMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		Color[] array = (Color[])(object)new Color[w * h];
		float num = (float)w / 2f;
		float num2 = (float)h / 2f;
		int num3 = _cyberNoiseSeed;
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				Color val2 = baseCol;
				if (i % 3 == 0)
				{
					val2.a *= 0.85f;
				}
				float num4 = ((float)j - num) / num;
				float num5 = ((float)i - num2) / num2;
				float num6 = Mathf.Sqrt(num4 * num4 + num5 * num5);
				float num7 = 1f - num6 * 0.21f;
				num7 = Mathf.Clamp01(num7);
				val2.r *= num7;
				val2.g *= num7;
				val2.b *= num7;
				num3 = (num3 * 1103515245 + 12345) & 0x7FFFFFFF;
				float num8 = ((float)(num3 % 1000) / 1000f - 0.5f) * 0.04f;
				val2.r = Mathf.Clamp01(val2.r + num8);
				val2.g = Mathf.Clamp01(val2.g + num8);
				val2.b = Mathf.Clamp01(val2.b + num8);
				if (j == 0 || i == h - 1)
				{
					val2.r = Mathf.Min(1f, val2.r * 1.2f);
					val2.g = Mathf.Min(1f, val2.g * 1.2f);
					val2.b = Mathf.Min(1f, val2.b * 1.2f);
				}
				else if (j == w - 1 || i == 0)
				{
					val2.r *= 0.5f;
					val2.g *= 0.5f;
					val2.b *= 0.5f;
				}
				array[i * w + j] = val2;
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	public static void DrawOutlinedLabel(Rect r, string content, GUIStyle s)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		if (s != null && !string.IsNullOrEmpty(content))
		{
			Color textColor = s.normal.textColor;
			Color textColor2 = default(Color);
			textColor2._002Ector(0f, 0f, 0f, 0.9f);
			s.normal.textColor = textColor2;
			GUI.Label(new Rect(r.x - 1f, r.y - 1f, r.width, r.height), content, s);
			GUI.Label(new Rect(r.x - 1f, r.y + 1f, r.width, r.height), content, s);
			GUI.Label(new Rect(r.x + 1f, r.y - 1f, r.width, r.height), content, s);
			GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), content, s);
			s.normal.textColor = textColor;
			GUI.Label(r, content, s);
		}
	}

	public static bool DrawCyberButton(Rect r, string label, GUIStyle s)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		if (s == null)
		{
			return false;
		}
		bool num = r.Contains(Event.current.mousePosition);
		bool flag = num && Input.GetMouseButton(0);
		Rect val = r;
		if (flag)
		{
			val.x = val.x + 1f;
			val.y = val.y + 2f;
		}
		if (num && !flag)
		{
			if ((Object)(object)_cachedHoverGlowTexture == (Object)null)
			{
				_cachedHoverGlowTexture = MakeTexture(4, 4, new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.15f));
			}
			GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), (Texture)(object)_cachedHoverGlowTexture);
		}
		return GUI.Button(val, label, s);
	}

	private unsafe static Texture2D MakeGlowFrameTexture(int width, int height, Color bgColor, Color glowColor, int thickness)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		string key = $"glowframe_{width}_{height}_{((object)(*(Color*)(&bgColor))/*cast due to .constrained prefix*/).GetHashCode()}_{((object)(*(Color*)(&glowColor))/*cast due to .constrained prefix*/).GetHashCode()}_{thickness}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int val2 = j;
				int val3 = width - 1 - j;
				int val4 = i;
				int val5 = height - 1 - i;
				int num = Math.Min(Math.Min(val2, val3), Math.Min(val4, val5));
				if (num < thickness)
				{
					float num2 = (float)num / (float)thickness;
					array[i * width + j] = Color.Lerp(glowColor, bgColor, num2);
				}
				else
				{
					array[i * width + j] = bgColor;
				}
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	public unsafe static Texture2D MakeGlassTexture(int width, int height, Color baseColor)
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		string key = $"glass_cyber_{width}_{height}_{((object)(*(Color*)(&baseColor))/*cast due to .constrained prefix*/).GetHashCode()}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Texture)val).filterMode = (FilterMode)1;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			float num = (float)i / (float)height;
			float num2 = 0.9f + num * 0.1f;
			for (int j = 0; j < width; j++)
			{
				Color val2 = baseColor;
				if ((j + i) % 4 == 0)
				{
					val2.r *= 0.95f;
					val2.g *= 0.95f;
					val2.b *= 0.95f;
				}
				if (i >= height - 2)
				{
					val2 += new Color(0.1f, 0.1f, 0.1f, 0f);
				}
				val2.a *= num2;
				array[i * width + j] = val2;
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	public static Texture2D GetGradientTexture()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_gradientTexture != (Object)null)
		{
			return _gradientTexture;
		}
		int num = 128;
		int num2 = 4;
		_gradientTexture = new Texture2D(num, num2, (TextureFormat)4, false);
		((Texture)_gradientTexture).wrapMode = (TextureWrapMode)1;
		((Texture)_gradientTexture).filterMode = (FilterMode)1;
		Color primary = Theme.Primary;
		new Color(primary.r, primary.g, primary.b, 0f);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num3 = (float)j / (float)num;
				float num4 = ((!(num3 < 0.5f)) ? ((1f - num3) * 2f) : (num3 * 2f));
				Color val = primary;
				val.a = num4 * 0.8f;
				_gradientTexture.SetPixel(j, i, val);
			}
		}
		_gradientTexture.Apply();
		((Object)_gradientTexture).hideFlags = (HideFlags)61;
		return _gradientTexture;
	}

	private static RectOffset CreateRectOffset(int left, int right, int top, int bottom)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		return new RectOffset
		{
			left = left,
			right = right,
			top = top,
			bottom = bottom
		};
	}

	private unsafe static Texture2D MakeLaserTexture(int width, int height, Color color)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		string key = $"laser_{width}_{height}_{((object)(*(Color*)(&color))/*cast due to .constrained prefix*/).GetHashCode()}";
		if (_textureCache.ContainsKey(key) && (Object)(object)_textureCache[key] != (Object)null)
		{
			return _textureCache[key];
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < width; i++)
		{
			float num = Mathf.Sin((float)i / (float)(width - 1) * (float)Math.PI);
			for (int j = 0; j < height; j++)
			{
				Color val2 = color;
				val2.a *= num;
				array[j * width + i] = val2;
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		_textureCache[key] = val;
		return val;
	}

	private static Texture2D MakeCircleTexture(int size, Color centerColor, Color edgeColor)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(size, size, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		float num = (float)size / 2f;
		Color[] array = (Color[])(object)new Color[size * size];
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num2 = (float)j - num + 0.5f;
				float num3 = (float)i - num + 0.5f;
				float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
				if (num4 <= num)
				{
					float num5 = num4 / num;
					Color val2 = Color.Lerp(centerColor, edgeColor, num5 * 0.6f);
					if (num4 > num - 1f)
					{
						val2.a *= num - num4;
					}
					array[i * size + j] = val2;
				}
				else
				{
					array[i * size + j] = Color.clear;
				}
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return val;
	}

	private static Texture2D MakeRoundedTexture(int width, int height, Color fillColor, Color borderColor, int borderWidth)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Object)val).hideFlags = (HideFlags)61;
		float num = (float)height / 2f;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				bool flag = false;
				float num2 = 0f;
				if ((float)j < num)
				{
					float num3 = (float)j - num;
					float num4 = (float)i - num;
					float num5 = Mathf.Sqrt(num3 * num3 + num4 * num4);
					flag = num5 <= num;
					num2 = num - num5;
				}
				else if ((float)j >= (float)width - num)
				{
					float num6 = (float)j - ((float)width - num);
					float num7 = (float)i - num;
					float num8 = Mathf.Sqrt(num6 * num6 + num7 * num7);
					flag = num8 <= num;
					num2 = num - num8;
				}
				else
				{
					flag = true;
					num2 = Mathf.Min(i, height - 1 - i);
				}
				if (flag)
				{
					Color val2 = ((num2 < (float)borderWidth) ? borderColor : fillColor);
					array[i * width + j] = val2;
				}
				else
				{
					array[i * width + j] = Color.clear;
				}
			}
		}
		val.SetPixels(InteropFix.Cast(array));
		val.Apply();
		return val;
	}

	public static void EnsureInitialized()
	{
		_ = WindowStyle;
		_ = HeaderBackgroundStyle;
		_ = TitleLabelStyle;
		_ = TitleBarButtonStyle;
		_ = HeaderStyle;
		_ = SubHeaderStyle;
		_ = ButtonStyle;
		_ = ToggleStyle;
		_ = SliderStyle;
		_ = LabelStyle;
		_ = TabStyle;
		_ = SelectedTabStyle;
		_ = ContainerStyle;
		_ = SectionStyle;
		_ = ErrorStyle;
		_ = IconStyle;
		_ = TooltipStyle;
		_ = StatusIndicatorStyle;
		_ = GlowStyle;
		_ = ShadowStyle;
		_ = HighlightStyle;
		_ = SeparatorStyle;
		_ = BetterToggleStyle;
		_ = TextFieldStyle;
		_ = CrewToggleStyle;
		_ = PrimaryButtonStyle;
		_ = HostButtonStyle;
		_ = PremiumBadgeStyle;
		_ = FreeBadgeStyle;
		_ = TimeRemainingStyle;
		_ = VisorAccentStyle;
	}

	public static void DrawTooltip(string tooltip, Rect rect)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(tooltip) && rect.Contains(Event.current.mousePosition))
		{
			float num = Mathf.Min(200f, (float)tooltip.Length * 7f + 20f);
			float num2 = 28f;
			GUI.Label(new Rect(Event.current.mousePosition.x + 15f, Event.current.mousePosition.y, num, num2), tooltip, TooltipStyle);
		}
	}

	public static void DrawStatusIndicator(bool isActive)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (isActive)
		{
			if (_cachedSuccessIndicatorStyle == null)
			{
				if ((Object)(object)_cachedSuccessTexture == (Object)null)
				{
					_cachedSuccessTexture = MakeTexture(1, 1, Theme.Success);
				}
				_cachedSuccessIndicatorStyle = new GUIStyle(StatusIndicatorStyle);
				_cachedSuccessIndicatorStyle.normal.background = _cachedSuccessTexture;
			}
			GUILayout.Box(GUIContent.none, _cachedSuccessIndicatorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			return;
		}
		if (_cachedErrorIndicatorStyle == null)
		{
			if ((Object)(object)_cachedErrorTexture == (Object)null)
			{
				_cachedErrorTexture = MakeTexture(1, 1, Theme.Error);
			}
			_cachedErrorIndicatorStyle = new GUIStyle(StatusIndicatorStyle);
			_cachedErrorIndicatorStyle.normal.background = _cachedErrorTexture;
		}
		GUILayout.Box(GUIContent.none, _cachedErrorIndicatorStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
	}

	public static void DrawSeparator()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		if (_separatorStyle == null || (Object)(object)_separatorTexture == (Object)null)
		{
			int num = 128;
			int num2 = 2;
			_separatorTexture = new Texture2D(num, num2, (TextureFormat)4, false);
			((Texture)_separatorTexture).filterMode = (FilterMode)1;
			((Texture)_separatorTexture).wrapMode = (TextureWrapMode)1;
			((Object)_separatorTexture).hideFlags = (HideFlags)61;
			Color[] array = (Color[])(object)new Color[num * num2];
			Color val = default(Color);
			for (int i = 0; i < num; i++)
			{
				float num3 = (float)i / (float)(num - 1);
				float num4 = 1f - Mathf.Abs(num3 * 2f - 1f);
				num4 *= num4;
				val._002Ector(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, num4 * 0.5f);
				for (int j = 0; j < num2; j++)
				{
					array[j * num + i] = val;
				}
			}
			_separatorTexture.SetPixels(InteropFix.Cast(array));
			_separatorTexture.Apply();
			_separatorStyle = new GUIStyle();
			_separatorStyle.normal.background = _separatorTexture;
			_separatorStyle.stretchWidth = true;
			_separatorStyle.fixedHeight = 2f;
		}
		GUILayout.Space(4f);
		GUILayout.Box(GUIContent.none, _separatorStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.ExpandWidth(true),
			GUILayout.Height(2f)
		});
		GUILayout.Space(4f);
	}

	public static bool DrawTab(string label, bool selected)
	{
		return GUILayout.Toggle(selected, label, selected ? SelectedTabStyle : TabStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
	}

	public static bool DrawBetterToggle(bool value, string label, string tooltip = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		if (value)
		{
			GUI.color = new Color(0.6f, 1f, 0.6f, 1f);
		}
		bool result = GUILayout.Toggle(value, label, BetterToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUI.color = color;
		Rect lastRect = GUILayoutUtility.GetLastRect();
		if (!string.IsNullOrEmpty(tooltip))
		{
			DrawTooltip(tooltip, lastRect);
		}
		return result;
	}

	public static string GetHeaderText(string text)
	{
		return $"{text} - {DateTime.Now:HH:mm:ss}";
	}

	public static GUIStyle GetAnimatedHeaderStyle()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		if (_cachedAnimatedHeaderStyle == null)
		{
			_cachedAnimatedHeaderStyle = new GUIStyle(HeaderStyle);
		}
		_cachedAnimatedHeaderStyle.normal.textColor = Color.Lerp(Theme.Accent, Theme.AccentSoft, Mathf.PingPong(Time.time * 2f, 1f));
		return _cachedAnimatedHeaderStyle;
	}

	public static string GetCurrentTime()
	{
		return DateTime.Now.ToString("HH:mm:ss");
	}

	public static void DrawKeyBadge(bool isPremium, string keyType = null)
	{
		if (isPremium)
		{
			GUILayout.Box(string.IsNullOrEmpty(keyType) ? "â˜… PREMIUM" : ("â˜… " + keyType.ToUpper()), PremiumBadgeStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(false) });
		}
		else
		{
			GUILayout.Box("FREE", FreeBadgeStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(false) });
		}
	}

	public static void DrawTimeRemaining(TimeSpan remaining)
	{
		string text = ((remaining.TotalDays >= 1.0) ? $"{(int)remaining.TotalDays}d {remaining.Hours}h" : ((!(remaining.TotalHours >= 1.0)) ? $"{remaining.Minutes}m {remaining.Seconds}s" : $"{(int)remaining.TotalHours}h {remaining.Minutes}m"));
		GUILayout.Label("â± " + text, TimeRemainingStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(false) });
	}

	public static void DrawCrewSeparator()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = Theme.Accent;
		GUILayout.Box("", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(2f),
			GUILayout.ExpandWidth(true)
		});
		GUI.color = color;
	}

	public static void DrawCrewCoreLogo()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.FlexibleSpace();
		if (_crewLogoStyleRed == null)
		{
			_crewLogoStyleRed = new GUIStyle(HeaderStyle)
			{
				fontSize = 20
			};
			_crewLogoStyleRed.normal.textColor = Theme.Accent;
		}
		GUILayout.Label("CREW", _crewLogoStyleRed, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(false) });
		if (_crewLogoStyleCyan == null)
		{
			_crewLogoStyleCyan = new GUIStyle(HeaderStyle)
			{
				fontSize = 20
			};
			_crewLogoStyleCyan.normal.textColor = Theme.Visor;
		}
		GUILayout.Label("CORE", _crewLogoStyleCyan, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(false) });
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	public static bool DrawCrewToggle(bool value, string label, string tooltip = null)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		bool result = GUILayout.Toggle(value, label, CrewToggleStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		Rect lastRect = GUILayoutUtility.GetLastRect();
		if (!string.IsNullOrEmpty(tooltip))
		{
			DrawTooltip(tooltip, lastRect);
		}
		return result;
	}

	public static float DrawCrewSlider(float value, float min, float max, string label, string format = "F1", string suffix = "", bool showMinMax = false)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected O, but got Unknown
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		if (_sliderLabelStyle == null)
		{
			_sliderLabelStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 13,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)3,
				padding = CreateRectOffset(0, 0, 0, 0),
				margin = CreateRectOffset(0, 0, 0, 0)
			};
			_sliderLabelStyle.normal.textColor = Theme.TextPrimary;
		}
		if (_sliderValueStyle == null)
		{
			_sliderValueStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 14,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)5,
				padding = CreateRectOffset(0, 0, 0, 0),
				margin = CreateRectOffset(0, 0, 0, 0)
			};
			_sliderValueStyle.normal.textColor = Theme.Visor;
		}
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(48f) });
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.Label(label, _sliderLabelStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		GUILayout.Label(value.ToString(format) + suffix, _sliderValueStyle, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(60f) });
		GUILayout.EndHorizontal();
		GUILayout.Space(2f);
		float result = GUILayout.HorizontalSlider(value, min, max, SliderStyle, SliderThumbStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(18f),
			GUILayout.ExpandWidth(true)
		});
		if (showMinMax)
		{
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			if (_sliderMinMaxStyle == null)
			{
				_sliderMinMaxStyle = new GUIStyle(_sliderLabelStyle)
				{
					fontSize = 10
				};
				_sliderMinMaxStyle.normal.textColor = Theme.TextMuted;
			}
			GUILayout.Label(min.ToString(format), _sliderMinMaxStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.FlexibleSpace();
			GUILayout.Label(max.ToString(format), _sliderMinMaxStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
		return result;
	}

	private static void DrawSliderFill(Rect sliderRect, float value, float min, float max)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp01((value - min) / (max - min));
		Rect val = default(Rect);
		val._002Ector(sliderRect.x + 9f, sliderRect.y + 5f, sliderRect.width - 18f, 8f);
		GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
		GUI.Box(val, GUIContent.none, GUIStyle.none);
		if (num > 0.01f)
		{
			Rect val2 = default(Rect);
			val2._002Ector(val.x, val.y, val.width * num, val.height);
			Color val3 = Color.Lerp(Theme.AccentDim, Theme.Accent, num);
			float num2 = 0.9f + Mathf.Sin(Time.realtimeSinceStartup * 2f) * 0.1f;
			GUI.color = val3 * num2;
			GUI.Box(val2, GUIContent.none, GUIStyle.none);
			if (num > 0.05f)
			{
				Color color = default(Color);
				color._002Ector(Theme.Visor.r, Theme.Visor.g, Theme.Visor.b, 0.4f * num);
				Rect val4 = new Rect(val2.xMax - 4f, val.y - 1f, 8f, val.height + 2f);
				GUI.color = color;
				GUI.Box(val4, GUIContent.none, GUIStyle.none);
			}
		}
		GUI.color = Color.white;
	}

	public static float DrawMiniSlider(float value, float min, float max, float width = 100f)
	{
		return GUILayout.HorizontalSlider(value, min, max, SliderStyle, SliderThumbStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(width),
			GUILayout.Height(16f)
		});
	}

	public static bool DrawEliteButton(string label, float height = 32f)
	{
		return GUILayout.Button(label, ButtonStyle, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(height),
			GUILayout.ExpandWidth(true)
		});
	}

	public static void DrawEliteSeparator(float alpha = 0.8f)
	{
		DrawSeparator();
	}

	public static Vector2 BeginEliteScrollView(Vector2 scrollPos, params GUILayoutOption[] options)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		return GUILayout.BeginScrollView(scrollPos, false, false, options);
	}

	public static void EndEliteScrollView()
	{
		GUILayout.EndScrollView();
	}

	public static void BeginEliteCard(string title = null)
	{
		GUILayout.BeginVertical(SectionStyle, System.Array.Empty<GUILayoutOption>());
		if (!string.IsNullOrEmpty(title))
		{
			GUILayout.Label(title, HeaderStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Space(4f);
		}
	}

	public static void EndEliteCard()
	{
		GUILayout.EndVertical();
	}

	public static void DrawEliteStatus(bool isActive, string label)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
		Rect rect = GUILayoutUtility.GetRect(12f, 12f, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(12f),
			GUILayout.Height(12f)
		});
		if ((int)Event.current.type == 7)
		{
			Color val = (isActive ? Theme.Success : Theme.TextDisabled);
			if (isActive)
			{
				float num = Animation.Pulse(2f, 0.7f);
				val._002Ector(val.r * num, val.g * num, val.b * num, val.a);
			}
			GUI.color = val;
			GUI.DrawTexture(rect, (Texture)(object)MakeTexture(12, 12, Color.white));
			GUI.color = Color.white;
		}
		GUILayout.Space(4f);
		if (_eliteStatusActiveStyle == null)
		{
			_eliteStatusActiveStyle = new GUIStyle(LabelStyle)
			{
				fontSize = 12
			};
			_eliteStatusActiveStyle.normal.textColor = Theme.TextPrimary;
			_eliteStatusInactiveStyle = new GUIStyle(LabelStyle)
			{
				fontSize = 12
			};
			_eliteStatusInactiveStyle.normal.textColor = Theme.TextInactive;
		}
		GUILayout.Label(label, isActive ? _eliteStatusActiveStyle : _eliteStatusInactiveStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
		GUILayout.EndHorizontal();
	}

	public static void DrawEliteProgressBar(float progress, string label = null, float height = 20f)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		progress = Mathf.Clamp01(progress);
		GUILayout.BeginVertical((Il2CppReferenceArray<GUILayoutOption>)null);
		if (!string.IsNullOrEmpty(label))
		{
			GUILayout.BeginHorizontal((Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.Label(label, LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.FlexibleSpace();
			GUILayout.Label($"{progress * 100f:F0}%", LabelStyle, (Il2CppReferenceArray<GUILayoutOption>)null);
			GUILayout.EndHorizontal();
		}
		Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(height),
			GUILayout.ExpandWidth(true)
		});
		if ((int)Event.current.type == 7)
		{
			GUI.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
			GUI.DrawTexture(rect, (Texture)(object)Texture2D.whiteTexture);
			if (progress > 0.01f)
			{
				Rect val = default(Rect);
				val._002Ector(rect.x, rect.y, rect.width * progress, rect.height);
				float num = Animation.Pulse();
				Color val2 = Color.Lerp(Theme.AccentActive, Theme.Accent, progress);
				val2._002Ector(val2.r * num, val2.g * num, val2.b * num, 1f);
				GUI.color = val2;
				GUI.DrawTexture(val, (Texture)(object)Texture2D.whiteTexture);
				if (progress < 0.99f)
				{
					Rect val3 = new Rect(val.xMax - 4f, rect.y, 8f, rect.height);
					GUI.color = new Color(1f, 1f, 1f, 0.3f * num);
					GUI.DrawTexture(val3, (Texture)(object)Texture2D.whiteTexture);
				}
			}
			GUI.color = Color.white;
		}
		GUILayout.EndVertical();
	}
}








