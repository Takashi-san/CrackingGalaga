using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsManager : MonoBehaviour
{
    public static ObjectsManager Instance;

    [SerializeField] int _poolInitialSize = 20;
    [SerializeField] GameObject _genericObjectPrefab = null;

    List<GenericObject> _objectPool = new List<GenericObject>();

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

    void Start() 
    {
        Init();
        UDPCommManager.Instance.ReceivedPacket += UpdateObjects;
    }

    void Init() 
    {
        for (int i = 0; i < _poolInitialSize; i++)
        {
            GenericObject __newObject = Instantiate(_genericObjectPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<GenericObject>();
            __newObject.gameObject.SetActive(false);
            _objectPool.Add(__newObject);
        }
    }

    GenericObject GetObjectFromPool() 
    {
        GenericObject __availableObject = _objectPool.Find(__object => !__object.gameObject.activeInHierarchy);
        if (__availableObject == null) 
        {
            __availableObject = Instantiate(_genericObjectPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<GenericObject>();
            __availableObject.gameObject.SetActive(false);
            _objectPool.Add(__availableObject);
        }
        __availableObject.gameObject.SetActive(true);
        return __availableObject;
    }

    public void ResetObjects() 
    {
        foreach (var __object in _objectPool)
        {
            __object.gameObject.SetActive(false);
        }
    }
    
    void UpdateObjects(UDPCommManager.ResponsePacket p_packetData) 
    {
        ResetObjects();
        foreach (var __object in p_packetData.objectList)
        {
            GetObjectFromPool().Set(__object);
        }
    }
}
