using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// ============================================================================
// 씬 보강: 씬 파일을 수정하지 않고, 로드 시점에 기존 오브젝트를 정리/연결
//  (오브젝트는 삭제하지 않고 숨기기만 함)
// ============================================================================
public static class NRScenePatcher
{
	public static void Patch(Scene scene)
	{
		NRUI.EnsureEventSystem();
		switch (scene.name)
		{
			case NRGame.SceneMainMenu:
				NRMainMenu.Build(scene);
				break;
			case NRGame.SceneDungeon:
				PatchDungeon(scene);
				break;
			case NRGame.SceneTown:
				PatchTown(scene);
				break;
		}
	}

	// ---------------------------------------------------------------------
	static void PatchDungeon(Scene scene)
	{
		NRHud.Create(true);
		var canvas = NRUtil.FindRoot(scene, "Canvas");
		if (canvas != null)
		{
			var c = canvas.transform;
			// 새 HUD로 대체된 기존 UI 숨김
			Hide(c, "bgHP_bar");
			Hide(c, "PlayerHP_Text");
			Hide(c, "TipBg_Img (1)");     // 항상 떠 있던 조작법 + 디버그 텍스트 → 설정 메뉴 '조작법'
			Hide(c, "TipBg_Img");
			Hide(c, "StatBtn Image");     // 기존 현황 버튼 → TAB / HUD 버튼
			Hide(c, "SettingBtn");        // 기존 톱니 → ESC / HUD 버튼
			Hide(c, "Dev_Btn");
			Hide(c, "MoneyUpTest_Btn");
			Hide(c, "PlayerHpUp_Btn");

			// 기존 버프 아이콘: ItemManager가 켜고 끄므로 숨김 보관함으로 이동 (HUD에 텍스트로 표시)
			var holder = new GameObject("NR Hidden Legacy UI", typeof(RectTransform));
			holder.transform.SetParent(c, false);
			holder.SetActive(false);
			Reparent(c, "HammerBuffSet", holder.transform);
			Reparent(c, "HpUpBuffSet", holder.transform);
			Reparent(c, "CrunchModeBuffSet", holder.transform);

			// 사망 연출: 기존 그래픽은 끄고 NR 사망 화면 사용 (Dead_set 활성화 로직은 유지)
			MuteGraphics(NRUtil.FindDeep(c, "DeadSet"));

			NRDialogueSkin.Apply(NRUtil.FindDeep(c, "DialogSet"));
			RestyleStore(NRUtil.FindDeep(c, "Store"));
		}

		if (QuestManager.Instance != null && QuestManager.Instance.d_txt != null)
			QuestManager.Instance.d_txt.gameObject.SetActive(false); // 좌상단 퀘스트 텍스트 → 목표 카드

		NRInteractableScanner.Install(false);
		SyncNpcTalkProgress();
		var player = Object.FindObjectOfType<Player>();
		NRPlayerFX.Attach(player);
		NRControls.Attach(player);
		DisablePropColliders(scene);

		// 집의 책상 = 캐릭터 / 조작 방식 변경
		var desk = NRUtil.FindInScene(scene, "desk");
		if (desk != null)
		{
			var dit = NRInteractable.Attach(desk.gameObject, "책상", "캐릭터 · 조작 방식 변경", NRPalette.Gold, "클릭 / E");
			dit.onInteract = () => NRSetupWizard.Show(false, null);
			dit.requireHome = true;
			dit.showRadius = 2f;
			dit.interactRadius = 1.3f;
		}

		// 집의 책장 = 영구 강화
		var shelf = NRUtil.FindInScene(scene, "bookShelf");
		if (shelf != null)
		{
			var it = NRInteractable.Attach(shelf.gameObject, "기억의 책장", "악몽 결정으로 영구 강화", NRPalette.Cyan, "클릭 / E");
			it.onInteract = NRMirror.Show;
			it.requireHome = true;
			it.showRadius = 3.5f;
			it.interactRadius = 2.2f;
		}

		NRRun.RestockShop();
	}

	static void PatchTown(Scene scene)
	{
		NRHud.Create(false);
		var canvas = NRUtil.FindRoot(scene, "Canvas");
		if (canvas != null)
		{
			var c = canvas.transform;
			Hide(c, "Reset_Btn");            // 개발용 '대화 초기화' 버튼 (개발자 메뉴로 이동)
			Hide(c, "gameRound_txt (1)");    // '집밖' → HUD 위치 표시
			Hide(c, "SettingBtn");
			Hide(c, "TipBg_Img");
			Hide(c, "TempTxt");
			Hide(c, "TalkCount");            // 좌상단 디버그 '회차별 대화 횟수'
			Hide(c, "DeadCount_Text");       // 좌상단 디버그 '죽은 횟수' → 스테이터스 창
			NRDialogueSkin.Apply(NRUtil.FindDeep(c, "DialogSet"));
		}
		NRInteractableScanner.Install(true);
		SyncNpcTalkProgress();
		var townPlayer = Object.FindObjectOfType<Player>();
		NRPlayerFX.Attach(townPlayer);
		NRControls.Attach(townPlayer);
		QuestManager.Town();

		// 기존 화살표 스프라이트는 은은하게 움직이도록
		foreach (var root in scene.GetRootGameObjects())
		{
			if (root.name == "arrow" || root.name.StartsWith("png-transparent-arrow"))
				root.AddComponent<NRBob>();
		}
	}

