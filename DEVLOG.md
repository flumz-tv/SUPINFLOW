# SUPINFLOW — Journal de développement

## 2026-07-07 (suite 4) — Ratio bicolore : le masque était le coupable

### Diagnostic (via les logs de la suite 3)
`Cyan 30/30` et `Red 30/30` confirmés en Console → comptage correct, hauteurs
calculées égales. La bande rouge écrasée vient du **SpriteMask** : il utilise le
sprite artistique de la fiole, dont l'intérieur est semi-transparent — les
pixels sous l'Alpha Cutoff masquent le haut de la couche rouge. La frontière où
le rouge s'arrête = frontière d'opacité du sprite, pas la hauteur réelle.

### Vérif éditeur
1. Désactiver le SpriteMask → Play : bandes égales (débordantes) = confirmé.
2. Alpha Cutoff à 0 : dépanne si l'intérieur du sprite a un peu d'alpha.

### Vrai fix (asset à générer via Gemini, pour les 3 verreries)
Sprite « silhouette intérieure » : forme blanche UNIE de l'intérieur du
contenant, même cadrage/canvas que le sprite principal, fond transparent, sans
contour ni glow → à assigner au SpriteMask à la place du sprite artistique.
Prompt type : « solid white filled silhouette of the INSIDE of this flask
(interior volume only, no outline, no glow), same framing and canvas size,
transparent background ».

## 2026-07-07 (suite 3) — « Plus de bleu que de rouge » : instrumentation

### Constat
Bicolore Cyan 30 / Red 30 : la bande rouge est nettement plus fine que la cyan.
Or le calcul donne des hauteurs **strictement égales** quand les deux couches
sont pleines (`fillHeight × requiredCount / totalRequired`, soit moitié/moitié
ici). Une bande plus fine = `currentCount` de la couche n'a pas atteint son
quota (particules perdues sur les côtés, quota `totalToSpawn` du spawner épuisé
avant la fin du versement rouge…). `ContainerBase` compte 1 particule absorbée
= 1 incrément, pas de fuite possible côté comptage.

### Fait (code — `ContainerFillLevel.cs`)
- Compteur multi-couches : le label affiche le détail par couche
  (« Cyan 30/30 · Red 7/30 ») au lieu du seul total — on voit immédiatement
  quelle couche est en retard.
- `Debug.Log` quand une couche individuelle atteint son quota
  (« Couche Red pleine (30) »).

### À vérifier en Play
1. Refaire le test bicolore en surveillant le label (ou la Console).
2. Si le rouge plafonne sous 30 : vérifier `totalToSpawn` du spawner (le sucre
   est limité par design — prévoir de la marge en test : 0 = illimité) et les
   pertes latérales pendant le versement rouge.
3. Si le label affiche bien « Red 30/30 » avec une bande fine → me le signaler,
   ce serait alors un vrai bug de rendu à creuser.

### Note gameplay
Même à quotas égaux et couches pleines, sur une fiole conique la couche du bas
occupe visuellement plus de SURFACE (elle est plus large) — les hauteurs, elles,
sont égales. C'est l'effet attendu du « moitié/moitié » du niveau 6 (quantités
égales, pas surfaces égales).

## 2026-07-07 (suite 2) — Liquide découpé à la forme du verre (SpriteMask)

### Demande
Le liquide (rectangle scalé) dépassait de la silhouette du bécher. Question
initiale : « remplacer le BoxCollider2D par un MeshCollider ? » → Non : le
collider du contenant n'est qu'un **trigger de détection** des particules, il ne
dessine rien (et MeshCollider est 3D uniquement). La bonne approche 2D est un
**SpriteMask** qui découpe le rendu du liquide à la forme du verre.

### Fait (code — `ContainerFillLevel.cs`)
- Champ optionnel `liquidMask` (auto-détecté via `GetComponentInChildren` sinon) ;
  si présent, toutes les couches de liquide passent en
  `maskInteraction = VisibleInsideMask` dans `Awake`.

### À faire dans l'éditeur Unity
1. Dupliquer l'enfant `Glass` du Container → renommer `LiquidMask` (il garde
   ainsi le même sprite, la même position et le même scale que le verre).
