using System.Collections.Generic;
using UnityEngine;

namespace Supinflow
{
    /// <summary>
    /// Conversion d'un trait dessiné en collider physique.
    /// Copie les points du trait dans un EdgeCollider2D pour que
    /// les particules puissent rouler dessus.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class LineToCollider : MonoBehaviour
    {
        private EdgeCollider2D edgeCollider;

        private void Awake()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
        }

        /// <summary>
        /// Appelé par LineDrawer quand le trait est terminé.
        /// Les points sont en coordonnées monde ; comme l'objet Line
        /// reste en (0,0,0), local = monde.
        /// </summary>
        public void UpdateCollider(List<Vector2> points)
        {
            edgeCollider.points = points.ToArray();
        }
    }
}
