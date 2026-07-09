using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Inverseur de gravité (niveau 5) : modifie la gravité du Rigidbody2D des
    /// particules entrant dans le trigger, selon le mode :
    /// - SetUpward / SetDownward : force le sens, idempotent (une particule qui
    ///   repasse ne change plus rien) — le choix robuste pour le niveau 5 ;
    /// - Toggle : inverse à chaque entrée, plus riche pour les niveaux mixtes.
    ///   ⚠ Dimensionner alors la zone assez haute pour que la particule fasse
    ///   demi-tour À L'INTÉRIEUR : si elle ressort par en dessous et re-rentre,
    ///   elle re-bascule à chaque passage (yo-yo sous la zone).
    /// Quand le sens change réellement, la vitesse verticale est multipliée par
    /// verticalMomentumKept (0 par défaut : la particule repart immédiatement
    /// dans le nouveau sens au lieu de finir sa course sur son élan).
    /// Le sens de la zone est montré au joueur par les flèches enfants du
    /// prefab (activées/centrées selon le mode, dans l'éditeur aussi).
    /// Les particules à gravité inversée montent : elles sont perdues au-delà
    /// du plafond (killYTop de ParticleController) si rien ne les arrête.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GravityInverter : MonoBehaviour
    {
        public enum InversionMode
        {
            SetUpward,
            SetDownward,
            Toggle
        }

        [Tooltip("SetUpward force la gravité vers le haut (recommandé, idempotent), SetDownward la rétablit vers le bas, Toggle inverse à chaque passage.")]
        [SerializeField] private InversionMode mode = InversionMode.SetUpward;

        [Range(0f, 1f)]
        [Tooltip("Part de la vitesse verticale conservée quand le sens s'inverse : 0 = la particule repart immédiatement dans le nouveau sens, 1 = elle garde tout son élan et décélère naturellement.")]
        [SerializeField] private float verticalMomentumKept;

        [Header("Visuel")]
        [Tooltip("Flèche vers le haut (enfant du prefab). Visible en SetUpward et Toggle.")]
        [SerializeField] private GameObject arrowUp;

        [Tooltip("Flèche vers le bas (enfant du prefab). Visible en SetDownward et Toggle.")]
        [SerializeField] private GameObject arrowDown;

        private void Awake()
        {
            ApplyArrowVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ParticleController particle = other.GetComponent<ParticleController>();
            if (particle == null) return;

            Rigidbody2D body = particle.GetComponent<Rigidbody2D>();
            float previousScale = body.gravityScale;
            float magnitude = Mathf.Abs(previousScale);

            switch (mode)
            {
                case InversionMode.SetUpward:
                    body.gravityScale = -magnitude;
                    break;
                case InversionMode.SetDownward:
                    body.gravityScale = magnitude;
                    break;
                case InversionMode.Toggle:
                    body.gravityScale = -previousScale;
                    break;
            }

            // Casse l'élan vertical uniquement quand le sens a réellement changé
            // (re-traverser une zone idempotente ne freine pas la particule).
            if (!Mathf.Approximately(previousScale, body.gravityScale))
            {
                Vector2 velocity = body.linearVelocity;
                velocity.y *= verticalMomentumKept;
                body.linearVelocity = velocity;
            }
        }

        /// <summary>Active et place les flèches selon le mode : une flèche centrée
        /// (SetUpward / SetDownward), les deux côte à côte (Toggle).</summary>
        private void ApplyArrowVisual()
        {
            if (arrowUp == null || arrowDown == null) return;

            bool both = mode == InversionMode.Toggle;
            arrowUp.SetActive(both || mode == InversionMode.SetUpward);
            arrowDown.SetActive(both || mode == InversionMode.SetDownward);

            arrowUp.transform.localPosition = new Vector3(both ? -0.35f : 0f, 0f, 0f);
            arrowDown.transform.localPosition = new Vector3(both ? 0.35f : 0f, 0f, 0f);
        }

        // Aperçu immédiat dans l'éditeur quand on change le mode dans l'Inspector.
        private void OnValidate()
        {
            ApplyArrowVisual();
        }
    }
}
