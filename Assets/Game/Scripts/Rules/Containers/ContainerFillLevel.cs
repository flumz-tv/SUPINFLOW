using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Une couche de liquide d'un contenant : une couleur acceptée (ou toutes,
    /// pour le sucre "normal" du niveau 8) et un quota de particules.
    /// Un contenant simple (bécher, éprouvette) a 1 couche ; l'erlenmeyer
    /// bicolore du niveau 6 en a 2 (moitié/moitié).
    /// </summary>
    [Serializable]
    public class FillLayer
    {
        [Tooltip("Couleur acceptée par cette couche.")]
        public ParticleColor color = ParticleColor.Blue;

        [Tooltip("Si vrai, cette couche accepte toutes les couleurs (sucre « normal »).")]
        public bool acceptAnyColor;

        [Tooltip("Particules nécessaires pour remplir cette couche.")]
        public int requiredCount = 30;

        [NonSerialized] public int currentCount;

        public bool IsFull => currentCount >= requiredCount;

        public bool Accepts(ParticleColor particleColor)
        {
            return acceptAnyColor || color == particleColor;
        }
    }

    /// <summary>
    /// Suivi du remplissage d'un contenant, organisé en couches (voir FillLayer).
    /// Compte les particules valides reçues via ContainerBase, empile les couches
    /// de liquide visuellement depuis le fond, et lève l'événement Filled quand
    /// toutes les couches ont atteint leur quota (écouté plus tard par le
    /// GameManager pour la détection de victoire).
    /// La zone de liquide est décrite par fillWidth / fillHeight / fillBottomLocalY
    /// en coordonnées locales du contenant — à caler sur la zone de remplissage du
    /// sprite de verrerie (60 % de la hauteur pour le bécher, 68 % pour l'éprouvette).
    /// </summary>
    public class ContainerFillLevel : MonoBehaviour
    {
        [Tooltip("Couches de liquide, de bas en haut. 1 couche = contenant simple.")]
        [SerializeField] private List<FillLayer> layers = new List<FillLayer> { new FillLayer() };

        [Tooltip("Un SpriteRenderer par couche (même ordre), teinté et redimensionné par le script.")]
        [SerializeField] private SpriteRenderer[] layerRenderers;

        [Tooltip("Largeur de la zone de liquide, en local.")]
        [SerializeField] private float fillWidth = 1.5f;

        [Tooltip("Hauteur de la zone de liquide quand le contenant est plein, en local.")]
        [SerializeField] private float fillHeight = 1.9f;

        [Tooltip("Ordonnée locale du fond de la zone de liquide.")]
        [SerializeField] private float fillBottomLocalY = -0.95f;

        [Tooltip("Texte affiché au joueur (particules restantes). Optionnel.")]
        [SerializeField] private TMP_Text counterLabel;

        [Tooltip("SpriteMask (enfant) épousant la silhouette de la verrerie : le liquide n'est visible qu'à l'intérieur. Optionnel — auto-détecté s'il existe un SpriteMask enfant.")]
        [SerializeField] private SpriteMask liquidMask;

        public IReadOnlyList<FillLayer> Layers => layers;

        /// <summary>Levé une seule fois, quand toutes les couches sont pleines.</summary>
        public event Action<ContainerFillLevel> Filled;

        public int RequiredCount
        {
            get
            {
                int total = 0;
                foreach (FillLayer layer in layers) total += layer.requiredCount;
                return total;
            }
        }

        public int CurrentCount
        {
            get
            {
                int total = 0;
                foreach (FillLayer layer in layers) total += layer.currentCount;
                return total;
            }
        }

        public float FillRatio => RequiredCount > 0 ? (float)CurrentCount / RequiredCount : 0f;

        public bool IsFull
        {
            get
            {
                foreach (FillLayer layer in layers)
                {
                    if (!layer.IsFull) return false;
                }
                return true;
            }
        }

        private void Awake()
        {
            EnsureLayerRenderers();
            ApplyLiquidMask();
            WarnIfMisconfigured();
            UpdateLiquidVisual();
            UpdateCounterLabel();
        }

        /// <summary>
        /// Si un SpriteMask est assigné (ou trouvé en enfant), restreint le rendu
        /// des couches de liquide à l'intérieur du masque — le rectangle de liquide
        /// est ainsi découpé à la forme du bécher/de la fiole.
        /// </summary>
        private void ApplyLiquidMask()
        {
            if (liquidMask == null) liquidMask = GetComponentInChildren<SpriteMask>();
            if (liquidMask == null || layerRenderers == null) return;

            foreach (SpriteRenderer renderer in layerRenderers)
            {
                if (renderer != null)
                {
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
        }

        /// <summary>
        /// Garantit un SpriteRenderer distinct par couche : les renderers manquants
        /// (ou assignés en double dans l'Inspector) sont créés en clonant le premier
        /// renderer valide. Permet de passer un contenant en bicolore en ajoutant
        /// simplement une couche, sans toucher à la hiérarchie du prefab.
        /// </summary>
        private void EnsureLayerRenderers()
        {
            SpriteRenderer template = null;
            if (layerRenderers != null)
            {
                foreach (SpriteRenderer renderer in layerRenderers)
                {
                    if (renderer != null) { template = renderer; break; }
                }
            }
            if (template == null) return; // signalé par WarnIfMisconfigured

            var result = new List<SpriteRenderer>(layers.Count);
            var used = new HashSet<SpriteRenderer>();

            for (int i = 0; i < layers.Count; i++)
            {
                SpriteRenderer renderer = layerRenderers != null && i < layerRenderers.Length
                    ? layerRenderers[i]
                    : null;

                if (renderer == null || !used.Add(renderer))
                {
                    renderer = Instantiate(template, transform);
                    renderer.name = $"{template.name}_{i + 1}";
                    used.Add(renderer);
                }
                result.Add(renderer);
            }

            layerRenderers = result.ToArray();
        }

        /// <summary>Vrai si une couche non pleine accepte cette couleur.</summary>
        public bool CanAccept(ParticleColor color)
        {
            foreach (FillLayer layer in layers)
            {
                if (!layer.IsFull && layer.Accepts(color)) return true;
            }
            return false;
        }

        /// <summary>Enregistre une particule dans la première couche compatible. Retourne vrai si comptée.</summary>
        public bool AddParticle(ParticleColor color)
        {
            foreach (FillLayer layer in layers)
            {
                if (layer.IsFull || !layer.Accepts(color)) continue;

                layer.currentCount++;
                UpdateLiquidVisual();
                UpdateCounterLabel();

                if (layer.IsFull)
                {
                    string layerName = layer.acceptAnyColor ? "toutes couleurs" : layer.color.ToString();
                    Debug.Log($"Couche {layerName} pleine ({layer.requiredCount} particules) : {name}", this);
                }

                if (IsFull)
                {
                    Filled?.Invoke(this);
                    Debug.Log($"Contenant rempli ({RequiredCount} particules) : {name}", this);
                }
                return true;
            }
            return false;
        }

        /// <summary>Empile les couches de liquide depuis le fond, chacune teintée à sa couleur.</summary>
        private void UpdateLiquidVisual()
        {
            if (layerRenderers == null) return;

            int totalRequired = RequiredCount;
            float cursorY = fillBottomLocalY;

            for (int i = 0; i < layers.Count && i < layerRenderers.Length; i++)
            {
                SpriteRenderer renderer = layerRenderers[i];
                if (renderer == null) continue;

                FillLayer layer = layers[i];
                float height = totalRequired > 0
                    ? fillHeight * layer.currentCount / totalRequired
                    : 0f;

                // Le scale est exprimé en unités monde : on divise par la taille
                // native du sprite (le Square intégré fait 1 unité, mais un autre
                // sprite — UISprite, verrerie… — peut faire n'importe quelle taille).
                Sprite sprite = renderer.sprite;
                Vector2 spriteSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
                float scaleX = fillWidth / Mathf.Max(spriteSize.x, 0.0001f);
                float scaleY = height / Mathf.Max(spriteSize.y, 0.0001f);

                // Compense un pivot non centré pour que le bas de la couche reste
                // ancré sur cursorY quel que soit le sprite.
                float pivotOffsetY = sprite != null ? sprite.bounds.center.y * scaleY : 0f;

                Transform t = renderer.transform;
                t.localScale = new Vector3(scaleX, scaleY, 1f);
                t.localPosition = new Vector3(0f, cursorY + height / 2f - pivotOffsetY, 0f);

                Color tint = layer.acceptAnyColor ? Color.white : layer.color.ToColor();
                tint.a = 0.85f;
                renderer.color = tint;

                cursorY += height;
            }
        }

        /// <summary>Affiche au joueur ce qu'il reste à verser ("12/30", puis "PLEIN").
        /// Multi-couches : détail par couche ("Cyan 30/30 · Red 7/30").</summary>
        private void UpdateCounterLabel()
        {
            if (counterLabel == null) return;

            if (IsFull)
            {
                counterLabel.text = "PLEIN";
            }
            else if (layers.Count <= 1)
            {
                counterLabel.text = $"{CurrentCount}/{RequiredCount}";
            }
            else
            {
                var parts = new List<string>(layers.Count);
                foreach (FillLayer layer in layers)
                {
                    string layerName = layer.acceptAnyColor ? "Tout" : layer.color.ToString();
                    parts.Add($"{layerName} {layer.currentCount}/{layer.requiredCount}");
                }
                counterLabel.text = string.Join(" · ", parts);
            }
        }

        /// <summary>Diagnostics de setup : signale en Console les oublis fréquents.</summary>
        private void WarnIfMisconfigured()
        {
            if (layers.Count == 0)
            {
                Debug.LogWarning($"{name} : aucune couche définie — le contenant n'acceptera rien.", this);
            }

            if (layerRenderers == null || layerRenderers.Length == 0 || layerRenderers[0] == null)
            {
                Debug.LogWarning($"{name} : aucun SpriteRenderer de liquide assigné — le remplissage sera invisible.", this);
                return;
            }

            foreach (SpriteRenderer renderer in layerRenderers)
            {
                if (renderer == null) continue;

                if (renderer.sprite == null)
                {
                    Debug.LogWarning($"{name} : le SpriteRenderer '{renderer.name}' n'a pas de sprite — assigner 'Square' dans l'Inspector, sinon le liquide est invisible.", renderer);
                }

                if (renderer.transform.parent != transform)
                {
                    Debug.LogWarning($"{name} : '{renderer.name}' doit être un enfant DIRECT du contenant — le liquide sera mal positionné sinon.", renderer);
                }
            }
        }

        /// <summary>Visualise la zone de liquide dans la Scene view (contenant sélectionné)
        /// pour caler fillWidth / fillHeight / fillBottomLocalY sur le sprite de verrerie.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 0.96f, 1f, 0.9f);
            Gizmos.DrawWireCube(
                new Vector3(0f, fillBottomLocalY + fillHeight / 2f, 0f),
                new Vector3(fillWidth, fillHeight, 0f));
        }
    }
}
