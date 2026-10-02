# Prompt IA — Application Blazor Server « Rallye QCM »

> **Contexte** : Tu es un développeur expert en C# / .NET 8+ / Blazor Server et MongoDB.
> Tu dois développer une application web complète de jeu de piste / rallye QCM par QR codes dans un village.
> L'application est déployée sur un serveur (Docker Compose) et accessible par les joueurs via leur navigateur mobile (Android / iOS) en scannant un QR code.

---

## 📌 État d'avancement du développement

### Déjà réalisé dans le code :
- ✅ **Application Blazor Server .NET 8** avec rendu Interactive Server.
- ✅ **Deux modes de stockage** : mémoire pour le développement local sans MongoDB et MongoDB via `IGameDataStore`, sélectionnés par `UseInMemoryStore`.
- ✅ **Docker Compose** : conteneurs applicatifs et MongoDB avec volume persistant ; la configuration Docker désactive le mode mémoire.
- ✅ **Modélisation BSON MongoDB** : entités `Team`, `Step`, `QcmStep`, `GameStep`, `Question`, `AnswerOption`, `Submission`, `GameState` et service d'accès MongoDB.
- ✅ **Parcours de rejoindre une équipe** : Interface mobile-first adaptée aux seniors (`/inscription`), sans saisie de nom, affichant les équipes disponibles avec leur nom et leur couleur.
- ✅ **Salle d'attente d'équipe** : Vue (`/attente/{TeamId}`) affichant l'équipe choisie et l'état de publication de son parcours.
- ✅ **Authentification admin persistante** : cookie de 30 jours, expiration glissante, protection des routes d'administration, connexion via `/admin/authenticate` et déconnexion.
- ✅ **Tableau de bord Organisateur** : vue responsive (`/admin`) avec création manuelle des équipes, nom, couleur et QR Code d'accès joueur (`QRCoder`).
- ✅ **Données de test** : bouton en bas de `/admin` créant de manière idempotente trois équipes, trois questions QCM, leurs étapes et des parcours mélangés.
- ✅ **Éditeur de parcours** (`/admin/steps`) : création des étapes QCM ou jeu, édition inline de la question QCM, de 2 à 4 réponses et de leur type (texte, son ou photo), bonne réponse, points et fourchette de points des jeux.
- ✅ **Confirmations de suppression** : équipes, étapes et questions liées.
- ✅ **Notifications internes** : événements Blazor pour les joueurs, équipes, stations, état de jeu et scores, utilisés par les écrans existants.
- ✅ **Première étape du modèle de jeu** : modèle `Step` typé (`QCM`/`GAME`), destination, consignes, QR code éventuel et lien vers une question ; repository `Steps` disponible en mémoire et dans MongoDB ; `Team.RouteStepIds` remplace la référence prévue aux stations.
- ✅ **Première vue de jeu mobile** : `/jeu/{TeamId}` affiche la progression, la destination, les consignes et le QCM de l'étape courante, avec confirmation et correction immédiate.
- ✅ **Protection de session d'équipe** : jusqu'à deux appareils persistants par équipe, jetons stockés côté navigateur et dans un cookie de secours, sous forme de hash côté serveur, avec verrouillage des validations concurrentes.
- ✅ **Préparation et publication des parcours** : `/admin/steps` permet de créer des étapes, de générer un ordre distinct par équipe et de publier les parcours.
- ✅ **Parcours mobile et étapes Jeu** : le parcours est conservé côté serveur ; `/jeu/{TeamId}` révèle uniquement l'étape courante, sa destination et son barème, puis permet de saisir un score, de l'enregistrer et d'avancer vers l'étape suivante.
- ✅ **Accès QR et code de secours** : les QR d'étape ouvrent `/scan/{token}` ; le joueur peut aussi saisir le code court sur `/code`. Dans les deux cas, seule l'étape courante de l'équipe est acceptée.

### Implemented end-to-end:
- ✅ **Automatic route assignment**: the launch action generates a distinct route for every team; no two routes share more than two consecutive steps.
- ✅ **Device administration**: active devices are listed per team and can be revoked individually.
- ✅ **Mobile gameplay and access**: teams submit QCM answers and game scores; QR tokens and short codes resolve only the expected step.
- ✅ **Game rankings**: raw scores are stored per team and game. Placement points are awarded only after every participating team submits a score. Tied teams share the points for that place.
- ✅ **Final-word puzzle**: the administrator enters the answer at launch; its letters are shuffled once and revealed by route progress, identically for all teams.
- ✅ **Game controls**: administrators can start, pause, resume, and finish a rally. Pauses and finished games reject new submissions.
- ✅ **Live projection (`/projection`)**: displays tied ranks, team status, progress, next destination, QCM results, raw game scores, and pending teams for each game.
- ✅ **Live supervision and CSV export**: `/admin/supervision` shows team progress, results, and devices; `/admin/export.csv` downloads the results for authenticated administrators.

