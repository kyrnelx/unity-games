using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.GamePlay
{
    public class Card : MonoBehaviour, IDragHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public RectTransform rectTransform;
        public Canvas canvas;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = FindObjectOfType<Canvas>();
        }
        
        private void Update()
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta  / canvas.scaleFactor;
        }
        
        public void OnPointerMove(PointerEventData eventData)
        {

        }

        public void OnPointerExit(PointerEventData eventData)
        {
            throw new NotImplementedException();
        }
    }   
}
