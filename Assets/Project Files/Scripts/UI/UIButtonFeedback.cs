using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace MyRacingUI
{
    public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("✨ Animation Settings")]
        [Tooltip("How much the button shrinks when pressed (e.g., 0.9)")]
        public float scaleFactor = 0.9f;

        private Vector3 originalScale;

        void Start ()
        {
            // Store the initial scale to ensure we return to the correct size
            originalScale = transform.localScale;
        }

        /// <summary>
        /// Triggered when the button is pressed 🖱️
        /// </summary>
        public void OnPointerDown (PointerEventData eventData)
        {
            // Shrink the button for visual feedback
            transform.localScale = originalScale * scaleFactor;
            Debug.Log(gameObject.name + " Pressed");
        }

        /// <summary>
        /// Triggered when the press is released ⬆️
        /// </summary>
        public void OnPointerUp (PointerEventData eventData)
        {
            // Return to the original scale
            transform.localScale = originalScale;
        }
    }
}