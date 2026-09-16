using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

	public static void House()
	{
		Instance.UpdtTxt("꿈속 결투: 침대에 다가가세요.\n밖에서의 대화: 문을 클릭하시오.");
	}
	public void UpdtTxt(string s)
	{
		d_txt.text = s;
	}
	public static void ReadyRoomZero()
	{
		Instance.UpdtTxt("클래스 구슬에 다가가서 각성하시오.\n" +
			"그다음, 보라색 회오리가 있는 문을 향해 다가거나 클릭하세요.");
	}

	public static void RoomOne()
	{
		Instance.UpdtTxt("적들이 나타났습니다. 주변을 돌아다녀서 적을 찾아" +
			" 모두 처치하세요.");
	}

	public static void AllKill_inRoom()
	{
		Instance.UpdtTxt("적을 모두 처치하였습니다.\n" +
			"가운데서 소환된 보상을 향해 다가가 획득하세요.\n" +
			"그 다음 각 보상을 얻을 수 있는 방으로 연결된 보라색 회오리" +
			"를 향해 다가가세요.");
	}
}
