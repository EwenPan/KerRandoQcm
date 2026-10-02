# Cahier des charges — KerRando QCM

> **Current implementation note**:
> 1. The application uses **Blazor Server (.NET 8 Interactive Server)**, optional MongoDB storage, and Docker Compose. Players use a mobile browser; no Android installation is required.
> 2. Players join manually created teams without entering personal names. Each team receives an independently ordered route, and generated routes share no more than two consecutive steps.
> 3. Administrators can create QCM and game steps, upload optimized photos, manage devices, start/pause/finish the rally, and inspect live progress.
> 4. Raw game scores, QCM results, placement points, and character reveals are stored per team. `/projection`, `/admin/supervision`, and the authenticated CSV export provide live and downloadable results.
> 5. Equal game scores currently share placement points. A secondary tie-break rule for equal overall scores remains to be confirmed; recorded start/end times are not used as a tie-breaker.

## Règles métier mises à jour

Le rallye comporte deux catégories d'épreuves :

- **QCM** : le groupe scanne un QR code pour obtenir une question et ses réponses. Il y a en général quatre solutions A, B, C et D, une seule étant exacte. Une réponse peut être affichée sous forme de texte, d'image, de son ou de vidéo. La correction est effectuée immédiatement après la validation.
- **Jeux bretons loués à Loanig Park** : le meilleur score obtenu par le groupe est saisi sur le smartphone de l'équipe après l'essai. Le temps d'essai, d'environ dix minutes par jeu, est inclus dans le fonctionnement du jeu mais ne remplace pas le classement par score.

Le nombre d'équipes est choisi avant le début du rallye : **6, 7 ou 8 équipes**, selon le nombre de participants présents le midi. Une équipe peut enregistrer **un ou deux smartphones autorisés**. Le deuxième appareil est un secours ; il ne doit pas permettre de répondre deux fois à la même question ni de doubler un score.

Le groupe est l'unité de jeu. Les réponses, scores, indices, étapes et événements sont rattachés à l'équipe. Après une réponse QCM corrigée ou un score de jeu enregistré, l'application peut révéler **un ou deux caractères**. Ces caractères doivent être associés à une position commune à toutes les équipes : leur ordre final ne dépend donc pas de l'ordre du parcours suivi par l'équipe. L'ensemble des caractères est rappelé après la dernière saisie du jeu concerné.

Après chaque étape, l'application indique clairement la destination suivante :

- un jeu précis dans la salle ;
- ou un lieu du bourg de Croixanvec où l'équipe doit scanner le QR code suivant.

Les jeux Loanig Park donnent des points selon le classement des scores, uniquement lorsque toutes les équipes ont terminé le jeu : **10 points pour la première équipe, puis 9, 8, etc.** Avant la clôture du jeu, l'application affiche les scores bruts et les équipes qui n'ont pas encore terminé, mais n'attribue pas définitivement les points de place.

Le classement général reste visible en temps réel. Il combine les points QCM déjà validés, les scores bruts des jeux et la progression. Les points de classement d'un jeu sont ajoutés au moment de sa clôture. La projection doit notamment afficher, pour chaque équipe, le nombre de QCM réalisés et leurs résultats ainsi que le score de chacun des jeux Loanig Park.

Le départage des égalités doit être défini avant la mise en production. L'heure de départ et l'heure de fin du rallye peuvent être enregistrées comme critère secondaire, mais le temps d'un jeu Loanig Park ne doit pas remplacer le classement principal par score.

### Modèle canonique des étapes

Le mot **étape** désigne toute épreuve du parcours. Une étape possède un type :

- `QCM` : le groupe se rend au lieu indiqué, scanne le QR code, consulte la question et valide une réponse ;
- `JEU` : le groupe se rend au jeu indiqué et utilise une interface pour enregistrer son score.

Chaque équipe reçoit un parcours ordonné contenant une liste d'étapes QCM et jeu. L'équipe avance uniquement dans l'ordre de cette liste. Après la validation d'une étape, l'application affiche les informations nécessaires pour rejoindre la suivante : nom, lieu, type d'étape, consigne, QR code éventuel et toute information pratique utile.

Une étape jeu ne demande pas de répondre à un QCM : elle fournit uniquement l'interface de saisie et de confirmation du score obtenu. Le score brut est conservé par équipe et par jeu. Les points de classement correspondants restent en attente jusqu'à la fin du jeu, puis sont attribués à toutes les équipes selon le classement des scores.

---

Quelles instructions donner à un développeur pour réaliser l’analyse de l’appli Android suivante :

elle est télé chargeable en ligne de manière privée

chaque question est accessible via un qrcode

les qrcodes sont placés à des endroits différents dans le village

on doit pouvoir flasher un qrcode pour obtenir un questionnaire à choix multiple

parmi les réponses certaines peuvent être sous forme de vidéo, de son ou d’image

