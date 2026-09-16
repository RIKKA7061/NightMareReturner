using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================================
// 화면/게임 설정 (PlayerPrefs)
// ============================================================================
public static class NRSettings
{
	const string KeyW = "nr.res.w";
	const string KeyH = "nr.res.h";
	const string KeyMode = "nr.res.mode";
	const string KeyVsync = "nr.vsync";
	const string KeyFps = "nr.fps";
	const string KeyShake = "nr.shake";
	const string KeyDmgNum = "nr.dmgnum";
	const string KeyHints = "nr.hints";
	const string KeyControls = "nr.controls";

	/// <summary>0: 기본(WASD/좌클릭/우클릭/Space/R/E)  1: LoL식(우클릭 이동/Q/W/E/R/F)</summary>
	public static int ControlScheme
	{
		get => Mathf.Clamp(PlayerPrefs.GetInt(KeyControls, 0), 0, 1);
		set { PlayerPrefs.SetInt(KeyControls, Mathf.Clamp(value, 0, 1)); PlayerPrefs.Save(); }
	}
	public static readonly string[] ControlSchemeNames = { "기본 (WASD + 마우스)", "LoL식 (우클릭 이동 + QWER)" };

	public static readonly string[] ModeNames = { "전체 화면", "테두리 없는 창", "창 모드" };
	public static readonly int[] FpsOptions = { 30, 60, 120, 144, 240, -1 };

	public static bool ScreenShake
	{
		get => PlayerPrefs.GetInt(KeyShake, 1) == 1;
		set { PlayerPrefs.SetInt(KeyShake, value ? 1 : 0); PlayerPrefs.Save(); }
	}

	public static bool DamageNumbers
	{
		get => PlayerPrefs.GetInt(KeyDmgNum, 1) == 1;
		set { PlayerPrefs.SetInt(KeyDmgNum, value ? 1 : 0); PlayerPrefs.Save(); }
	}

	public static bool InteractHints
	{
		get => PlayerPrefs.GetInt(KeyHints, 1) == 1;
		set { PlayerPrefs.SetInt(KeyHints, value ? 1 : 0); PlayerPrefs.Save(); }
	}

	public static bool VSync
	{
		get => PlayerPrefs.GetInt(KeyVsync, 1) == 1;
		set { PlayerPrefs.SetInt(KeyVsync, value ? 1 : 0); PlayerPrefs.Save(); ApplyFrameSettings(); }
	}

	public static int FpsIndex
	{
		get => Mathf.Clamp(PlayerPrefs.GetInt(KeyFps, 1), 0, FpsOptions.Length - 1);
		set { PlayerPrefs.SetInt(KeyFps, Mathf.Clamp(value, 0, FpsOptions.Length - 1)); PlayerPrefs.Save(); ApplyFrameSettings(); }
	}

	public static string FpsLabel(int index) => FpsOptions[index] < 0 ? "무제한" : FpsOptions[index] + " FPS";

	/// <summary>0: 전체화면(독점) 1: 테두리 없는 창 2: 창 모드</summary>
	public static int CurrentModeIndex
	{
		get
		{
			switch (Screen.fullScreenMode)
			{
				case FullScreenMode.ExclusiveFullScreen: return 0;
				case FullScreenMode.FullScreenWindow: return 1;
				case FullScreenMode.MaximizedWindow: return 1;
				default: return 2;
			}
		}
	}

	public static FullScreenMode ModeFromIndex(int i)
	{
		switch (i)
		{
			case 0: return FullScreenMode.ExclusiveFullScreen;
			case 1: return FullScreenMode.FullScreenWindow;
			default: return FullScreenMode.Windowed;
		}
	}

	/// <summary>중복(주사율만 다른 항목) 제거된 해상도 목록</summary>
	public static List<Vector2Int> GetResolutions()
	{
		var list = new List<Vector2Int>();
		foreach (var r in Screen.resolutions)
		{
			if (r.width < 960 || r.height < 540) continue;
			var v = new Vector2Int(r.width, r.height);
			if (!list.Contains(v)) list.Add(v);
		}
		// 흔한 16:9 해상도 보강 (창 모드용)
		foreach (var v in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080) })
		{
			if (!list.Contains(v) && (Screen.currentResolution.width >= v.x || list.Count == 0)) list.Add(v);
		}
		var cur = new Vector2Int(Screen.width, Screen.height);
		if (!list.Contains(cur)) list.Add(cur);
		return list.OrderBy(v => v.x * v.y).ThenBy(v => v.x).ToList();
	}

	public static void ApplyDisplay(int width, int height, int modeIndex)
	{
		var mode = ModeFromIndex(modeIndex);
		Screen.SetResolution(width, height, mode);
		PlayerPrefs.SetInt(KeyW, width);
		PlayerPrefs.SetInt(KeyH, height);
		PlayerPrefs.SetInt(KeyMode, modeIndex);
		PlayerPrefs.Save();
	}

	public static void ApplySavedDisplay()
	{
		ApplyFrameSettings();
		if (!PlayerPrefs.HasKey(KeyW)) return;
#if !UNITY_EDITOR
		int w = PlayerPrefs.GetInt(KeyW, Screen.width);
		int h = PlayerPrefs.GetInt(KeyH, Screen.height);
		int m = PlayerPrefs.GetInt(KeyMode, 1);
		if (w >= 640 && h >= 360) Screen.SetResolution(w, h, ModeFromIndex(m));
#endif
	}

	public static void ApplyFrameSettings()
	{
		QualitySettings.vSyncCount = VSync ? 1 : 0;
		int fps = FpsOptions[FpsIndex];
		Application.targetFrameRate = VSync ? -1 : fps;
	}
}
