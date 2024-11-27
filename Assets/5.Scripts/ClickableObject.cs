using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    // 실행할 함수
    public void ExecuteFunction()
    {
        Debug.Log($"{gameObject.name}이(가) 클릭되었습니다!");
        // 여기에 실행하고자 하는 동작 추가
    }
}

public class InputManager : MonoBehaviour
{
    void Update()
    {
        // 마우스 좌클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            CheckObjectUnderMouse();
        }
    }

    void CheckObjectUnderMouse()
    {
        // 카메라에서 마우스 위치로 Ray 생성
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.Log("나 클릭 ㅋㅋ");
        // Ray가 Collider와 충돌하면
        if (Physics.Raycast(ray, out hit))
        {
            //충돌한 오브젝트의 스크립트 가져오기
            ClickableObject clickable = hit.collider.GetComponent<ClickableObject>();
            if (clickable != null)
            {
                // 해당 오브젝트의 함수 실행
                clickable.ExecuteFunction();
            }
        }
    }
}
