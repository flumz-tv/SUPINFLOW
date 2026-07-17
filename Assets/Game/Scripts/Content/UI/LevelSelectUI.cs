using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Supinflow
{
    /// <summary>
    /// Écran de sélection des niveaux. À poser sur le panneau qui contient les
    /// boutons de niveaux (PlayPanel). Les boutons sont auto-découverts par le
    /// nom de leur GameObject (« lvl1 » … « lvl10 », espace toléré : « lvl 7 »)
    /// — aucun câblage Inspector. Trois états par bouton, selon SaveSystem :
    /// - VERROUILLÉ : numéro masqué, cadenas « lock » affiché, bouton grisé ;
    /// - DÉVERROUILLÉ : numéro visible, clic = chargement de la scène
    ///   (sceneNameFormat). Si la scène manque dans les Build Settings, le
    ///   bouton reste grisé avec un avertissement en console ;
    /// - TERMINÉ : icône « victory » à la place du numéro, bouton toujours
    ///   cliquable (un niveau fini reste rejouable). Sans icône disponible, le
    ///   numéro reste affiché.
    /// Les icônes d'un bouton sont ses enfants dont le nom commence par
    /// « lock » / « victory » ; s'il n'en a pas, elles sont clonées au
    /// lancement depuis lockTemplate / victoryTemplate (assignables dans
    /// l'Inspector, sinon premiers objets de ces noms trouvés sous ce panneau
    /// — il suffit d'en poser un de chaque, même désactivés). Le clone est
    /// étiré à la taille de son bouton, moins une marge relative (iconPadding),
    /// sans déformer le sprite ; une icône posée à la main garde sa mise en
    /// page. L'état est rafraîchi à chaque ouverture du panneau (OnEnable).
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Tooltip("Nom de scène chargé au clic, {0} = numéro du niveau (scènes à ajouter aux Build Settings).")]
        [SerializeField] private string sceneNameFormat = "Level_{0}";

        [Tooltip("Cadenas cloné dans les boutons sans enfant « lock ». Vide = premier objet nommé lock trouvé sous ce panneau.")]
        [SerializeField] private GameObject lockTemplate;

        [Tooltip("Icône de niveau terminé, clonée dans les boutons sans enfant « victory ». Vide = premier objet nommé victory trouvé sous ce panneau.")]
        [SerializeField] private GameObject victoryTemplate;

        [FormerlySerializedAs("lockPadding")]
        [Range(0f, 0.45f)]
        [Tooltip("Marge des icônes clonées (cadenas, victoire) à l'intérieur de leur bouton, en fraction de sa taille (0 = plein bouton).")]
        [SerializeField] private float iconPadding = 0.15f;

        // « lvl » + numéro, insensible à la casse, espace toléré (« lvl 7 »).
        // Ne matche pas les autres boutons du panneau (BACK, « lvl lock »…).
        private static readonly Regex ButtonNamePattern =
            new Regex(@"^lvl\s*(\d+)$", RegexOptions.IgnoreCase);

        private class LevelEntry
        {
            public int index;
            public Button button;
            public GameObject label;
            public GameObject lockIcon;
            public GameObject victoryIcon;
            public bool sceneExists;
        }

        private readonly List<LevelEntry> entries = new List<LevelEntry>();

        private void Awake()
        {
            lockTemplate = ResolveTemplate(lockTemplate, "lock");
            victoryTemplate = ResolveTemplate(victoryTemplate, "victory");

            if (lockTemplate == null)
            {
                Debug.LogWarning($"LevelSelectUI : aucun objet « lock » sous {name} — les boutons sans cadenas propre n'afficheront pas d'état verrouillé.", this);
            }
            if (victoryTemplate == null)
            {
                Debug.LogWarning($"LevelSelectUI : aucun objet « victory » sous {name} — les niveaux terminés garderont leur numéro.", this);
            }

            foreach (Button candidate in GetComponentsInChildren<Button>(true))
            {
                Match match = ButtonNamePattern.Match(candidate.gameObject.name);
                if (!match.Success) continue;

                int index = int.Parse(match.Groups[1].Value);
                string sceneName = string.Format(sceneNameFormat, index);
                bool sceneExists = Application.CanStreamedLevelBeLoaded(sceneName);
                if (!sceneExists)
                {
                    Debug.LogWarning($"LevelSelectUI : la scène {sceneName} est absente des Build Settings — {candidate.name} restera grisé même déverrouillé.", candidate);
                }

                candidate.onClick.AddListener(() => SceneManager.LoadScene(sceneName));

                entries.Add(new LevelEntry
                {
                    index = index,
                    button = candidate,
                    label = FindLabel(candidate),
                    lockIcon = FindOrCreateIcon(candidate, lockTemplate, "lock"),
                    victoryIcon = FindOrCreateIcon(candidate, victoryTemplate, "victory"),
                    sceneExists = sceneExists
                });
            }

            if (entries.Count == 0)
            {
                Debug.LogWarning($"LevelSelectUI : aucun bouton « lvlN » trouvé sous {name} — rien à gérer.", this);
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>Applique l'état verrouillé/déverrouillé/terminé de chaque
        /// bouton. Appelé à chaque ouverture du panneau ; peut aussi l'être
        /// après un déblocage si le panneau est resté ouvert.</summary>
        public void Refresh()
        {
            foreach (LevelEntry entry in entries)
            {
                bool unlocked = SaveSystem.IsLevelUnlocked(entry.index);
                bool completed = unlocked && SaveSystem.IsLevelCompleted(entry.index);
                // Sans icône de victoire, le numéro reste le repli des niveaux
                // terminés.
                bool showVictory = completed && entry.victoryIcon != null;

                if (entry.label != null) entry.label.SetActive(unlocked && !showVictory);
                if (entry.lockIcon != null) entry.lockIcon.SetActive(!unlocked);
                if (entry.victoryIcon != null) entry.victoryIcon.SetActive(showVictory);
                entry.button.interactable = unlocked && entry.sceneExists;
            }
        }

        /// <summary>Gabarit d'icône : assigné dans l'Inspector, sinon premier
        /// objet sous ce panneau (inactifs compris) dont le nom commence par
        /// iconName. Peut rester introuvable si chaque bouton a déjà le sien.</summary>
        private GameObject ResolveTemplate(GameObject assigned, string iconName)
        {
            if (assigned != null) return assigned;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name.StartsWith(iconName, StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>Texte du numéro : premier texte TMP du bouton (inactifs compris).</summary>
        private static GameObject FindLabel(Button button)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                Debug.LogWarning($"LevelSelectUI : aucun texte TMP sous {button.name} — pas de numéro à masquer.", button);
                return null;
            }
            return text.gameObject;
        }

        /// <summary>Icône d'état du bouton (« lock », « victory ») : son enfant
        /// de ce nom s'il existe (mise en page respectée), sinon un clone du
        /// gabarit étiré à la taille du bouton moins iconPadding, sans déformer
        /// le sprite. Null si ni l'un ni l'autre.</summary>
        private GameObject FindOrCreateIcon(Button button, GameObject template, string iconName)
        {
            GameObject icon = null;

            foreach (Transform child in button.GetComponentsInChildren<Transform>(true))
            {
                if (child != button.transform && child.name.StartsWith(iconName, StringComparison.OrdinalIgnoreCase))
                {
                    icon = child.gameObject;
                    break;
                }
            }

            if (icon == null)
            {
                if (template == null) return null;

                icon = Instantiate(template, button.transform);
                icon.name = iconName;

                if (icon.transform is RectTransform rect)
                {
                    rect.anchorMin = new Vector2(iconPadding, iconPadding);
                    rect.anchorMax = new Vector2(1f - iconPadding, 1f - iconPadding);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                }

                Image image = icon.GetComponent<Image>();
                if (image != null)
                {
                    image.preserveAspect = true;
                }
            }

            // L'icône est purement décorative : aucun de ses Graphics ne doit
            // capter les clics, sinon un Button embarqué dans le gabarit
            // (prefabs « lvl lock » / « lvl finish ») avale le clic destiné au
            // bouton de niveau — impossible alors de rejouer un niveau fini.
            foreach (Graphic graphic in icon.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            return icon;
        }
    }
}
