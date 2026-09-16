using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// [NR] 보스는 화면 상단 보스 체력바(NRHud)를 사용하므로 머리 위 체력바는 숨김
public class UI_MonsterHP : MonoBehaviour
{
	private Rigidbody2D rb;//중력
	private Player player;//플레이어
	private MonsterHP monsterHP;

	//체력바 프리펩
	[SerializeField]                    // private형 변수를 외부에서 조절할 수 있게 바꿔줌
	private GameObject prfHpBar;        // 프리펩 체력바

	RectTransform bghp_bar;             // bghp_bar 어두운 배경 체력바
	Image hp_bar;                       // hp_bar 현재 체력바

	public float height = 1.7f;         // 체력바 Y 높이

	[Header("머리 위 체력바 표시 (보스는 상단 체력바 사용)")]
	public bool showOverheadBar = false;

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		player = FindObjectOfType<Player>();    // 무조건 해줘야됨 (초기화)
		monsterHP = GetComponent<MonsterHP>();  // 자기 오브젝트에 같이 있는 스크립트 친구

		// prfHpBar 프리팹을 이용해 canvas에다가 체력바 생성.
		var canvas = GameObject.Find("Canvas");
		if (prfHpBar == null || canvas == null) return;
		bghp_bar = Instantiate(prfHpBar, canvas.transform).GetComponent<RectTransform>(); // bghp_bar생성
		hp_bar = bghp_bar.transform.GetChild(0).GetComponent<Image>(); // bghp_bar에 자식 오브젝트 컴포넌트 가져오기
		bghp_bar.pivot = new Vector2(0.9f, 0.5f); // 중앙 기준 (필요에 따라 수정 가능)
		bghp_bar.gameObject.SetActive(showOverheadBar);
	}

	void Update()
	{
		if (bghp_bar == null || !showOverheadBar || Camera.main == null)
		{
			return;
		}

		// 카메라 보는 기준 체력바 좌표 위치 설정
		Vector3 _hpBarPos = Camera.main.WorldToScreenPoint(new Vector3(transform.position.x, transform.position.y + height, 0));
		bghp_bar.position = _hpBarPos; // 해당 좌표의 위치 적용하기

		hp_bar.fillAmount = (float)monsterHP.nowHP / (float)monsterHP.maxHP; // 체력 수치 적용하기
	}

	void OnDestroy()
	{
		DestroyHP_UI();
	}

	public void DestroyHP_UI()
	{
		if (bghp_bar != null) Destroy(bghp_bar.gameObject);
		bghp_bar = null;
	}
}
