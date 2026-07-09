using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Émetteur de particules (robinet / burette du labo).
    /// Instancie le prefab unique de particule à cadence fixe et lui applique
    /// la couleur sélectionnée via SetColor(). La couleur peut changer en cours
    /// de partie (SetColor public) : les particules suivantes la prennent
    /// immédiatement. Un léger décalage horizontal aléatoire évite que les
    /// particules apparaissent exactement au même point (empilement instable
    /// pour le moteur physique).
    /// </summary>
    public class ParticleSpawner : MonoBehaviour
    {
        [Tooltip("Le prefab unique Particle (avec ParticleController et ses 5 sprites).")]
        [SerializeField] private ParticleController particlePrefab;

        [Tooltip("Couleur émise par ce spawner.")]
        [SerializeField] private ParticleColor color = ParticleColor.Blue;

        [Tooltip("Temps en secondes entre deux particules.")]
        [SerializeField] private float spawnInterval = 0.08f;

        [Tooltip("Nombre total de particules émises pour le niveau (0 = illimité).")]
        [SerializeField] private int totalToSpawn = 100;

        [Tooltip("Décalage horizontal aléatoire maximal à l'émission.")]
        [SerializeField] private float spawnJitter = 0.05f;

        [Tooltip("Si vrai, l'émission démarre dès le chargement de la scène.")]
        [SerializeField] private bool spawnOnStart = true;

        public ParticleColor Color => color;
        public int TotalToSpawn => totalToSpawn;
        public int SpawnedCount { get; private set; }
        public bool IsSpawning { get; private set; }

        /// <summary>Vrai quand le quota d'émission du niveau est entièrement consommé
        /// (jamais vrai si totalToSpawn = 0 : émission illimitée).</summary>
        public bool IsExhausted => totalToSpawn > 0 && SpawnedCount >= totalToSpawn;

        private float timer;

        private void Start()
        {
            IsSpawning = spawnOnStart;
        }

        private void Update()
        {
            if (!IsSpawning || particlePrefab == null) return;

            timer += Time.deltaTime;
            while (timer >= spawnInterval)
            {
                timer -= spawnInterval;
                Spawn();

                if (totalToSpawn > 0 && SpawnedCount >= totalToSpawn)
                {
                    IsSpawning = false;
                    return;
                }
            }
        }

        public void StartSpawning()
        {
            IsSpawning = true;
        }

        public void StopSpawning()
        {
            IsSpawning = false;
        }

        /// <summary>Change la couleur émise en cours de partie.</summary>
        public void SetColor(ParticleColor newColor)
        {
            color = newColor;
        }

        private void Spawn()
        {
            Vector3 position = transform.position
                + Vector3.right * Random.Range(-spawnJitter, spawnJitter);
            ParticleController particle = Instantiate(particlePrefab, position, Quaternion.identity);
            particle.SetColor(color);
            SpawnedCount++;
        }
    }
}