2. Sur `LiquidMask` : supprimer le composant SpriteRenderer, ajouter un composant
   **Sprite Mask** et y assigner le sprite de la fiole (le même que `Glass`).
   Ajuster « Alpha Cutoff » (~0.3–0.5) si le halo/glow du sprite élargit le masque.
3. Play : le liquide n'apparaît plus qu'à l'intérieur de la silhouette.

### Gap entre les couches empilées (constaté après le masque)
Bande sombre entre le cyan et le rouge du bicolore : le sprite du `Liquid` était
« UISprite » (pilule arrondie, bords adoucis + ombre intégrée) — les bords des
deux pilules empilées créaient le gap. L'empilement calculé est bien bord à bord.
→ Fix éditeur : remettre le sprite **Square** sur l'enfant `Liquid` (les couches
clonées en héritent). Le SpriteMask s'occupe désormais de la forme du verre ;
le liquide n'a plus besoin d'un sprite arrondi.

### Notes
- Un SpriteMask est global à la scène : avec plusieurs contenants proches, le
  liquide de l'un pourrait être « visible » dans le masque de l'autre s'ils se
  chevauchent à l'écran — non-problème tant que les contenants sont espacés ;
  sinon utiliser Custom Range / sorting layers par contenant.
- Idéal à terme : générer via Gemini, pour chaque verrerie, un sprite « zone
  intérieure » dédié (silhouette pleine, sans contour ni glow) à utiliser comme
  masque à la place du sprite complet.

## 2026-07-07 (suite) — Bicolore : renderers de couche auto-créés

### Bug constaté (test bicolore cyan + rouge)
La 2ᵉ couleur versée était comptée mais **invisible** : le prefab n'a qu'un seul
enfant `Liquid` et `UpdateLiquidVisual` ne dessine que les couches ayant un
renderer assigné. Les particules rouges étaient donc absorbées « en silence »
(quota consommé sans retour visuel), d'où l'impression que rien ne se passait
quand le rouge coulait en premier.

