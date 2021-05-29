using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    bool _inGame = false;

    void Start() 
    {
        UDPCommManager.Instance.SendTimedOut += EndGame;
    }

    public void StartGame() 
    {
        if (_inGame) 
        {
            return;
        }
        UDPCommManager.Instance.InitiateCommunication();
        ObjectsManager.Instance.ResetObjects();
        _inGame = true;
    }

    public void EndGame()
    {
        UDPCommManager.Instance.EndCommunication();
        _inGame = false;
    }
}
