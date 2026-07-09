using System.Collections;
using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Durée de vie d'un trait dessiné : après un délai, le trait s'estompe
    /// (fondu de l'alpha du LineRenderer) puis est détruit avec son collider.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LineLifetime : MonoBehaviour
    {
        [Tooltip("Durée en secondes avant le début de la disparition.")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("Durée du fondu en secondes avant destruction.")]
        [SerializeField] private float fadeDuration = 0.5f;

        private IEnumerator Start()
        {
            LineRenderer line = GetComponent<LineRenderer>();
            Color startColor = line.startColor;
            Color endColor = line.endColor;

            yield return new WaitForSeconds(lifetime);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - elapsed / fadeDuration);
                line.startColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a * alpha);
                line.endColor = new Color(endColor.r, endColor.g, endColor.b, endColor.a * alpha);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
