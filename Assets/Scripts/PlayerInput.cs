using UnityEngine;
using System;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance;
    public Action<Enums.Input> InputUpdate;
    
    Enums.Input _input;

    void Awake() 
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) 
        {
            _input = Enums.Input.SHOOT;
            InputUpdate?.Invoke(_input);
            return;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) 
        {
            _input = Enums.Input.MOVE_LEFT;
            InputUpdate?.Invoke(_input);
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            _input = Enums.Input.MOVE_RIGHT;
            InputUpdate?.Invoke(_input);
            return;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) 
        {
            _input = Enums.Input.NONE;
            InputUpdate?.Invoke(_input);
        }
    }

    public Enums.Input GetInput() 
    {
        return _input;
    }
}
