# KerRandoQcm

Application de rallye pédestre avec questionnaire par étapes, développée en Blazor Server avec MongoDB.

## Fonctionnalités

- Accès joueur par scan de QR Code et choix direct d'une équipe
- Équipes nommées et colorées préparées par l'organisateur
- Session d'équipe conservée dans le navigateur et un cookie de secours
- Interface mobile claire et lisible pour les participants
- Panneau d'administration sécurisé par mot de passe pour gérer les inscriptions, les groupes et le tirage des équipes

---

## Démarrage avec Docker Compose

Lancer l'application et la base de données MongoDB :

```bash
docker compose up -d --build
```

### Accès :
- **Accueil** : http://localhost:5000
- **Inscription participant** : http://localhost:5000/inscription
- **Administration** : http://localhost:5000/admin (mot de passe configuré : `admin123`)
- **MongoDB** : port `27017`

---

## Stack technique

- **Application** : .NET 8 (Blazor Server)
- **Base de données** : MongoDB 7.0
- **QR Code** : QRCoder
- **Conteneurisation** : Docker & Docker Compose
