using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Supinflow
{
    /// <summary>
    /// Dessin des traits par le joueur.
    /// Capture l'input souris (nouveau Input System), filtre les points
    /// trop proches, alimente le LineRenderer du trait en cours et
    /// notifie LineToCollider au relâchement du bouton.
    /// </summary>
    public class LineDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private float minDistance = 0.1f;

        private LineRenderer currentLine;
        private LineToCollider currentCollider;
        private readonly List<Vector2> points = new List<Vector2>();
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return; // pas de souris branchée

            if (mouse.leftButton.wasPressedThisFrame)
            {
                StartLine();
            }
            else if (mouse.leftButton.isPressed)
            {
                UpdateLine();
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                EndLine();
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            return cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        private void StartLine()
        {
            GameObject lineObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
            currentLine = lineObj.GetComponent<LineRenderer>();
            currentCollider = lineObj.GetComponent<LineToCollider>();
            points.Clear();
            AddPoint(GetMouseWorldPosition());
        }

        private void UpdateLine()
        {
            if (currentLine == null) return;

            Vector2 mousePos = GetMouseWorldPosition();
            if (Vector2.Distance(mousePos, points[points.Count - 1]) > minDistance)
            {
                AddPoint(mousePos);
            }
        }

        private void AddPoint(Vector2 point)
        {
            points.Add(point);
            currentLine.positionCount = points.Count;
            currentLine.SetPosition(points.Count - 1, point);

            // Le collider suit le trait pendant le dessin, pas seulement à la fin
            // (un EdgeCollider2D a besoin d'au moins 2 points).
            if (points.Count >= 2)
            {
                currentCollider.UpdateCollider(points);
            }
        }

        private void EndLine()
        {
            if (currentLine == null) return;

            // Un simple clic sans glisser ne produit pas de trait exploitable.
            if (points.Count < 2)
            {
                Destroy(currentLine.gameObject);
            }

            currentLine = null;
            currentCollider = null;
        }
    }
}