### Fait (code — `ContainerFillLevel.cs`)
- `EnsureLayerRenderers()` (appelé dans `Awake`) : garantit un SpriteRenderer
  distinct par couche en **clonant automatiquement** le premier renderer valide
  pour les couches manquantes (ou assignées en double dans l'Inspector).
  → Passer un contenant en bicolore = ajouter une couche dans `Layers`, rien
  d'autre ; plus besoin de dupliquer `Liquid` à la main.
- Warnings simplifiés en conséquence (seul cas restant : aucun renderer du tout).

### Comportement attendu (à valider en Play)
- Bicolore cyan puis rouge : le rouge s'empile **au-dessus** du cyan.
- Rouge versé en premier : la couche rouge apparaît au fond, puis est repoussée
  vers le haut au fur et à mesure que le cyan se remplit dessous. L'ordre visuel
  bas → haut = l'ordre de la liste `Layers`, quel que soit l'ordre de versement.

## 2026-07-07 — Correctif : liquide qui « remonte » au remplissage

### Bug constaté (test en Play)
Le bas du liquide montait avec le remplissage (de ~-2 vers ~0) au lieu de rester
ancré au fond du contenant. Cause : `UpdateLiquidVisual()` calculait le scale en
supposant un sprite de **1 unité** (le Square intégré). Le sprite du `Liquid`
ayant été remplacé dans la scène par « UISprite » (0,32 unité, la pilule blanche
arrondie), la hauteur rendue ne valait qu'un tiers de la hauteur calculée — le
centrage supposant la pleine hauteur, le bas visuel grimpait à chaque particule.

### Fait (code — `ContainerFillLevel.cs`)
- Scale désormais normalisé par `sprite.bounds.size` : fonctionne avec n'importe
  quel sprite (Square, UISprite, future verrerie), et compensation du pivot si
  le sprite n'est pas centré.
- **Gizmo** (`OnDrawGizmosSelected`) : rectangle cyan dans la Scene view quand le
  contenant est sélectionné = zone de liquide (`fillWidth` / `fillHeight` /
  `fillBottomLocalY`). C'est LE moyen de caler la zone sur le sprite de verrerie.
- Nouveaux diagnostics dans `WarnIfMisconfigured` : renderer qui n'est pas enfant
  direct du contenant, même renderer assigné à plusieurs couches.

### À savoir / à faire dans l'éditeur Unity
1. **Le placement manuel du `Liquid` est écrasé au Play** (position + scale sont
   pilotés par le script). Ne pas caler le liquide à la main : sélectionner le
   Container et ajuster `fillWidth` / `fillHeight` / `fillBottomLocalY` jusqu'à
   ce que le rectangle cyan du gizmo épouse l'intérieur de la fiole.
   Dans `Level_Test`, `fillHeight` (1.9) est resté à la valeur du prefab alors
   que la fiole a été agrandie — à recaler.
2. **Test bicolore (2 couches)** : dupliquer l'enfant `Liquid` (→ `Liquid2`) et
   assigner les DEUX renderers dans `Layer Renderers` (ordre = ordre des couches).
   Un seul renderer partagé = warning Console désormais.
3. ⚠️ Les modifications faites en **mode Play sont annulées** par Unity à l'arrêt.
   La scène sauvegardée ne contient qu'une couche `acceptAnyColor` — reconfigurer
   les 2 couches colorées en mode édition puis sauvegarder.
4. L'override `acceptAnyColor` encore présent dans la scène sur le
   `ColorMatchChecker` est un résidu d'une ancienne version du script — inoffensif.
5. Le sprite de fiole actuel contient du **liquide dessiné dans l'image** — pour
   que le remplissage dynamique soit lisible, générer une version « fiole vide »
   (verre seul) via Gemini.

## 2026-07-06 (suite) — Contenants + validation couleur

### Fait (code)

**Scripts implémentés** (`Assets/Game/Scripts/Rules/Containers/`)

Généralisés en **couches** après récupération de la spec officielle des 10 niveaux
(consignée dans CLAUDE.md) — le niveau 6 exige un contenant bicolore moitié/moitié
et le niveau 8 un contenant « sucre normal » (toutes couleurs) :
- `ContainerFillLevel.cs` : liste de `FillLayer` (couleur OU `acceptAnyColor` +
  `requiredCount` chacune) — source de vérité unique des couleurs acceptées.
  1 couche = bécher/éprouvette, 2 couches = erlenmeyer bicolore. Visuel : les
  couches de liquide s'empilent depuis le fond de la zone décrite par
  `fillWidth`/`fillHeight`/`fillBottomLocalY` (à caler sur la zone de remplissage
  du sprite : 60 % bécher, 68 % éprouvette), chacune teintée à sa couleur (blanc
  = couche universelle). Événement `Filled` levé une seule fois quand toutes les
  couches sont pleines (futur GameManager / victoire).
- `ColorMatchChecker.cs` : délègue au `ContainerFillLevel` — une particule est
  valide si une couche non pleine accepte sa couleur.
- `ContainerBase.cs` : orchestre — `OnTriggerEnter2D` détecte une particule,
  la valide via `ColorMatchChecker`, la compte via `ContainerFillLevel` puis la
  **détruit** (absorption). Particule refusée = traverse sans effet.
- `ParticleColorExtensions.ToColor()` (dans `ParticleColor.cs`) : palette
  ParticleColor → Color Unity (liquides, future UI).

**Prefab `Container`** (`Prefabs/Containers/Container.prefab`, construit hors éditeur)
- Racine : `BoxCollider2D` **trigger** (1.6 × 2) + les 3 scripts
- Enfant `Glass` : Square built-in blanc alpha 0.08 (silhouette provisoire —
  à remplacer par les sprites de verrerie Gemini quand ils seront générés,
  `Art/Sprites/Glassware/` est encore vide)
- Enfant `Liquid` : Square built-in, état plein (1.5 × 1.9), teinté à la couleur
  acceptée au runtime, monte avec le remplissage

Ajouts : compteur joueur (`counterLabel` TMP optionnel, "12/30" puis "PLEIN") et
diagnostics de setup dans `Awake()` (couches manquantes, sprite non assigné → 
warnings Console explicites).

### À faire dans l'éditeur Unity
1. Glisser `Container.prefab` dans `Level_Test`, sous le spawner (pas pile en
   dessous). Dans `ContainerFillLevel` : ouvrir `Layers`, régler la couleur de la
   couche = couleur du spawner, `Required Count` ex. 30 (avec `totalToSpawn` = 100).
2. Vérifier que `Glass` et `Liquid` ont bien le sprite **Square** assigné (sinon
   invisible — un warning Console le signale au Play).
3. Compteur : clic droit sur le Container → `3D Object > Text - TextMeshPro`,
   placer le texte au-dessus du contenant (scale ~0.05, alignement centré),
   puis le glisser dans le champ `Counter Label` du `ContainerFillLevel`.
4. Play : guider le flux → le liquide monte, le compteur avance, log
   "Contenant rempli" à quota atteint.

### Validation attendue
- Particule de la bonne couleur : absorbée, liquide qui monte
- Particule d'une autre couleur : traverse le contenant sans compter
- Contenant plein : événement levé une fois, les particules suivantes ignorées

## 2026-07-06 — Particules (étape 3 du plan) + correctif LineLifetime

### Fait (code)

**Scripts implémentés** (`Assets/Game/Scripts/Rules/Particles/`)
- `ParticleColor.cs` (nouveau) : enum `Red, Blue, Green, Purple, Cyan` — clé logique
  commune pour le futur mélange (`ParticleColorMixer`) et la validation
  des contenants (`ColorMatchChecker`).
- `ParticleController.cs` : porte les 5 sprites d'orbe (ordre de l'enum) et sa
  couleur logique **sérialisée dans l'Inspector** ; tout changement de couleur passe
  par `SetColor()` (spawner, futur portail recolorant, mixer) qui met à jour le
  sprite. `OnValidate` donne l'aperçu immédiat dans l'éditeur. Force
  `Collision Detection = Continuous` sur le Rigidbody2D dans `Awake()`
  (anti-tunneling, cf. entrée du 2026-07-02), se détruit sous `killY`
  (particule perdue).
