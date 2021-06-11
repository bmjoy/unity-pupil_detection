using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnManaer : MonoBehaviour
{
    public bool enableSpawn = false;
    public GameObject[] Enemy; //Prefab을 받을 public 변수 입니다.
    public static Transform playBoard;
    public Transform stackEnemy;

    void SpawnEnemy()
    {
        if (gameManager.instance.isPause) return;
        if (DlibFaceLandmarkDetectorExample.WebCamTextureToMatExample.canShoot) // 게임 시작 버튼이 눌리면
        {
            float randomX = Random.Range(playBoard.GetComponent<RectTransform>().position.x - (playBoard.GetComponent<RectTransform>().lossyScale.x / 2), playBoard.GetComponent<RectTransform>().position.x + (playBoard.GetComponent<RectTransform>().lossyScale.x / 2)); //적이 나타날 X좌표를 랜덤으로 생성
            if (enableSpawn)
            {
                int rVal = Random.Range(0, Enemy.Length);
                GameObject enemy = (GameObject)Instantiate(Enemy[rVal], new Vector3(randomX, /*playBoard.GetComponent<RectTransform>().position.y +*/ (playBoard.GetComponent<RectTransform>().localScale.y / 2) - Enemy[rVal].transform.localScale.y, 0.0f),Quaternion.identity); //랜덤한 위치와, 화면 제일 위에서 Enemy를 하나 생성
                enemy.tag = "Enemy";
                enemy.transform.parent = stackEnemy;
                enemy.transform.localPosition = new Vector3(enemy.transform.localPosition.x,enemy.transform.localPosition.y,0.0f);
                enemy.transform.localScale = Vector3.one * 40.0f;
            }
        }
    }
    void Start()
    {
        InvokeRepeating("SpawnEnemy", 4, 5); //4초후 부터, SpawnEnemy함수를 n초마다 반복해서 실행
        playBoard = GameObject.Find("PlayBoard").transform;
    }
}
