using UnityEngine;
using System.Collections;

public class enemy : MonoBehaviour
{
    public const float moveSpeed = 10.0f;
    public Transform PlayBoard;
    //상수로 움직일 속도를 지정해 줍니다.
    void Start()
    {
        PlayBoard = SpawnManaer.playBoard;
    }

    void Update()
    {
        if (gameManager.instance.isPause) return;

        moveControl();
        //프레임이 변화할때 마다 움직임을 관리해주는 함수를 호출해줍시다.

        Vector3 view = Camera.main.WorldToScreenPoint(transform.position - transform.localScale);//월드 좌표를 스크린 좌표로 변형한다.
        if (view.y < PlayBoard.GetComponent<RectTransform>().localScale.y - 330.0f)
        {          
            Destroy(gameObject);    //스크린 좌표가 -50 이하일시 삭제  
        }
    }
    void moveControl()
    {
        float distanceY = moveSpeed * Time.deltaTime;
        //움직일 거리를 계산해줍니다.
        this.gameObject.transform.Translate(0, -1 * distanceY, 0);
        //움직임을 반영합니다.
    }

    void OnBecameInvisible()
    {
        Destroy(this.gameObject);// 자기 자신을 지웁니다.
    }
}
