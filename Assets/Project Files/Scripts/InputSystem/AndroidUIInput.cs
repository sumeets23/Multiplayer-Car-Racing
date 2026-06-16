using UnityEngine;
using UnityEngine.EventSystems;

namespace InputSystem
{
    /// <summary>
    /// Attach this script to a UI Manager or an empty GameObject in your Canvas.
    /// Provide a reference to the GameplayInputReader, and link the UI Buttons using EventTriggers (PointerDown / PointerUp) to the methods in this script.
    /// </summary>
    public class AndroidUIInput : MonoBehaviour
    {
        [Tooltip("The ScriptableObject that manages inputs. Must be assigned.")]
        [SerializeField] private GameplayInputReader inputReader;

        private bool _leftPressed;
        private bool _rightPressed;

        // ================= GAS (ACCELERATOR) =================

        /// <summary>
        /// Call this on the Gas UI Button's EventTrigger (PointerDown).
        /// </summary>
        public void OnGasDown()
        {
            if (inputReader != null)
                inputReader.MobileGasDown();
        }

        /// <summary>
        /// Call this on the Gas UI Button's EventTrigger (PointerUp).
        /// </summary>
        public void OnGasUp()
        {
            if (inputReader != null)
                inputReader.MobileGasUp();
        }

        // ================= BRAKE =================
        
        /// <summary>
        /// Call this on the Brake UI Button's EventTrigger (PointerDown).
        /// </summary>
        public void OnBrakeDown()
        {
            if (inputReader != null)
                inputReader.MobileBrakeDown();
        }

        /// <summary>
        /// Call this on the Brake UI Button's EventTrigger (PointerUp).
        /// </summary>
        public void OnBrakeUp()
        {
            if (inputReader != null)
                inputReader.MobileBrakeUp();
        }

        // ================= STEERING (LEFT) =================

        /// <summary>
        /// Call this on the Left UI Button's EventTrigger (PointerDown).
        /// </summary>
        public void OnSteerLeftDown()
        {
            _leftPressed = true;
            UpdateSteering();
        }

        /// <summary>
        /// Call this on the Left UI Button's EventTrigger (PointerUp).
        /// </summary>
        public void OnSteerLeftUp()
        {
            _leftPressed = false;
            UpdateSteering();
        }

        // ================= STEERING (RIGHT) =================

        /// <summary>
        /// Call this on the Right UI Button's EventTrigger (PointerDown).
        /// </summary>
        public void OnSteerRightDown()
        {
            _rightPressed = true;
            UpdateSteering();
        }

        /// <summary>
        /// Call this on the Right UI Button's EventTrigger (PointerUp).
        /// </summary>
        public void OnSteerRightUp()
        {
            _rightPressed = false;
            UpdateSteering();
        }

        // ================= INTERNAL UPDATE =================

        private void UpdateSteering()
        {
            if (inputReader == null) return;

            float steerValue = 0f;
            if (_leftPressed) steerValue -= 1f;
            if (_rightPressed) steerValue += 1f;
            
            inputReader.MobileSteerContext(steerValue);
        }

        public void Test()
        {
            Debug.Log("Test");
        }
    }
}
