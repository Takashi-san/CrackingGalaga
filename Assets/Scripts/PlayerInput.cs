using UnityEngine;
using System;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance;
    
    Enums.Input _input;
    bool _alternateAutoFire;

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
        if (Input.GetKeyDown(KeyCode.LeftArrow)) 
        {
            _input = Enums.Input.MOVE_LEFT;
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            _input = Enums.Input.MOVE_RIGHT;
            return;
        }
    }

    public Enums.Input GetInput() 
    {
        Enums.Input __input = _input;
        _input = _alternateAutoFire ? Enums.Input.SHOOT : Enums.Input.NONE;
        _alternateAutoFire = !_alternateAutoFire;
        return __input;
    }
}
