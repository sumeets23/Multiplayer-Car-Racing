using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



namespace MyRacingUI
{
    public class UI_GameSystemManager : MonoBehaviour
    {
        [Header("Pause System")]
        public GameObject pausePanel;
        private bool isPaused = false;

        [Header("Audio Settings")]
        public Image audioButtonImage;
        public Sprite audioOnSprite;
        public Sprite audioOffSprite;
        private bool isMuted = false;

        [Header("Day/Night Settings")]
        public Camera mainCamera;
        public Light worldLight; 
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


        public void PauseGame ()
        {
            isPaused = true;
            Time.timeScale = 0f; 
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void ResumeGame ()
        {
            isPaused = false;
            Time.timeScale = 1f; 
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void TogglePause ()
        {
         
            if (isPaused) ResumeGame();
            else PauseGame();
        }


        public void RestartGame ()
        {
            // Reset time scale before reloading the scene to avoid issues
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ExitGame ()
        {
          
            Application.Quit();
        }

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


        public void ToggleDayNight ()
        {
            isNight = !isNight;

        
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = isNight ? nightColor : dayColor;
            }

         
            if (worldLight != null)
            {
                worldLight.intensity = isNight ? nightIntensity : dayIntensity;

                
                worldLight.color = isNight ? nightColor : Color.white;
            }
        }
    }
}