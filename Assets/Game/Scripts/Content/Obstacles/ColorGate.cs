using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Portail recolorant (niveau 4) : toute particule qui traverse le trigger
    /// prend la couleur du portail — trivial grâce au prefab unique de particule
    /// (SetColor échange le sprite d'une particule vivante, cf. DEVLOG
    /// 2026-07-06). Le visuel du portail est teinté à la couleur cible, dans
    /// l'éditeur aussi (OnValidate), pour que le level design reste lisible.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ColorGate : MonoBehaviour
    {
        [Tooltip("Couleur appliquée aux particules qui traversent.")]
        [SerializeField] private ParticleColor targetColor = ParticleColor.Red;

        [Tooltip("Visuel du portail, teinté à la couleur cible. Optionnel — auto-détecté sur le même GameObject.")]
        [SerializeField] private SpriteRenderer tintRenderer;

        private void Awake()
        {
            ApplyTint();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ParticleController particle = other.GetComponent<ParticleController>();
            if (particle == null) return;

            particle.SetColor(targetColor);
        }

        private void ApplyTint()
        {
            if (tintRenderer == null) tintRenderer = GetComponent<SpriteRenderer>();
            if (tintRenderer == null) return;

            Color tint = targetColor.ToColor();
            tint.a = 0.45f; // semi-transparent : on voit les particules traverser
            tintRenderer.color = tint;
        }

        private void OnValidate()
        {
            ApplyTint();
        }
    }
}
