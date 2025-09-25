using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRGreyboxing
{
    
    /**i
     * Update selected colour on drag of colour picker depending on position on ui
     */
    public class SVImageControl : MonoBehaviour, IDragHandler, IPointerClickHandler
    {

        public RawImage pickerImage;
        private RawImage SVImage;
        private ColorPickerControl pickerControl;
        private RectTransform _rectTransform;
        private RectTransform _pickerTransform;

        private void Awake()
        {
            SVImage = GetComponent<RawImage>();
            pickerControl = GetComponentInParent<ColorPickerControl>();
            _rectTransform = GetComponent<RectTransform>();
            
            _pickerTransform = pickerImage.GetComponent<RectTransform>();
            _pickerTransform.position = new Vector3(-(_rectTransform.sizeDelta.x / 2), -(_rectTransform.sizeDelta.y / 2),0);
        }

        private void UpdateColour(PointerEventData eventData)
        {
            Vector2 pos = _rectTransform.transform.InverseTransformPoint(eventData.pointerCurrentRaycast.worldPosition);
            
            float deltaX = _rectTransform.sizeDelta.x / 2;
            float deltaY = _rectTransform.sizeDelta.y / 2;

            if (pos.x < -deltaX)
            {
                pos.x = -deltaX;
            }
            else if (pos.x > deltaX)
            {
                pos.x = deltaX;
            }

            if (pos.y < -deltaY)
            {
                pos.y = -deltaY;
            }
            else if (pos.y > deltaY)
            {
                pos.y = deltaY;
            }
            
            float x = pos.x+deltaX;
            float y = pos.y+deltaY;
            
            float xNorm = x / _rectTransform.sizeDelta.x;
            float yNorm = y / _rectTransform.sizeDelta.y;

            _pickerTransform.localPosition = pos;
            pickerImage.color = Color.HSVToRGB(0,0,1-yNorm);
            
            pickerControl.SetSV(xNorm, yNorm);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateColour(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UpdateColour(eventData);
        }
    }
}
