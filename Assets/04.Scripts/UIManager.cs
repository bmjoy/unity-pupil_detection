using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager S = null;

    public enum STATE
    {
        GAMEREADY,
        GAMESTART,
        GAMEPAUSE,
    }

    public STATE UIState = STATE.GAMEREADY;

    public List<GameObject> GameReadyUIList = new List<GameObject>();
    public List<GameObject> GameStartUIList = new List<GameObject>();
    public List<GameObject> GamePauseUIList = new List<GameObject>();

    private void Awake()
    {
        S = this;
    }

    public void ChangeUIState()
    {
        if (UIState == STATE.GAMEREADY)
        {
            foreach (var item in GameReadyUIList)
            {
                item.gameObject.SetActive(false);
            }

            UIState = STATE.GAMESTART;

            foreach (var item in GameStartUIList)
            {
                item.gameObject.SetActive(true);
            }
        }
        else if (UIState == STATE.GAMESTART)
        {
            foreach (var item in GameStartUIList)
            {
                item.gameObject.SetActive(false);
            }

            UIState = STATE.GAMEPAUSE;

            foreach (var item in GamePauseUIList)
            {
                item.gameObject.SetActive(true);
            }
        }
        else if(UIState == STATE.GAMEPAUSE)
        {
            foreach (var item in GamePauseUIList)
            {
                item.gameObject.SetActive(false);
            }

            UIState = STATE.GAMESTART;

            foreach (var item in GameStartUIList)
            {
                item.gameObject.SetActive(true);
            }
        }
    }
}
