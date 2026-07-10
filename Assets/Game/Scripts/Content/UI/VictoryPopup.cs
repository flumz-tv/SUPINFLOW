using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Supinflow
{
    /// <summary>
    /// Panneaux de fin de partie : victoire (VictoryPanel) et défaite
    /// (LoosePanel). À poser sur le Canvas de la scène de niveau, avec les deux
    /// instances de prefab assignées. S'abonne au StateChanged du GameManager
    /// (auto-découvert) et affiche le panneau correspondant à l'issue ; les
    /// deux panneaux sont masqués au lancement quel que soit leur état dans
    /// l'éditeur (on peut donc les laisser visibles pour les mettre en page).
    /// Boutons (auto-découverts : premier Button enfant de chaque panneau,
    /// assignables dans l'Inspector si le panneau en gagne d'autres) :
    /// - NEXT (victoire) : charge nextSceneName si renseigné, sinon grisé —
    ///   la navigation entre niveaux arrivera avec LevelManager ;
    /// - RETRY (défaite) : recharge la scène via GameManager.ReloadLevel().
    /// La touche R du GameManager reste active en parallèle. Tant que ce
    /// composant est présent, le bandeau OnGUI provisoire du GameManager se
    /// tait (il reste le filet de sécurité des scènes sans UI câblée).
    /// Apparition animée par code (aucun Animator à câbler) : délai pour voir
    /// la dernière particule, puis fondu + pop d'échelle avec léger dépassement
    /// (ease out back). Un CanvasGroup est ajouté au panneau si absent ; les
    /// boutons ne sont cliquables qu'une fois l'animation terminée.
    /// </summary>
    public class VictoryPopup : MonoBehaviour
    {
        [Tooltip("Instance du prefab VictoryPanel (enfant du Canvas).")]
        [SerializeField] private GameObject victoryPanel;

        [Tooltip("Instance du prefab LoosePanel (enfant du Canvas).")]
        [SerializeField] private GameObject loosePanel;

        [Tooltip("Scène chargée par le bouton NEXT (doit figurer dans les Build Settings). Vide = bouton grisé, en attendant LevelManager.")]
        [SerializeField] private string nextSceneName;

        [Header("Animation d'apparition")]
        [Min(0f)]
        [Tooltip("Délai avant l'apparition du panneau, le temps de voir la fin de partie à l'écran (secondes).")]
        [SerializeField] private float appearDelay = 0.4f;

        [Min(0f)]
        [Tooltip("Durée du fondu + pop d'échelle (secondes). 0 = apparition sèche.")]
        [SerializeField] private float appearDuration = 0.35f;

        [Range(0f, 1f)]
        [Tooltip("Échelle de départ du pop (1 = pas de pop, juste le fondu).")]
        [SerializeField] private float appearStartScale = 0.8f;

        [Header("Boutons (optionnel — auto-découverts sinon)")]
        [Tooltip("Bouton NEXT du panneau de victoire. Vide = premier Button enfant du panneau.")]
        [SerializeField] private Button nextButton;

        [Tooltip("Bouton RETRY du panneau de défaite. Vide = premier Button enfant du panneau.")]
        [SerializeField] private Button retryButton;

        private GameManager gameManager;

        private void Awake()
        {
            gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("VictoryPopup : pas de GameManager dans la scène — les panneaux ne s'afficheront jamais.", this);
            }
            else
            {
                gameManager.StateChanged += OnStateChanged;
            }

            SetupPanel(victoryPanel, ref nextButton, "VictoryPanel");
            SetupPanel(loosePanel, ref retryButton, "LoosePanel");

            if (nextButton != null)
            {
                if (string.IsNullOrEmpty(nextSceneName))
                {
                    // Pas encore de niveau suivant à charger : bouton visible
                    // mais grisé (LevelManager prendra le relais).
                    nextButton.interactable = false;
                }
                else
                {
                    nextButton.onClick.AddListener(LoadNextScene);
                }
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= OnStateChanged;
            }
        }

        /// <summary>Masque le panneau et résout son bouton s'il n'est pas assigné.</summary>
        private void SetupPanel(GameObject panel, ref Button button, string label)
        {
            if (panel == null)
            {
                Debug.LogWarning($"VictoryPopup : {label} non assigné dans l'Inspector — cette issue n'aura pas de panneau.", this);
                return;
            }

            panel.SetActive(false);

            if (button == null)
            {
                button = panel.GetComponentInChildren<Button>(true);
                if (button == null)
                {
                    Debug.LogWarning($"VictoryPopup : aucun Button trouvé sous {label}.", this);
                }
            }
        }

        private void OnStateChanged(GameState state)
        {
            GameObject panel = state == GameState.Won ? victoryPanel : loosePanel;
            if (panel != null)
            {
                StartCoroutine(ShowPanel(panel));
            }
        }

        /// <summary>Apparition du panneau : délai, puis fondu + pop d'échelle
        /// (ease out back, léger dépassement avant de se poser). Interaction
        /// bloquée pendant l'animation.</summary>
        private IEnumerator ShowPanel(GameObject panel)
        {
            // WaitForSecondsRealtime : l'apparition ne dépend pas du timeScale
            // (un futur ralenti/pause de fin de partie ne la gèlerait pas).
            if (appearDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(appearDelay);
            }

            panel.SetActive(true);
            if (appearDuration <= 0f) yield break;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            Transform panelTransform = panel.transform;
            Vector3 targetScale = panelTransform.localScale;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < appearDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / appearDuration);
                group.alpha = progress;
                // LerpUnclamped : l'ease out back dépasse 1, l'échelle doit
                // pouvoir dépasser sa cible pour donner le rebond.
                panelTransform.localScale = targetScale
                    * Mathf.LerpUnclamped(appearStartScale, 1f, EaseOutBack(progress));
                yield return null;
            }

            group.alpha = 1f;
            panelTransform.localScale = targetScale;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        /// <summary>Ease out back standard : part vite, dépasse légèrement la
        /// cible puis revient s'y poser.</summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private void Retry()
        {
            if (gameManager != null)
            {
                gameManager.ReloadLevel();
            }
        }

        private void LoadNextScene()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
