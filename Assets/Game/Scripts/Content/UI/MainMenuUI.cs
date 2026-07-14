using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Supinflow
{
    /// <summary>
    /// Menu principal : boutons PLAY, SETTINGS et QUIT. À poser sur le Canvas
    /// de la scène MainMenu. Les boutons sont auto-découverts par leur nom de
    /// GameObject (PlayButton / SettingsButton / QuitButton) parmi les Buttons
    /// enfants, assignables dans l'Inspector si les noms changent. Le câblage
    /// onClick se fait en code — rien à renseigner dans les OnClick de
    /// l'éditeur.
    /// - PLAY : charge playSceneName (doit figurer dans les Build Settings).
    ///   Pointe sur le niveau de test en attendant l'écran de sélection de
    ///   niveaux (LevelSelectUI), qui prendra le relais.
    /// - SETTINGS : ouvre/ferme le panneau settingsPanel s'il est assigné,
    ///   sinon bouton grisé (le panneau arrivera avec SettingsPanel).
    /// - QUIT : quitte l'application (arrête le mode Play dans l'éditeur).
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Tooltip("Scène chargée par PLAY (doit figurer dans les Build Settings). Sera remplacée par l'écran de sélection de niveaux.")]
        [SerializeField] private string playSceneName = "Level_Test";

        [Tooltip("Panneau de réglages ouvert par SETTINGS. Vide = bouton grisé, en attendant SettingsPanel.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Boutons (optionnel — auto-découverts par nom sinon)")]
        [Tooltip("Vide = Button enfant nommé PlayButton.")]
        [SerializeField] private Button playButton;

        [Tooltip("Vide = Button enfant nommé SettingsButton.")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Vide = Button enfant nommé QuitButton.")]
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            ResolveButton(ref playButton, "PlayButton");
            ResolveButton(ref settingsButton, "SettingsButton");
            ResolveButton(ref quitButton, "QuitButton");

            if (playButton != null)
            {
                if (string.IsNullOrEmpty(playSceneName))
                {
                    playButton.interactable = false;
                }
                else
                {
                    playButton.onClick.AddListener(Play);
                }
            }

            if (settingsButton != null)
            {
                if (settingsPanel == null)
                {
                    // Pas encore de panneau de réglages : bouton visible mais
                    // grisé (SettingsPanel prendra le relais).
                    settingsButton.interactable = false;
                }
                else
                {
                    settingsPanel.SetActive(false);
                    settingsButton.onClick.AddListener(ToggleSettings);
                }
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(Quit);
            }
        }

        /// <summary>Résout un bouton par le nom de son GameObject parmi les
        /// Buttons enfants (inactifs compris) s'il n'est pas assigné.</summary>
        private void ResolveButton(ref Button button, string gameObjectName)
        {
            if (button != null) return;

            foreach (Button candidate in GetComponentsInChildren<Button>(true))
            {
                if (candidate.gameObject.name == gameObjectName)
                {
                    button = candidate;
                    return;
                }
            }

            Debug.LogWarning($"MainMenuUI : aucun Button nommé {gameObjectName} trouvé sous {name} — ce bouton restera muet.", this);
        }

        private void Play()
        {
            SceneManager.LoadScene(playSceneName);
        }

        private void ToggleSettings()
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            // Application.Quit est sans effet dans l'éditeur : on arrête le
            // mode Play pour que le bouton reste testable.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
