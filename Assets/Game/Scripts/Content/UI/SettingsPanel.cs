using UnityEngine;
using UnityEngine.UI;

namespace Supinflow
{
    /// <summary>
    /// Panneau de réglages. À poser sur le panneau ouvert par le bouton
    /// SETTINGS (celui assigné dans MainMenuUI). Le slider de volume musique
    /// est auto-découvert (premier Slider enfant, inactifs compris,
    /// assignable dans l'Inspector si le panneau en gagne d'autres) et pilote
    /// AudioManager.MusicVolume : application immédiate, valeur persistée
    /// (SaveSystem) et resynchronisée à chaque ouverture du panneau.
    /// TODO: slider de volume SFX, bouton de réinitialisation de progression.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Tooltip("Slider du volume musique. Vide = premier Slider enfant.")]
        [SerializeField] private Slider musicSlider;

        private void Awake()
        {
            if (musicSlider == null)
            {
                musicSlider = GetComponentInChildren<Slider>(true);
            }

            if (musicSlider == null)
            {
                Debug.LogWarning($"SettingsPanel : aucun Slider sous {name} — pas de réglage de volume.", this);
                return;
            }

            // Le slider travaille en 0–1, comme AudioManager.MusicVolume.
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        private void OnEnable()
        {
            // À chaque ouverture du panneau, le slider reflète le volume
            // courant (sans re-déclencher onValueChanged).
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume);
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioManager.MusicVolume = value;
        }
    }
}