### Remaining decisions before production:
- ⏳ Confirm whether equal overall scores should remain tied or use a secondary rule. The application currently records start and finish times but does not use them to break ties.
- ⏳ Complete production verification with the target MongoDB deployment, backup strategy, and multi-device field conditions.

### Écarts importants entre les règles et le code actuel

- L'administrateur crée explicitement chaque équipe avec son nom et sa couleur ; aucune équipe n'est générée automatiquement et aucun thème par défaut n'est imposé.
- `/jeu/{TeamId}` reveals only the current step, supports text, audio, and image answers, stores game scores, and advances the team's route.
- `Step` is the polymorphic base for `QcmStep` and `GameStep`. Automatic route generation at launch, score storage, game rankings, character reveals, and route access validation are implemented.
- Une question contient entre deux et quatre `AnswerOption` et un type commun `AnswerType` (`TEXT`, `AUDIO` ou `IMAGE`) appliqué à toutes ses réponses. Les anciens champs `OptionA` à `OptionD` sont conservés pour compatibilité.
- Live updates use the singleton `GameEventService` within the Blazor Server process. Multi-instance deployments require a shared backplane or distributed event transport.
- Le stockage mémoire reste activé par défaut dans `appsettings.json` ; il est adapté au développement local mais ne doit pas être utilisé pour une partie réelle si les données doivent survivre à un redémarrage.

## 📌 Règles métier mises à jour — version 3

Le rallye est constitué d'un ensemble ordonné d'**étapes**. Chaque étape est d'un seul type :

1. **QCM** : une question est affichée à l'étape attendue. Elle contient de deux à quatre réponses A à D, dont une seule est exacte. Le type des réponses est commun à la question : texte, photo ou son. La correction est immédiate et l'équipe voit tout de suite si sa réponse est correcte.
2. **Jeu** : l'étape affiche une interface permettant de saisir le score obtenu au jeu breton loué à Loanig Park. Ce score est ensuite utilisé pour classer les équipes et attribuer les points.

Les règles suivantes sont obligatoires :

- Le nombre d'équipes est déterminé par le nombre d'équipes créées explicitement par l'organisateur ; aucune valeur par défaut ne doit être appliquée.
- Une équipe peut enregistrer un ou deux smartphones autorisés. Les deux appareils partagent la même session d'équipe ; une seule validation est possible par étape, quel que soit l'appareil utilisé.
- Chaque équipe reçoit un parcours ordonné composé d'étapes QCM et d'étapes jeu. Elle ne peut avancer qu'à l'étape suivante de son propre parcours.
- Le groupe est l'unité de jeu : les réponses QCM, les scores de jeux, les indices et la progression appartiennent à l'équipe, pas à un joueur individuel.
- Au lancement, l'administrateur saisit le mot final, une indication de transition et une image commune. The word is shuffled once; every team receives the same letters in the same order of route progress, regardless of its route order.
- The app stores each team's revealed letters, shows the full shuffled set after the last step, and lets the team submit the final word.
- Between validated steps, the player screen shows the administrator's shared transition hint and map image, plus the next destination; the team explicitly continues before seeing the next question or game.
- Les parcours peuvent avoir un ordre différent, mais aucun parcours ne doit suivre celui d'une autre équipe pendant plus de deux étapes consécutives.
- Les scores d'un jeu sont comparés lorsque toutes les équipes ont terminé ce jeu. Les points sont alors attribués selon le classement : première équipe 10 points, puis 9, 8, etc. Le classement intermédiaire affiche les scores bruts et les équipes restantes, sans figer les points de place avant la fin.
- Le classement général reste actualisé en direct avec les points QCM déjà acquis, les scores bruts des jeux et la progression. Les points de place d'un jeu ne sont ajoutés qu'à sa clôture.
- La projection doit montrer, pour chaque équipe, sa progression détaillée : nombre d'étapes terminées, nombre total d'étapes du parcours, pourcentage d'avancement, étape actuelle, prochaine destination et état de l'équipe (en attente, en cours, terminée ou bloquée).
- La projection doit également montrer, pour chaque équipe, les QCM réalisés et leurs résultats, ainsi que les scores obtenus à chaque jeu Loanig Park.
- Le système doit prévoir un départage des égalités, à confirmer avec l'organisateur, sans utiliser le temps d'un jeu Loanig Park pour remplacer son classement par score.

---

## 1. Vue d'ensemble du projet

### 1.1 Objectif
Développer une **application Blazor Server (.NET 8+)** avec base de données **MongoDB** permettant d'organiser un jeu de piste / rallye QCM dans un village.

Les joueurs (potentiellement des personnes âgées) **scannent un QR code avec leur téléphone** pour accéder à l'application et choisissent directement l'équipe indiquée par l'organisateur.

