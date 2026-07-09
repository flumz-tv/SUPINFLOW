using System;
using System.Collections;
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
    /// En mode overflow (éprouvette de précision), le quota correspond à une
    /// graduation du verre : atteindre le quota ne valide PAS immédiatement —
    /// le niveau doit se maintenir exactement à la graduation pendant
    /// settleDuration secondes. Tout surplus pendant ce temps fait déborder le
    /// contenant, définitivement (le liquide ne se retire pas) : le surplus
    /// monte au-dessus de la graduation (jusqu'à overflowMaxHeight) et
    /// l'événement Overflowed prévient le GameManager → défaite.
    /// </summary>
    public class ContainerFillLevel : MonoBehaviour
    {
        [Tooltip("Couches de liquide, de bas en haut. 1 couche = contenant simple.")]
        [SerializeField] private List<FillLayer> layers = new List<FillLayer> { new FillLayer() };

        [Tooltip("Un SpriteRenderer par couche (même ordre), teinté et redimensionné par le script.")]
        [SerializeField] private SpriteRenderer[] layerRenderers;

        [Tooltip("Largeur de la zone de liquide, en local.")]
        [SerializeField] private float fillWidth = 1.5f;

        [Tooltip("Hauteur du liquide quand les quotas sont atteints (la graduation cible), en local.")]
        [SerializeField] private float fillHeight = 1.9f;

        [Tooltip("Ordonnée locale du fond de la zone de liquide.")]
        [SerializeField] private float fillBottomLocalY = -0.95f;

        [Header("Overflow (contenants de précision)")]
        [Tooltip("Contenant de précision : atteindre le quota ne valide pas immédiatement — le niveau doit tenir Settle Duration secondes sans surplus. Tout surplus fait déborder le contenant (définitif) et le liquide monte jusqu'à Overflow Max Height.")]
        [SerializeField] private bool allowOverflow;

        [Tooltip("Hauteur totale max du liquide en débordement (bord intérieur du verre, là où le SpriteMask s'arrête). Doit être > Fill Height.")]
        [SerializeField] private float overflowMaxHeight = 2.5f;

        [Tooltip("Durée (s) pendant laquelle le niveau doit rester exactement au quota pour valider le contenant.")]
        [SerializeField] private float settleDuration = 3f;

        [Tooltip("Texte affiché au joueur (particules restantes). Optionnel.")]
        [SerializeField] private TMP_Text counterLabel;

        [Tooltip("SpriteMask (enfant) épousant la silhouette de la verrerie : le liquide n'est visible qu'à l'intérieur. Optionnel — auto-détecté s'il existe un SpriteMask enfant.")]
        [SerializeField] private SpriteMask liquidMask;

        public IReadOnlyList<FillLayer> Layers => layers;

        /// <summary>Levé une seule fois, quand le contenant est définitivement validé :
        /// au quota atteint — ou, en mode overflow, après le maintien réussi.</summary>
        public event Action<ContainerFillLevel> Filled;

        /// <summary>Levé une seule fois si le contenant déborde (mode overflow) :
        /// la validation devient impossible — défaite côté GameManager.</summary>
        public event Action<ContainerFillLevel> Overflowed;

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

        /// <summary>
        /// Particules absorbables en tout : les quotas, plus le débordement en mode
        /// overflow. La densité est constante (fillHeight = RequiredCount particules),
        /// donc la capacité du verre est proportionnelle à overflowMaxHeight.
        /// </summary>
        public int MaxCapacity
        {
            get
            {
                int required = RequiredCount;
                if (!allowOverflow || required <= 0 || fillHeight <= 0f || overflowMaxHeight <= fillHeight)
                {
                    return required;
                }
                return Mathf.FloorToInt(required * overflowMaxHeight / fillHeight);
            }
        }

        /// <summary>Particules absorbées au-delà des quotas (0 sans overflow).</summary>
        public int OverflowCount => Mathf.Max(0, CurrentCount - RequiredCount);

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

        /// <summary>Débordé (mode overflow) : du surplus est entré au-dessus de la
        /// graduation — la validation est définitivement impossible.</summary>
        public bool IsOverfilled => overfilled;

        /// <summary>Le contenant compte pour la victoire : quotas atteints — et, en
        /// mode overflow, niveau maintenu settleDuration secondes sans surplus.</summary>
        public bool IsValidated => allowOverflow ? settled : IsFull;

        private Coroutine settleRoutine;
        private float settleRemaining;
        private bool settled;
        private bool overfilled;

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

        /// <summary>Vrai si une couche non pleine accepte cette couleur — ou, en mode
        /// overflow, si une couche compatible existe et que le verre n'est pas à ras bord.</summary>
        public bool CanAccept(ParticleColor color)
        {
            return FindTargetLayer(color) != null;
        }

        /// <summary>Enregistre une particule dans la première couche compatible. Retourne vrai si comptée.</summary>
        public bool AddParticle(ParticleColor color)
        {
            FillLayer target = FindTargetLayer(color);
            if (target == null) return false;

            bool wasFull = IsFull;
            target.currentCount++;

            if (target.currentCount == target.requiredCount)
            {
                string layerName = target.acceptAnyColor ? "toutes couleurs" : target.color.ToString();
                Debug.Log($"Couche {layerName} pleine ({target.requiredCount} particules) : {name}", this);
            }

            if (allowOverflow && OverflowCount > 0)
            {
                MarkOverfilled();
            }
            else if (!wasFull && IsFull)
            {
                if (allowOverflow)
                {
                    // Contenant de précision : le quota doit tenir settleDuration
                    // secondes sans surplus avant de compter pour la victoire.
                    settleRoutine = StartCoroutine(SettleCountdown());
                }
                else
                {
                    Filled?.Invoke(this);
                    Debug.Log($"Contenant rempli ({RequiredCount} particules) : {name}", this);
                }
            }

            UpdateLiquidVisual();
            UpdateCounterLabel();
            return true;
        }

        /// <summary>Mode overflow : du surplus est entré au-dessus de la graduation.
        /// Le liquide ne se retire pas, l'échec est définitif — maintien annulé,
        /// Overflowed levé une seule fois.</summary>
        private void MarkOverfilled()
        {
            if (overfilled) return;

            overfilled = true;
            if (settleRoutine != null)
            {
                StopCoroutine(settleRoutine);
                settleRoutine = null;
            }
            settleRemaining = 0f;
            Overflowed?.Invoke(this);
            Debug.Log($"Contenant débordé (surplus au-dessus de la graduation) : {name}", this);
        }

        /// <summary>Compte à rebours de validation du mode overflow : si aucun surplus
        /// ne l'interrompt (MarkOverfilled), le contenant est validé et Filled est levé.</summary>
        private IEnumerator SettleCountdown()
        {
            settleRemaining = settleDuration;
            while (settleRemaining > 0f)
            {
                UpdateCounterLabel();
                yield return null;
                settleRemaining -= Time.deltaTime;
            }

            settleRemaining = 0f;
            settleRoutine = null;
            settled = true;
            UpdateCounterLabel();
            Filled?.Invoke(this);
            Debug.Log($"Contenant validé (niveau maintenu {settleDuration:0.#} s) : {name}", this);
        }

        /// <summary>
        /// Couche qui recevrait une particule de cette couleur : la première couche
        /// compatible non pleine, sinon — en mode overflow et tant que le verre n'est
        /// pas à ras bord — la première couche compatible (le surplus s'empile
        /// au-dessus de la graduation).
        /// </summary>
        private FillLayer FindTargetLayer(ParticleColor color)
        {
            foreach (FillLayer layer in layers)
            {
                if (!layer.IsFull && layer.Accepts(color)) return layer;
            }

            if (allowOverflow && CurrentCount < MaxCapacity)
            {
                foreach (FillLayer layer in layers)
                {
                    if (layer.Accepts(color)) return layer;
                }
            }
            return null;
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
        /// Multi-couches : détail par couche ("Cyan 30/30 · Red 7/30").
        /// Mode overflow : compte à rebours de maintien ("TENIR 2.3s"), puis "PLEIN"
        /// une fois validé — ou "DÉBORDÉ !" si du surplus est entré.</summary>
        private void UpdateCounterLabel()
        {
            if (counterLabel == null) return;

            if (allowOverflow)
            {
                if (overfilled)
                {
                    counterLabel.text = "DÉBORDÉ !";
                    return;
                }
                if (settled)
                {
                    counterLabel.text = "PLEIN";
                    return;
                }
                if (settleRoutine != null)
                {
                    counterLabel.text = $"TENIR {Mathf.Max(settleRemaining, 0f):0.0}s";
                    return;
                }
            }

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

            if (allowOverflow && overflowMaxHeight <= fillHeight)
            {
                Debug.LogWarning($"{name} : Allow Overflow est actif mais Overflow Max Height ({overflowMaxHeight}) ≤ Fill Height ({fillHeight}) — aucun débordement possible. Caler Overflow Max Height sur le bord intérieur du verre (gizmo rouge).", this);
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
        /// pour caler fillWidth / fillHeight / fillBottomLocalY sur le sprite de verrerie.
        /// Cyan = zone de quota (le haut = la graduation cible) ; rouge = zone de
        /// débordement jusqu'au bord du verre (mode overflow).</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 0.96f, 1f, 0.9f);
            Gizmos.DrawWireCube(
                new Vector3(0f, fillBottomLocalY + fillHeight / 2f, 0f),
                new Vector3(fillWidth, fillHeight, 0f));

            if (allowOverflow && overflowMaxHeight > fillHeight)
            {
                float extra = overflowMaxHeight - fillHeight;
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                Gizmos.DrawWireCube(
                    new Vector3(0f, fillBottomLocalY + fillHeight + extra / 2f, 0f),
                    new Vector3(fillWidth, extra, 0f));
            }
        }
    }
}
