using System.Collections;
using System.Text;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModMenuCrew.Patches;

public class VersionShowerFx : MonoBehaviour
{
	private TextMeshPro _text;

	private RectTransform _textRectTransform;

	private string _baseText;

	private string _modText;

	private readonly StringBuilder _textBuilder = new StringBuilder(256);

	private bool _isEffectRunning;

	private bool _isGlitchActive;

	private Coroutine _schedulerRoutine;

	private Coroutine _breathingRoutine;

	private Vector2 _baseAnchoredPosition;

	private float _baseScale;

	private Color _baseColor;

	private Color _baseOutlineColor;

	private float _baseOutlineWidth;

	private static readonly WaitForSeconds sWaitFrame = new WaitForSeconds(0.016f);

	private static readonly WaitForSeconds sWait025 = new WaitForSeconds(0.025f);

	private static readonly WaitForSeconds sWait04 = new WaitForSeconds(0.04f);

	private static readonly WaitForSeconds sWait05 = new WaitForSeconds(0.05f);

	private static readonly WaitForSeconds sWait08 = new WaitForSeconds(0.08f);

	private static readonly WaitForSeconds sWait1 = new WaitForSeconds(0.1f);

	private static readonly WaitForSeconds sWait15 = new WaitForSeconds(0.15f);

	private static readonly WaitForSeconds sWait2 = new WaitForSeconds(0.2f);

	private static readonly WaitForSeconds sWait3 = new WaitForSeconds(0.3f);

	private static readonly WaitForSeconds sWait4 = new WaitForSeconds(0.4f);

	private static readonly WaitForSeconds sWait5 = new WaitForSeconds(0.5f);

	private static readonly WaitForSeconds sWait6 = new WaitForSeconds(0.6f);

	private static readonly WaitForSeconds sWait07 = new WaitForSeconds(0.7f);

	private static readonly WaitForSeconds sWait10 = new WaitForSeconds(1f);

	private static readonly WaitForSeconds sWait15s = new WaitForSeconds(1.5f);

	private static readonly WaitForSeconds sWait20s = new WaitForSeconds(2f);

	private static float sProbPhantom = 0.06f;

	private static float sProbMythic = 0.14f;

	private static float sProbRare = 0.32f;

	private float _heavyCooldownSeconds = 6f;

	private float _nextHeavyAllowedTime;

	private static readonly Color FnafGreen = new Color(0.6f, 1f, 0.3f);

	private static readonly Color AlertOrange = new Color(1f, 0.4f, 0f);

	private static readonly Color DeadRed = new Color(1f, 0f, 0f);

	private static readonly Color BloodDark = new Color(0.5f, 0f, 0f);

	private static readonly Color GhostCyan = new Color(0.4f, 1f, 1f, 0.8f);

	private static readonly Color CursedPurple = new Color(0.4f, 0f, 0.6f);

	private static readonly Color GoldenYellow = new Color(1f, 0.85f, 0f);

	private static readonly Color CorpseGreen = new Color(0.3f, 0.5f, 0.2f);

	private static readonly Color RottenPurple = new Color(0.35f, 0.1f, 0.4f);

	private static readonly Color FleshPink = new Color(0.9f, 0.5f, 0.5f);

	private static readonly Color BoneWhite = new Color(0.95f, 0.92f, 0.85f);

	private static readonly Color OxidizedGreen = new Color(0.2f, 0.4f, 0.3f);

	private static readonly Color VisceralRed = new Color(0.7f, 0.1f, 0.15f);

	private static readonly Color DecayBrown = new Color(0.4f, 0.25f, 0.1f);

	private static readonly Color StaticGray = new Color(0.6f, 0.6f, 0.6f);

	private static readonly System.Random sRandom = new System.Random();

	private static readonly string[] sSystemMessages = new string[56]
	{
		"SUS", "[REDACTED]", "ACCESS DENIED", "SECURITY ALERT", "UNKNOWN SIGNAL", "WHO IS IT?", "NOT THE IMPOSTOR", "VENT ERROR", "AUDIO ERROR", "VIDEO ERROR",
		"CAM SYS ERROR", "REBOOT ALL", "BODY FOUND", "INTRUDER ALERT", "LOCKDOWN", "RUN", "HIDE", "THEY ARE HERE", "DON'T LOOK BACK", "I SEE YOU",
		"YOU ARE NEXT", "IT'S TOO LATE", "NO ESCAPE", "KILL", "DEAD", "HELP ME", "BEHIND YOU", "ERROR 666", "WATCHING...", "DO NOT MOVE",
		"IMPOSTOR WIN", "GAME OVER", "BLOOD", "DARKNESS", "IT'S ME", "SAVE THEM", "SAVE HIM", "HELP THEM", "YOU CAN'T", "FOLLOW ME",
		"HE ALWAYS COMES BACK", "I WILL PUT YOU BACK TOGETHER", "I'M STILL HERE", "MY NAME IS SPRINGTRAP", "THE JOY OF CREATION", "WAS THAT THE BITE OF '87?", "FIVE NIGHTS", "NIGHTMARE", "4", "BAD ENDING",
		"YOU DIED", "GAME OVER", "VENT SEALED", "AUDIO LURE FAILED", "CAMERA DISABLED", "SYSTEM OVERLOAD"
	};

	private static readonly string[] sVentilationMessages = new string[12]
	{
		"VENT. ERROR", "SEAL VENT", "AUDIO ERROR", "CAM ERROR", "REBOOT", "VENT 1: SEALED", "VENT 2: ERROR", "VENT 3: OPEN", "VENT 4: ???", "AIR FLOW: CRITICAL",
		"O2 LEVELS: LOW", "PRESSURE: DROPPING"
	};

	private static readonly string[] sSpringtrapLore = new string[20]
	{
		"I FOUND YOU", "THERE IS NO ESCAPE", "I AM STILL HERE", "THE SUIT IS MINE", "JOIN US", "THEY LOCKED ME AWAY", "30 YEARS", "THE SAFE ROOM", "I PUT THEM BACK TOGETHER", "DID YOU MISS ME?",
		"FAZBEAR'S FRIGHT", "ONE MORE SOUL", "I ALWAYS COME BACK", "THE SPRING LOCKS", "MY NAME IS SPRINGTRAP", "PURPLE GUY", "WILLIAM AFTON", "THE MURDERER", "FATHER?", "I'M GOING TO COME FIND YOU"
	};

	private static readonly string[] sVictimMessages = new string[22]
	{
		"HELP US", "SET US FREE", "IT HURTS", "WHY WON'T YOU SAVE US?", "WE ARE STILL HERE", "GIVE GIFTS", "GIVE LIFE", "SAVE ME", "HE KILLED US", "WE CAN'T LEAVE",
		"THE PUPPET SAVED US", "FIVE CHILDREN", "MISSING CHILDREN INCIDENT", "WE REMEMBER", "GABRIEL", "JEREMY", "SUSIE", "FRITZ", "CASSIDY", "THE BITE VICTIM",
		"PUT US BACK TOGETHER", "WE WANT TO BE FREE"
	};

	private static readonly string[] sGrotesqueMessages = new string[19]
	{
		"FLESH", "BONES", "BLOOD", "DECAY", "ROT", "CORPSE", "THE SMELL", "TRAPPED INSIDE", "CRUSHED", "SPRING LOCK FAILURE",
		"HEAR THE SCREAMING", "FEEL THE METAL", "PIERCING FLESH", "ORGANS EXPOSED", "MUMMIFIED", "30 YEARS IN DARKNESS", "THE WIRES TANGLE", "ENDOSKELETON EXPOSED", "ROTTING SUIT"
	};

	private static readonly string[] sCreepyFaces = new string[30]
	{
		"O_O", "â—‰_â—‰", "âŠ™_âŠ™", "â—_â—", "â—‹_â—‹", "â—Ž_â—Ž", "X_X", "âœ–_âœ–", "â˜ ", "\ud83d\udc41",
		"â—‰\u203fâ—‰", "âŠ™\u203fâŠ™", "( \u0361Â° \u035cÊ– \u0361Â°)", "à² _à² ", "Ê˜\u203fÊ˜", "â—•\u203fâ—•", "âŠ™\ufe4fâŠ™", "âŠ™Ï‰âŠ™", "à° _à° ", "(â•¬ Ã’\ufe4fÃ“)",
		"áƒš(à² ç›Šà² áƒš)", "(à² \u203fà² )", "â—‰â—¡â—‰", "âŠ™â–ƒâŠ™", "â—â–‚â—", "â˜‰_â˜‰", "(âŠ™_âŠ™)", "Ù©(\u0361à¹\u032f\u0361à¹)Û¶", "(\u00b4Â°\u0325\u0325\u0325Ï‰Â°\u0325\u0325\u0325`)", "q(â‚\u203fâ‚)p"
	};

