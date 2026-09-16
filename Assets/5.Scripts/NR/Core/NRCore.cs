using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================================
// 악몽 회귀자 - 출시 보강 시스템(NR) 핵심
// 씬 파일을 직접 수정하지 않고, 런타임에 기존 씬을 보강하는 구조입니다.
// ============================================================================

public static class NRBoot
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Init()
	{
		if (NRGame.Instance != null) return;
		var go = new GameObject("[NR Game]");
		UnityEngine.Object.DontDestroyOnLoad(go);
		go.AddComponent<NRGame>();
	}
}

/// <summary>게임 전역 관리자 (씬 전환에도 유지)</summary>
public class NRGame : MonoBehaviour
{
	public static NRGame Instance { get; private set; }
	public static bool IsQuitting { get; private set; }

	public const string SceneMainMenu = "MainMenu";
	public const string SceneDungeon = "SampleScene";
	public const string SceneTown = "SideViewScene";

	public string CurrentScene { get; private set; } = "";

	void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;

		NRSave.Load();
		NRSettings.ApplySavedDisplay();
		NRFont.Init();

		SceneManager.sceneLoaded += OnSceneLoaded;
		gameObject.AddComponent<NRFontSweeper>();
		gameObject.AddComponent<NRUIRoot>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		gameObject.AddComponent<NRDevMenu>();
#endif
	}

	void OnDestroy()
	{
		if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		CurrentScene = scene.name;
		NRTime.ResetAll();
		NRUIState.ResetAll();
		try { NRScenePatcher.Patch(scene); }
		catch (Exception e) { Debug.LogException(e); }
	}

	void Update()
	{
		NRSave.PollGameState();
		NRInput.Tick();
		NRUIState.HandleGlobalKeys();
	}

	void OnApplicationQuit()
	{
		IsQuitting = true;
		NRSave.Save();
	}

	void OnApplicationPause(bool pause)
	{
		if (pause) NRSave.Save();
	}

	public static Coroutine Run(IEnumerator routine)
	{
		return Instance != null ? Instance.StartCoroutine(routine) : null;
	}
}

// ---------------------------------------------------------------------------
// 시간 (일시정지 스택 / 개발 배속 / 히트스톱)
// ---------------------------------------------------------------------------
public static class NRTime
{
	static readonly HashSet<string> pauseKeys = new HashSet<string>();
	static float devScale = 1f;
	static float slowMo = 1f;

	public static bool IsPaused => pauseKeys.Count > 0;
	public static float DevScale => devScale;

	public static void Pause(string key) { pauseKeys.Add(key); Apply(); }
	public static void Resume(string key) { pauseKeys.Remove(key); Apply(); }
	public static bool IsPausedBy(string key) => pauseKeys.Contains(key);

	public static void SetDevScale(float s) { devScale = Mathf.Max(0.1f, s); Apply(); }

	public static void ResetAll()
	{
		pauseKeys.Clear();
		slowMo = 1f;
		Apply();
	}

	static void Apply()
	{
		Time.timeScale = IsPaused ? 0f : devScale * slowMo;
	}

	/// <summary>짧은 역경직(타격감). 일시정지 중이면 무시.</summary>
	public static void HitStop(float duration = 0.05f, float scale = 0.05f)
	{
		if (IsPaused || NRGame.Instance == null) return;
		NRGame.Run(SlowRoutine(duration, scale));
	}

	/// <summary>슬로 모션 연출 (실시간 기준)</summary>
	public static void SlowMotion(float duration, float scale)
	{
		if (NRGame.Instance == null) return;
		NRGame.Run(SlowRoutine(duration, scale));
	}

	static IEnumerator SlowRoutine(float duration, float scale)
	{
		slowMo = scale;
		Apply();
		yield return new WaitForSecondsRealtime(duration);
		slowMo = 1f;
		Apply();
	}
}

// ---------------------------------------------------------------------------
// 입력 (Input System 직접 조회. 전역 클래스 Keyboard와 이름 충돌을 피하려고 전체 경로 사용)
// ---------------------------------------------------------------------------
public static class NRInput
{
	public static void Tick() { }

	static UnityEngine.InputSystem.Keyboard KB => UnityEngine.InputSystem.Keyboard.current;
	static UnityEngine.InputSystem.Mouse MS => UnityEngine.InputSystem.Mouse.current;