cette appli doit permettre de saisir la réponse par une lettre A, B, C, D

Les résultats : bonnes ou mauvaises réponses doivent être automatiquement interprétées et enregistrées en direct

le classement doit être mis à jour en temps réel,

le classement est projeté en temps réel avec les détails

les équipes font des parcours différents dans le village

l’appli permet créer les parcours des différentes équipes : elles peuvent se croiser, mais jamais se suivre plus de 2 cheminements dans le village

il y aura entre 6 et 8 équipes

il y aura 5 endroits avec des qrcodes dans le village et 3 sites de jeux dans la salle

Remarque, mais non intégrée à la demande de solution : pour départager les ex-aequo, il serait peut-être judicieux de placer une heure de départ et une heure d’arrivée pour chaque équipe

Chatgpt

Oui. Pour ce projet, je donnerais au développeur un **cahier des charges fonctionnel et technique très précis**, car le point délicat n’est pas seulement le scan des QR codes : c’est surtout la **gestion en temps réel des équipes, des parcours, des réponses et du classement projeté**.

Voici une version que vous pouvez lui transmettre directement.
# **Cahier des charges — Application Android de jeu par QR codes**
## **1. Objectif général**
Développer une application privée accessible depuis les navigateurs mobiles permettant à 6, 7 ou 8 équipes de participer simultanément à un jeu de piste dans le bourg de Croixanvec et dans la salle de jeux Loanig Park.

Chaque équipe suit un parcours ordonné qui lui est attribué. Le parcours comporte des étapes de deux types :

- 5 lieux dans le village équipés de QR codes ;
- 3 sites de jeux situés dans une salle ;
- des étapes QCM avec correction immédiate ;
- des étapes jeu correspondant aux jeux bretons loués à Loanig Park, avec saisie du score ;
- des réponses pouvant intégrer du texte, des images, des sons ou des vidéos ;
- une saisie de réponse par lettre : **A, B, C ou D**.

Les réponses doivent être corrigées automatiquement et transmises immédiatement au serveur.

Le classement général doit être actualisé en temps réel et affiché sur un écran de projection. Les points de classement des jeux sont attribués uniquement lorsque toutes les équipes ont terminé le jeu concerné.

# **2. Principe de fonctionnement**
## **Côté équipe**
Chaque équipe dispose d'un smartphone principal et peut enregistrer un second smartphone de secours. Les deux appareils utilisent la même identité d'équipe ; le serveur empêche toute double validation.

Au lancement de l'application :

1. l'équipe est identifiée sur un ou deux smartphones autorisés ;
1. elle voit la prochaine étape de son parcours ;
1. elle reçoit les informations nécessaires pour rejoindre cette étape ;
1. si l'étape est un QCM, elle flashe le QR code, consulte la question, les médias et les réponses, puis valide A, B, C ou D ;
1. si l'étape est un jeu, elle ouvre l'interface du jeu, saisit le score obtenu et le confirme ;
1. le serveur valide une seule fois l'étape pour l'équipe ;
1. le résultat QCM ou le score brut du jeu est enregistré ;
1. les caractères éventuels sont révélés ;
1. la prochaine destination est affichée ;
1. le classement est actualisé, sans attribuer les points de place d'un jeu avant sa clôture.

# **3. Architecture recommandée**
Prévoir une architecture client/serveur.
### **Application Android**
Application destinée aux équipes :

- Kotlin ;
- Android Studio ;
- architecture moderne Android ;
- scanner QR code ;
- affichage des questionnaires ;
- lecture des médias ;
- saisie A/B/C/D ;
- communication avec l'API ;
- fonctionnement avec une connexion réseau intermittente.
### **Serveur**
Le serveur doit être la source de vérité.

Il doit gérer :

- les équipes et leurs appareils autorisés ;
- les joueurs et les appareils associés ;
- les parcours composés de QCM et de jeux ;
- les étapes ;
- les QR codes ;
- les questions ;
- les réponses ;
- les bonnes réponses ;
- les réponses QCM, les scores bruts des jeux et les points de classement attribués à la clôture ;
- les temps ;
- le classement ;
- les médias ;
- les sites de jeux Loanig Park, leur durée indicative et leur état de clôture ;
- les caractères révélés et leur position finale commune ;
- l'état de progression des équipes.
### **Interface d'administration**
Prévoir une interface web d'administration permettant notamment de :

- créer/modifier/supprimer une équipe ;
- créer les parcours ;
- affecter un parcours à une équipe ;
- créer les questions ;
- définir la bonne réponse ;
- associer une image, une vidéo ou un son ;
- créer les QR codes ;
- définir les points ;
- définir les pénalités éventuelles ;
- visualiser les équipes en direct ;
- visualiser les réponses ;
- corriger exceptionnellement une réponse si nécessaire ;
- afficher le classement ;
- démarrer/arrêter le jeu.

