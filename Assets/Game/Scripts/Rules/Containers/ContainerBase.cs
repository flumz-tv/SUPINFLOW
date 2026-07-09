using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Classe de base des contenants (bécher, éprouvette, erlenmeyer).
    /// Détecte l'entrée des particules via un trigger (OnTriggerEnter2D),
    /// délègue la validation à ColorMatchChecker et le comptage à
    /// ContainerFillLevel. Une particule valide est absorbée (détruite et
    /// comptée) ; une particule refusée traverse sans effet.
    /// </summary>
    [RequireComponent(typeof(ColorMatchChecker))]
    [RequireComponent(typeof(ContainerFillLevel))]
    public class ContainerBase : MonoBehaviour
    {
        private ColorMatchChecker colorChecker;
        private ContainerFillLevel fillLevel;

        public ContainerFillLevel FillLevel => fillLevel;
        public ColorMatchChecker ColorChecker => colorChecker;

        private void Awake()
        {
            colorChecker = GetComponent<ColorMatchChecker>();
            fillLevel = GetComponent<ContainerFillLevel>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ParticleController particle = other.GetComponent<ParticleController>();
            if (particle == null) return;

            if (colorChecker.IsMatch(particle))
            {
                if (fillLevel.AddParticle(particle.Color))
                {
                    Destroy(particle.gameObject);
                }
            }
            // Particule refusée : elle traverse simplement le contenant.
        }
    }
}
