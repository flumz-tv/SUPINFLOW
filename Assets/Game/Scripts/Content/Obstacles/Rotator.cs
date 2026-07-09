using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Obstacle rotatif (agitateur, pale) : rotation continue pilotée par le
    /// moteur physique — Rigidbody2D kinematic + MoveRotation dans FixedUpdate,
    /// pour que les particules rebondissent proprement sur le collider en
    /// mouvement (une rotation par transform.Rotate serait invisible pour la
    /// physique entre deux pas de simulation). Collider NON trigger.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Rotator : MonoBehaviour
    {
        [Tooltip("Vitesse de rotation en degrés par seconde (négatif = sens horaire).")]
        [SerializeField] private float degreesPerSecond = 90f;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        private void FixedUpdate()
        {
            body.MoveRotation(body.rotation + degreesPerSecond * Time.fixedDeltaTime);
        }
    }
}