# **4. QR codes**
Il y aura 5 emplacements QR codes dans le village.

Chaque QR code doit posséder un identifiant unique.

Exemple :

QR-VILLAGE-01\
QR-VILLAGE-02\
QR-VILLAGE-03\
QR-VILLAGE-04\
QR-VILLAGE-05

Le QR code ne doit pas nécessairement contenir directement la question.

Il doit plutôt contenir un identifiant sécurisé permettant à l'application de demander au serveur :

« Quelle est la question correspondant à ce QR code ? »

Cela permettra de modifier les questions sans devoir réimprimer les QR codes.
### **Important**
Le serveur doit vérifier :

- que le QR code existe ;
- qu'il est actif ;
- que l'équipe a le droit de répondre à cette étape ;
- que cette étape appartient bien au parcours de l'équipe ;
- que la question n'a pas déjà été validée ;
- que la réponse n'est pas envoyée plusieurs fois.

# **5. QCM**
Chaque question comporte :

- un identifiant ;
- un titre/énoncé ;
- éventuellement une image ;
- éventuellement une vidéo ;
- éventuellement un fichier audio ;
- 4 réponses :
  - A
  - B
  - C
  - D ;
- une bonne réponse ;
- un nombre de points ;
- éventuellement une pénalité ;
- éventuellement une durée maximale.

Exemple :

**Question : Quel bâtiment se trouve devant vous ?**

A — L'église\
B — La mairie\
C — L'école\
D — Le château

L'équipe appuie simplement sur :

**A / B / C / D**

# **6. Médias**
Une question peut contenir :
### **Image**
Afficher l'image avant ou pendant la question.
### **Son**
Permettre la lecture audio depuis l'application.
### **Vidéo**
Permettre la lecture vidéo directement dans l'application.

Les médias doivent être stockés sur le serveur ou un stockage cloud et téléchargés/consultés par l'application.

Prévoir une optimisation afin que les vidéos ne provoquent pas de temps d'attente excessif.

# **7. Validation des réponses QCM et des scores de jeux**
Lorsqu'une équipe sélectionne A, B, C ou D :

1. l'application envoie la réponse au serveur ;
1. le serveur enregistre :
   1. équipe ;
   1. joueur/appareil ;
   1. question ;
   1. réponse ;
   1. heure précise ;
   1. lieu/QR code ;
   1. résultat ;
   1. points obtenus ;
1. le serveur compare la réponse avec la bonne réponse ;
1. le serveur enregistre le résultat et les points QCM ;
1. le serveur révèle le ou les caractères associés à cette étape, s'il y en a ;
1. le score de l'équipe est recalculé ;
1. le classement provisoire est recalculé ;
1. les écrans connectés sont mis à jour immédiatement.

L'application doit empêcher le double envoi accidentel d'une même réponse.

Pour un jeu Loanig Park :

1. l'animateur ou l'organisateur saisit le meilleur score du groupe ;
1. le serveur enregistre le score brut, l'heure de saisie, l'équipe et le jeu ;
1. le serveur révèle le ou les caractères associés au jeu ;
1. le score brut est immédiatement visible dans la projection ;
1. les points de classement sont attribués uniquement lorsque toutes les équipes ont terminé ce jeu ;
1. les scores sont triés du meilleur au moins bon, puis convertis en 10, 9, 8 points, etc.

# **8. Temps réel**
Le classement doit être réellement temps réel.

Éviter une simple actualisation toutes les 30 secondes.

Prévoir une communication temps réel entre serveur et écran de classement, par exemple :

- WebSocket ;
- ou technologie équivalente.

Lorsqu'une équipe répond :

**Équipe 4 → bonne réponse → +10 points**

le classement projeté doit pouvoir être mis à jour immédiatement.

# **9. Classement**
L'écran de projection doit afficher au minimum :

|**Rang**|**Équipe**|**Points**|**Questions**|**Bonnes réponses**|
| :- | :- | -: | -: | -: |
|1|Équipe A|80|10|8|
|2|Équipe C|70|10|7|
|3|Équipe F|60|10|6|

Prévoir éventuellement une vue plus détaillée :

- position ;
- nom de l'équipe ;
- score ;
- nombre de réponses ;
- bonnes réponses ;
- mauvaises réponses ;
- QCM réalisés et résultat de chacun ;
- score brut de chacun des jeux Loanig Park ;
- points de classement déjà attribués et jeux encore en attente de clôture ;
- caractères déjà révélés et progression de la phrase finale ;
- dernière étape réalisée ;
- temps écoulé ;
- progression du parcours.

L'écran doit être lisible à distance sur un vidéoprojecteur.

Prévoir un mode « projection » sans menus d'administration.

# **10. Gestion des parcours**
C'est un point essentiel du projet.

