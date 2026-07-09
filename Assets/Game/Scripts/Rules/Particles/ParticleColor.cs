using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Couleur logique d'une particule chimique.
    /// Sert de clé commune pour le rendu (sprite d'orbe), le mélange
    /// (ParticleColorMixer) et la validation des contenants (ColorMatchChecker).
    /// L'ordre des valeurs doit correspondre aux tableaux de sprites
    /// sérialisés dans l'Inspector.
    /// </summary>
    public enum ParticleColor
    {
        Red,
        Blue,
        Green,
        Purple,
        Cyan
    }

    /// <summary>
    /// Conversion ParticleColor → Color UnityEngine (teinte des liquides,
    /// icônes UI, etc.). Palette alignée sur la DA néon du projet.
    /// </summary>
    public static class ParticleColorExtensions
    {
        public static Color ToColor(this ParticleColor color)
        {
            switch (color)
            {
                case ParticleColor.Red: return new Color(1f, 0.27f, 0.27f);
                case ParticleColor.Blue: return new Color(0.2f, 0.53f, 1f);
                case ParticleColor.Green: return new Color(0f, 1f, 0.53f);   // #00FF88
                case ParticleColor.Purple: return new Color(0.54f, 0.17f, 0.89f); // #8A2BE2
                case ParticleColor.Cyan: return new Color(0f, 0.96f, 1f);    // #00F5FF
                default: return Color.white;
            }
        }
    }
}
