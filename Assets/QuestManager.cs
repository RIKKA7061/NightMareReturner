using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// [NR] 기존 정적 함수 이름은 그대로 두고, 화면 표시는 새 목표 HUD(NRObjective)로 전달
//      QuestManager가 없는 씬(거리)에서도 안전하게 동작
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
	private void Awake()
	{
		Instance = this;
	}
	public TextMeshProUGUI d_txt;// description
    void Start()
    {
		House();
	}

	void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	static void Show(string title, string body, string legacyText, bool combat = false)
	{
		if (Instance != null) Instance.UpdtTxt(legacyText);
		NRObjective.Set(title, body, combat);
	}

	public static void House()
	{
		// 거리 씬에서도 Player.Start가 House()를 호출하므로 거리 목표로 대체
		if (NRGame.Instance != null && NRGame.Instance.CurrentScene == NRGame.SceneTown) { Town(); return; }
		Show("꿈속으로",
			"포탈을 향해 이동하면 악몽 속 결투가 시작됩니다.\n현관문을 클릭하면 거리로 나갈 수 있습니다.\n책장에서 악몽 결정으로 영구 강화를 할 수 있습니다.\n책상에서 조작 방식과 캐릭터를 바꿀 수 있습니다.",
			"꿈속 결투: 침대에 다가가세요.\n밖에서의 대화: 문을 클릭하시오.");
	}
	public void UpdtTxt(string s)
	{
		if (d_txt != null) d_txt.text = s;
	}
	public static void ReadyRoomZero()
	{
		Show("감정의 각성",
			"클래스 구슬에 다가가 각성하세요.\n그다음 보라색 회오리 문으로 이동합니다.",
			"클래스 구슬에 다가가서 각성하시오.\n그다음, 보라색 회오리가 있는 문을 향해 다가거나 클릭하세요.");
	}

	public static void RoomOne()
	{
		Show("악몽을 물리쳐라",
			"방 안의 적을 모두 처치하세요.",
			"적들이 나타났습니다. 주변을 돌아다녀서 적을 찾아 모두 처치하세요.", true);
	}

	public static void AllKill_inRoom()
	{
		bool store = Player.gameRound >= FindObjectOfType<PrefabSpawner>()?.OverRoom;
		Show("보상 획득",
			store ? "보상을 얻고, 상점으로 가는 회오리로 이동하세요."
				  : "보상을 얻고, 다음 방으로 이어지는 문을 고르세요.\n감정의 방에서는 증강을 선택할 수 있습니다.",
			"적을 모두 처치하였습니다.\n가운데서 소환된 보상을 향해 다가가 획득하세요.\n그 다음 각 보상을 얻을 수 있는 방으로 연결된 보라색 회오리를 향해 다가가세요.");
	}

	// ---- [NR] 추가 목표 ----
	public static void Store()
	{
		Show("수문장을 만나기 전에",
			"상인에게 다가가 E로 대화하면 아이템을 살 수 있습니다.\n준비가 끝나면 수문장의 방으로 이동하세요.",
			"상점: 준비 후 보스방으로 이동하세요.");
	}

	public static void Boss(string bossName)
	{
		Show(bossName + " 처치",
			"붉은 예고 표시를 보고 구르기(Space)로 피하세요.\n공격 직후의 빈틈을 노리세요.",
			"보스를 처치하세요.", true);
	}

	public static void FloorClear(int floor)
	{
		Show(floor + "계층 돌파",
			"수문장의 잔향에서 증강을 선택하면 다음 계층으로 내려갑니다.",
			floor + "계층을 돌파했습니다.");
	}

	public static void Town()
	{
		NRObjective.Set("거리",
			"사람들에게 다가가 E로 대화하세요. 죽을 때마다 이야기가 달라집니다.\n집 문을 클릭하면 돌아갈 수 있습니다.", false);
	}
}