- `ParticleSpawner.cs` : référence le prefab unique `Particle`, émission à cadence
  fixe (`spawnInterval`), couleur changeable en cours de partie (`SetColor()`),
  quota configurable (`totalToSpawn`, 0 = illimité), léger jitter horizontal à
  l'émission pour éviter l'empilement instable, API `StartSpawning()` /
  `StopSpawning()` + `SpawnedCount` (pour le futur GameManager / détection de fin
  de niveau).

**Décision de design** (après avoir essayé un prefab par couleur) : **prefab unique**
`Prefabs/Particles/Particle.prefab` + échange de sprite via `SetColor()`. Raison :
un portail recolorant ou le futur `ParticleColorMixer` doivent changer la couleur
d'une particule **vivante** — trivial par échange de sprite, alors que l'approche
multi-prefabs imposait de détruire/réinstancier en recopiant position + vélocité.
Le prefab a été construit hors éditeur (YAML) : SpriteRenderer + CircleCollider2D +
Rigidbody2D + `ParticleController` avec les 5 sprites pré-assignés.

**Piège rencontré** : poser `ParticleController` sur un GameObject vide de la scène
lui ajoute automatiquement un Rigidbody2D (`RequireComponent`) → l'objet tombe sous
la gravité. Ce script ne va QUE sur les prefabs de particules ; l'objet spawner de
la scène ne porte que `ParticleSpawner`.

**Correctif** : `LineLifetime.cs` était en réalité resté un squelette (contrairement
à ce que disait l'entrée du 2026-07-02 — le trait ne disparaissait pas). Implémenté :
délai configurable, fondu de l'alpha du LineRenderer, puis `Destroy`.

### À faire dans l'éditeur Unity (non scriptable)
1. Vérifier `Particle.prefab` dans l'Inspector : le `ParticleController` doit
   afficher les 5 sprites dans `Color Sprites` (ordre Red, Blue, Green, Purple,
   Cyan) — construit hors éditeur, à contrôler après recompilation.
2. Dans `Level_Test` : ajouter un GameObject vide `ParticleSpawner` en haut de
   l'écran (script `ParticleSpawner` uniquement — surtout pas `ParticleController`,
   son `RequireComponent` ajoute un Rigidbody2D et l'objet tomberait), assigner
   `Particle.prefab`, choisir couleur/cadence/quota. Supprimer `TestBall`.
3. Vérifier que le prefab `Line` a bien `LineLifetime` attaché (le script est
   désormais fonctionnel) et régler `lifetime` / `fadeDuration`.

### Validation attendue
- Flux de particules qui tombe du spawner et roule sur les traits dessinés
- Les particules disparaissent sous le bas de l'écran
- Le trait s'estompe puis disparaît après son délai de vie

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
