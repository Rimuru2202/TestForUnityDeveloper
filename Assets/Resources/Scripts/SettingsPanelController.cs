using UnityEngine;
using UnityEngine.UI;

namespace Resources.Scripts
{
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Панель с настройками звука (объект, который нужно показать/спрятать).")]
        public GameObject settingsPanel;

        [Tooltip("Кнопка 'Настройки' на экране.")]
        public Button openSettingsButton;

        [Tooltip("Кнопка 'Закрыть' внутри панели настроек.")]
        public Button closeSettingsButton;

        [Tooltip("Ссылка на менеджер аудио.")]
        public AudioManager audioManager;

        [Header("Options")]
        [Tooltip("Можно ли закрывать панель нажатием ESC.")]
        public bool closeOnEsc = true;

        private bool _isOpen;

        private void Start()
        {
            if (openSettingsButton != null)
                openSettingsButton.onClick.AddListener(OpenPanel);

            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(ClosePanel);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (openSettingsButton != null)
                openSettingsButton.onClick.RemoveListener(OpenPanel);

            if (closeSettingsButton != null)
                closeSettingsButton.onClick.RemoveListener(ClosePanel);
        }

        private void Update()
        {
            if (closeOnEsc && _isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanel();
            }
        }

        private void OpenPanel()
        {
            if (settingsPanel == null) return;

            settingsPanel.SetActive(true);
            _isOpen = true;

            if (audioManager == null) return;
            audioManager.ApplyMusicVolumeFromSlider();
        }

        private void ClosePanel()
        {
            if (settingsPanel == null) return;

            settingsPanel.SetActive(false);
            _isOpen = false;

            if (audioManager == null) return;
            audioManager.ApplyMusicVolumeFromSlider();
        }
    }
}
