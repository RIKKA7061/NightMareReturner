using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memo : MonoBehaviour
{
//사용 설명서


/*





	1. 조사 액션 안될 때
		boxcollider 2D컴포넌트 부착 - rigidbody2d 컴포넌트 부착 (Kintic설정, gravity scale 0)

	2. 텔레포트 안될 때
		콜라이더 컴포넌트에 isTrigger 체크 하셈

	3. NullReference 원인 모를 에러 발생시 (발생해도 빌드는 돌아갈시)
		유니티 껏다 키셈 (세이브 꼭하고)

	4. isometric 시점 오브젝트 콜라이더 설정
		edge collider 2d로 직접 콜라이더 만드셈

	5. 적 공격력 
		적 프리펩 -> 원거리 공격 -> Weapon2스크립트

	6. 다른 스크립트 값 가져올 때
		private 선언시 무조건 start나 awake에서 선언해주기
		player = FindObjectOfType<Player>();//무조건 해줘야됨 (초기화)




*/
}