Il existe :

- 6, 7 ou 8 équipes ;
- les QR codes QCM du village ;
- 3 sites de jeux dans la salle.

Les équipes ne doivent pas nécessairement suivre le même ordre.

Exemple :
### **Équipe A**
QR1 → QR3 → Jeu 1 → QR5 → Jeu 2 → QR2 → Jeu 3 → QR4
### **Équipe B**
QR3 → QR5 → Jeu 2 → QR1 → Jeu 3 → QR4 → Jeu 1 → QR2
### **Équipe C**
QR5 → QR2 → Jeu 3 → QR4 → Jeu 1 → QR1 → Jeu 2 → QR3

Les parcours peuvent se croiser.

Cependant, **deux équipes ne doivent jamais avoir exactement plus de deux cheminements consécutifs identiques.**

Le logiciel de création des parcours doit donc pouvoir contrôler cette contrainte.

# **11. Générateur/contrôleur de parcours**
L'administrateur doit pouvoir créer les parcours manuellement.

Idéalement, prévoir également un bouton :

**« Générer automatiquement les parcours »**

Le système prend en compte :

- nombre d'équipes ;
- nombre de QR codes ;
- nombre de jeux ;
- ordre des étapes ;
- contraintes de circulation ;
- impossibilité de suivre trop longtemps une autre équipe.

Le système doit signaler les conflits avant validation.

Exemple :

⚠️ Conflit détecté : Équipe A et Équipe B suivent le même cheminement pendant 3 étapes consécutives.

L'administrateur doit pouvoir modifier les parcours avant de lancer le jeu.

# **12. Sites de jeux**
Les sites de jeux Loanig Park sont des étapes de type `JEU`. Leur nombre et leur nom doivent être configurables.

Ils doivent être gérés comme des étapes du parcours, au même titre qu'une étape QCM. Une étape jeu ne présente pas de question : elle affiche le jeu concerné, les consignes éventuelles et un champ de saisie du score.

Exemple :

- JEU-01
- JEU-02
- JEU-03

L'application doit donc pouvoir indiquer :

« Rendez-vous au Jeu 2 »

Le jeu peut être déclenché par :

- un QR code ;
- un bouton/validation de l'animateur ;
- ou un mécanisme défini ultérieurement.

Prévoir cette possibilité dans l'architecture.

# **13. Sécurisation du parcours**
Une équipe ne doit pas pouvoir scanner n'importe quel QR code pour avancer.

Lorsqu'une équipe scanne un QR code, le serveur doit vérifier :

« Est-ce bien la prochaine étape autorisée pour cette équipe ? »

Si oui :

Question affichée.

Sinon :

« Ce lieu ne correspond pas à votre parcours actuel. »

Cela évite qu'une équipe puisse volontairement scanner le QR code d'une autre étape.

Prévoir toutefois un mode administrateur permettant de débloquer manuellement une équipe en cas de problème.

# **14. Connexion réseau**
Le système doit être conçu pour supporter une connexion réseau imparfaite.

Important car les équipes se déplacent dans le village.

Prévoir :

- mise en cache des informations nécessaires ;
- téléchargement préalable des médias lorsque possible ;
- file d'attente locale des événements en cas de coupure ;
- synchronisation automatique lorsque la connexion revient ;
- identifiant unique pour chaque réponse afin d'éviter les doublons.

Cependant, la validation définitive des scores doit être faite par le serveur dès que possible.

# **15. Base de données**
Prévoir au minimum les entités suivantes :
### **Team**
- id
- nom
- couleur éventuelle
- code d'accès
- parcours\_id
- un ou deux appareils autorisés
- score
- statut
### **Route**
- id
- nom
- liste ordonnée des étapes
### **RouteStep**
- id
- route\_id
- ordre
- type (`QCM` ou `JEU`)
- destination\_id
- consigne de déplacement
- QR code éventuel
- caractère éventuel et position commune
### **QRCode**
- id
- code
- lieu
- actif
- question\_id
### **Question**
- id
- texte
- media
- réponse\_A
- réponse\_B
- réponse\_C
- réponse\_D
- bonne\_réponse
- points
### **Answer**
- id
- team\_id
- question\_id
- réponse
- correct
- points
- timestamp
- QR\_code\_id
### **GameSite**
- id
- nom
- description
- consignes
- format du score attendu

### **GameScore**
- id
- team\_id
- step\_id
- score brut
- points de classement, nuls avant la clôture du jeu
- statut de clôture
- date et appareil de saisie
### **Event**
Conserver les événements importants pour permettre un historique complet :

- scan QR ;
- affichage question ;
- réponse ;
- validation ;
- changement de parcours ;
- modification du score ;
- intervention administrateur.

# **16. Tableau de bord administrateur**
Prévoir plusieurs écrans.
### **Écran « Équipes »**
Afficher :

