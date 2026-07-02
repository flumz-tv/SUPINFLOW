# SUPINFLOW — Journal de développement

## 2026-07-02 — Système de dessin (étape 2 du plan)

### Fait

**Scènes**
- `MainMenu.unity` créée (vide pour l'instant, sera câblée après le gameplay)
- `Level_Test.unity` créée : scène bac à sable pour valider le gameplay
  - Main Camera orthographique (0, 0, -10)
  - `DrawingManager` (GameObject vide portant `LineDrawer`)
  - `TestBall` : sprite particule + `CircleCollider2D` + `Rigidbody2D` pour valider la physique
  - Canvas conservé pour le futur HUD (Panel désactivé)

**Prefab `Line`** (`Assets/Game/Prefabs/`)
- `LineRenderer` : width ~0.15, material Sprites-Default, End Cap / Corner Vertices = 8 (trait arrondi), couleur cyan néon (#00F5FF, cohérent DA)
- `EdgeCollider2D` : Edge Radius = moitié de la largeur du trait (aligne physique et visuel)
- Scripts `LineToCollider` + `LineLifetime`

**Scripts implémentés** (`Assets/Game/Scripts/Drawing/`)
- `LineDrawer.cs` : capture souris via le **nouveau Input System** (`Mouse.current`),
  instancie le prefab Line au clic, ajoute des points filtrés par distance minimale
  (`minDistance`, réglable dans l'Inspector), met à jour le collider **en temps réel**
  pendant le dessin, détruit les traits d'un seul point (clic sans glisser)
- `LineToCollider.cs` : copie les points du trait dans l'`EdgeCollider2D`
  (`[RequireComponent(typeof(EdgeCollider2D))]`) — l'objet Line reste en (0,0,0)
  pour que coordonnées locales = monde
- `LineLifetime.cs` : destruction du trait après un délai

### Problèmes rencontrés et résolus
1. **`InvalidOperationException` sur `UnityEngine.Input`** : le projet utilise
   l'Input System package (Active Input Handling). → Migration du code vers
   `Mouse.current.leftButton.wasPressedThisFrame` / `isPressed` / `wasReleasedThisFrame`.
2. **La balle traversait le trait à grande vitesse** (tunneling) :
   → `Collision Detection = Continuous` sur le Rigidbody2D de la balle.
   ⚠️ À reporter sur le prefab des particules quand il sera créé.
3. **La balle traversait le trait en cours de dessin** : le collider n'était créé
   qu'au relâchement du clic. → `UpdateCollider` appelé à chaque point ajouté
   (dès 2 points).

### Validation
- Dessin fluide au clic-glisser, trait arrondi cyan
- La balle roule sur le trait, y compris pendant le dessin et en chute rapide
- Le trait disparaît après son délai de vie

### Prochaines étapes (ordre prévu)
1. **Particules** : `ParticleSpawner` + `ParticleController` (Rigidbody2D + sprite orbe,
   Collision Detection = Continuous d'office)
2. **Contenants + victoire** : `ContainerBase` (trigger), `ContainerFillLevel` (seuil),
   `ColorMatchChecker`
   → jalon : un niveau jouable de bout en bout
3. `GameManager` / `LevelManager` (états, chargement de scènes)
4. Câblage UI : `MainMenuUI`, HUD, `VictoryPopup`, `SaveSystem`, étoiles
5. Mélange de couleurs (`ParticleColorMixer`), obstacles (`Portal`, `Rotator`, `Deleter`)
6. Les 10 niveaux + polish (Bloom URP pour le look néon)
