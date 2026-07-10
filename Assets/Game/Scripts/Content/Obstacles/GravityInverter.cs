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
    /// Quand le sens change réellement, l'élan vertical est RENVOYÉ dans le
    /// nouveau sens de la gravité (× verticalMomentumKept) : à 1 la particule
    /// rebondit en miroir et conserve son angle d'incidence ; à 0 elle est
    /// stoppée net — déconseillé sous un flux continu, les particules stagnantes
    /// se font percuter par les suivantes et partent dans tous les sens.
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
        [Tooltip("Part de l'élan vertical renvoyée dans le nouveau sens quand la gravité s'inverse : 1 = rebond miroir (l'angle d'arrivée est conservé), 0 = la particule est stoppée net et repart de zéro.")]
        [SerializeField] private float verticalMomentumKept = 1f;

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

            // Uniquement quand le sens a réellement changé (re-traverser une
            // zone idempotente ne touche pas à la vitesse) : l'élan vertical est
            // renvoyé dans le nouveau sens de la gravité — réflexion miroir, la
            // particule repart avec le même angle au lieu de stagner dans la
            // zone où les suivantes la percuteraient.
            if (!Mathf.Approximately(previousScale, body.gravityScale))
            {
                Vector2 velocity = body.linearVelocity;
                float newDirection = body.gravityScale < 0f ? 1f : -1f;
                velocity.y = newDirection * Mathf.Abs(velocity.y) * verticalMomentumKept;
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