- équipe ;
- parcours ;
- étape actuelle ;
- score ;
- dernière activité ;
- état de connexion.
### **Écran « Live »**
Vue temps réel :

Équipe A — QR3 — réponse correcte — +10\
Équipe D — Jeu2 — terminé\
Équipe B — QR5 — mauvaise réponse
### **Écran « Parcours »**
Visualiser graphiquement les parcours.
### **Écran « Questions »**
Créer/modifier les questions.
### **Écran « Classement »**
Visualiser le classement en temps réel.
### **Écran « Projection »**
Interface spécialement conçue pour le vidéoprojecteur.

# **17. Mode projection**
Créer une page web dédiée accessible depuis un ordinateur connecté au vidéoprojecteur.

Exemple :

/projection

Cette page doit fonctionner en plein écran.

Elle reçoit les modifications du serveur en temps réel.

Lorsqu'une équipe gagne des points, l'interface peut afficher une animation courte :

**Équipe B +10**

puis recalculer le classement.

Prévoir également une vue détaillée permettant d'afficher :

- classement ;
- score ;
- progression ;
- dernières réponses ;
- éventuellement position/étape actuelle.

# **18. Identification des équipes**
Prévoir un système simple et robuste.

Par exemple :

**Équipe 1 — code 4821**

Au démarrage, le responsable de l'équipe sélectionne son équipe et saisit son code.

Une fois authentifiée, l'application est associée à cette équipe.

Il ne faut pas permettre à un utilisateur de changer simplement d'équipe pendant la partie.

Un administrateur doit pouvoir réinitialiser l'appareil si nécessaire.

# **19. Gestion des erreurs**
Prévoir des messages simples et explicites.

Exemples :

**QR code inconnu**

Ce QR code n'est pas reconnu.

**Mauvaise étape**

Ce lieu ne correspond pas à votre parcours actuel.

**Problème réseau**

Connexion interrompue. Votre réponse sera synchronisée automatiquement.

**Réponse enregistrée**

Réponse B enregistrée.

**Bonne réponse**

Bonne réponse ! +10 points

**Mauvaise réponse**

Mauvaise réponse. 0 point.

# **20. Journalisation**
Toutes les opérations importantes doivent être historisées.

L'objectif est de pouvoir comprendre après le jeu :

- qui a répondu ;
- à quelle heure ;
- à quelle question ;
- quelle réponse a été donnée ;
- si elle était correcte ;
- combien de points ont été attribués ;
- quel était le classement à ce moment-là.

Cela est important en cas de contestation.

# **21. Export des résultats**
Prévoir un export CSV ou Excel comprenant au minimum :

- équipe ;
- parcours ;
- question ;
- réponse ;
- bonne/mauvaise réponse ;
- points ;
- heure ;
- étape.

Prévoir également un classement final exportable.

# **22. Tests indispensables**
Avant l'événement, effectuer un test grandeur nature avec :

- 8 appareils Android ;
- 8 équipes virtuelles ;
- les QR codes QCM ;
- les 3 jeux ;
- la connexion réseau réelle du village ;
- le vidéoprojecteur.

Tester notamment :

1. deux équipes scannent simultanément ;
1. huit équipes répondent simultanément ;
1. deux équipes scannent le même QR code ;
1. une équipe scanne un QR code qui n'est pas prévu dans son parcours ;
1. perte de réseau pendant une réponse ;
1. reconnexion réseau ;
1. double appui sur A/B/C/D ;
1. redémarrage de l'application ;
1. redémarrage du serveur ;
1. affichage du classement sur le vidéoprojecteur ;
1. lecture simultanée de vidéos ;
1. parcours avec croisements entre équipes.

# **23. Critères de réussite**
Le projet sera considéré comme fonctionnel lorsque :

- 8 équipes peuvent jouer simultanément ;
- les QR codes sont reconnus rapidement ;
- les bonnes/mauvaises réponses sont automatiquement déterminées ;
- les scores sont enregistrés sans intervention humaine ;
- les scores sont actualisés en temps réel ;
- le classement est actualisé en temps réel ;
- le vidéoprojecteur affiche le classement sans actualisation manuelle ;
- chaque équipe respecte son parcours ;
- une équipe ne peut pas utiliser librement le parcours d'une autre ;
- les parcours peuvent se croiser ;
- les parcours respectent la règle des deux cheminements maximum ;
- les images, sons et vidéos fonctionnent ;
- une coupure réseau temporaire ne provoque pas de perte de réponse ;
- toutes les actions sont historisées.

# **24. Recommandation technique importante**
Ne pas concevoir l'application comme une simple application Android autonome.

Le système doit être pensé comme :

**Applications Android → API/serveur central → base de données → système temps réel → écran de projection**

avec :

**Interface administrateur → serveur central**

