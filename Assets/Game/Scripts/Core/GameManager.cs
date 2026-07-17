using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Supinflow
{
    /// <summary>État d'une partie dans une scène de niveau.</summary>
    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// Chef d'orchestre d'une scène de niveau. Un GameManager par scène, sans
    /// singleton persistant : recharger la scène remet toute la partie à zéro,
    /// ce qui rend le reset trivial (la navigation entre niveaux et la
    /// progression sauvegardée relèveront de LevelManager / SaveSystem).
    /// Découvre seul, à l'Awake, les contenants, spawners et le LineDrawer
    /// actifs de la scène — aucun câblage Inspector :
    /// - VICTOIRE quand tous les contenants sont validés (événement Filled ;
    ///   pour un contenant de précision, quota MAINTENU sans surplus) ;
    /// - DÉFAITE si un contenant de précision déborde (événement Overflowed,
    ///   la victoire devient impossible), ou quand le sucre est épuisé — tous
    ///   les spawners à quota taris, plus aucune particule vivante — avec des
    ///   quotas incomplets. Quotas tous atteints mais maintiens en cours :
    ///   on laisse les comptes à rebours aboutir (plus rien ne peut déborder).
    ///   Une particule immobilisée sur un obstacle maintient la partie ouverte :
    ///   c'est au joueur de recommencer (R, ou ReloadLevel() du futur HUD) ;
    /// - à l'issue : robinets coupés, dessin désactivé, StateChanged levé
    ///   (futur VictoryPopup / HUDController).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private ContainerFillLevel[] containers;
        private ParticleSpawner[] spawners;
        private LineDrawer lineDrawer;
        private VictoryPopup endPanels;

        public GameState State { get; private set; } = GameState.Playing;

        // Titre du bandeau de défaite (la cause varie : épuisement, débordement).
        private string lossTitle = "";

        /// <summary>Levé une seule fois par partie, à la victoire ou à la défaite.</summary>
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            containers = FindObjectsByType<ContainerFillLevel>(FindObjectsSortMode.None);
            spawners = FindObjectsByType<ParticleSpawner>(FindObjectsSortMode.None);
            lineDrawer = FindAnyObjectByType<LineDrawer>();
            endPanels = FindAnyObjectByType<VictoryPopup>();

            if (containers.Length == 0)
            {
                Debug.LogWarning("GameManager : aucun contenant actif dans la scène — la victoire est impossible.", this);
            }
            if (spawners.Length == 0)
            {
                Debug.LogWarning("GameManager : aucun spawner actif dans la scène — la défaite par épuisement est impossible.", this);
            }
            if (lineDrawer == null)
            {
                Debug.LogWarning("GameManager : pas de LineDrawer dans la scène — rien à désactiver en fin de partie.", this);
            }

            foreach (ContainerFillLevel container in containers)
            {
                container.Filled += OnContainerFilled;
                container.Overflowed += OnContainerOverflowed;
            }
        }

        private void OnDestroy()
        {
            foreach (ContainerFillLevel container in containers)
            {
                if (container != null)
                {
                    container.Filled -= OnContainerFilled;
                    container.Overflowed -= OnContainerOverflowed;
                }
            }
        }

        private void Update()
        {
            // Reset possible à tout moment (règle du sujet), y compris après l'issue.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                ReloadLevel();
                return;
            }

            // Quotas tous atteints = seuls des maintiens de précision restent :
            // plus aucune particule ne peut déborder, on les laisse aboutir.
            if (State == GameState.Playing && IsOutOfSugar() && !AllQuotasReached())
            {
                Lose("Niveau perdu : plus de particules et des quotas incomplets — R pour recommencer.",
                    "PLUS DE PARTICULES");
            }
        }

        private void OnContainerFilled(ContainerFillLevel _)
        {
            if (State != GameState.Playing) return;

            foreach (ContainerFillLevel container in containers)
            {
                if (!container.IsValidated) return;
            }
            Win();
        }

        private void OnContainerOverflowed(ContainerFillLevel container)
        {
            if (State != GameState.Playing) return;

            Lose($"Niveau perdu : {container.name} a débordé — R pour recommencer.",
                "DÉBORDEMENT !");
        }

        /// <summary>Tous les quotas sont atteints (les maintiens de précision, eux,
        /// peuvent encore être en cours — voir IsValidated).</summary>
        private bool AllQuotasReached()
        {
            foreach (ContainerFillLevel container in containers)
            {
                if (!container.IsFull) return false;
            }
            return true;
        }

        /// <summary>Plus aucune particule à venir (spawners épuisés) ni en vie.</summary>
        private bool IsOutOfSugar()
        {
            if (spawners.Length == 0) return false;
            if (ParticleController.AliveCount > 0) return false;

            foreach (ParticleSpawner spawner in spawners)
            {
                if (!spawner.IsExhausted) return false;
            }
            return true;
        }

        private void Win()
        {
            State = GameState.Won;
            EndRound();
            SaveVictory();
            Debug.Log("Niveau réussi : tous les contenants sont validés.", this);
        }

        /// <summary>Enregistre la victoire dans la progression (SaveSystem, lue
        /// par LevelSelectUI) : niveau courant marqué terminé, niveau suivant
        /// déverrouillé. Le numéro du niveau est lu à la fin du nom de la scène
        /// (« Level_3 » → 3) ; les scènes sans numéro (Level_Test) ne comptent
        /// pas.</summary>
        private void SaveVictory()
        {
            Match match = Regex.Match(SceneManager.GetActiveScene().name, @"(\d+)$");
            if (!match.Success) return;

            int level = int.Parse(match.Groups[1].Value);
            SaveSystem.MarkLevelCompleted(level);
            SaveSystem.UnlockLevel(level + 1);
        }

        private void Lose(string logMessage, string bannerTitle)
        {
            State = GameState.Lost;
            lossTitle = bannerTitle;
            EndRound();
            Debug.Log(logMessage, this);
        }

        /// <summary>Fige la partie : robinets coupés, dessin désactivé, UI prévenue.</summary>
        private void EndRound()
        {
            foreach (ParticleSpawner spawner in spawners)
            {
                spawner.StopSpawning();
            }

            if (lineDrawer != null)
            {
                lineDrawer.enabled = false;
            }

            StateChanged?.Invoke(State);
        }

        /// <summary>Recharge la scène courante : reset complet de la partie.
        /// La scène doit figurer dans les Build Settings. Aussi destiné au
        /// bouton Reset du futur HUD.</summary>
        public void ReloadLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---- Témoin visuel de secours ---------------------------------------
        // Affiche l'issue de la partie sans aucun setup de scène. Se tait dès
        // qu'un VictoryPopup gère les vrais panneaux de fin de partie ; reste
        // le filet de sécurité des scènes de test sans UI câblée.
        private void OnGUI()
        {
            if (State == GameState.Playing) return;
            if (endPanels != null) return;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = State == GameState.Won
                ? new Color(0f, 1f, 0.53f)  // vert "complété" de la DA (#00FF88)
                : new Color(1f, 0.3f, 0.3f);

            var hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            hintStyle.normal.textColor = new Color(0f, 0.96f, 1f); // cyan DA (#00F5FF)

            string title = State == GameState.Won ? "NIVEAU RÉUSSI" : lossTitle;
            GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, 60f), title, titleStyle);
            GUI.Label(new Rect(0f, Screen.height * 0.35f + 70f, Screen.width, 30f), "R — RECOMMENCER", hintStyle);
        }
    }
}