L'application comporte :
- Une **vue joueur** mobile-first ultra-ergonomique, adaptée aux personnes âgées (smartphones Android & iOS)
- Une **vue administration** desktop-first responsive sécurisée par un mot de passe défini en variable d'environnement
- Une **vue projection plein écran** pour vidéoprojecteur affichant le classement en temps réel

---

## 2. Architecture technique

### 2.1 Stack technologique
- **Framework** : Blazor Server (.NET 8+)
- **Langage** : C#
- **Base de données** : MongoDB
- **Driver BDD** : `MongoDB.Driver` officiel (.NET)
- **Temps réel** : SignalR (intégré nativement dans Blazor Server)
- **Authentification admin** : mot de passe unique stocké en **variable d'environnement** (`ADMIN_PASSWORD`)
- **QR Codes** : génération côté serveur (bibliothèque `QRCoder` ou équivalente)
- **CSS** : Vanilla CSS responsive, sans framework CSS externe

### 2.2 Architecture applicative
```
┌──────────────────────────────────────────────────┐
│                  Blazor Server                   │
│                                                  │
│  ┌────────────┐  ┌─────────────┐  ┌───────────┐ │
│  │ Vue Joueur │  │  Vue Admin  │  │ Vue       │ │
│  │ (Mobile)   │  │  (Desktop)  │  │ Projection│ │
│  └─────┬──────┘  └──────┬──────┘  └─────┬─────┘ │
│        │                │               │        │
│        └────────┬───────┘───────────────┘        │
│                 │                                 │
│          ┌──────▼──────┐                         │
│          │  Services   │                         │
│          │  métier     │                         │
│          └──────┬──────┘                         │
│                 │                                 │
│          ┌──────▼──────┐     ┌──────────────┐    │
│          │   MongoDB   │────▶│   MongoDB    │    │
│          │   Driver    │     │   Database   │    │
│          └─────────────┘     └──────────────┘    │
│                                                  │
│          SignalR Hub (temps réel)                 │
└──────────────────────────────────────────────────┘
```

---

## 3. Fonctionnalité clé : équipes créées par l'administrateur

### 3.1 Règles des équipes
Les équipes sont préparées directement par l'administrateur :
1. L'administrateur crée chaque équipe manuellement avec un nom et une couleur.
2. Le nombre d'équipes correspond exactement au nombre d'équipes créées.
3. Les joueurs choisissent directement l'équipe qu'ils doivent rejoindre ; aucune répartition automatique n'est effectuée.

### 3.3 Workflow de choix d'équipe
1. L'admin génère un **QR code d'accès joueur** (URL vers la page `/inscription`).
2. Le QR code est imprimé ou affiché sur un écran dans la salle d'accueil.
3. Le joueur **scanne le QR code** avec son smartphone (caméra native → ouvre le navigateur mobile).
4. Il arrive sur `/inscription` et choisit une équipe en voyant son **nom et sa couleur**.
5. Le téléphone conserve l'identifiant de l'équipe dans `localStorage` et dans un cookie de secours.
6. Le joueur arrive sur `/attente/{TeamId}` et attend la publication du parcours.
7. L'organisateur crée les équipes avant ou pendant l'ouverture de l'accès joueur.

---

## 4. Vues de l'application

### 4.1 Vue Joueur (Mobile-first — Android & iOS)

> **CRITIQUE** : Les joueurs peuvent être des **personnes âgées**. L'interface doit être **extrêmement simple, lisible et ergonomique**.

#### Principes d'ergonomie obligatoires :
- **Taille de police minimale** : 18px pour le texte courant, 24px+ pour les titres et boutons
- **Boutons très grands** : hauteur minimale 56px, largeur 100% de l'écran quand possible
- **Contraste élevé** : ratio de contraste minimum 7:1 (WCAG AAA)
- **Pas de double-tap, pas de swipe, pas de gestes complexes** : uniquement des taps simples
- **Feedback visuel clair** : changement d'état immédiat au tap, confirmations explicites
- **Pas de scroll horizontal** : tout le contenu s'adapte à la largeur de l'écran
- **Icônes + texte** : ne jamais utiliser une icône seule sans texte explicatif
- **Espacement généreux** : padding 16px minimum entre les zones d'interaction
- **Messages simples et bienveillants** : « Bravo ! », « Bien joué ! », aucun jargon technique
- **Pas de compte à rebours stressant** : laisser le temps de lire et répondre sereinement

#### Écrans joueur (Parcours complet & intuitif) :
1. **Page de choix d'équipe** (`/inscription`)
   - Liste claire des équipes avec leur nom et leur couleur.
   - Un seul tap pour rejoindre une équipe.
   - Persistance automatique de l'équipe dans `localStorage` et dans un cookie de secours.

2. **Écran d'attente & révélation** (`/attente/{TeamId}`)
   - Message rassurant en attente de la publication du parcours.
   - Affichage de l'**équipe choisie** (nom, couleur).
   - Bouton pour changer d'équipe et effacer la session locale.