Le serveur doit être l'autorité concernant :

- les parcours ;
- les questions ;
- les réponses ;
- les scores ;
- le classement.

L'application Android ne doit jamais décider elle-même du score définitif.

# **25. Évolution possible**
Prévoir dès la conception la possibilité d'ajouter ultérieurement :

- davantage d'équipes ;
- davantage de QR codes ;
- davantage de jeux ;
- plusieurs villages ;
- plusieurs manches ;
- bonus ;
- malus ;
- chronomètre ;
- classement par manche ;
- classement général ;
- géolocalisation facultative ;
- photos prises par les équipes ;
- validation par animateur ;
- statistiques détaillées.

L'objectif est donc de construire une petite plateforme de jeu de piste, et non une application codée spécifiquement de manière rigide pour une seule édition.
### **Je conseillerais surtout d'ajouter un point au cahier des charges**
Il faut **définir précisément la règle des parcours**. La phrase « jamais se suivre plus de 2 cheminements » peut être interprétée de plusieurs façons par un développeur.

Par exemple, je proposerais cette règle :

**Deux équipes peuvent avoir la même étape, mais elles ne doivent jamais avoir une séquence de plus de deux étapes consécutives identiques dans le même ordre.**

Le nombre de QCM et de jeux doit rester configurable. La règle annoncée par l'organisateur selon laquelle le total des QCM et des jeux correspond au nombre d'équipes doit être confirmée avant de contraindre le générateur de parcours.

Si vous le souhaitez, je peux aussi vous préparer **le cahier des charges beaucoup plus technique pour le développeur**, avec **schéma de la base de données, architecture serveur/API, logique exacte de génération des 6 à 8 parcours et écrans de l'application**, de façon à pouvoir quasiment le donner directement à une agence ou à un développeur freelance.

## **26. Décisions à confirmer avant développement**

Les points suivants ne doivent pas être déduits automatiquement par le développeur :

- Que signifie exactement « le total QCM + nombre de jeux correspondra au nombre d'équipes » : chaque équipe réalise-t-elle toutes les étapes, ou chaque activité est-elle attribuée à une équipe différente ?
- Quel est le nombre prévu de QCM et de jeux pour chacune des configurations à 6, 7 ou 8 équipes ?
- Les deux smartphones sont-ils bien un appareil principal et un appareil de secours, sans saisies simultanées autorisées ?
- Qui saisit et qui valide le score d'un jeu Loanig Park : l'équipe, l'animateur ou l'administrateur ? Une correction du score doit-elle être historisée ?
- Le seuil d'environ dix minutes par jeu est-il une limite stricte ou seulement une durée indicative ?
- En cas d'égalité de score à un jeu, quel départage appliquer avant d'attribuer les points 10, 9, 8, etc. ?
- Les réponses QCM fausses valent-elles zéro point, et existe-t-il des pénalités ou des bonus ?
- Les caractères forment-ils un mot, une phrase ou une réponse finale dont la longueur est connue à l'avance ?
- Les lots annoncés sont-ils définitifs : confitures, pâté de lapin, rillettes de poulet, rouelle confite, noix et un lot encore à définir ?

### **1. Format encodé dans les QR codes**

Pour empêcher les participants de deviner les identifiants des stations ou de générer leurs propres QR codes, le contenu des QR codes doit comporter un jeton de sécurité ou un identifiant unique (UUID).

#### **Option recommandée : Deep Link avec identifiant unique**
Chaque QR code contient une URL au format HTTPS. Ce format permet à l'application de s'ouvrir automatiquement lors du scan, ou de rediriger le joueur s'il utilise l'appareil photo natif.

https://rallye.app/scan?s=st\_v01\_a8f9c2
#### **Contenu décodé par l'application**
Le paramètre s (ou le payload du QR code si encodé en JSON brut) correspond à la clé token de la station :

JSON

{

`  `"station\_id": "STATION\_VILLAGE\_01",

`  `"token": "a8f9c2e4-7b1d-4f3a-9e2c-1b5a8f9c2e4b"

}

- **Sécurité :** L'application vérifie que le token scanné existe bien en base avant de libérer la question.

- **Flexibilité :** Si une station doit être déplacée ou son QR code réimprimé, il suffit de régénérer un token en base sans toucher au reste des questions.

### **2. Modèle de base de données (Schéma relationnel / BaaS)**
Voici les 5 tables fondamentales à transmettre au développeur (structure au format relationnel PostgreSQL / Supabase, facilement adaptable en collections Firestore).
#### **Table stations (Les 8 sites du village et de la salle)**

|**Champ**|**Type**|**Description**|
| :- | :- | :- |
|id|UUID / String|Identifiant unique (ex : st\_v01)|
|nom|String|Nom du lieu (ex : Fontaine de la Mairie)|
|type|Enum|VILLAGE (5) ou SALLE (3)|
|qr\_token|String|Clé unique encodée dans le QR code physique|
#### **Table teams (Les 6, 7 ou 8 équipes)**

