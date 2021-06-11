using UnityEngine;
using System.Collections;

public class lineRendererManager : MonoBehaviour
{
    private LineRenderer lineRenderer;

    // Use this for initialization
    void Start()
    {
        //라인렌더러 설정
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.startWidth = 0.1f; 
        lineRenderer.endWidth = 0.1f;

        //라인렌더러 처음위치 나중위치
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + new Vector3(0, 100, 0));
    }

    // Update is called once per frame
    void Update()
    {
    }
}