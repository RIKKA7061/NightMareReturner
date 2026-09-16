using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// ============================================================================
// [에디터 전용] Neo둥근모 TMP 폰트 에셋 자동 생성
//  - 프로젝트를 열면 Assets/Resources/NR/Fonts/NeoDunggeunmo SDF.asset 이 없을 때 자동 생성
//  - 메뉴: Tools/악몽 회귀자/폰트 에셋 다시 만들기
// ============================================================================
[InitializeOnLoad]
public static class NRFontAssetBuilder
{
	const string TtfPath = "Assets/Resources/NR/Fonts/neodgm.ttf";
	const string AssetPath = "Assets/Resources/NR/Fonts/NeoDunggeunmo SDF.asset";

	static NRFontAssetBuilder()
	{
		EditorApplication.delayCall += () =>
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;
			if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) == null) Build(false);
		};
	}

	[MenuItem("Tools/악몽 회귀자/폰트 에셋 다시 만들기")]
	static void Rebuild() => Build(true);

	static void Build(bool force)
	{
		var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
		if (font == null)
		{
			Debug.LogWarning("[NR] 폰트 파일이 없습니다: " + TtfPath);
			return;
		}
		if (force && File.Exists(AssetPath)) AssetDatabase.DeleteAsset(AssetPath);

		var asset = TMP_FontAsset.CreateFontAsset(font, 32, 4, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
		if (asset == null)
		{
			Debug.LogError("[NR] TMP 폰트 에셋 생성 실패");
			return;
		}
		asset.name = "NeoDunggeunmo SDF";
		AssetDatabase.CreateAsset(asset, AssetPath);
		asset.material.name = "NeoDunggeunmo SDF Material";
		AssetDatabase.AddObjectToAsset(asset.material, asset);
		asset.atlasTextures[0].name = "NeoDunggeunmo SDF Atlas";
		AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);

		// 자주 쓰는 문자 미리 등록 (나머지는 실행 중 자동 추가)
		asset.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~◆◇◈■□▶◀▼▲★·…→←");

		EditorUtility.SetDirty(asset);
		AssetDatabase.SaveAssets();
		AssetDatabase.ImportAsset(AssetPath);
		Debug.Log("[NR] Neo둥근모 TMP 폰트 에셋 생성 완료: " + AssetPath);
	}
}