	// ---------------------------------------------------------------------
	/// <summary>
	/// 씬을 다시 불러오면 NPC의 EachTalkCount가 0으로 돌아가 같은 회차 대사를 못 보던 문제 보정.
	/// 저장된 NPC별 대화 진행(EachTalkCountSaveNum)만큼 맞춰 둔다. (대화 로직 자체는 변경 없음)
	/// </summary>
	static void SyncNpcTalkProgress()
	{
		var saved = _Object.EachTalkCountSaveNum;
		if (saved == null) return;
		foreach (var npc in Object.FindObjectsOfType<_Object>(true))
		{
			int idx = npc.id / 100;
			if (idx >= 0 && idx < saved.Length && saved[idx] > npc.EachTalkCount)
				npc.EachTalkCount = saved[idx];
		}
	}

	/// <summary>
	/// 항아리·그루터기·기둥·촛대 같은 장식은 길을 막지 않도록 충돌 해제.
	/// Deco / Daco2는 통째로 끄고, 장식이 벽 타일맵(Wall / Wall2)에 섞여 있는 칸은 타일 단위로 해제한다.
	/// </summary>
	static void DisablePropColliders(Scene scene)
	{
		foreach (var name in new[] { "Deco", "Daco2" })
		{
			var t = NRUtil.FindInScene(scene, name);
			if (t == null) continue;
			foreach (var col in t.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
			var rb = t.GetComponent<Rigidbody2D>();
			if (rb != null) rb.simulated = false;
		}

		foreach (var name in new[] { "Wall", "Wall2" })
		{
			var t = NRUtil.FindInScene(scene, name);
			var map = t != null ? t.GetComponent<Tilemap>() : null;
			if (map == null) continue;
			int cleared = 0;
			foreach (var cell in map.cellBounds.allPositionsWithin)
			{
				var tile = map.GetTile(cell);
				if (tile == null || !IsPropTile(tile.name)) continue;
				if (map.GetColliderType(cell) == Tile.ColliderType.None) continue;
				map.SetColliderType(cell, Tile.ColliderType.None);
				cleared++;
			}
			if (cleared == 0) continue;
			var comp = t.GetComponent<CompositeCollider2D>();
			if (comp != null) comp.GenerateGeometry();
		}
	}

	/// <summary>벽 타일맵에 섞여 있는 장식 타일(항아리 prop004_*, 그루터기 prop005_*, 기둥 prop007_*, 촛대 prop006).</summary>
	static bool IsPropTile(string n)
	{
		return n == "prop006" || n.StartsWith("prop004_") || n.StartsWith("prop005_") || n.StartsWith("prop007");
	}

	static void Hide(Transform root, string name)
	{
		var t = NRUtil.FindDeep(root, name);
		if (t != null) t.gameObject.SetActive(false);
	}

	static void Reparent(Transform root, string name, Transform newParent)
	{
		var t = NRUtil.FindDeep(root, name);
		if (t != null) t.SetParent(newParent, false);
	}

	static void MuteGraphics(Transform t)
	{
		if (t == null) return;
		foreach (var g in t.GetComponentsInChildren<Graphic>(true)) g.enabled = false;
	}

	static void RestyleStore(Transform store)
	{
		if (store == null) return;
		foreach (var img in store.GetComponentsInChildren<Image>(true))
		{
			if (img.gameObject.name == "Store_UI")
			{
				img.sprite = NRSprites.Frame(NRPalette.Bg1.WithAlpha(0.96f), NRPalette.Gold, NRPalette.Gold.WithAlpha(0.5f), NRPalette.Bg0);
				img.type = Image.Type.Sliced;
				img.pixelsPerUnitMultiplier = 1f;
				img.color = Color.white;
			}
		}
		foreach (var btn in store.GetComponentsInChildren<Button>(true))
		{
			if (btn.GetComponent<ShopManager>() == null) continue;
			var img = btn.GetComponent<Image>();
			if (img != null)
			{
				img.sprite = NRSprites.Frame(NRPalette.Bg2, NRPalette.Border, NRPalette.BorderHi.WithAlpha(0.4f), NRPalette.Bg0);
				img.type = Image.Type.Sliced;
				img.pixelsPerUnitMultiplier = 1f;
				img.color = Color.white;
			}
			var colors = btn.colors;
			colors.highlightedColor = new Color(1f, 0.85f, 1f);
			colors.pressedColor = new Color(0.8f, 0.7f, 0.9f);
			colors.disabledColor = new Color(0.55f, 0.55f, 0.6f);
			btn.colors = colors;
			foreach (var txt in btn.GetComponentsInChildren<Text>(true))
			{
				if (txt.gameObject.name == "NameText") txt.color = NRPalette.Gold;
				else if (txt.gameObject.name == "Item Text") txt.color = NRPalette.Text;
				else if (txt.gameObject.name == "Price Text") txt.color = NRPalette.Cyan;
			}
		}
	}

	/// <summary>ESC: 기존 상점 등 열려 있는 옛 UI 닫기</summary>
	public static bool TryCloseLegacyPanels()
	{
		bool closed = false;
		var scene = SceneManager.GetActiveScene();
		var canvas = NRUtil.FindRoot(scene, "Canvas");
		if (canvas == null) return false;
		foreach (var name in new[] { "Store", "Setting", "MyData" })
		{
			var t = NRUtil.FindDeep(canvas.transform, name);
			if (t != null && t.gameObject.activeInHierarchy)
			{
				t.gameObject.SetActive(false);
				closed = true;
			}
		}
		if (closed)
		{
			NRTime.Resume("legacy");
			var btn = Object.FindObjectOfType<BtnManager>();
			if (btn != null) btn.OFF();
		}
		return closed;
	}
}

/// <summary>위아래로 살짝 움직임 (안내 화살표)</summary>
public class NRBob : MonoBehaviour
{
	Vector3 basePos;
	void Start() { basePos = transform.position; }
	void Update() { transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3.5f) * 0.12f; }
}
