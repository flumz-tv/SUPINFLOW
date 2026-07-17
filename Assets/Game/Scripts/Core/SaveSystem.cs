using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Progression et préférences du joueur, persistées dans les PlayerPrefs.
    /// Le niveau 1 est toujours déverrouillé ; chaque victoire marque le
    /// niveau terminé et déverrouille le suivant (GameManager.Win). Classe
    /// statique : accessible de partout, aucun composant à poser dans les
    /// scènes.
    /// TODO: étoiles obtenues par niveau, volume SFX.
    /// </summary>
    public static class SaveSystem
    {
        private const string HighestUnlockedLevelKey = "Supinflow.HighestUnlockedLevel";
        private const string CompletedLevelsKey = "Supinflow.CompletedLevels";
        private const string MusicVolumeKey = "Supinflow.MusicVolume";

        // Niveaux terminés stockés en masque de bits dans un seul entier
        // (bit n-1 levé = niveau n terminé) : une seule clé PlayerPrefs à
        // réinitialiser, jusqu'à 31 niveaux — large pour les 10 du jeu.
        private const int MaxTrackedLevel = 31;

        /// <summary>Numéro du niveau le plus avancé accessible (1 minimum).</summary>
        public static int HighestUnlockedLevel =>
            Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1));

        public static bool IsLevelUnlocked(int level) => level <= HighestUnlockedLevel;

        /// <summary>Déverrouille un niveau. La progression ne recule jamais :
        /// déverrouiller un niveau déjà accessible ne fait rien.</summary>
        public static void UnlockLevel(int level)
        {
            if (level <= HighestUnlockedLevel) return;

            PlayerPrefs.SetInt(HighestUnlockedLevelKey, level);
            PlayerPrefs.Save();
        }

        /// <summary>Le niveau a-t-il déjà été terminé (au moins une victoire) ?</summary>
        public static bool IsLevelCompleted(int level)
        {
            if (level < 1 || level > MaxTrackedLevel) return false;

            return (PlayerPrefs.GetInt(CompletedLevelsKey, 0) & (1 << (level - 1))) != 0;
        }

        /// <summary>Marque un niveau comme terminé (rejouable : le rester ne
        /// dépend pas des parties suivantes).</summary>
        public static void MarkLevelCompleted(int level)
        {
            if (level < 1 || level > MaxTrackedLevel || IsLevelCompleted(level)) return;

            int mask = PlayerPrefs.GetInt(CompletedLevelsKey, 0) | (1 << (level - 1));
            PlayerPrefs.SetInt(CompletedLevelsKey, mask);
            PlayerPrefs.Save();
        }

        /// <summary>Volume musique persisté (0–1) ; defaultVolume s'il n'a
        /// jamais été réglé.</summary>
        public static float GetMusicVolume(float defaultVolume) =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, defaultVolume));

        public static void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        /// <summary>Reverrouille tout sauf le niveau 1 et oublie les niveaux
        /// terminés (menu Supinflow de l'éditeur, futur SettingsPanel). Ne
        /// touche pas aux préférences (volume).</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
            PlayerPrefs.DeleteKey(CompletedLevelsKey);
            PlayerPrefs.Save();
        }
    }
}
