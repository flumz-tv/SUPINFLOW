using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Validation de la couleur des particules entrant dans un contenant.
    /// Les couleurs acceptées sont définies par les couches du
    /// ContainerFillLevel (source de vérité unique) : une particule est
    /// valide si au moins une couche non pleine accepte sa couleur.
    /// Les particules refusées traversent le contenant sans effet
    /// (décision de ContainerBase).
    /// </summary>
    [RequireComponent(typeof(ContainerFillLevel))]
    public class ColorMatchChecker : MonoBehaviour
    {
        private ContainerFillLevel fillLevel;

        private void Awake()
        {
            fillLevel = GetComponent<ContainerFillLevel>();
        }

        public bool IsMatch(ParticleController particle)
        {
            return fillLevel.CanAccept(particle.Color);
        }
    }
}