	private static readonly string[] sGrotesqueFaces = new string[31]
	{
		"â–“â–“â–“â˜ â–“â–“â–“", "â—‰ â–„â–„ â—‰", "â— â–€â–€ â—", "âŠ™ â–“â–“ âŠ™", "â—‰     â—‰", "â—  â—", "â—‹   â—‹", "â˜‰    â˜‰", "â—‰â•­â–“â–“â•®â—‰", "â—â•­â–ˆâ–ˆâ•®â—",
		"âŠ™â•­â–‘â–‘â•®âŠ™", "â—‰\u0334_\u0334â—‰\u0334", "â—\u0337_\u0337â—\u0337", "âŠ™\u0338_\u0338âŠ™\u0338", "â—‰â•­â•®â—‰", "â—Ð”â—", "âŠ™Ð”âŠ™", "â—ŽÐ”â—Ž", "â“_â“", "âœž_âœž",
		"â˜¢_â˜¢", "â˜£_â˜£", "â—‰â—‰â—‰â—‰â—‰", "â—â—â—â—â—", "âŠ™âŠ™âŠ™âŠ™âŠ™", "[â—‰_â—‰]", "[â—_â—]", "<â—‰_â—‰>", "â—‹   â—‹", "      ",
		"..."
	};

	private static readonly string[] sAudioPatterns = new string[10] { "â–“â–‘â–“â–‘â–“ KRRRshhh â–“â–‘â–“â–‘â–“", "~~~STATIC~~~NOISE~~~", "...bzzt...crackle...", "â™ªâ™«...............â™«â™ª", ">>>AUDIO LURE<<<", "...PLAYING SOUND...", "â—„â—„ â–ˆâ–“â–’â–‘ â–ºâ–º", "â‰‹â‰‹â‰‹ SIGNAL â‰‹â‰‹â‰‹", "### ERROR ###", "â–“â–“â–“ NO SIGNAL â–“â–“â–“" };

		private static readonly char[] sNoisePool = new char[33]
		{
			'.', ':', ';', '#', '+', '*', '~', '!',
			'_', '#', '/', '!', '?', 'O', '$', '@',
			'^', '|', 'X', '&', '%', '=', '[', ']',
			'{', '}', '(', ')', '<', '>', '-', '+',
			'`'
		};

	private static readonly string[] sCamRooms = new string[28]
	{
		"CAM 01", "CAM 02", "CAM 03", "CAM 04", "CAM 05", "CAM 06", "CAM 07", "CAM 08", "CAM 09", "CAM 10",
		"VENT CAM", "ARCADE", "OFFICE", "HALLWAY", "PARTS/SERVICE", "Electrical", "MedBay", "Security", "Reactor", "O2",
		"Admin", "Navigation", "Cafeteria", "Storage", "THE VOID", "MORGUE", "SAFE ROOM", "????"
	};

	private static readonly Color[] _colorCycle = (Color[])(object)new Color[6]
	{
		DeadRed,
		BloodDark,
		Color.black,
		DeadRed,
		GhostCyan,
		Color.grey
	};

	private static readonly Color[] _flashColors = (Color[])(object)new Color[4]
	{
		DeadRed,
		GhostCyan,
		Color.white,
		BloodDark
	};

	public VersionShowerFx(System.IntPtr ptr)
		: base(ptr)
	{
	}

