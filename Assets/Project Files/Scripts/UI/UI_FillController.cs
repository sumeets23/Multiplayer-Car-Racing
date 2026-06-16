using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


namespace MyRacingUI
{
    public class UI_FillController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("UI References")]
        public Slider fillSlider;
        public TextMeshProUGUI valueText;

        [Header("Animation Settings")]
        [Range(0.5f, 1.0f)]
        public float scaleFactor = 0.9f;
        private Vector3 originalScale;

        [Header("Value Settings")]
        public string suffix = "/100";
        public bool showAsPercentage = false;

        void Start ()
        {
            
            originalScale = transform.localScale;

         
            SyncUI();
        }



        public void OnPointerDown (PointerEventData eventData)
        {
            // Shrinks the element when pressed
            transform.localScale = originalScale * scaleFactor;
        }

        public void OnPointerUp (PointerEventData eventData)
        {
            // Resets the scale when the press is released
            transform.localScale = originalScale;
        }

 

        /// <summary>
        /// Call this from other scripts (e.g., CarController) to update the bar.
        /// </summary>
        /// <param name="current">The current amount (e.g., current fuel)</param>
        /// <param name="max">The maximum possible amount (e.g., tank capacity)</param>
        public void UpdateValue (float current, float max)
        {
            if (fillSlider != null)
            {
                fillSlider.maxValue = max;
                fillSlider.value = current;
            }
            SyncUI();
        }

        /// <summary>
        /// Updates the text label based on the slider's current state.
        /// </summary>
        public void SyncUI ()
        {
            if (fillSlider == null || valueText == null) return;

            if (showAsPercentage)
            {
               
                float percent = (fillSlider.value / fillSlider.maxValue) * 100f;
                valueText.text = percent.ToString("0") + "%";
            }
            else
            {
         
                valueText.text = fillSlider.value.ToString("0") + suffix;
            }
        }



        private void OnValidate ()
        {
            
            if (!Application.isPlaying)
            {
                SyncUI();
            }
        }
    }
}