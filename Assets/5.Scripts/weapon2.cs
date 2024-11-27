using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class weapon2 : MonoBehaviour
{
    //원거리 공격용 스크립트
    //공격 구분짓기 위함
    [Header("주민등록번호")]
    public int id; //무기 or 공격 아이디
    public int prefabId; //프리펩 아이디

    [Header("이게 되는 거임;; 공격력")]
    private int damage; //데미지

    [Header("관통력")]
    public int count; //관통 되는 수 // EX. 0 = 한개

    [Header("탄속")]
    public float speed; // 무기 속도

	[Header("보스")]
	public bool isBoss; // 무기 속도

	//public int per;

	float timer;
    Enemy enemy;

    void Awake()
    {
		if (!isBoss)
		{
			damage = TableManager.Enemy1Atk; //데미지
		}
		else if (isBoss)
		{
			damage = TableManager.BossATK; //데미지
		}
		enemy = GetComponentInParent<Enemy>();
    }

    void Start()
    {
		Init();
    }

    void Update()
    {
        switch (id)
        {
            case 0:
                //transform.Rotate(Vector3.back * speed * Time.deltaTime); //무기회전
                break;
            default:
                timer += Time.deltaTime;

                if (timer > speed)
                {
                    timer = 0f;
                    Fire();
                }
                break;
        }
		Scan();
	}

    public void Init()
    {
        switch (id)
        {
            case 0:
                //speed = -150; //무기회전 속도
                Batch();
                break;
            default:
                speed = 0.8f;
                break;
        }
    }

    void Batch()
    {
        for (int index = 0; index > count; index++)
        {
            Transform FarATK = GameManager.Instance.pool.Get(prefabId).transform;
            FarATK.parent = transform;
            if(Vector3.zero != null)
            {
				FarATK.GetComponent<FarATK>().Init(damage, count, Vector3.zero); //count 자리 -1은 무한으로 관통
			}
        }

    }
	public Transform nearestTarget;
	public float scanRadius = 10f; // 탐지 반경
	public LayerMask targetLayer; // 탐지 대상 레이어


	void Scan()
	{
		Collider[] colliders = Physics.OverlapSphere(transform.position, scanRadius, targetLayer);

		float nearestDistance = float.MaxValue;
		nearestTarget = null;

		foreach (Collider col in colliders)
		{
			// 특정 컴포넌트를 가진 Collider만 필터링
			Player playerComp = col.GetComponent<Player>();
			if (playerComp == null) continue; // PlayerComponent가 없으면 무시

			float distance = Vector3.Distance(transform.position, col.transform.position);
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestTarget = col.transform; // 타겟으로 설정
			}
		}
	}

	void Fire()
	{
		//Debug.Log("총 발사");

		if (!enemy.scanner2.nearestTarget)
			return;

		Vector3 targetPos = enemy.scanner2.nearestTarget.position;
		Vector3 dir = targetPos - transform.position;
		dir = dir.normalized;

		Transform FarATK = GameManager.Instance.pool.Get(prefabId).transform;
		FarATK.position = transform.position;
		FarATK.rotation = Quaternion.FromToRotation(Vector3.up, dir);
		FarATK.GetComponent<FarATK>().Init(damage, count, dir);
	}

}