	[HideFromIl2Cpp]
	public void Initialize(TextMeshPro text)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_text != (Object)null)
		{
			return;
		}
		if ((Object)(object)text == (Object)null)
		{
			Debug.LogError(InteropFix.Cast("[VersionShowerFx] TextMeshPro is null!"));
			return;
		}
		_text = text;
		_textRectTransform = ((TMP_Text)text).rectTransform;
		_baseText = ((TMP_Text)text).text;
		_modText = "Mod Menu Crew 6.0.8 | Decompiled by jakartanosia";
		_baseAnchoredPosition = _textRectTransform.anchoredPosition;
		_baseScale = ((Transform)_textRectTransform).localScale.x;
		_baseColor = ((Graphic)_text).color;
		_baseOutlineColor = InteropFix.Cast(((TMP_Text)_text).outlineColor);
		_baseOutlineWidth = ((TMP_Text)_text).outlineWidth;
		if (!_isEffectRunning)
		{
			_isEffectRunning = true;
			_nextHeavyAllowedTime = Time.time + 3f;
			if (_schedulerRoutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(_schedulerRoutine);
			}
			if (_breathingRoutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(_breathingRoutine);
			}
			_schedulerRoutine = ((MonoBehaviour)this).StartCoroutine(CollectionExtensions.WrapToIl2Cpp(GlitchScheduler()));
			_breathingRoutine = ((MonoBehaviour)this).StartCoroutine(CollectionExtensions.WrapToIl2Cpp(IdleBreathing()));
			Debug.Log(InteropFix.Cast("[VersionShowerFx] Efeitos de terror inicializados!"));
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator GlitchScheduler()
	{
		SetText(_modText);
		while (_isEffectRunning && (Object)(object)_text != (Object)null)
		{
			float num = (float)sRandom.NextDouble() * 2f + 1.5f;
			yield return (object)new WaitForSeconds(num);
			if (!_isGlitchActive && !((Object)(object)_text == (Object)null))
			{
				_isGlitchActive = true;
				bool flag = Time.time >= _nextHeavyAllowedTime;
				double num2 = sRandom.NextDouble();
				if (num2 < (double)sProbPhantom && flag)
				{
					_nextHeavyAllowedTime = Time.time + _heavyCooldownSeconds;
					yield return PhantomTierEvent();
				}
				else if (num2 < (double)(sProbPhantom + sProbMythic) && flag)
				{
					_nextHeavyAllowedTime = Time.time + _heavyCooldownSeconds;
					yield return MythicTierEvent();
				}
				else if (num2 < (double)(sProbPhantom + sProbMythic + sProbRare))
				{
					yield return RareTierEvent();
				}
				else if (num2 < 0.8500000238418579)
				{
					yield return UncommonTierEvent();
				}
				else
				{
					yield return CommonTierEvent();
				}
				_isGlitchActive = false;
				ResetVisualsToStable();
				yield return sWait2;
			}
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator IdleBreathing()
	{
		float seed = (float)sRandom.NextDouble() * 100f;
		float _nextIdleGlitchTime = Time.time + 4f + (float)sRandom.NextDouble() * 6f;
		while (_isEffectRunning && !((Object)(object)_text == (Object)null))
		{
			if (!_isGlitchActive)
			{
				RectTransform textRectTransform = _textRectTransform;
				float num = Time.time + seed;
				float num2 = 1.5f + Mathf.Sin(num * 0.3f);
				float num3 = Mathf.Sin(num * num2) * 0.08f + Mathf.Sin(num * 5f) * 0.02f;
				float num4 = Mathf.Sin(num * 0.8f) * 1.2f;
				((Transform)textRectTransform).localRotation = Quaternion.Euler(0f, 0f, num4);
				((Transform)textRectTransform).localScale = new Vector3(_baseScale + num3, _baseScale + num3, 1f);
				float num5 = Mathf.PerlinNoise(num * 0.4f, seed) * 0.15f;
				((Graphic)_text).color = new Color(_baseColor.r - num5 * 0.5f, _baseColor.g + num5, _baseColor.b - num5 * 0.3f, 1f);
				float num6 = Mathf.Sin(num * 1.2f) * 0.05f;
				((TMP_Text)_text).outlineWidth = _baseOutlineWidth + num6;
				if (Time.time >= _nextIdleGlitchTime)
				{
					_nextIdleGlitchTime = Time.time + 5f + (float)sRandom.NextDouble() * 10f;
					SetText(CorruptText(_modText, 1), isGlitching: true);
					yield return sWaitFrame;
					yield return sWaitFrame;
					SetText(_modText);
				}
			}
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator CommonTierEvent()
	{
		switch (sRandom.Next(0, 6))
		{
		case 0:
			yield return Jitter(0.3f, 1f, 1.5f);
			break;
		case 1:
			yield return TextCorruption(0.4f, 6, _modText);
			break;
		case 2:
			yield return VHSStaticBurst(0.35f);
			break;
		case 3:
			yield return GreenPulse(0.4f);
			break;
		case 4:
			yield return NoiseScroll(0.4f);
			break;
		case 5:
			yield return ColorFlash(DeadRed, 0.25f);
			break;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator UncommonTierEvent()
	{
		switch (sRandom.Next(0, 8))
		{
		case 0:
			yield return ChromaticAberration(0.5f);
			break;
		case 1:
			yield return VerticalRoll(0.3f, 40f, 3f);
			break;
		case 2:
			yield return NoiseRain(0.6f);
			break;
		case 3:
			yield return SystemWarning("RUN AWAY", 0.7f, DeadRed);
			break;
		case 4:
			yield return Wobble(0.5f, 15f, 3f);
			break;
		case 5:
			yield return CameraLabelFlash(0.6f);
			break;
		case 6:
			yield return MotionPing(0.7f);
			break;
		case 7:
			yield return NoiseHalo(0.5f);
			break;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator RareTierEvent()
	{
		switch (sRandom.Next(0, 34))
		{
		case 0:
			yield return ImpostorFlash(DeadRed);
			break;
		case 1:
			yield return TextCorruption(1f, 15, sSystemMessages[sRandom.Next(sSystemMessages.Length)]);
			break;
		case 2:
			yield return Jitter(0.8f, 4f, 5f);
			break;
		case 3:
			yield return SystemWarning("O2 DEPLETED", 1f, AlertOrange);
			break;
		case 4:
			yield return SystemWarning("REACTOR CRITICAL", 1.2f, DeadRed);
			break;
		case 5:
			yield return TrackingNoise(0.9f);
			break;
		case 6:
			yield return NoiseFrame(0.8f);
			break;
		case 7:
			yield return ColorDrain(1f);
			break;
		case 8:
			yield return TypewriterText(sSystemMessages[sRandom.Next(sSystemMessages.Length)], 0.05f);
			break;
		case 9:
			yield return CharacterSwapGlitch(0.8f);
			break;
		case 10:
			yield return BurnInPulse(0.9f);
			break;
		case 11:
			yield return CRTCrosstalk(0.8f);
			break;
		case 12:
			yield return HeartbeatHorror();
			break;
		case 13:
			yield return BloodDripEffect();
			break;
		case 14:
			yield return VentilationError();
			break;
		case 15:
			yield return StaticWall(0.8f);
			break;
		case 16:
			yield return FaceFlash();
			break;
		case 17:
			yield return AudioDistortion(0.7f);
			break;
		case 18:
			yield return SpringtrapWarning();
			break;
		case 19:
			yield return CameraDisabled();
			break;
		case 20:
			yield return ScanlineEffect(0.8f);
			break;
		case 21:
			yield return SlowCorruption();
			break;
		case 22:
			yield return BinaryHorror();
			break;
		case 23:
			yield return ScrambledMessage(sSystemMessages[sRandom.Next(sSystemMessages.Length)]);
			break;
		case 24:
			yield return ImpostorFlash(CursedPurple);
			break;
		case 25:
			yield return SystemWarning("MEMORY LEAK", 0.9f, GhostCyan);
			break;
		case 26:
			yield return FleshReveal();
			break;
		case 27:
			yield return CorpseReveal();
			break;
		case 28:
			yield return VictimsCrying();
			break;
		case 29:
			yield return EndoskeletonExposed();
			break;
		case 30:
			yield return TypewriterText(sSpringtrapLore[sRandom.Next(sSpringtrapLore.Length)], 0.06f);
			break;
		case 31:
			yield return TypewriterText(sVictimMessages[sRandom.Next(sVictimMessages.Length)], 0.05f);
			break;
		case 32:
			yield return TypewriterText(sGrotesqueMessages[sRandom.Next(sGrotesqueMessages.Length)], 0.04f);
			break;
		case 33:
			yield return SystemWarning(sGrotesqueMessages[sRandom.Next(sGrotesqueMessages.Length)], 1f, VisceralRed);
			break;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator MythicTierEvent()
	{
		switch (sRandom.Next(0, 24))
		{
		case 0:
			yield return Sequence_EmergencyMeeting();
			break;
		case 1:
			yield return Sequence_ImpostorReveal();
			break;
		case 2:
			yield return Sequence_SystemReboot();
			break;
		case 3:
			yield return Sequence_SabotageCritical();
			break;
		case 4:
			yield return Sequence_CriticalBreach();
			break;
		case 5:
			yield return Sequence_TheStare();
			break;
		case 6:
			yield return Sequence_SpringtrapApproach();
			break;
		case 7:
			yield return Sequence_PhantomJumpscare();
			break;
		case 8:
			yield return Sequence_NightmareMode();
			break;
		case 9:
			yield return Sequence_BadEnding();
			break;
		case 10:
			yield return Sequence_6AM();
			break;
		case 11:
			yield return Sequence_GoldenFreddy();
			break;
		case 12:
			yield return PowerOutage();
			break;
		case 13:
			yield return CountdownTerror();
			break;
		case 14:
			yield return Sequence_SpringLockFailure();
			break;
		case 15:
			yield return Sequence_AftonDecomposition();
			break;
		case 16:
			yield return SoulCapture();
			break;
		case 17:
			yield return Hallucination();
			break;
		case 18:
			yield return Sequence_PhantomMangle();
			break;
		case 19:
			yield return Sequence_NightmareFredbear();
			break;
		case 20:
			yield return Sequence_Dismemberment();
			break;
		case 21:
			yield return EyesInDarkness();
			break;
		case 22:
			yield return Sequence_VentilationCascade();
			break;
		case 23:
			yield return Sequence_SafeRoom();
			break;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator PhantomTierEvent()
	{
		if (sRandom.NextDouble() < 0.01)
		{
			yield return RareScreen_SpringBonnie();
			yield break;
		}
		switch (sRandom.Next(0, 18))
		{
		case 0:
			yield return PhantomAppearance("I SEE YOU", DeadRed, 0.8f);
			break;
		case 1:
			yield return PhantomAppearance("BEHIND YOU", Color.black, 0.7f);
			break;
		case 2:
			yield return PhantomAppearance("IT'S HERE", DeadRed, 0.8f);
			break;
		case 3:
			yield return CrewmateColorCycle();
			break;
		case 4:
			yield return PhantomSignal(1f);
			break;
		case 5:
			yield return TerrifyingSequence();
			break;
		case 6:
			yield return PhantomAppearance("IT'S ME", DeadRed, 0.5f);
			break;
		case 7:
			yield return PhantomAppearance("SAVE THEM", GhostCyan, 0.6f);
			break;
		case 8:
			yield return Sequence_SpringtrapStare();
			break;
		case 9:
			yield return Sequence_PurpleGuy();
			break;
		case 10:
			yield return PhantomAppearance(sSpringtrapLore[sRandom.Next(sSpringtrapLore.Length)], RottenPurple, 0.7f);
			break;
		case 11:
			yield return PhantomAppearance(sVictimMessages[sRandom.Next(sVictimMessages.Length)], GhostCyan, 0.6f);
			break;
		case 12:
			yield return PhantomAppearance(sGrotesqueMessages[sRandom.Next(sGrotesqueMessages.Length)], VisceralRed, 0.8f);
			break;
		case 13:
			yield return PhantomAppearance(sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)], OxidizedGreen, 0.5f);
			break;
		case 14:
			yield return PhantomAppearance("SPRING LOCK", RottenPurple, 1f);
			break;
		case 15:
			yield return PhantomAppearance("GIVE LIFE", BloodDark, 0.9f);
			break;
		case 16:
			yield return Sequence_PhantomFreddyWalkBy();
			break;
		case 17:
			yield return Sequence_FazbearsFright();
			break;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_TheStare()
	{
		yield return SystemWarning("DON'T BLINK", 1f, Color.white);
		yield return sWait1;
		SetText("", isGlitching: true);
		yield return sWait1;
		((Graphic)_text).color = DeadRed;
		SetText("O_O", isGlitching: true);
		yield return sWait5;
		yield return Jitter(0.5f, 6f, 5f);
		yield return ImpostorFlash(Color.black);
	}

	[HideFromIl2Cpp]
	private IEnumerator HeartbeatHorror()
	{
		for (int i = 0; i < 4; i++)
		{
			((Graphic)_text).color = DeadRed;
			((TMP_Text)_text).outlineColor = InteropFix.Cast(BloodDark);
			((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.4f, _baseScale * 1.4f, 1f);
			SetText("THUMP", isGlitching: true);
			yield return sWait05;
			((Transform)_textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
			SetText(_modText, isGlitching: true);
			yield return sWait08;
			((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.2f, _baseScale * 1.2f, 1f);
			((Graphic)_text).color = BloodDark;
			yield return sWait05;
			((Transform)_textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
			yield return sWait6;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator BloodDripEffect()
	{
		((Graphic)_text).color = DeadRed;
		string current = _modText;
		for (int i = 0; i < current.Length; i++)
		{
			_textBuilder.Clear().Append(current.Substring(0, i)).Append("<color=red>â–ˆ</color>")
				.Append(current.Substring(i + 1));
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait05;
		}
		yield return ImpostorFlash(DeadRed);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_EmergencyMeeting()
	{
		SetText("", isGlitching: true);
		yield return sWait15;
		yield return TypewriterText("WHO IS THE IMPOSTOR?", 0.06f);
		yield return sWait2;
		yield return Jitter(0.5f, 5f, 5f);
		SetText("", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_ImpostorReveal()
	{
		yield return TypewriterText("THE IMPOSTOR IS...", 0.08f);
		yield return sWait2;
		yield return Jitter(0.4f, 15f, 15f);
		yield return ImpostorFlash(DeadRed);
		SetText(CorruptText("IMPOSTOR", 10), isGlitching: true);
		yield return sWait15;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_SystemReboot()
	{
		yield return SystemWarning("SYSTEM FAILURE", 1.5f, DeadRed);
		SetText("", isGlitching: true);
		yield return sWait10;
		yield return TypewriterText("REBOOTING...", 0.1f);
		yield return sWait2;
		SetText(CorruptText("............", 12), isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_SabotageCritical()
	{
		float duration = 1.5f;
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = 1f - (endTime - Time.time) / duration;
			Color color = ((Time.frameCount % 8 < 4) ? DeadRed : Color.yellow);
			((Graphic)_text).color = color;
			SetText((sRandom.Next(0, 2) == 1) ? "SABOTAGE" : CorruptText("SABOTAGE", (int)(num * 15f)), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_CriticalBreach()
	{
		yield return IntensiveGlitchSequence(1.2f);
		yield return sWait15;
		yield return SystemWarning("BREACH", 1.2f, DeadRed);
		yield return sWait15;
		yield return TypewriterText("INTRUDER", 0.08f);
	}

	[HideFromIl2Cpp]
	private IEnumerator PhantomAppearance(string name, Color color, float duration)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((Graphic)_text).color = color;
		((TMP_Text)_text).outlineColor = InteropFix.Cast(color * 0.6f);
		((TMP_Text)_text).outlineWidth = 0.4f;
		string phantomText = CorruptText(name, 5);
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			SetText(phantomText, isGlitching: true);
			yield return sWait08;
			SetText("", isGlitching: true);
			yield return sWait08;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator CrewmateColorCycle()
	{
		int iterations = _colorCycle.Length * 2;
		for (int i = 0; i < iterations; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = _colorCycle[i % _colorCycle.Length];
			((TMP_Text)_text).outlineColor = InteropFix.Cast(_colorCycle[i % _colorCycle.Length] * 0.7f);
			SetText(CorruptText(_modText, 3), isGlitching: true);
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator TerrifyingSequence()
	{
		for (int i = 0; i < 3; i++)
		{
			yield return Jitter(0.4f + (float)i * 0.1f, 3f + (float)i * 1.5f, 6f + (float)i * 3f);
			yield return sWait15;
			yield return ChromaticAberration(0.35f);
			yield return sWait15;
		}
		yield return PhantomAppearance("IT'S HERE", DeadRed, 0.6f);
		yield return sWait15;
		yield return Jitter(1f, 8f, 25f);
	}

	[HideFromIl2Cpp]
	private void SetText(string content, bool isGlitching = false)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_text == (Object)null))
		{
			string value = ColorUtility.ToHtmlStringRGB(_baseColor);
			string value2 = (isGlitching ? content : _modText);
			_textBuilder.Clear().Append(_baseText).Append(" <color=#")
				.Append(value)
				.Append("><b><i>")
				.Append(value2)
				.Append("</i></b></color>");
			((TMP_Text)_text).text = _textBuilder.ToString();
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator TextCorruption(float duration, int intensity, string baseText)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			SetText(CorruptText(baseText, intensity), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator Jitter(float duration, float posRange, float rotRange)
	{
		float endTime = Time.time + duration;
		RectTransform rt = _textRectTransform;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = ((float)sRandom.NextDouble() - 0.5f) * posRange;
			float num2 = ((float)sRandom.NextDouble() - 0.5f) * posRange;
			float num3 = ((float)sRandom.NextDouble() - 0.5f) * 2f * rotRange;
			rt.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num, _baseAnchoredPosition.y + num2);
			((Transform)rt).localRotation = Quaternion.Euler(0f, 0f, num3);
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator ChromaticAberration(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			_textBuilder.Clear().Append(_modText).Append("\n<color=#FF0000>")
				.Append(_modText)
				.Append("</color>\n<color=#00FF00>")
				.Append(_modText)
				.Append("</color>");
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator VerticalRoll(float duration, float frequency, float amplitude)
	{
		float endTime = Time.time + duration;
		RectTransform rt = _textRectTransform;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = Mathf.Sin(Time.time * frequency) * amplitude;
			rt.anchoredPosition = new Vector2(_baseAnchoredPosition.x, _baseAnchoredPosition.y + num);
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator ImpostorFlash(Color color)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_text == (Object)null))
		{
			((Graphic)_text).color = color;
			((TMP_Text)_text).outlineColor = InteropFix.Cast(color * 0.7f);
			((TMP_Text)_text).outlineWidth = 0.45f;
			yield return sWait1;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator SystemWarning(string message, float duration, Color color)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			((Graphic)_text).color = color;
			((TMP_Text)_text).outlineColor = InteropFix.Cast(color * 0.6f);
			SetText((sRandom.Next(0, 2) == 1) ? message : CorruptText(message, 3), isGlitching: true);
			yield return sWait08;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator TrackingNoise(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			_textBuilder.Clear().Append(_modText).Append("\n")
				.Append(CorruptText("â–ˆ â–‘ â–ˆ â–‘ â–ˆ", 6));
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator ColorDrain(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = (endTime - Time.time) / duration;
			((Graphic)_text).color = Color.Lerp(new Color(0.1f, 0.1f, 0.1f), _baseColor, num);
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator TypewriterText(string text, float delayPerChar)
	{
		WaitForSeconds val = ((delayPerChar <= 0.02f) ? sWait025 : ((delayPerChar <= 0.04f) ? sWait04 : ((delayPerChar <= 0.06f) ? sWait05 : ((delayPerChar <= 0.09f) ? sWait08 : ((!(delayPerChar <= 0.12f)) ? sWait15 : sWait1)))));
		object wait = val;
		for (int i = 0; i <= text.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			_textBuilder.Clear().Append(text, 0, i);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return wait;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator CharacterSwapGlitch(float duration)
	{
		float endTime = Time.time + duration;
		char[] chars = _modText.ToCharArray();
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			int num = sRandom.Next(chars.Length);
			int num2 = sRandom.Next(chars.Length);
			char c = chars[num];
			chars[num] = chars[num2];
			chars[num2] = c;
			_textBuilder.Clear().Append(chars);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator VHSStaticBurst(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
				_textBuilder.Clear().Append(_modText).Append("\n")
					.Append('#', sRandom.Next(8, 16));
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait025;
			SetText(_modText, isGlitching: true);
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator CRTCrosstalk(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			_textBuilder.Clear().Append(_modText).Append("\n<color=#FFFF00>")
				.Append(_modText)
				.Append("</color>");
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator GreenPulse(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = Mathf.PingPong(Time.time * 2f, 1f);
			((Graphic)_text).color = Color.Lerp(_baseColor, FnafGreen, num);
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator NoiseScroll(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string value = RandomNoise(20);
			_textBuilder.Clear().Append(_modText).Append("\n")
				.Append(value);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator NoiseRain(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string value = RandomNoise(18);
			string value2 = RandomNoise(18);
			_textBuilder.Clear().Append(_modText).Append("\n")
				.Append(value)
				.Append("\n")
				.Append(value2);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator NoiseHalo(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string value = RandomNoise(10);
			_textBuilder.Clear().Append(_modText).Append(" ")
				.Append(value);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator NoiseFrame(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string value = RandomNoise(20);
			string value2 = RandomNoise(20);
			_textBuilder.Clear().Append(value).Append("\n")
				.Append(_modText)
				.Append("\n")
				.Append(value2);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator CameraLabelFlash(float duration)
	{
		float endTime = Time.time + duration;
		int cam = sRandom.Next(1, 9);
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			_textBuilder.Clear().Append("CAM ").Append(cam.ToString("00"));
			SetText(_textBuilder.ToString(), isGlitching: true);
			((Graphic)_text).color = Color.cyan;
			cam = cam % 8 + 1;
			yield return sWait1;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator BurnInPulse(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = Mathf.PingPong(Time.time * 2f, 1f);
			((TMP_Text)_text).outlineWidth = Mathf.Lerp(_baseOutlineWidth, 0.5f, num);
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator Wobble(float duration, float frequency, float amplitude)
	{
		float endTime = Time.time + duration;
		RectTransform rt = _textRectTransform;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = Time.time * frequency;
			float num2 = Mathf.Sin(num) * amplitude;
			float num3 = Mathf.Cos(num * 0.5f) * amplitude * 0.5f;
			rt.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num2, _baseAnchoredPosition.y + num3);
			yield return sWaitFrame;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator MotionPing(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string value = RandomRoom();
			((Graphic)_text).color = Color.yellow;
			_textBuilder.Clear().Append("MOTION - ").Append(value);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait1;
			SetText(_modText, isGlitching: true);
			yield return sWait1;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator PhantomSignal(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string content = RandomNoise(3) + " SIGNAL " + RandomNoise(3);
			SetText(content, isGlitching: true);
			((Graphic)_text).color = GhostCyan;
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator ColorFlash(Color color, float duration)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_text == (Object)null))
		{
			((Graphic)_text).color = color;
			((TMP_Text)_text).outlineColor = InteropFix.Cast(color);
			yield return (duration <= 0.06f) ? sWait05 : ((duration <= 0.12f) ? sWait1 : ((duration <= 0.2f) ? sWait15 : ((duration <= 0.3f) ? sWait2 : ((!(duration <= 0.45f)) ? sWait5 : sWait3))));
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator IntensiveGlitchSequence(float duration)
	{
		float endTime = Time.time + duration;
		RectTransform rt = _textRectTransform;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			float num = 1f - (endTime - Time.time) / duration;
			float num2 = Mathf.Lerp(1f, 2.5f, num);
			SetText(CorruptText(_modText, (int)(num2 * 5f)), isGlitching: true);
			float num3 = ((float)sRandom.NextDouble() - 0.5f) * num2 * 2f;
			float num4 = ((float)sRandom.NextDouble() - 0.5f) * num2 * 2f;
			rt.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num3, _baseAnchoredPosition.y + num4);
			((Graphic)_text).color = _flashColors[(int)(num * 4f) % 4];
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator VentilationError()
	{
		((Graphic)_text).color = AlertOrange;
		int count = Math.Min(5, sVentilationMessages.Length);
		for (int i = 0; i < count; i++)
		{
			SetText(sVentilationMessages[sRandom.Next(sVentilationMessages.Length)], isGlitching: true);
			yield return sWait2;
			yield return Jitter(0.15f, 2f, 3f);
		}
		((Graphic)_text).color = DeadRed;
		SetText("VENT OPEN", isGlitching: true);
		yield return sWait3;
	}

	[HideFromIl2Cpp]
	private IEnumerator StaticWall(float duration)
	{
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			_textBuilder.Clear();
			for (int i = 0; i < 3; i++)
			{
				_textBuilder.Append(RandomNoise(25)).Append("\n");
			}
			SetText(_textBuilder.ToString(), isGlitching: true);
			((Graphic)_text).color = Color.Lerp(Color.white, Color.gray, (float)sRandom.NextDouble());
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator FaceFlash()
	{
		string face = sCreepyFaces[sRandom.Next(sCreepyFaces.Length)];
		SetText("", isGlitching: true);
		yield return sWait1;
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2f, _baseScale * 2f, 1f);
		SetText(face, isGlitching: true);
		yield return sWait08;
		yield return Jitter(0.2f, 5f, 10f);
		SetText("", isGlitching: true);
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
		yield return sWait1;
	}

	[HideFromIl2Cpp]
	private IEnumerator AudioDistortion(float duration)
	{
		string[] audioGlitches = new string[5] { "â–“â–“â–“AUDIOâ–“â–“â–“", "â–ˆERRORâ–ˆ", "###SND###", "~~~STATIC~~~", ">>>NOISE<<<" };
		float endTime = Time.time + duration;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			string input = audioGlitches[sRandom.Next(audioGlitches.Length)];
			((Graphic)_text).color = Color.Lerp(GhostCyan, Color.white, (float)sRandom.NextDouble());
			SetText(CorruptText(input, 4), isGlitching: true);
			yield return sWait04;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator SpringtrapWarning()
	{
		yield return TypewriterText("HE'S HERE", 0.1f);
		yield return sWait15;
		for (int i = 0; i < 3; i++)
		{
			((Graphic)_text).color = ((i % 2 == 0) ? DeadRed : Color.black);
			yield return sWait08;
		}
		yield return Jitter(0.5f, 4f, 8f);
		SetText("", isGlitching: true);
		yield return sWait1;
		((Graphic)_text).color = DeadRed;
		SetText("TOO LATE", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator CameraDisabled()
	{
		string content = sCamRooms[sRandom.Next(sCamRooms.Length)];
		((Graphic)_text).color = Color.cyan;
		SetText(content, isGlitching: true);
		yield return sWait2;
		for (int i = 0; i < 5; i++)
		{
			SetText(RandomNoise(15), isGlitching: true);
			yield return sWait05;
		}
		((Graphic)_text).color = DeadRed;
		SetText("SIGNAL LOST", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_SpringtrapApproach()
	{
		string[] distances = new string[5] { "CAM 10", "CAM 09", "CAM 08", "HALLWAY", "DOOR" };
		for (int i = 0; i < distances.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			for (int s = 0; s < 2; s++)
			{
				((Graphic)_text).color = StaticGray;
				SetText(RandomNoise(15), isGlitching: true);
				yield return sWait025;
			}
			((Graphic)_text).color = Color.Lerp(OxidizedGreen, DeadRed, (float)i / (float)distances.Length);
			SetText(distances[i], isGlitching: true);
			yield return (object)new WaitForSeconds(2.5f - (float)i * 0.3f);
			yield return Jitter(0.1f + (float)i * 0.15f, 1f + (float)i, 2f + (float)i);
		}
		SetText("", isGlitching: true);
		yield return sWait10;
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.8f, _baseScale * 1.8f, 1f);
		SetText("HE'S HERE", isGlitching: true);
		yield return Jitter(0.5f, 6f, 10f);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_PhantomJumpscare()
	{
		SetText("", isGlitching: true);
		yield return sWait15;
		for (int i = 0; i < 4; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = Color.Lerp(StaticGray, OxidizedGreen, (float)i / 3f);
			SetText(RandomNoise(20), isGlitching: true);
			yield return sWait08;
		}
		((Graphic)_text).color = OxidizedGreen;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2.5f, _baseScale * 2.5f, 1f);
		string content = sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)];
		SetText(content, isGlitching: true);
		yield return Jitter(0.4f, 10f, 18f);
		SetText("", isGlitching: true);
		((Graphic)_text).color = Color.black;
		yield return sWait15;
		((Graphic)_text).color = AlertOrange;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
		SetText("VENT: ERROR", isGlitching: true);
		yield return sWait10;
		((Graphic)_text).color = FnafGreen;
		yield return TypewriterText("REBOOTING...", 0.05f);
		yield return sWait5;
		SetText("VENT: ONLINE", isGlitching: true);
		yield return sWait10;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_NightmareMode()
	{
		float num = 2f;
		float endTime = Time.time + num;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			((Graphic)_text).color = new Color((float)sRandom.NextDouble(), (float)sRandom.NextDouble(), (float)sRandom.NextDouble());
			string input = sSystemMessages[sRandom.Next(sSystemMessages.Length)];
			SetText(CorruptText(input, 10), isGlitching: true);
			float num2 = ((float)sRandom.NextDouble() - 0.5f) * 6f;
			float num3 = ((float)sRandom.NextDouble() - 0.5f) * 6f;
			_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num2, _baseAnchoredPosition.y + num3);
			yield return sWait025;
		}
		((Graphic)_text).color = DeadRed;
		SetText("NIGHTMARE", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_BadEnding()
	{
		yield return TypewriterText("GAME OVER", 0.15f);
		yield return sWait2;
		((Graphic)_text).color = BloodDark;
		yield return TypewriterText("BAD ENDING", 0.1f);
		yield return sWait15;
		for (float t = 0f; t < 1f; t += 0.1f)
		{
			((Graphic)_text).color = Color.Lerp(BloodDark, DeadRed, t);
			yield return sWait1;
		}
		yield return StaticWall(0.5f);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_SpringtrapStare()
	{
		SetText("", isGlitching: true);
		yield return sWait1;
		((Graphic)_text).color = Color.white;
		SetText("â—‰   â—‰", isGlitching: true);
		yield return sWait2;
		for (float scale = 1f; scale < 2f; scale += 0.1f)
		{
			((Transform)_textRectTransform).localScale = new Vector3(_baseScale * scale, _baseScale * scale, 1f);
			yield return sWait05;
		}
		((Graphic)_text).color = DeadRed;
		SetText("I SEE YOU", isGlitching: true);
		yield return Jitter(0.5f, 5f, 10f);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_PurpleGuy()
	{
		Color purpleGuy = new Color(0.5f, 0f, 0.5f);
		((Graphic)_text).color = purpleGuy;
		yield return TypewriterText("I ALWAYS COME BACK", 0.08f);
		yield return sWait2;
		for (int i = 0; i < 5; i++)
		{
			((Graphic)_text).color = ((i % 2 == 0) ? purpleGuy : Color.black);
			SetText(CorruptText("I ALWAYS COME BACK", i * 2), isGlitching: true);
			yield return sWait1;
		}
		SetText("", isGlitching: true);
		yield return sWait1;
		((Graphic)_text).color = DeadRed;
		SetText("...", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator ScanlineEffect(float duration)
	{
		float endTime = Time.time + duration;
		int scanPos = 0;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			_textBuilder.Clear();
			for (int i = 0; i < 4; i++)
			{
				if (i == scanPos % 4)
				{
					_textBuilder.Append("<color=#FFFFFF>â–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆâ–ˆ</color>\n");
				}
				else
				{
					_textBuilder.Append("<color=#333333>â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘â–‘</color>\n");
				}
			}
			SetText(_textBuilder.ToString(), isGlitching: true);
			scanPos++;
			yield return sWait025;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator CountdownTerror()
	{
		for (int i = 5; i >= 0; i--)
		{
			float num = 1f - (float)i / 5f;
			((Graphic)_text).color = Color.Lerp(AlertOrange, DeadRed, num);
			float num2 = _baseScale * (1f + 0.1f * (float)(5 - i));
			((Transform)_textRectTransform).localScale = new Vector3(num2, num2, 1f);
			SetText((i > 0) ? i.ToString() : "TIME'S UP", isGlitching: true);
			yield return Jitter(0.1f, 1f + (float)(5 - i), 2f);
			yield return sWait07;
		}
		yield return ImpostorFlash(DeadRed);
	}

	[HideFromIl2Cpp]
	private IEnumerator ScrambledMessage(string secret)
	{
		if (string.IsNullOrEmpty(secret))
		{
			yield break;
		}
		int len = secret.Length;
		char[] display = new char[len];
		bool[] revealed = new bool[len];
		for (int i = 0; i < len; i++)
		{
			display[i] = sNoisePool[sRandom.Next(sNoisePool.Length)];
		}
		for (int pass = 0; pass < len + 5; pass++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			if (pass < len && sRandom.NextDouble() > 0.3)
			{
				int num = sRandom.Next(len);
				if (!revealed[num])
				{
					revealed[num] = true;
					display[num] = secret[num];
				}
			}
			for (int j = 0; j < len; j++)
			{
				if (!revealed[j])
				{
					display[j] = sNoisePool[sRandom.Next(sNoisePool.Length)];
				}
			}
			((Graphic)_text).color = Color.Lerp(GhostCyan, DeadRed, (float)pass / (float)(len + 5));
			_textBuilder.Clear().Append(display);
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait08;
		}
		SetText(secret, isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator PowerOutage()
	{
		((Graphic)_text).color = AlertOrange;
		SetText("POWER: 1%", isGlitching: true);
		yield return sWait2;
		for (int i = 0; i < 6; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = ((i % 2 == 0) ? Color.white : Color.black);
			SetText((i % 2 == 0) ? _modText : "", isGlitching: true);
			yield return (sRandom.Next(0, 2) == 0) ? sWait08 : sWait1;
		}
		((Graphic)_text).color = Color.black;
		SetText("", isGlitching: true);
		yield return sWait15s;
		((Graphic)_text).color = Color.white;
		SetText("â—‰     â—‰", isGlitching: true);
		yield return sWait20s;
		((Graphic)_text).color = DeadRed;
		SetText("IT'S ME", isGlitching: true);
		yield return sWait08;
	}

	[HideFromIl2Cpp]
	private IEnumerator BinaryHorror()
	{
		string[] array = new string[3] { "01001000 01000101 01001100 01010000", "01000100 01000101 01000001 01000100", "01010010 01010101 01001110" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			((Graphic)_text).color = FnafGreen;
			yield return TypewriterText(text, 0.02f);
			yield return sWait1;
		}
		((Graphic)_text).color = DeadRed;
		SetText("HELP. DEAD. RUN.", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator SlowCorruption()
	{
		string text = _modText;
		int len = text.Length;
		for (int corruption = 0; corruption <= len; corruption++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = Color.Lerp(_baseColor, DeadRed, (float)corruption / (float)len);
			SetText(CorruptText(text, corruption), isGlitching: true);
			yield return sWait1;
		}
		yield return sWait2;
		((Graphic)_text).color = DeadRed;
		SetText("CORRUPTED", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_6AM()
	{
		SetText("", isGlitching: true);
		yield return sWait15;
		for (int i = 0; i < 4; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			SetText(RandomNoise(20), isGlitching: true);
			yield return sWait05;
		}
		((Graphic)_text).color = GoldenYellow;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.8f, _baseScale * 1.8f, 1f);
		SetText("6 AM", isGlitching: true);
		yield return sWait20s;
		for (float t = 0f; t < 1f; t += 0.1f)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = Color.Lerp(GoldenYellow, Color.white, t);
			yield return sWait1;
		}
		SetText("YOU SURVIVED...", isGlitching: true);
		yield return sWait2;
		((Graphic)_text).color = DeadRed;
		SetText("THIS TIME", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_GoldenFreddy()
	{
		SetText("", isGlitching: true);
		yield return sWait15s;
		((Graphic)_text).color = GoldenYellow;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2.5f, _baseScale * 2.5f, 1f);
		SetText("IT'S ME", isGlitching: true);
		yield return sWait05;
		for (int i = 0; i < 8; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = ((i % 2 == 0) ? GoldenYellow : Color.black);
			SetText(CorruptText("IT'S ME", i), isGlitching: true);
			float num = ((float)sRandom.NextDouble() - 0.5f) * 10f;
			float num2 = ((float)sRandom.NextDouble() - 0.5f) * 10f;
			_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num, _baseAnchoredPosition.y + num2);
			yield return sWait025;
		}
		((Graphic)_text).color = Color.white;
		SetText("FATAL ERROR", isGlitching: true);
		yield return sWait2;
		yield return StaticWall(0.5f);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_SpringLockFailure()
	{
		((Graphic)_text).color = RottenPurple;
		yield return TypewriterText("SPRING LOCKS...", 0.08f);
		yield return sWait1;
		for (int i = 0; i < 5; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			string input = i switch
			{
				0 => "* CRACK *", 
				1 => "* SNAP! *", 
				2 => "* CRUNCH *", 
				3 => "AAAHHH!!!", 
				_ => "* SQUELCH *", 
			};
			((Graphic)_text).color = Color.Lerp(RottenPurple, VisceralRed, (float)i / 4f);
			SetText(CorruptText(input, i), isGlitching: true);
			yield return Jitter(0.15f, 2f + (float)i, 5f + (float)(i * 2));
			yield return sWait08;
		}
		((Graphic)_text).color = DeadRed;
		for (int i = 0; i < 8; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			_textBuilder.Clear();
			for (int j = 0; j < i; j++)
			{
				_textBuilder.Append("â–ˆ");
			}
			_textBuilder.Append("â–“â–“â–“ BLOOD â–“â–“â–“");
			SetText(_textBuilder.ToString(), isGlitching: true);
			yield return sWait1;
		}
		SetText("", isGlitching: true);
		yield return sWait15s;
		((Graphic)_text).color = BloodDark;
		SetText("...", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator FleshReveal()
	{
		string text = _modText;
		int len = text.Length;
		for (int layer = 0; layer < 4; layer++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			Color color = (Color)(layer switch
			{
				0 => _baseColor, 
				1 => DecayBrown, 
				2 => FleshPink, 
				_ => VisceralRed, 
			});
			((Graphic)_text).color = color;
			for (int i = 0; i < len; i++)
			{
				if (!((Object)(object)_text != (Object)null))
				{
					break;
				}
					char value = layer switch
					{
						1 => '#',
						2 => '.',
						3 => '@',
						_ => text[i],
					};
				_textBuilder.Clear().Append(text.Substring(0, i)).Append(value)
					.Append(text.Substring(i + 1));
				SetText(_textBuilder.ToString(), isGlitching: true);
				yield return sWait025;
			}
			yield return sWait15;
		}
		((Graphic)_text).color = VisceralRed;
		SetText("FLESH EXPOSED", isGlitching: true);
		yield return Jitter(0.3f, 4f, 8f);
	}

	[HideFromIl2Cpp]
	private IEnumerator EyesInDarkness()
	{
		SetText("", isGlitching: true);
		((Graphic)_text).color = Color.black;
		yield return sWait1;
		string[] array = new string[5]
		{
			"â—‰          â—‰",
			"â—‰ â—‰      â—‰ â—‰",
			"â—‰ â—‰ â—‰  â—‰ â—‰ â—‰",
			"â—‰â—‰â—‰â—‰â—‰â—‰â—‰â—‰â—‰â—‰â—‰",
			sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)]
		};
		string[] array2 = array;
		foreach (string eyes in array2)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			for (int f = 0; f < 3; f++)
			{
				((Graphic)_text).color = ((f % 2 == 0) ? Color.white : StaticGray);
				SetText(eyes, isGlitching: true);
				yield return sWait08;
			}
			SetText("", isGlitching: true);
			yield return sWait3;
		}
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2f, _baseScale * 2f, 1f);
		SetText("WE SEE YOU", isGlitching: true);
		yield return Jitter(0.5f, 6f, 12f);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_AftonDecomposition()
	{
		string[] decompositionStages = new string[6] { "YEAR 1: FRESH", "YEAR 5: DECAY", "YEAR 10: BONES", "YEAR 20: DUST", "YEAR 30: FUSED", "NOW: ETERNAL" };
		Color[] stageColors = (Color[])(object)new Color[6] { FleshPink, DecayBrown, BoneWhite, StaticGray, OxidizedGreen, RottenPurple };
		for (int i = 0; i < decompositionStages.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = stageColors[i];
			yield return TypewriterText(decompositionStages[i], 0.05f);
			yield return Jitter(0.2f, 1f + (float)i, 3f);
			yield return sWait15;
		}
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.5f, _baseScale * 1.5f, 1f);
		SetText("I AM STILL HERE", isGlitching: true);
		yield return sWait20s;
	}

	[HideFromIl2Cpp]
	private IEnumerator SoulCapture()
	{
		((Graphic)_text).color = GhostCyan;
		SetText("âœ§ SOUL âœ§", isGlitching: true);
		yield return sWait1;
		string[] spiralFrames = new string[14]
		{
			"     âœ§     ", "    âœ§ âœ§    ", "   âœ§   âœ§   ", "  âœ§     âœ§  ", " âœ§       âœ§ ", "âœ§         âœ§", " â˜…       â˜… ", "  â˜…     â˜…  ", "   â˜…   â˜…   ", "    â˜… â˜…    ",
			"     â˜…     ", "     â–“     ", "     â–ˆ     ", "           "
		};
		for (int i = 0; i < spiralFrames.Length; i++)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			((Graphic)_text).color = Color.Lerp(GhostCyan, RottenPurple, (float)i / (float)spiralFrames.Length);
			SetText(spiralFrames[i], isGlitching: true);
			yield return sWait08;
		}
		((Graphic)_text).color = RottenPurple;
		SetText("CAPTURED", isGlitching: true);
		yield return sWait2;
		((Graphic)_text).color = DeadRed;
		SetText("GIVE LIFE", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Hallucination()
	{
		float num = 2.5f;
		float endTime = Time.time + num;
		while (Time.time < endTime && (Object)(object)_text != (Object)null)
		{
			((Graphic)_text).color = new Color((float)sRandom.NextDouble(), (float)sRandom.NextDouble(), (float)sRandom.NextDouble());
			SetText(CorruptText(sRandom.Next(0, 5) switch
			{
				0 => sSystemMessages[sRandom.Next(sSystemMessages.Length)], 
				1 => sSpringtrapLore[sRandom.Next(sSpringtrapLore.Length)], 
				2 => sVictimMessages[sRandom.Next(sVictimMessages.Length)], 
				3 => sGrotesqueMessages[sRandom.Next(sGrotesqueMessages.Length)], 
				_ => sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)], 
			}, sRandom.Next(3, 8)), isGlitching: true);
			float num2 = ((float)sRandom.NextDouble() - 0.5f) * 8f;
			float num3 = ((float)sRandom.NextDouble() - 0.5f) * 8f;
			_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num2, _baseAnchoredPosition.y + num3);
			((Transform)_textRectTransform).localRotation = Quaternion.Euler(0f, 0f, ((float)sRandom.NextDouble() - 0.5f) * 20f);
			float num4 = _baseScale * (0.8f + (float)sRandom.NextDouble() * 0.8f);
			((Transform)_textRectTransform).localScale = new Vector3(num4, num4, 1f);
			yield return sWait025;
		}
		SetText("", isGlitching: true);
		yield return sWait1;
		((Graphic)_text).color = DeadRed;
		SetText("WAKE UP", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_PhantomMangle()
	{
		((Graphic)_text).color = OxidizedGreen;
		for (int i = 0; i < 6; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			SetText(sAudioPatterns[sRandom.Next(sAudioPatterns.Length)], isGlitching: true);
			yield return sWait15;
		}
		string[] mangleFrames = new string[7] { "â–‘â–‘â–‘â–‘ M â–‘â–‘â–‘â–‘", "â–‘â–‘â–‘ MA â–‘â–‘â–‘", "â–‘â–‘ MAN â–‘â–‘", "â–‘ MANG â–‘", "MANGLE", "   MANGLE   ", " â–€â–€MANGLEâ–€â–€ " };
		for (int i = 0; i < mangleFrames.Length; i++)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			((Graphic)_text).color = Color.Lerp(OxidizedGreen, DeadRed, (float)i / (float)mangleFrames.Length);
			SetText(mangleFrames[i], isGlitching: true);
			yield return sWait08;
		}
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2f, _baseScale * 2f, 1f);
		SetText(sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)], isGlitching: true);
		yield return Jitter(0.3f, 5f, 10f);
		yield return StaticWall(0.4f);
	}

	[HideFromIl2Cpp]
	private IEnumerator CorpseReveal()
	{
		string[] corpseArt = new string[6] { "     â˜      ", "   / â˜  \\   ", "  â”€/ â˜  \\â”€  ", " â”€â”¼/ â˜  \\â”¼â”€ ", "â”€â”¼â”¼/ â˜  \\â”¼â”¼â”€", "CORPSE FOUND" };
		((Graphic)_text).color = DecayBrown;
		for (int i = 0; i < corpseArt.Length; i++)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			SetText(corpseArt[i], isGlitching: true);
			((Graphic)_text).color = Color.Lerp(DecayBrown, BoneWhite, (float)i / (float)corpseArt.Length);
			yield return sWait4;
		}
		yield return ImpostorFlash(DeadRed);
		((Graphic)_text).color = DeadRed;
		SetText("BODY REPORTED", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_NightmareFredbear()
	{
		SetText("", isGlitching: true);
		yield return sWait10;
		string[] whispers = new string[4] { "...", "...do you...", "...hear me...", "...I'm here..." };
		((Graphic)_text).color = BloodDark;
		for (int i = 0; i < whispers.Length; i++)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			SetText(whispers[i], isGlitching: true);
			yield return sWait6;
		}
		SetText("", isGlitching: true);
		yield return sWait15s;
		((Graphic)_text).color = GoldenYellow;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 3f, _baseScale * 3f, 1f);
		SetText("â–€â–€â–€ TEETH â–€â–€â–€", isGlitching: true);
		yield return sWait05;
		for (int i = 0; i < 10; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = ((i % 2 == 0) ? GoldenYellow : DeadRed);
			string input = sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)];
			SetText(CorruptText(input, i), isGlitching: true);
			float num = ((float)sRandom.NextDouble() - 0.5f) * 12f;
			float num2 = ((float)sRandom.NextDouble() - 0.5f) * 12f;
			_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num, _baseAnchoredPosition.y + num2);
			yield return sWait025;
		}
		SetText("", isGlitching: true);
		((Graphic)_text).color = Color.black;
		yield return sWait20s;
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
		SetText("NIGHTMARE", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_Dismemberment()
	{
		string text = _modText;
		int len = text.Length;
		((Graphic)_text).color = _baseColor;
		SetText(text, isGlitching: true);
		yield return sWait1;
		int i = len - 1;
		while (i >= 0 && (Object)(object)_text != (Object)null)
		{
			for (int drop = 0; drop < 3; drop++)
			{
				_textBuilder.Clear();
				_textBuilder.Append(text.Substring(0, i));
				for (int j = 0; j < drop; j++)
				{
					_textBuilder.Append(" ");
				}
				_textBuilder.Append(sNoisePool[sRandom.Next(sNoisePool.Length)]);
				SetText(_textBuilder.ToString(), isGlitching: true);
				yield return sWait025;
			}
			text = text.Substring(0, i);
			((Graphic)_text).color = Color.Lerp(_baseColor, DeadRed, 1f - (float)i / (float)len);
			i--;
		}
		((Graphic)_text).color = DeadRed;
		SetText("â–“â–’â–‘ DESTROYED â–‘â–’â–“", isGlitching: true);
		yield return Jitter(0.4f, 4f, 8f);
	}

	[HideFromIl2Cpp]
	private IEnumerator RareScreen_SpringBonnie()
	{
		SetText("", isGlitching: true);
		yield return sWait1;
		string[] reveal = new string[11]
		{
			"   \ud83d\udc30   ", " SPRING ", " BONNIE ", "   ...  ", "  \\\ud83d\udc30/  ", "REMOVING", "  HEAD  ", "   ...  ", "  â˜  â˜    ", " AFTON  ",
			" INSIDE "
		};
		((Graphic)_text).color = GoldenYellow;
		for (int i = 0; i < reveal.Length; i++)
		{
			if ((Object)(object)_text == (Object)null)
			{
				yield break;
			}
			SetText(reveal[i], isGlitching: true);
			if (i > reveal.Length / 2)
			{
				((Graphic)_text).color = Color.Lerp(GoldenYellow, VisceralRed, (float)(i - reveal.Length / 2) / (float)(reveal.Length / 2));
			}
			yield return sWait5;
		}
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2f, _baseScale * 2f, 1f);
		((Graphic)_text).color = DeadRed;
		SetText("IT'S ME", isGlitching: true);
		yield return Jitter(0.5f, 8f, 15f);
		yield return StaticWall(0.5f);
	}

	[HideFromIl2Cpp]
	private IEnumerator VictimsCrying()
	{
		((Graphic)_text).color = GhostCyan;
		for (int i = 0; i < 5; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			string victim = sVictimMessages[sRandom.Next(sVictimMessages.Length)];
			for (int shake = 0; shake < 4; shake++)
			{
				float num = Mathf.Sin((float)shake * 2f) * 2f;
				_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x, _baseAnchoredPosition.y + num);
				SetText(CorruptText(victim, shake), isGlitching: true);
				yield return sWait08;
			}
			yield return sWait15;
		}
		((Graphic)_text).color = BloodDark;
		SetText("WE ARE STILL HERE", isGlitching: true);
		yield return sWait2;
	}

	[HideFromIl2Cpp]
	private IEnumerator EndoskeletonExposed()
	{
		string[] layers = new string[6]
		{
			_modText,
			"[ " + _modText + " ]",
			"[â–“" + _modText + "â–“]",
			"[â–ˆâ–“ ENDO â–“â–ˆ]",
			"[â–ˆâ–“ 01 â–“â–ˆ]",
			"[â—_â—]"
		};
		Color[] layerColors = (Color[])(object)new Color[6]
		{
			_baseColor,
			StaticGray,
			OxidizedGreen,
			BoneWhite,
			DeadRed,
			Color.white
		};
		for (int i = 0; i < layers.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = layerColors[i];
			SetText(layers[i], isGlitching: true);
			yield return Jitter(0.15f, 1f, 3f);
			yield return sWait2;
		}
		for (int i = 0; i < 4; i++)
		{
			((Graphic)_text).color = ((i % 2 == 0) ? Color.white : DeadRed);
			yield return sWait08;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_VentilationCascade()
	{
		((Graphic)_text).color = AlertOrange;
		((TMP_Text)_text).outlineColor = InteropFix.Cast(AlertOrange * 0.5f);
		SetText("VENT: WARNING", isGlitching: true);
		yield return sWait15;
		string[] systems = new string[3] { "VENTILATION", "CAM SYSTEM", "AUDIO DEVICE" };
		string[] statuses = new string[3] { "OFFLINE", "ERROR", "MALFUNCTION" };
		for (int i = 0; i < systems.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			((Graphic)_text).color = Color.Lerp(AlertOrange, DeadRed, (float)i / (float)systems.Length);
			SetText(systems[i] + ": " + statuses[i], isGlitching: true);
			yield return sWait10;
			for (int s = 0; s < 3; s++)
			{
				SetText(RandomNoise(18), isGlitching: true);
				yield return sWait025;
			}
		}
		SetText("", isGlitching: true);
		((Graphic)_text).color = Color.black;
		yield return sWait10;
		string phantomFace = sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)];
		for (int i = 0; i < 6; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			if (i % 2 == 0)
			{
				((Graphic)_text).color = OxidizedGreen;
				((Transform)_textRectTransform).localScale = new Vector3(_baseScale * (1.5f + (float)i * 0.15f), _baseScale * (1.5f + (float)i * 0.15f), 1f);
				SetText(phantomFace, isGlitching: true);
			}
			else
			{
				SetText("", isGlitching: true);
			}
			yield return sWait08;
		}
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 2.5f, _baseScale * 2.5f, 1f);
		SetText(sGrotesqueFaces[sRandom.Next(sGrotesqueFaces.Length)], isGlitching: true);
		yield return Jitter(0.4f, 8f, 15f);
		SetText("", isGlitching: true);
		yield return sWait15;
		((Graphic)_text).color = FnafGreen;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
		string[] array = new string[4] { "REBOOTING...", "VENT: ONLINE", "CAM: ONLINE", "AUDIO: ONLINE" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			if ((Object)(object)_text == (Object)null)
			{
				break;
			}
			yield return TypewriterText(text, 0.04f);
			yield return sWait3;
		}
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_PhantomFreddyWalkBy()
	{
		SetText("", isGlitching: true);
		yield return sWait5;
		((Graphic)_text).color = OxidizedGreen;
		((TMP_Text)_text).outlineColor = InteropFix.Cast(Color.black);
		((TMP_Text)_text).outlineWidth = 0.3f;
		string[] walkFrames = new string[12]
		{
			"                    â—‰", "                 â—‰ â—‰", "              â—‰_â—‰", "           â—‰_â—‰", "        â—‰_â—‰", "     â—‰_â—‰", "  â—‰_â—‰", "â—‰_â—‰", "â—‰_â—‰  ", " â—‰_â—‰    ",
			"  â—‰_â—‰      ", "   â—‰_â—‰        "
		};
		for (int i = 0; i < walkFrames.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			float num = Mathf.Clamp01((float)i / ((float)walkFrames.Length * 0.6f));
			((Graphic)_text).color = Color.Lerp(Color.black, OxidizedGreen, num);
			SetText(walkFrames[i], isGlitching: true);
			float num2 = Mathf.Sin((float)i * 1.2f) * 1.5f;
			_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x, _baseAnchoredPosition.y + num2);
			yield return sWait2;
		}
		SetText("", isGlitching: true);
		yield return sWait15;
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.8f, _baseScale * 1.8f, 1f);
		SetText("â—‰_â—‰", isGlitching: true);
		yield return sWait3;
		yield return Jitter(0.3f, 6f, 12f);
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_FazbearsFright()
	{
		((Graphic)_text).color = BoneWhite;
		yield return TypewriterText("LOCAL NEWS", 0.08f);
		yield return sWait15;
		SetText("", isGlitching: true);
		yield return sWait5;
		((Graphic)_text).color = Color.white;
		yield return TypewriterText("FAZBEAR'S FRIGHT", 0.06f);
		yield return sWait10;
		((Graphic)_text).color = AlertOrange;
		yield return TypewriterText("BURNS DOWN", 0.1f);
		yield return sWait10;
		string headline = "FAZBEAR'S FRIGHT";
		char[] burning = headline.ToCharArray();
			char[] fireChars = new char[4] { '#', '.', '@', '*' };
		for (int pass = 0; pass < headline.Length; pass++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			int num = sRandom.Next(burning.Length);
			burning[num] = fireChars[sRandom.Next(fireChars.Length)];
			((Graphic)_text).color = Color.Lerp(AlertOrange, DeadRed, (float)pass / (float)headline.Length);
			_textBuilder.Clear().Append(burning);
			SetText(_textBuilder.ToString(), isGlitching: true);
			float num2 = ((float)sRandom.NextDouble() - 0.5f) * (1f + (float)pass * 0.3f);
			float num3 = ((float)sRandom.NextDouble() - 0.5f) * (1f + (float)pass * 0.3f);
			_textRectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x + num2, _baseAnchoredPosition.y + num3);
			yield return sWait08;
		}
		SetText("", isGlitching: true);
		yield return sWait15;
		((Graphic)_text).color = StaticGray;
		SetText("ASHES", isGlitching: true);
		yield return sWait10;
		((Graphic)_text).color = RottenPurple;
		SetText("BUT HE SURVIVED", isGlitching: true);
		yield return sWait20s;
	}

	[HideFromIl2Cpp]
	private IEnumerator Sequence_SafeRoom()
	{
		((Graphic)_text).color = RottenPurple;
		yield return TypewriterText("FOLLOW ME", 0.12f);
		yield return sWait15;
		string[] children = new string[5] { "GABRIEL", "JEREMY", "SUSIE", "FRITZ", "CASSIDY" };
		((Graphic)_text).color = GhostCyan;
		for (int i = 0; i < children.Length; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			SetText(children[i], isGlitching: true);
			yield return sWait5;
			SetText("", isGlitching: true);
			yield return sWait15;
		}
		((Graphic)_text).color = StaticGray;
		yield return TypewriterText("SAFE ROOM", 0.1f);
		yield return sWait10;
		((Graphic)_text).color = GoldenYellow;
		SetText("THE SUIT", isGlitching: true);
		yield return sWait10;
		((Graphic)_text).color = RottenPurple;
		for (int i = 0; i < 4; i++)
		{
			if (!((Object)(object)_text != (Object)null))
			{
				break;
			}
			string[] array = new string[4] { "*click*", "*SNAP*", "*CRUNCH*", "..." };
			SetText(array[i], isGlitching: true);
			((Graphic)_text).color = Color.Lerp(RottenPurple, DeadRed, (float)i / 3f);
			yield return Jitter(0.1f, 1f + (float)i * 2f, 3f + (float)i * 3f);
			yield return sWait3;
		}
		SetText("", isGlitching: true);
		yield return sWait15s;
		((Graphic)_text).color = BloodDark;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 0.8f, _baseScale * 0.8f, 1f);
		yield return TypewriterText("SEALED INSIDE", 0.15f);
		yield return sWait10;
		((Graphic)_text).color = DecayBrown;
		SetText("30 YEARS...", isGlitching: true);
		yield return sWait20s;
		((Graphic)_text).color = DeadRed;
		((Transform)_textRectTransform).localScale = new Vector3(_baseScale * 1.5f, _baseScale * 1.5f, 1f);
		SetText("I AM STILL HERE", isGlitching: true);
		yield return Jitter(0.5f, 5f, 10f);
	}

	[HideFromIl2Cpp]
	private string CorruptText(string input, int passes)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}
		char[] array = input.ToCharArray();
		for (int i = 0; i < passes && i < array.Length; i++)
		{
			int num = sRandom.Next(0, array.Length);
			array[num] = sNoisePool[sRandom.Next(sNoisePool.Length)];
		}
		_textBuilder.Clear().Append(array);
		return _textBuilder.ToString();
	}

	[HideFromIl2Cpp]
	private string RandomNoise(int length)
	{
		if (length <= 0)
		{
			return "";
		}
		_textBuilder.Clear();
		for (int i = 0; i < length; i++)
		{
			_textBuilder.Append(sNoisePool[sRandom.Next(sNoisePool.Length)]);
		}
		return _textBuilder.ToString();
	}

	[HideFromIl2Cpp]
	private string RandomRoom()
	{
		return sCamRooms[sRandom.Next(sCamRooms.Length)];
	}

	private void ResetVisualsToStable()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_text == (Object)null))
		{
			RectTransform textRectTransform = _textRectTransform;
			textRectTransform.anchoredPosition = _baseAnchoredPosition;
			((Transform)textRectTransform).localRotation = Quaternion.identity;
			((Transform)textRectTransform).localScale = new Vector3(_baseScale, _baseScale, 1f);
			((Graphic)_text).color = _baseColor;
			((TMP_Text)_text).outlineColor = InteropFix.Cast(_baseOutlineColor);
			((TMP_Text)_text).outlineWidth = _baseOutlineWidth;
			SetText(_modText);
		}
	}

	private void OnDisable()
	{
		_isEffectRunning = false;
		if (_schedulerRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(_schedulerRoutine);
		}
		if (_breathingRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(_breathingRoutine);
		}
		if ((Object)(object)_text != (Object)null)
		{
			ResetVisualsToStable();
		}
	}

	private void OnDestroy()
	{
		OnDisable();
		_text = null;
		_textRectTransform = null;
	}
}