	public static bool EscDown => KB != null && KB.escapeKey.wasPressedThisFrame;
	public static bool TabDown => KB != null && KB.tabKey.wasPressedThisFrame;
	public static bool InteractDown => KB != null && KB.eKey.wasPressedThisFrame;
	public static bool SpaceDown => KB != null && KB.spaceKey.wasPressedThisFrame;
	public static bool SpaceHeld => KB != null && KB.spaceKey.isPressed;
	public static bool EnterDown => KB != null && (KB.enterKey.wasPressedThisFrame || KB.numpadEnterKey.wasPressedThisFrame);
	public static bool LeftClickDown => MS != null && MS.leftButton.wasPressedThisFrame;
	public static Vector2 MousePosition => MS != null ? MS.position.ReadValue() : Vector2.zero;

	public static bool NumberDown(int n)
	{
		if (KB == null) return false;
		switch (n)
		{
			case 1: return KB.digit1Key.wasPressedThisFrame || KB.numpad1Key.wasPressedThisFrame;
			case 2: return KB.digit2Key.wasPressedThisFrame || KB.numpad2Key.wasPressedThisFrame;
			case 3: return KB.digit3Key.wasPressedThisFrame || KB.numpad3Key.wasPressedThisFrame;
			case 4: return KB.digit4Key.wasPressedThisFrame || KB.numpad4Key.wasPressedThisFrame;
		}
		return false;
	}

	public static bool KeyDown(UnityEngine.InputSystem.Key key)
	{
		return KB != null && KB[key].wasPressedThisFrame;
	}

	/// <summary>마우스가 클릭 가능한 UI 위에 있는지 (게임플레이 클릭 차단용)</summary>
	public static bool PointerOverInteractiveUI()
	{
		var es = EventSystem.current;
		if (es == null) return false;
		var data = new PointerEventData(es) { position = MousePosition };
		raycastResults.Clear();
		es.RaycastAll(data, raycastResults);
		foreach (var r in raycastResults)
		{
			if (r.gameObject == null) continue;
			if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;
			if (r.gameObject.GetComponentInParent<NRBlocksPointer>() != null) return true;
		}
		return false;
	}
	static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
}

/// <summary>이 컴포넌트가 붙은 UI 위에서는 게임플레이 클릭이 막힙니다.</summary>
public class NRBlocksPointer : MonoBehaviour { }

// ---------------------------------------------------------------------------
// 오디오 (기존 AudioManager 재사용 + Resources 클립)
// ---------------------------------------------------------------------------
public static class NRAudio
{
	static AudioSource uiSource;
	static AudioSource musicSource;
	static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

	public static float MusicVolume => PlayerPrefs.GetFloat("musicVolume", 1f);
	public static float SfxVolume => PlayerPrefs.GetFloat("musicVolume2", 1f);

	static AudioManager FindManager()
	{
		var go = GameObject.FindGameObjectWithTag("Audio");
		return go != null ? go.GetComponent<AudioManager>() : null;
	}

	public static AudioClip Clip(string name)
	{
		if (cache.TryGetValue(name, out var c) && c != null) return c;
		c = Resources.Load<AudioClip>("NR/Audio/" + name);
		cache[name] = c;
		return c;
	}

	static AudioSource EnsureSource(ref AudioSource src, string name, bool loop)
	{
		if (src != null) return src;
		if (NRGame.Instance == null) return null;
		var go = new GameObject(name);
		go.transform.SetParent(NRGame.Instance.transform, false);
		src = go.AddComponent<AudioSource>();
		src.playOnAwake = false;
		src.loop = loop;
		src.ignoreListenerPause = true;
		return src;
	}

	/// <summary>UI 효과음: 기존 AudioManager의 UI 클립(4번)을 우선 사용</summary>
	public static void PlayUI()
	{
		var am = FindManager();
		if (am != null && am.audio != null && am.audio.Length > 4 && am.audio[4] != null)
		{
			am.PlayerSFX(am.audio[4]);
			return;
		}
	}

	public static void PlaySfx(string resourceName, float volume = 1f)
	{
		var clip = Clip(resourceName);
		if (clip == null) return;
		var src = EnsureSource(ref uiSource, "NR SFX", false);
		if (src == null) return;
		src.PlayOneShot(clip, volume * SfxVolume);
	}

	public static void PlayLegacySfx(int index, float volume = 1f)
	{
		var am = FindManager();
		if (am != null && am.audio != null && index >= 0 && index < am.audio.Length && am.audio[index] != null)
			am.PlayerSFX(am.audio[index]);
	}