3. **Écran d'orientation & Prochaine étape** (`/jeu`)
   - Indication claire : « Étape 3 sur [nombre total] » et « Prochaine étape : [Nom de la destination] ».
   - Indice de localisation textuel ou visuel (ex: « Rendez-vous près du lavoir »).
   - Accompagnement bienveillant : si l'équipe scanne un QR code qui ne correspond pas à l'étape attendue, l'application lui indique l'erreur et rappelle la destination correcte.

4. **Accès à l'étape QCM (Double mode intuitif)** :
   - Le parcours actuel affiche directement l'étape attendue dans `/jeu/{TeamId}`.
   - Le scan des QR codes et l'utilisation du code court de secours restent à intégrer au parcours joueur.

5. **Écran d'étape jeu** (`/jeu/{stepId}`) :
   - Affichage du nom du jeu et des consignes utiles.
   - Saisie du score obtenu par l'équipe.
   - Contrôle et confirmation avant enregistrement définitif.
   - Affichage du score enregistré et de la prochaine destination.

6. **Écran de question QCM & médias** (`/jeu/{TeamId}`)
   - Énoncé de la question en gros caractères lisibles.
   - Affichage de la question et de chaque réponse avec son propre contenu : texte, photo ou son.
   - De 2 à 4 grands boutons pleine largeur : **A**, **B**, **C**, **D**, selon le nombre de réponses configurées.
   - **Étape de confirmation anti-erreur** : boîte de confirmation simple (« Vous avez sélectionné la réponse B. Valider définitivement ? ») pour éviter les clics accidentels.

7. **Écran de résultat & Synchronisation d'équipe** (`/resultat`)
   - Résultat immédiat : « Bonne réponse ! +10 points » ou « Mauvaise réponse. La bonne réponse était A. »
   - Synchronisation temps réel : dès qu'un membre de l'équipe valide la réponse, les écrans de tous les coéquipiers affichent le résultat.
   - Grand bouton « Étape suivante → » orientant vers la destination suivante.

8. **Écran de fin de parcours & Palmarès** (`/fin`)
   - Affiché une fois toutes les étapes du parcours complétées.
   - Récapitulatif : résultats des QCM, scores bruts des jeux, caractères révélés et points de classement disponibles.
   - Consigne d'invitation à regagner la salle pour la cérémonie des résultats sur grand écran.

### 4.2 Vue Admin (Desktop-first — responsive mais optimisée PC)

L'administrateur accède à `/admin` et s'authentifie via le mot de passe défini dans la variable d'environnement `ADMIN_PASSWORD`.

#### Authentification admin :
- Page de login simple avec champ mot de passe
- Comparaison sécurisée avec `Environment.GetEnvironmentVariable("ADMIN_PASSWORD")`
- Session maintenue côté Blazor Server
- Pas de gestion multi-comptes : mot de passe unique

#### Écrans admin :
1. **Dashboard** (`/admin`)
   - Vue globale : inscrits, équipes, état d'avancement du rallye
   - Liens directs vers chaque module

2. **Création des équipes** (`/admin`)
   - Création manuelle de chaque équipe
   - Saisie du nom et choix de la couleur
   - Suppression d'une équipe avant le début du jeu
   - Les joueurs ne sont pas nommés et ne sont pas répartis automatiquement

3. **Gestion du parcours et du contenu** (`/admin/steps`)
   - Création des étapes dans un ordre unique
   - Pour une étape QCM : édition de la question, choix d'un type commun texte/son/photo, de 2 à 4 réponses, de la bonne réponse et des points
   - Pour une étape jeu : édition de la fourchette de points minimum/maximum
   - Génération et export/impression des QR codes des étapes QCM
   - Génération et impression du QR code d'inscription

7. **Gestion des parcours** (`/admin/parcours`)
   - Ordre des étapes pour chaque équipe
   - Algorithme de génération sous contrainte : jamais plus de 2 étapes consécutives identiques entre 2 équipes
   - Visualisation graphique et signalement des conflits

8. **Pilotage du jeu en direct** (`/admin/jeu`)
   - Contrôle général : Démarrer, Pause, Arrêter
   - Flux d'événements en direct (soumissions, scores, scans)
   - Outil de déblocage manuel d'une équipe ou correction de score

9. **Classement admin & export** (`/admin/classement`)
   - Tableau complet détaillé
   - Export CSV / Excel des résultats, réponses et statistiques

10. **Vue projection** (`/projection`)
    - Écran dédié vidéoprojecteur, sans menus administratifs
    - Affichage plein écran, typographies très grandes, contrastées
   - Une ligne ou une carte lisible par équipe avec : nom/couleur, progression, étape actuelle, prochaine destination et état
   - Barre ou indicateur visuel d'avancement calculé à partir des étapes validées et du total du parcours
   - Affichage séparé des QCM réalisés, des résultats QCM, des scores bruts des jeux et des points de classement déjà attribués
   - Animations lors des gains de points et actualisation temps réel via SignalR

