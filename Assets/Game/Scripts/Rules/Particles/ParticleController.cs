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

        [Tooltip("Ordonnée monde au-dessus de laquelle la particule est perdue (gravité inversée qui rate le plafond).")]
        [SerializeField] private float killYTop = 12f;

        [Tooltip("Abscisse |x| au-delà de laquelle la particule est perdue — le sucre qui sort par un bord latéral est perdu (règle du sujet).")]
        [SerializeField] private float killX = 12f;

        private SpriteRenderer spriteRenderer;

        public ParticleColor Color => color;

        /// <summary>Particules actuellement vivantes dans la scène — le GameManager
        /// s'en sert pour déclarer la défaite quand le sucre est épuisé.</summary>
        public static int AliveCount { get; private set; }

        private void OnEnable()
        {
            AliveCount++;
        }

        private void OnDisable()
        {
            AliveCount--;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ApplySprite();
        }

        private void Update()
        {
            Vector3 position = transform.position;
            if (position.y < killY || position.y > killYTop || Mathf.Abs(position.x) > killX)
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