	/// <summary>NR 전용 배경음 재생 (null이면 정지하고 씬 음악 복귀)</summary>
	public static void PlayMusic(string resourceName, bool muteSceneMusic = true)
	{
		var src = EnsureSource(ref musicSource, "NR Music", true);
		if (src == null) return;
		var am = FindManager();
		if (string.IsNullOrEmpty(resourceName))
		{
			src.Stop();
			if (am != null) am.SetMusicMuted(false);
			return;
		}
		var clip = Clip(resourceName);
		if (clip == null) return;
		if (src.clip == clip && src.isPlaying) return;
		src.clip = clip;
		src.volume = MusicVolume;
		src.Play();
		if (am != null && muteSceneMusic) am.SetMusicMuted(true);
	}

	public static void StopMusic() => PlayMusic(null);

	public static void RefreshVolumes()
	{
		if (musicSource != null) musicSource.volume = MusicVolume;
	}

	public static void SetMusicVolume(float v)
	{
		PlayerPrefs.SetFloat("musicVolume", v);
		var am = FindManager();
		if (am != null) am.SetMusicVolume(v);
		RefreshVolumes();
	}

	public static void SetSfxVolume(float v)
	{
		PlayerPrefs.SetFloat("musicVolume2", v);
		var am = FindManager();
		if (am != null) am.SetSfxVolume(v);
	}
}

// ---------------------------------------------------------------------------
// 색상 팔레트 (메인 타이틀 아트의 보라/분홍/남색 톤에 맞춤)
// ---------------------------------------------------------------------------
public static class NRPalette
{
	public static readonly Color Bg0 = Hex("0C0A18");
	public static readonly Color Bg1 = Hex("161228");
	public static readonly Color Bg2 = Hex("211A3A");
	public static readonly Color Border = Hex("3B2E6B");
	public static readonly Color BorderHi = Hex("8A63E8");
	public static readonly Color Text = Hex("EEE8F7");
	public static readonly Color TextDim = Hex("A39BBF");
	public static readonly Color TextMute = Hex("6B638A");
	public static readonly Color Pink = Hex("EC6FCB");
	public static readonly Color Cyan = Hex("7FE3F0");
	public static readonly Color Gold = Hex("F2C45A");
	public static readonly Color Crimson = Hex("E5486B");
	public static readonly Color Green = Hex("7EDC8A");
	public static readonly Color Hp = Hex("E5486B");
	public static readonly Color HpBack = Hex("3A1422");
	public static readonly Color Shield = Hex("8FD8FF");

	public static readonly Color Rage = Hex("E85A55");
	public static readonly Color Anxiety = Hex("A96CF0");
	public static readonly Color Sorrow = Hex("58A8EC");
	public static readonly Color Will = Hex("F0BD55");

	public static readonly Color RarityCommon = Hex("BDB6CC");
	public static readonly Color RarityRare = Hex("58A8EC");
	public static readonly Color RarityEpic = Hex("B46CF0");
	public static readonly Color RarityLegendary = Hex("F2C45A");

	public static Color Hex(string hex)
	{
		ColorUtility.TryParseHtmlString("#" + hex, out var c);
		return c;
	}

	public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

	public static string ToHex(Color c) => ColorUtility.ToHtmlStringRGB(c);
}

// ---------------------------------------------------------------------------
// 유틸
// ---------------------------------------------------------------------------
public static class NRUtil
{
	/// <summary>비활성 오브젝트까지 포함해 이름으로 자식 검색</summary>
	public static Transform FindDeep(Transform root, string name)
	{
		if (root == null) return null;
		if (root.name == name) return root;
		for (int i = 0; i < root.childCount; i++)
		{
			var r = FindDeep(root.GetChild(i), name);
			if (r != null) return r;
		}
		return null;
	}

	/// <summary>씬 루트 전체에서 이름으로 검색 (비활성 포함)</summary>
	public static Transform FindInScene(Scene scene, string name)
	{
		foreach (var root in scene.GetRootGameObjects())
		{
			var r = FindDeep(root.transform, name);
			if (r != null) return r;
		}
		return null;
	}

	public static GameObject FindRoot(Scene scene, string name)
	{
		foreach (var root in scene.GetRootGameObjects())
			if (root.name == name) return root;
		return null;
	}

	public static void SetActiveSafe(Transform t, bool active)
	{
		if (t != null && t.gameObject.activeSelf != active) t.gameObject.SetActive(active);
	}

	public static Player FindPlayer() => UnityEngine.Object.FindObjectOfType<Player>();

	public static string Josa(string word, string withBatchim, string withoutBatchim)
	{
		if (string.IsNullOrEmpty(word)) return withoutBatchim;
		char last = word[word.Length - 1];
		if (last < 0xAC00 || last > 0xD7A3) return withoutBatchim;
		return ((last - 0xAC00) % 28) > 0 ? withBatchim : withoutBatchim;
	}
}
