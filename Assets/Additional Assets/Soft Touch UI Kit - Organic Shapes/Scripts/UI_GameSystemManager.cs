using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;




namespace SoftTouchUIKit
{
    public class UI_GameSystemManager : MonoBehaviour
    {
        [Header("⏸️ Pause System")]
        public GameObject pausePanel;
        private bool isPaused = false;

        [Header("🔊 Audio Settings")]
        public Image audioButtonImage;
        public Sprite audioOnSprite;
        public Sprite audioOffSprite;
        private bool isMuted = false;

        [Header("🌓 Day/Night Settings")]
        public Camera mainCamera;
        public Light worldLight; // For 3D environments (Directional Light)
        public Color dayColor = new Color(0.5f, 0.7f, 1f);
        public Color nightColor = new Color(0.1f, 0.1f, 0.2f);
        [Range(0, 1)] public float dayIntensity = 1f;   // Light intensity during the day
        [Range(0, 1)] public float nightIntensity = 0.1f; // Light intensity during the night
        private bool isNight = false;

        void Start ()
        {
            // Resets time scale to normal at the start of the game
            Time.timeScale = 1f;

            // Ensure the pause panel is hidden when the game starts
            if (pausePanel != null) pausePanel.SetActive(false);
        }


        /// <summary>
        /// Loads a specific scene by name. Useful for Back buttons or Menu navigation.
        /// </summary>
        /// <param name="sceneName">The exact name of the target scene.</param>
        public void LoadScene (string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                Time.timeScale = 1f; // Always reset time before switching scenes
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("Scene name is empty! Please assign it in the Inspector.");
            }
        }

        // --- PAUSE & RESUME LOGIC 🕹️ ---
        public void PauseGame ()
        {
            isPaused = true;
            Time.timeScale = 0f; // Stops game physics and time-based movements
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void ResumeGame ()
        {
            isPaused = false;
            Time.timeScale = 1f; // Resumes game to normal speed
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void TogglePause ()
        {
            // Switches between pause and resume states
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        // --- SCENE MANAGEMENT 🔄 ---

        public void RestartGame ()
        {
            // Reset time scale before reloading the scene to avoid issues
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ExitGame ()
        {
            // Quits the application (Only works in built builds)
            Application.Quit();
        }

        // --- AUDIO CONTROLS 🎵 ---

        public void ToggleAudio ()
        {
            isMuted = !isMuted;

            // Mutes or unmutes the entire global audio listener
            AudioListener.pause = isMuted;

            // Update the button UI icon based on the current mute state
            if (audioButtonImage != null && audioOnSprite != null && audioOffSprite != null)
            {
                audioButtonImage.sprite = isMuted ? audioOffSprite : audioOnSprite;
            }
        }

        // --- VISUAL SETTINGS (2D & 3D) 🎨 ---

        public void ToggleDayNight ()
        {
            isNight = !isNight;

            // 2D Mode: Modifies the camera's background solid color
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = isNight ? nightColor : dayColor;
            }

            // 3D Mode: Adjusts the Directional Light's intensity and color
            if (worldLight != null)
            {
                worldLight.intensity = isNight ? nightIntensity : dayIntensity;

                // Optional: Changes the light's color to a midnight blue tint during the night
                worldLight.color = isNight ? nightColor : Color.white;
            }
        }

       
    }
}