|**Champ**|**Type**|**Description**|
| :- | :- | :- |
|id|UUID / String|Identifiant unique de l'équipe|
|nom|String|Nom de l'équipe (ex : Les Renards)|
|code\_connexion|String|Code PIN pour connecter les téléphones de l'équipe|
|etape\_actuelle|Integer|Ordre de l'étape en cours (1 à 8)|
|score\_total|Integer|Somme des points accumulés (mis à jour en temps réel)|
#### **Table questions (Contenu du QCM)**

|**Champ**|**Type**|**Description**|
| :- | :- | :- |
|id|UUID / String|Identifiant unique de la question|
|station\_id|Foreign Key|Référence vers stations.id|
|intitule|Text|Texte de la question|
|media\_type|Enum|NONE, IMAGE, AUDIO, VIDEO|
|media\_url|String (Null)|URL du fichier hébergé sur le CDN/Storage|
|option\_a|Text|Libellé ou URL média pour la réponse A|
|option\_b|Text|Libellé ou URL média pour la réponse B|
|option\_c|Text|Libellé ou URL média pour la réponse C|
|option\_d|Text|Libellé ou URL média pour la réponse D|
|bonne\_reponse|Enum|A, B, C ou D|
|points|Integer|Nombre de points attribués (ex : 10)|
#### **Table routes (Séquence du parcours attribué à chaque équipe)**

