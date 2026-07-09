using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Téléporteur de particules (niveau 3 : trou dans le sol → sortie au
    /// plafond). Unidirectionnel : toute particule entrant dans le trigger est
    /// déplacée au point de sortie, vélocité conservée (elle ressort du plafond
    /// en poursuivant sa chute), décalage horizontal d'entrée conservé pour que
    /// le flux ne ressorte pas empilé sur un point unique.
    /// Aller-retour (niveaux mixtes) : placer deux Portals dont chaque ExitPoint
    /// est HORS du trigger de l'autre, sinon boucle de téléportation immédiate.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Portal : MonoBehaviour
    {
        [Tooltip("Point d'apparition des particules téléportées (enfant ExitPoint du prefab, à placer librement).")]
        [SerializeField] private Transform exitPoint;

        private void Awake()
        {
            if (exitPoint == null)
            {
                Debug.LogWarning($"{name} : pas d'ExitPoint assigné — le portail est inactif.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (exitPoint == null) return;

            ParticleController particle = other.GetComponent<ParticleController>();
            if (particle == null) return;

            float offsetX = particle.transform.position.x - transform.position.x;

            // Téléportation via le Rigidbody2D (pas le Transform) : le moteur
            // physique prend le déplacement en compte immédiatement, sans
            // interpolation parasite entre l'entrée et la sortie.
            Rigidbody2D body = particle.GetComponent<Rigidbody2D>();
            body.position = (Vector2)exitPoint.position + Vector2.right * offsetX;
        }
    }
}
