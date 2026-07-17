using UnityEditor;
using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Outils d'éditeur pour la progression (SaveSystem). Dossier « Editor » :
    /// compilé uniquement dans l'éditeur, absent des builds.
    /// </summary>
    public static class SaveSystemMenu
    {
        [MenuItem("Supinflow/Réinitialiser la progression")]
        private static void ResetProgress()
        {
            SaveSystem.ResetProgress();
            Debug.Log("Progression réinitialisée : seul le niveau 1 est déverrouillé.");
        }
    }
}