---

## 5. Modèle de données MongoDB

Le stockage repose sur **MongoDB** via le driver C# officiel `MongoDB.Driver`.

### 5.1 Modélisation des documents BSON

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// Collection : "friend_groups"
public class FriendGroup
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Collection : "players"
public class Player
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("last_name")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("friend_group_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string FriendGroupId { get; set; } = string.Empty;

    [BsonElement("team_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? TeamId { get; set; }

    [BsonElement("connection_token")]
    public string ConnectionToken { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

// Collection : "teams"
public class Team
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("current_step")]
    public int CurrentStep { get; set; } = 1;

    [BsonElement("total_score")]
    public int TotalScore { get; set; } = 0;

      [BsonElement("route_step_ids")]
    [BsonRepresentation(BsonType.ObjectId)]
      public List<string> RouteStepIds { get; set; } = new(); // Ordre des étapes du parcours

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("started_at")]
    public DateTime? StartedAt { get; set; } // Heure de top départ (départage ex-aequo)

    [BsonElement("finished_at")]
    public DateTime? FinishedAt { get; set; } // Heure de franchissement de la dernière étape
}

public enum StepType { QCM, GAME }

// Collection : "steps"
[BsonKnownTypes(typeof(QcmStep), typeof(GameStep))]
public class Step
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
   public StepType Type { get; set; }

   [BsonElement("destination")]
   public string Destination { get; set; } = string.Empty;

   [BsonElement("instructions")]
   public string Instructions { get; set; } = string.Empty;

    [BsonElement("qr_token")]
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("short_code")]
   public string ShortCode { get; set; } = string.Empty; // Code numérique généré automatiquement

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
}

public class QcmStep : Step
{
   [BsonElement("question_id")]
   [BsonRepresentation(BsonType.ObjectId)]
   public string? QuestionId { get; set; }
}

public class GameStep : Step
{
   [BsonElement("min_points")]
   public int MinPoints { get; set; }

   [BsonElement("max_points")]
   public int MaxPoints { get; set; }
}

// Collection : "game_scores"
public class GameScore
{
   [BsonId]
   [BsonRepresentation(BsonType.ObjectId)]
   public string Id { get; set; } = string.Empty;

   [BsonElement("team_id")]
   [BsonRepresentation(BsonType.ObjectId)]
   public string TeamId { get; set; } = string.Empty;

   [BsonElement("step_id")]
   [BsonRepresentation(BsonType.ObjectId)]
   public string StepId { get; set; } = string.Empty;

   [BsonElement("raw_score")]
   public decimal RawScore { get; set; }

   [BsonElement("ranking_points")]
   public int? RankingPoints { get; set; }

   [BsonElement("is_final")]
   public bool IsFinal { get; set; }

   [BsonElement("submitted_at")]
   public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public enum MediaType { NONE, IMAGE, AUDIO, VIDEO }
public enum AnswerContentType { TEXT, AUDIO, IMAGE }
public enum Choice { A, B, C, D }

// Collection : "questions"
public class Question
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("station_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StationId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

   [BsonElement("answer_type")]
   [BsonRepresentation(BsonType.String)]
   public AnswerContentType AnswerType { get; set; } = AnswerContentType.TEXT;

   [BsonElement("options")]
   public List<AnswerOption> Options { get; set; } = new();

    [BsonElement("media_type")]
    [BsonRepresentation(BsonType.String)]
    public MediaType MediaType { get; set; } = MediaType.NONE;

    [BsonElement("media_url")]
    public string? MediaUrl { get; set; }

    [BsonElement("image_data")]
    public string? ImageData { get; set; } // Stockage direct de l'image redimensionnée en BDD (Base64 Data URI)

    [BsonElement("option_a")]
    public string OptionA { get; set; } = string.Empty;

    [BsonElement("option_b")]
    public string OptionB { get; set; } = string.Empty;

    [BsonElement("option_c")]
    public string OptionC { get; set; } = string.Empty;

    [BsonElement("option_d")]
    public string OptionD { get; set; } = string.Empty;

    [BsonElement("correct_answer")]
    [BsonRepresentation(BsonType.String)]
    public Choice CorrectAnswer { get; set; }

    [BsonElement("points")]
    public int Points { get; set; } = 10;
}

public class AnswerOption
{
   [BsonElement("choice")]
   [BsonRepresentation(BsonType.String)]
   public Choice Choice { get; set; }

