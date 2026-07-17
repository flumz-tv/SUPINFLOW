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
        [Tooltip("Vide = Button enfant nommé QuitButton.")]
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            ResolveButton(ref quitButton, "QuitButton");

           
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
