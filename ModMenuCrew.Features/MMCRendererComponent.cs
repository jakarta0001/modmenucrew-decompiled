using Il2CppInterop.Runtime.Injection;
using Il2CppSystem;
using UnityEngine;

namespace ModMenuCrew.Features;

public class MMCRendererComponent : MonoBehaviour
{
	private static MMCRendererComponent _instance;

	private static GameObject _gameObject;

	private float _lastUpdateTime;

	private const float UPDATE_INTERVAL = 0.1f;

	public static void EnsureExists()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		if ((Object)(object)_instance != (Object)null)
		{
			return;
		}
		try
		{
			try
			{
				ClassInjector.RegisterTypeInIl2Cpp<MMCRendererComponent>();
			}
			catch
			{
			}
			_gameObject = new GameObject("MMCRenderer");
			((Object)_gameObject).hideFlags = (HideFlags)61;
			Object.DontDestroyOnLoad((Object)(object)_gameObject);
			_instance = _gameObject.AddComponent<MMCRendererComponent>();
			Debug.Log(InteropFix.Cast("[MMCIdentification] MMCRendererComponent inicializado automaticamente."));
		}
		catch (Exception ex)
		{
			Debug.LogError(InteropFix.Cast("[MMCIdentification] Erro ao criar MMCRendererComponent: " + ex.Message));
		}
	}

	private void Update()
	{
		if (Time.time - _lastUpdateTime < 0.1f)
		{
			return;
		}
		_lastUpdateTime = Time.time;
		try
		{
			MMCIdentification.Update();
		}
		catch
		{
		}
		try
		{
			PlayerInfoDisplay.UpdatePlayerInfoTags();
		}
		catch
		{
		}
	}

	private void OnDestroy()
	{
		_instance = null;
		_gameObject = null;
	}
}