   [BsonElement("content")]
   public string Content { get; set; } = string.Empty;
}

// Collection : "submissions"
public class Submission
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("team_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("player_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PlayerId { get; set; }

    [BsonElement("question_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string QuestionId { get; set; } = string.Empty;

    [BsonElement("station_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StationId { get; set; } = string.Empty;

    [BsonElement("chosen_answer")]
    [BsonRepresentation(BsonType.String)]
    public Choice ChosenAnswer { get; set; }

    [BsonElement("is_correct")]
    public bool IsCorrect { get; set; }

    [BsonElement("points_earned")]
    public int PointsEarned { get; set; }

    [BsonElement("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public enum GamePhase 
{ 
    REGISTRATION,    // Inscriptions ouvertes
    TEAM_BUILDING,   // Constitution des équipes
    READY,           // Équipes validées, en attente de départ
    IN_PROGRESS,     // Rallye en cours
    PAUSED,          // Pause
    FINISHED         // Rallye terminé
}

// Collection : "game_state" (document unique)
public class GameState
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("phase")]
    [BsonRepresentation(BsonType.String)]
    public GamePhase Phase { get; set; } = GamePhase.REGISTRATION;

    [BsonElement("started_at")]
    public DateTime? StartedAt { get; set; }

    [BsonElement("ended_at")]
    public DateTime? EndedAt { get; set; }
}
```

### 5.2 Index MongoDB recommandés
- `stations.qr_token` : index unique pour recherche immédiate lors du scan QR
- `submissions` : index composé `{ team_id: 1, question_id: 1 }` (unique) pour empêcher les doubles soumissions
- `players.connection_token` : index unique pour reprise de session joueur
- `teams.total_score` (-1) + `teams.updated_at` (1) : pour tri ultra-rapide du classement

---

## 6. Temps réel (SignalR)

SignalR (intégré à Blazor Server) gère la synchronisation bidirectionnelle :
1. **Inscriptions instantanées** : actualisation de la liste admin sans rechargement
2. **Attribution automatique** : notification en direct aux téléphones des joueurs dès validation des équipes
3. **Mise à jour des scores** : recalcule et pousse les nouveaux scores à la vue projection et aux mobiles
4. **Fil d'activité live** : notifications des événements dans le tableau de bord admin

---

## 7. QR Codes

### 7.1 QR code d'inscription
- Encode l'URL : `https://{domaine}/inscription`
- Généré côté serveur et affiché dans l'interface admin (téléchargeable / imprimable)

### 7.2 QR codes des étapes QCM & Code court de secours
- Encode l'URL : `https://{domaine}/station/{qr_token}`
- **Code court de secours** : chaque étape reçoit automatiquement un code numérique unique à six chiffres, affiché dans l'administration pour pouvoir être imprimé avec le support physique. Il ne doit pas être demandé à l'administrateur lors de la création.
- Vérifications serveur lors de la validation :
  - Validité et état actif de la station
  - Correspondance avec l'étape courante de l'équipe (ou redirection guidée si l'équipe s'est trompée de station)
  - Absence de réponse déjà enregistrée pour cette station (anti-doublon)

### 7.3 Implémentation
- Bibliothèque NuGet recommandée : `QRCoder`
- Génération en SVG ou PNG haute résolution, prête pour l'impression des fiches terrain

---

## 8. Logique des parcours & Départage

- **Nombre d'étapes configurable** : lieux QCM dans le bourg et jeux Loanig Park dans la salle.
- **Nombre d'équipes configurable**, choisi explicitement par l'organisateur.
- **Règle fondamentale des parcours** : deux équipes ne doivent jamais avoir plus de 2 étapes consécutives identiques dans le même ordre.
- **Générateur automatique** : attribution d'un ordre de passage personnalisé par équipe avec contrôle matriciel anti-suivi.
- **Classement différé des jeux** : un jeu n'attribue ses points de place qu'après la saisie de toutes les équipes ; les scores bruts restent visibles avant cette clôture.
- **Départage des ex-aequo** : le critère doit être confirmé avant la mise en production. Le temps d'un jeu ne remplace pas son classement par score.

---

## 9. Gestion réseau et ergonomie mobile

- Reconnexion automatique Blazor Server / SignalR en cas de baisse de signal 4G/WiFi
- Indicateur visuel bienveillant en cas de déconnexion (« Connexion en cours de rétablissement... »)
- Sauvegarde de l'identifiant d'équipe et du jeton d'appareil dans `localStorage`, avec cookie de secours de sept jours pour réauthentification transparente après rechargement

---

## 10. Variables d'environnement

## 10 bis. Décisions métier à confirmer

Avant d'implémenter le modèle final, demander confirmation sur les points suivants :

- le nombre de QCM et de jeux, et le fait que chaque équipe réalise ou non toutes les étapes ;
- le rôle exact des deux smartphones (principal et secours) et la règle en cas de soumission concurrente ;
- la personne autorisée à saisir et corriger le score d'un jeu Loanig Park ;
- le caractère indicatif ou strict de la durée d'environ dix minutes par jeu ;
- le départage des égalités de score dans un jeu et du classement général ;
- la valeur d'une mauvaise réponse, les pénalités et les éventuels bonus QCM ;
- la forme finale des caractères révélés et la liste définitive des lots, le dernier lot étant encore à définir.

| Variable | Description | Exemple |
|---|---|---|
| `ADMIN_PASSWORD` | Mot de passe de la vue administrateur | `MonSecretAdmin2026!` |
| `MONGODB_URI` | Chaîne de connexion MongoDB | `mongodb://localhost:27017` ou `mongodb+srv://...` |
| `MONGODB_DATABASE` | Nom de la base de données MongoDB | `RallyeQcmDb` |
| `BASE_URL` | URL racine publique du site | `https://rallye.mondomaine.fr` |

---

## 11. Structure recommandée du projet Blazor Server

```
KerRandoQcm/
├── Program.cs
├── appsettings.json
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   ├── PlayerLayout.razor        # Layout épuré mobile
│   │   └── AdminLayout.razor         # Layout barre latérale PC
│   ├── Pages/
│   │   ├── Player/
│   │   │   ├── Inscription.razor     # Formulaire inscription mobile
│   │   │   ├── Attente.razor         # Salle d'attente
│   │   │   ├── MonEquipe.razor       # Fiche équipe & coéquipiers
│   │   │   ├── Jeu.razor             # Vue d'étape courante
│   │   │   ├── Question.razor        # Question QCM A/B/C/D
│   │   │   ├── Resultat.razor        # Feedback réponse
│   │   │   └── Classement.razor      # Classement mobile
│   │   ├── Admin/
│   │   │   ├── Login.razor           # Login admin
│   │   │   ├── Dashboard.razor       # Synthèse générale
│   │   │   ├── Steps.razor           # Parcours et contenu QCM/jeu
│   │   │   ├── LiveControl.razor     # Supervision en direct
│   │   │   └── ClassementAdmin.razor # Classement complet & exports
│   │   └── Projection/
│   │       └── Projection.razor      # Écran vidéoprojecteur
│   └── Shared/
│       ├── PlayerNav.razor
│       └── AdminNav.razor
├── Data/
│   └── MongoContext.cs               # Accès collections MongoDB
├── Models/                           # Classes BSON (Player, Team, Station, etc.)
├── Services/
│   ├── GameService.cs                # Déroulement du jeu
│   ├── RouteService.cs               # Génération et validation des parcours
│   ├── QrCodeService.cs             # Production des QR codes (QRCoder)
│   └── ExportService.cs             # Exportation CSV / Excel
└── wwwroot/
    ├── css/
    │   ├── player.css                # Style mobile, gros contrastes, grands boutons
    │   ├── admin.css                 # Style desktop
    │   └── projection.css            # Style vidéoprojecteur
    ├── js/
    │   └── cameraScanner.js          # Interop JS éventuel pour caméra web
    └── uploads/                      # Médias stockés
```

---

## 12. Feuille de route de développement séquentielle

Cette liste doit être suivie **dans l'ordre**. L'IA doit traiter une seule tâche à la fois, vérifier que le projet compile et que les tests disponibles passent, puis seulement commencer la tâche suivante. Elle ne doit pas implémenter une fonctionnalité annoncée comme déjà réalisée et ne doit pas modifier les règles métier sans les signaler.

Pour chaque tâche :

1. lire le code existant concerné ;
2. identifier les fichiers à modifier ;
3. réaliser la modification minimale ;
4. compiler et exécuter le test ciblé ;
5. mettre à jour ce prompt si l'état d'avancement change ;
6. signaler les décisions ou blocages avant de poursuivre.

### Tâche 1 — Stabiliser le modèle et le stockage des étapes — TERMINÉE

Le modèle `Step`, son type `QCM`/`GAME`, son repository mémoire/MongoDB et la référence `Team.RouteStepIds` sont maintenant intégrés. La construction et la validation des parcours restent réservées à la tâche 3.

- Créer le modèle `Step` avec les types `QCM` et `GAME`.
- Ajouter le nom, la destination, les consignes, le QR code éventuel, le code court et l'état actif.
- Remplacer progressivement les références de parcours à `Station` par des identifiants d'étapes.
- Ajouter les repositories et le support MongoDB/in-memory dans `IGameDataStore`.
- Conserver la compatibilité avec les données existantes pendant la migration.
- Vérifier que les deux modes de stockage compilent et fonctionnent.

### Tâche 2 — Ajouter les appareils autorisés d'une équipe

- Permettre l'association d'un ou deux smartphones à une équipe.
- Utiliser un identifiant d'appareil ou un jeton sécurisé, sans stocker inutilement de données personnelles.
- Autoriser les deux appareils à consulter la progression de la même équipe.
- Empêcher une double validation concurrente d'une même étape.
- Prévoir la réinitialisation d'un appareil par l'administrateur.
- Tester l'utilisation avec un seul appareil puis avec deux appareils.

### Tâche 3 — Créer les parcours des équipes — TERMINÉE

- Ajouter le modèle de parcours ordonné et ses étapes.
- Générer un parcours distinct pour chaque équipe automatiquement au lancement du jeu.
- Vérifier qu'une équipe ne peut valider que l'étape suivante de son parcours.
- Enregistrer l'étape courante, les étapes terminées et la prochaine destination.
- Afficher la destination, le lieu, le type, la consigne et le QR code éventuel entre deux étapes.

### Tâche 4 — Générer les parcours automatiquement — TERMINÉE

- Générer un ordre aléatoire à partir du nombre d'équipes et des étapes actives au lancement du jeu.
- Éviter qu'un parcours complet soit identique à celui d'une autre équipe ; deux équipes peuvent néanmoins se trouver sur la même étape à des moments différents.
- Refuser le lancement et expliquer le problème si le nombre d'ordres conformes ne suffit pas.
- Ne pas imposer de nombre fixe d'équipes ni de nombre fixe d'étapes.
- Ajouter des tests sur plusieurs nombres d'équipes et plusieurs ensembles d'étapes. Un test couvre déjà les parcours distincts et le cas impossible.

### Tâche 5 — Construire l'écran joueur de progression — PARTIELLEMENT TERMINÉE

- Créer la page de l'étape courante.
- Afficher la progression : étapes terminées, total, pourcentage, étape courante et prochaine destination.
- Ajouter un état clair : en attente, en cours, terminée ou bloquée.
- Synchroniser cette progression sur les deux smartphones autorisés.
- Afficher le parcours complet dans `/attente/{TeamId}` et l'étape Jeu courante dans `/jeu/{TeamId}`.
- Gérer les QR codes inconnus, les mauvaises étapes, les doublons et les pertes de connexion.

### Tâche 6 — Implémenter les étapes QCM

- Ouvrir un QCM à partir du QR code de l'étape attendue.
- Afficher la question et entre deux et quatre réponses A, B, C et D.
- Permettre pour chaque réponse le choix d'un contenu texte, son ou photo. **Cette partie est terminée dans `/admin/steps` et `/jeu/{TeamId}`.**
- Ajouter une confirmation avant validation définitive.
- Corriger immédiatement la réponse et enregistrer le résultat, les points et l'heure.
- Garantir une seule réponse par équipe et par étape.

### Tâche 7 — Implémenter les étapes jeu — PARTIELLEMENT TERMINÉE

- Créer une interface distincte des QCM pour les jeux Loanig Park.
- Afficher le nom du jeu, sa destination et ses consignes.
- Permettre de terminer et confirmer l'étape Jeu. La saisie du score obtenu par l'équipe reste à ajouter.
- Enregistrer le score brut, l'équipe, le jeu, l'appareil et l'heure.
- Empêcher la modification non autorisée et historiser toute correction admin.
- Afficher immédiatement le score enregistré et la destination suivante.

### Tâche 8 — Implémenter caractères et classement des jeux

- Shuffle the admin-defined final word once at game launch and distribute its letters by each team's route progress.
- Collect the same shuffled letters for every team, then allow a final-word submission after the last route step.
- Show the shared hint and map image between each pair of steps.
- Comparer les scores bruts d'un jeu uniquement lorsque toutes les équipes l'ont terminé.
- Attribuer ensuite les points de classement dans l'ordre 10, 9, 8, etc.
- Définir et tester le comportement des égalités avant de figer le classement.

### Tâche 9 — Construire la supervision et la projection

- Créer `/projection` sans navigation administrative.
- Afficher une ligne ou une carte lisible pour chaque équipe.
- Afficher progression, étape actuelle, prochaine destination, état, QCM réalisés, résultats QCM, scores bruts des jeux et points attribués.
- Mettre à jour la projection en temps réel avec `GameEventService`/SignalR.
- Ajouter une vue admin de supervision, de déblocage et de correction contrôlée.
- Tester la lisibilité sur vidéoprojecteur et avec un nombre variable d'équipes.

### Tâche 10 — Finaliser les médias et l'administration

- Ajouter l'import et le stockage administratifs des fichiers audio et photo ; les réponses typées texte, son et photo sont déjà prises en charge via leur contenu.
- Conserver le redimensionnement et l'optimisation des images déjà réalisés.
- Ajouter la gestion administrative complète des étapes, parcours, scores et caractères.
- Ajouter les exports CSV des réponses, scores, progression et événements.

### Tâche 11 — Robustesse et validation finale

- Tester les reconnexions Blazor/SignalR et les soumissions répétées.
- Tester les deux appareils d'une équipe sur plusieurs étapes.
- Tester le mode mémoire et le mode MongoDB.
- Tester les données après redémarrage de l'application.
- Vérifier les droits d'accès admin et la sécurité des jetons QR/appareils.
- Mettre à jour la section « État d'avancement » et la documentation lorsque toutes les vérifications sont terminées.
