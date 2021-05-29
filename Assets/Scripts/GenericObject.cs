using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GenericObject : MonoBehaviour
{
    [SerializeField] RectTransform _rectTransform = null;
    [SerializeField] Image _image = null;
    [SerializeField] TextMeshProUGUI _id = null;
    
    public void Set(GalagaObject p_objectData) 
    {
        _rectTransform.anchoredPosition = new Vector2(p_objectData.horizontalPosition, p_objectData.verticalPosition) * _rectTransform.rect.width;
        _id.text = p_objectData.id.ToString();
        
        float __r = p_objectData.id >> 5;
        float __b = (p_objectData.id & 0b00011100) >> 2;
        float __g = p_objectData.id & 0b00000011;
        __r /= 7;
        __b /= 7;
        __g /= 3;
        _image.color = new Color(__r, __g, __b, 0.7f);
        _id.color = new Color(1 - __r, 1 - __g, 1 - __b, 0.7f);
    }
}
