using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Zone de destruction des particules (flamme de bec Bunsen, bac d'acide…).
    /// Toute particule entrant dans le trigger est détruite : le sucre est
    /// perdu, alors que le quota d'émission du niveau est déjà consommé — c'est
    /// un danger à éviter, pas un simple mur.
    /// (Feedback visuel/sonore à brancher ici quand l'AudioManager existera.)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Deleter : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            ParticleController particle = other.GetComponent<ParticleController>();
            if (particle == null) return;

            Destroy(particle.gameObject);
        }
    }
}
