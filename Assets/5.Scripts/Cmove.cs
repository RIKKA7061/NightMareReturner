using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cmove : MonoBehaviour
{
    /*플레이어 좌표에서 카메라 좌표를 뺀후 그 뺀 수만큼을 향해 따라가는 코드*/

    //카메라 속도 변수값 지정
    public float cameraSpeed = 5.0f;

    //외부 하이아라키 창에 플레이어 가져와서 player 변수에 담기
    public GameObject player;

    private void Update()
    {
        //dir = 플레이어 좌표 - 카메라 좌표
        Vector3 dir = player.transform.position - this.transform.position;

        //moveVector = dir에 입력된 x좌표, y좌표에 속도랑 시간을 곱해서 넘겨준다.
        Vector3 moveVector = new Vector3(dir.x * cameraSpeed * Time.deltaTime, dir.y * cameraSpeed * Time.deltaTime, 0.0f);

        //이 스크립트가 달린 물체는 moveVector값을 향해 변환한다.
        this.transform.Translate(moveVector);
    }
}