using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Comportement d'une particule colorée individuelle.
    /// Prefab unique : porte les 5 sprites d'orbe (ordre de l'enum ParticleColor)
    /// et sa couleur logique, visible dans l'Inspector. Changer la couleur passe
    /// toujours par SetColor() — spawner à l'émission, portail recolorant,
    /// ParticleColorMixer — qui met à jour le sprite affiché en même temps.
    /// Le Rigidbody2D est forcé en Collision Detection = Continuous pour
    /// éviter le tunneling à travers les traits fins (cf. DEVLOG 2026-07-02).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParticleController : MonoBehaviour
    {
        [Tooltip("Sprites d'orbe dans l'ordre de l'enum ParticleColor : Red, Blue, Green, Purple, Cyan.")]
        [SerializeField] private Sprite[] colorSprites;

        [Tooltip("Couleur logique de la particule. En jeu, toujours modifiée via SetColor().")]
        [SerializeField] private ParticleColor color = ParticleColor.Blue;

        [Tooltip("Ordonnée monde sous laquelle la particule est considérée perdue et détruite.")]
        [SerializeField] private float killY = -12f;

        private SpriteRenderer spriteRenderer;

        public ParticleColor Color => color;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ApplySprite();
        }

        private void Update()
        {
            if (transform.position.y < killY)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Change la couleur logique et le sprite affiché.</summary>
        public void SetColor(ParticleColor newColor)
        {
            color = newColor;
            ApplySprite();
        }

        private void ApplySprite()
        {
            int index = (int)color;
            if (colorSprites != null && index < colorSprites.Length && colorSprites[index] != null)
            {
                spriteRenderer.sprite = colorSprites[index];
            }
            else
            {
                Debug.LogWarning($"ParticleController : pas de sprite assigné pour la couleur {color}.", this);
            }
        }

        // Aperçu immédiat dans l'éditeur quand on change la couleur dans l'Inspector.
        private void OnValidate()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            int index = (int)color;
            if (renderer != null && colorSprites != null
                && index < colorSprites.Length && colorSprites[index] != null)
            {
                renderer.sprite = colorSprites[index];
            }
        }
    }
}
