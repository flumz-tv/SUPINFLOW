using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Supinflow
{
    /// <summary>
    /// Musique du jeu. Un seul exemplaire survit aux changements de scène
    /// (DontDestroyOnLoad) : posé dans la scène MainMenu, il peut aussi être
    /// posé dans chaque scène de niveau pour les lancements directs depuis
    /// l'éditeur — les doublons s'autodétruisent au chargement. Deux pistes,
    /// jouées en boucle sur une AudioSource ajoutée au besoin :
    /// - menuMusic dans les scènes de menu ;
    /// - levelsMusic dans les scènes dont le nom commence par levelScenePrefix.
    ///   Cette piste n'est PAS relancée d'un niveau à l'autre (ni au reset R) :
    ///   elle continue où elle en était, et ne repart du début qu'en revenant
    ///   du menu.
    /// Le volume (MusicVolume) est piloté par SettingsPanel et persisté via
    /// SaveSystem ; la valeur de l'Inspector ne sert que de défaut au premier
    /// lancement. C'est une position perceptuelle 0–1 : elle passe par une
    /// courbe de puissance (volumeResponse) avant d'arriver à l'AudioSource,
    /// car l'oreille entend le volume en logarithmique — en linéaire, tout se
    /// joue dans les premiers 10 % du slider.
    /// TODO: SFX (dessin, particule collectée, victoire...) et volume SFX.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Tooltip("Musique des menus (boucle). Vide = silence dans les menus.")]
        [SerializeField] private AudioClip menuMusic;

        [Tooltip("Musique des niveaux (boucle), continue d'un niveau à l'autre.")]
        [SerializeField] private AudioClip levelsMusic;

        [Tooltip("Les scènes dont le nom commence ainsi jouent levelsMusic ; toutes les autres, menuMusic.")]
        [SerializeField] private string levelScenePrefix = "Level";

        [Range(0f, 1f)]
        [Tooltip("Volume de la musique au premier lancement (ensuite, la valeur persistée prime).")]
        [SerializeField] private float musicVolume = DefaultMusicVolume;

        [Range(1f, 4f)]
        [Tooltip("Réponse du volume : 1 = linéaire, plus haut = courbe perceptuelle (le slider agit uniformément à l'oreille). 3 est un bon défaut.")]
        [SerializeField] private float volumeResponse = 3f;

        private const float DefaultMusicVolume = 0.6f;

        private static AudioManager instance;

        private AudioSource source;

        /// <summary>Volume de la musique (0–1), persisté via SaveSystem et
        /// appliqué immédiatement. Utilisable même sans AudioManager en vie :
        /// la valeur sera reprise à son prochain Awake.</summary>
        public static float MusicVolume
        {
            get => instance != null
                ? instance.musicVolume
                : SaveSystem.GetMusicVolume(DefaultMusicVolume);
            set
            {
                float clamped = Mathf.Clamp01(value);
                SaveSystem.SetMusicVolume(clamped);

                if (instance == null) return;

                instance.musicVolume = clamped;
                if (instance.source != null)
                {
                    instance.source.volume = instance.ToAmplitude(clamped);
                }
            }
        }

        private void Awake()
        {
            // Un seul AudioManager en vie : le premier chargé gagne, les
            // exemplaires embarqués dans les scènes suivantes s'autodétruisent.
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            // DontDestroyOnLoad n'agit que sur les objets RACINE de la scène :
            // on se détache au besoin (AudioManager posé sous Core, par
            // exemple), sinon l'objet mourrait avec la scène et la musique
            // avec lui.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = true;
            // Volume persisté ; la valeur de l'Inspector sert de défaut au
            // premier lancement.
            musicVolume = SaveSystem.GetMusicVolume(musicVolume);
            source.volume = ToAmplitude(musicVolume);

            SceneManager.sceneLoaded += OnSceneLoaded;
            // La scène de départ ne repasse pas par sceneLoaded : on applique
            // sa musique immédiatement.
            ApplyMusicFor(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        // Répercute le volume de l'Inspector en cours de partie (réglage live).
        private void OnValidate()
        {
            if (source != null)
            {
                source.volume = ToAmplitude(musicVolume);
            }
        }

        /// <summary>Position perceptuelle du slider (0–1) → amplitude de
        /// l'AudioSource. Courbe de puissance : approximation simple de la
        /// perception logarithmique de l'oreille, qui atteint exactement 0 et
        /// 1 aux extrémités.</summary>
        private float ToAmplitude(float perceived)
        {
            return Mathf.Pow(Mathf.Clamp01(perceived), volumeResponse);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyMusicFor(scene.name);
        }

        /// <summary>Joue la piste attendue par la scène. Si c'est déjà celle en
        /// cours, ne fait rien — c'est ce qui préserve l'avancement de la
        /// musique des niveaux d'un niveau à l'autre.</summary>
        private void ApplyMusicFor(string sceneName)
        {
            AudioClip expected = sceneName.StartsWith(levelScenePrefix, StringComparison.OrdinalIgnoreCase)
                ? levelsMusic
                : menuMusic;

            if (expected == null)
            {
                // Piste non assignée : silence plutôt que la piste de l'autre
                // contexte.
                Debug.LogWarning($"AudioManager : pas de musique assignée pour la scène {sceneName} — silence.", this);
                source.Stop();
                source.clip = null;
                return;
            }

            if (source.clip == expected && source.isPlaying) return;

            source.clip = expected;
            source.Play();
        }
    }
}