|**Champ**|**Type**|**Description**|
| :- | :- | :- |
|id|UUID / String|Identifiant de l'étape du parcours|
|team\_id|Foreign Key|Référence vers teams.id|
|station\_id|Foreign Key|Référence vers stations.id|
|ordre\_passage|Integer|Position dans le parcours (1, 2, 3... jusqu'à 8)|
#### **Table submissions (Enregistrement des réponses)**

|**Champ**|**Type**|**Description**|
| :- | :- | :- |
|id|UUID / String|Identifiant unique de la soumission|
|team\_id|Foreign Key|Référence vers teams.id|
|question\_id|Foreign Key|Référence vers questions.id|
|station\_id|Foreign Key|Référence vers stations.id|
|reponse\_choisie|Enum|A, B, C ou D|
|est\_correcte|Boolean|Calculé automatiquement lors de la soumission|
|points\_gagnes|Integer|Points accordés (0 si faux, valeur de la question si vrai)|
|horodatage|Timestamp|Date et heure de validation (pour le départage en cas d'égalité)|




Voici le script SQL complet au format **PostgreSQL** (compatible Supabase), incluant les types énumérés, les clés primaires/étrangères, les contraintes d'intégrité, les valeurs par défaut et les index de performance pour la synchronisation temps réel.


SQL

-- =============================================================================

-- SCRIPT DE CRÉATION DE LA BASE DE DONNÉES - APPLICATION RALLYE

-- Dialecte : PostgreSQL 13+

-- =============================================================================

-- Activation de l'extension pour la génération de UUIDs si nécessaire

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

\-- -----------------------------------------------------------------------------

-- 1. ENUMS (Types énumérés)

\-- -----------------------------------------------------------------------------

CREATE TYPE station\_type\_enum AS ENUM ('VILLAGE', 'SALLE');

CREATE TYPE media\_type\_enum AS ENUM ('NONE', 'IMAGE', 'AUDIO', 'VIDEO');

CREATE TYPE choice\_enum AS ENUM ('A', 'B', 'C', 'D');

\-- -----------------------------------------------------------------------------

-- 2. TABLES

\-- -----------------------------------------------------------------------------

-- Table : STATIONS (Les 8 emplacements physiques)

CREATE TABLE stations (

`    `id UUID PRIMARY KEY DEFAULT gen\_random\_uuid(),

`    `nom VARCHAR(100) NOT NULL,

`    `type station\_type\_enum NOT NULL,

`    `qr\_token VARCHAR(255) NOT NULL UNIQUE,

`    `created\_at TIMESTAMPTZ DEFAULT CURRENT\_TIMESTAMP

);

-- Table : TEAMS (Les 6, 7 ou 8 équipes)

CREATE TABLE teams (

`    `id UUID PRIMARY KEY DEFAULT gen\_random\_uuid(),

`    `nom VARCHAR(100) NOT NULL UNIQUE,

`    `code\_connexion VARCHAR(10) NOT NULL,

`    `etape\_actuelle INT NOT NULL DEFAULT 1 CONSTRAINT chk\_etape CHECK (etape\_actuelle BETWEEN 1 AND 8),

`    `score\_total INT NOT NULL DEFAULT 0 CONSTRAINT chk\_score CHECK (score\_total >= 0),

`    `created\_at TIMESTAMPTZ DEFAULT CURRENT\_TIMESTAMP,

`    `updated\_at TIMESTAMPTZ DEFAULT CURRENT\_TIMESTAMP

);

-- Table : QUESTIONS (Le QCM associé aux stations)

CREATE TABLE questions (

`    `id UUID PRIMARY KEY DEFAULT gen\_random\_uuid(),

`    `station\_id UUID NOT NULL REFERENCES stations(id) ON DELETE CASCADE,

`    `intitule TEXT NOT NULL,

`    `media\_type media\_type\_enum NOT NULL DEFAULT 'NONE',

`    `media\_url TEXT NULL,

`    `option\_a TEXT NOT NULL,

`    `option\_b TEXT NOT NULL,

`    `option\_c TEXT NOT NULL,

`    `option\_d TEXT NOT NULL,

`    `bonne\_reponse choice\_enum NOT NULL,

`    `points INT NOT NULL DEFAULT 10 CONSTRAINT chk\_points CHECK (points > 0),

`    `created\_at TIMESTAMPTZ DEFAULT CURRENT\_TIMESTAMP,



`    `-- Contrainte : Une seule question active par station pour ce rallye

`    `CONSTRAINT unq\_station\_question UNIQUE (station\_id)

);

-- Table : ROUTES (L'ordre des stations attribué à chaque équipe)

CREATE TABLE routes (

`    `id UUID PRIMARY KEY DEFAULT gen\_random\_uuid(),

`    `team\_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,

`    `station\_id UUID NOT NULL REFERENCES stations(id) ON DELETE CASCADE,

`    `ordre\_passage INT NOT NULL CONSTRAINT chk\_ordre CHECK (ordre\_passage BETWEEN 1 AND 8),

`    `created\_at TIMESTAMPTZ DEFAULT CURRENT\_TIMESTAMP,

`    `-- Contraintes d'unicité : pas de doublon d'étape ou de station pour une même équipe

`    `CONSTRAINT unq\_team\_ordre UNIQUE (team\_id, ordre\_passage),

`    `CONSTRAINT unq\_team\_station UNIQUE (team\_id, station\_id)

);

-- Table : SUBMISSIONS (Historique et enregistrement des réponses envoyées)

CREATE TABLE submissions (

`    `id UUID PRIMARY KEY DEFAULT gen\_random\_uuid(),

`    `team\_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,

`    `question\_id UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,

`    `station\_id UUID NOT NULL REFERENCES stations(id) ON DELETE CASCADE,

`    `reponse\_choisie choice\_enum NOT NULL,

`    `est\_correcte BOOLEAN NOT NULL,

`    `points\_gagnes INT NOT NULL DEFAULT 0 CONSTRAINT chk\_points\_gagnes CHECK (points\_gagnes >= 0),

`    `horodatage TIMESTAMPTZ NOT NULL DEFAULT CURRENT\_TIMESTAMP,

`    `-- Empêche une équipe de soumettre plusieurs réponses à la même question

`    `CONSTRAINT unq\_team\_question\_submission UNIQUE (team\_id, question\_id)

);

\-- -----------------------------------------------------------------------------

-- 3. INDEX DE PERFORMANCE

\-- -----------------------------------------------------------------------------

-- Scan du QR Code : Recherche ultra-rapide par token lors du flash

CREATE INDEX idx\_stations\_qr\_token ON stations(qr\_token);

-- Progression des équipes : Chargement rapide de l'étape courante de l'équipe

CREATE INDEX idx\_routes\_team\_ordre ON routes(team\_id, ordre\_passage);

-- Dashboard Live / Projection : Tri optimisé pour le classement en temps réel

CREATE INDEX idx\_teams\_leaderboard ON teams(score\_total DESC, updated\_at ASC);

-- Historique : Recherche rapide des réponses d'une équipe

CREATE INDEX idx\_submissions\_team ON submissions(team\_id);

CREATE INDEX idx\_submissions\_horodatage ON submissions(horodatage DESC);
### **Points clés de cette modélisation :**
1. **Sécurité & Intégrité (CONSTRAINT) :**

   1. Une équipe ne peut **pas soumettre deux fois** une réponse à la même question (unq\_team\_question\_submission).

   1. Une équipe ne peut **pas repasser deux fois** par la même station au cours de son parcours (unq\_team\_station).

1. **Optimisation Scan QR (idx\_stations\_qr\_token) :**

   1. L'index B-Tree sur qr\_token garantit un temps de réponse en quelques millisecondes lorsque l'application valide le QR code scanné.

1. **Classement Temps Réel (idx\_teams\_leaderboard) :**

   1. En cas d'égalité de points entre deux équipes, l'index intègre updated\_at ASC pour départager automatiquement l'équipe qui a atteint ce score en premier.


