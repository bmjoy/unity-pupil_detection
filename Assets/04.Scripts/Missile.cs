using UnityEngine;
using System.Collections;

public class Missile : MonoBehaviour
{
    
    

    /// <summary>
    /// 미사일이 나가는 방향
    /// </summary>
    public Vector3 moveDir;
    private const float rotateSpeed = 5.0f;
    private const float moveSpeed = 10.0f;//총알이 움직일 속도를 상수로 지정해줍시다.

    static public Quaternion GetRotFromVectors(Vector2 posStart, Vector2 posEnd)
    {
        return Quaternion.Euler(0, 0, -Mathf.Atan2(posEnd.x - posStart.x, posEnd.y - posStart.y) * Mathf.Rad2Deg);
    }

    void Start()
    {
        Vector3 swap = gameObject.transform.localPosition;
        swap.z = 0.0f;
        gameObject.transform.localPosition = swap;

        if (moveDir.x == 0 && moveDir.y == 0)
            moveDir = new Vector2(1, 1);
        transform.rotation = GetRotFromVectors(transform.position, transform.position + moveDir);

    }
    void Update()
    {
        if (gameManager.instance.isPause) return;
        this.transform.position = Vector3.Lerp(this.transform.position, this.transform.position + moveDir, moveSpeed * Time.deltaTime);
        this.transform.localRotation = Quaternion.Slerp(this.transform.localRotation, this.transform.localRotation * new Quaternion(0,2.0f,0,2.0f) ,rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    //rigidBody가 무언가와 충돌할때 호출되는 함수 입니다.
    //Collider2D other로 부딪힌 객체를 받아옵니다.
    {
        if (other.gameObject.tag.Equals("Enemy"))
        //부딪힌 객체의 태그를 비교해서 적인지 판단합니다.
        {
            gameManager.instance.AddScore(100);
            Destroy(other.gameObject); // 맞은 적 삭제
            //적을 파괴합니다.
            Destroy(this.gameObject); //자기 자신을 지웁니다. -> 레이저 또는 미사일 삭제

        }
    }

    void OnBecameInvisible() //화면밖으로 나가 보이지 않게 되면 호출이 된다.
    {
        Destroy(this.gameObject); //객체를 삭제한다.
    }

}