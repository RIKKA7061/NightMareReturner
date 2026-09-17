using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// ============================================================================
// 폰트: Neo둥근모(OFL 1.1)로 게임 전체 통일
//  - 에디터에서는 NRFontAssetBuilder가 Resources/NR/Fonts/NeoDunggeunmo SDF.asset 을 자동 생성
//  - 에셋이 없으면 런타임에 동적 폰트 에셋을 생성 (폴백)
// ============================================================================
public enum NRTextFx { None, Shadow, Outline, OutlineShadow }

public static class NRFont
{
	public const string AssetPath = "NR/Fonts/NeoDunggeunmo SDF";
	public const string TtfPath = "NR/Fonts/neodgm";

	public static TMP_FontAsset Asset { get; private set; }
	public static Font Legacy { get; private set; }

	static readonly Dictionary<NRTextFx, Material> fxMaterials = new Dictionary<NRTextFx, Material>();
	static bool initialized;

	public static void Init()
	{
		if (initialized && Asset != null) return;
		initialized = true;

		Legacy = Resources.Load<Font>(TtfPath);
		Asset = Resources.Load<TMP_FontAsset>(AssetPath);

		if (Asset == null && Legacy != null)
		{
			try
			{
				Asset = TMP_FontAsset.CreateFontAsset(Legacy, 32, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
				if (Asset != null) Asset.name = "NeoDunggeunmo SDF (runtime)";
			}
			catch (Exception e)
			{
				Debug.LogWarning("[NRFont] 런타임 폰트 에셋 생성 실패: " + e.Message);
				Asset = null;
			}
		}

		if (Asset == null) Debug.LogWarning("[NRFont] Neo둥근모 폰트 에셋을 찾지 못해 기존 폰트를 유지합니다.");
	}

	public static Material GetMaterial(NRTextFx fx)
	{
		if (Asset == null) return null;
		if (fx == NRTextFx.None) return Asset.material;
		if (fxMaterials.TryGetValue(fx, out var cached) && cached != null) return cached;

		string templateName = fx == NRTextFx.Shadow ? "NR_TMP_Shadow" : fx == NRTextFx.Outline ? "NR_TMP_Outline" : "NR_TMP_OutlineShadow";
		var template = Resources.Load<Material>("NR/Materials/" + templateName);
		var mat = new Material(Asset.material);
		mat.name = Asset.name + " " + fx;
		if (template != null)
		{
			mat.shaderKeywords = template.shaderKeywords;
			CopyFloat(template, mat, "_OutlineWidth");
			CopyFloat(template, mat, "_UnderlayOffsetX");
			CopyFloat(template, mat, "_UnderlayOffsetY");
			CopyFloat(template, mat, "_UnderlayDilate");
			CopyFloat(template, mat, "_UnderlaySoftness");
			CopyColor(template, mat, "_OutlineColor");
			CopyColor(template, mat, "_UnderlayColor");
		}
		try { ShaderUtilities.UpdateShaderRatios(mat); } catch { }
		fxMaterials[fx] = mat;
		return mat;
	}

	static void CopyFloat(Material from, Material to, string prop)
	{
		if (from.HasProperty(prop) && to.HasProperty(prop)) to.SetFloat(prop, from.GetFloat(prop));
	}

	static void CopyColor(Material from, Material to, string prop)
	{
		if (from.HasProperty(prop) && to.HasProperty(prop)) to.SetColor(prop, from.GetColor(prop));
	}

	/// <summary>기존 텍스트의 폰트만 교체 (크기/정렬 유지)</summary>
	public static void ApplyTo(TMP_Text t)
	{
		if (Asset == null || t == null) return;
		if (t.font == Asset) return;
		t.font = Asset;
		t.fontSharedMaterial = GetMaterial(NRTextFx.Shadow);
	}

	public static void ApplyTo(Text t)
	{
		if (Legacy == null || t == null) return;
		if (t.font == Legacy) return;
		t.font = Legacy;
	}

	public static void Style(TMP_Text t, NRTextFx fx)
	{
		if (Asset == null || t == null) return;
		t.font = Asset;
		t.fontSharedMaterial = GetMaterial(fx);
	}
}

/// <summary>씬 안의 모든 텍스트(나중에 생성되는 텍스트 포함)를 Neo둥근모로 교체</summary>
public class NRFontSweeper : MonoBehaviour
{
	readonly HashSet<int> done = new HashSet<int>();
	float next;

	void OnEnable()
	{
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { done.Clear(); next = 0f; };
	}

	void LateUpdate()
	{
		if (Time.unscaledTime < next) return;
		next = Time.unscaledTime + (Time.timeSinceLevelLoad < 3f ? 0.25f : 1.0f);
		Sweep();
	}

	public void Sweep()
	{
		if (NRFont.Asset != null)
		{
			foreach (var t in FindObjectsOfType<TMP_Text>(true))
			{
				int id = t.GetInstanceID();
				if (done.Contains(id)) continue;
				done.Add(id);
				if (t.GetComponentInParent<NRKeepFont>() != null) continue;
				NRFont.ApplyTo(t);
			}
		}
		if (NRFont.Legacy != null)
		{
			foreach (var t in FindObjectsOfType<Text>(true))
			{
				int id = t.GetInstanceID();
				if (done.Contains(id)) continue;
				done.Add(id);
				NRFont.ApplyTo(t);
			}
		}
	}
}

/// <summary>폰트 자동 교체에서 제외할 오브젝트 표시</summary>
public class NRKeepFont : MonoBehaviour { }
