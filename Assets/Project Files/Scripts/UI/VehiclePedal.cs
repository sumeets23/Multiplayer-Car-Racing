using UnityEngine;
using UnityEngine.EventSystems;


namespace MyRacingUI
{
    public class VehiclePedal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // This variable indicates whether the pedal is currently being held down
        [HideInInspector]
        public bool isPressed = false;

        [Header("✨ Visual Feedback")]
        public Vector3 pressedScale = new Vector3(0.95f, 0.95f, 0.95f);
        private Vector3 originalScale;

        void Start ()
        {
            // Store the initial scale to return to it later
            originalScale = transform.localScale;
        }

        /// <summary>
        /// Triggered when the player touches or clicks the pedal 🖱️📱
        /// </summary>
        public void OnPointerDown (PointerEventData eventData)
        {
            isPressed = true;
            transform.localScale = pressedScale; // Visual feedback: Shrink
            Debug.Log(gameObject.name + " Pressed");
        }

        /// <summary>
        /// Triggered when the player releases the pedal ⬆️
        /// </summary>
        public void OnPointerUp (PointerEventData eventData)
        {
            isPressed = false;
            transform.localScale = originalScale; // Return to original size
            Debug.Log(gameObject.name + " Released");
        }
    }
}