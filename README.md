# GestionScolaire — MVP

Logiciel de gestion scolaire (directeurs, professeurs, parents). Backend .NET 10 en Clean Architecture, frontend React (Vite + TypeScript + Tailwind v4).

## Structure

```
GestionScolaire/
├── src/
│   ├── GestionScolaire.Domain          # Entités, enums (aucune dépendance)
│   ├── GestionScolaire.Application     # DTOs, interfaces, logique métier (calcul des moyennes)
│   ├── GestionScolaire.Infrastructure  # EF Core (PostgreSQL), JWT, BCrypt, QuestPDF, Hangfire, DbSeeder
│   └── GestionScolaire.Api             # Controllers ASP.NET Core, Program.cs, Dockerfile
├── tests/
│   ├── GestionScolaire.Application.Tests  # xUnit — calcul des moyennes (15 tests)
│   └── GestionScolaire.Api.Tests          # xUnit — tests d'intégration HTTP (29 tests, Testcontainers PostgreSQL)
├── client/                             # React + Vite + TS + Tailwind + Lucide + react-router, Dockerfile (nginx)
├── docker-compose.yml                  # Postgres + Redis + api + client
└── GestionScolaire.slnx
```

## Prérequis

**Option Docker (recommandée)** : Docker Desktop uniquement — voir section dédiée plus bas.

**Option locale** :
- .NET 10 SDK
- Node.js 20+
- PostgreSQL (base `gestionscolaire`)
- Redis (optionnel en dev, requis pour le cache/Hangfire en prod)

## Démarrage — Backend

1. Ajuster `src/GestionScolaire.Api/appsettings.json` :
   - `ConnectionStrings:DefaultConnection` (PostgreSQL)
   - `Jwt:Secret` (⚠️ à remplacer en production, ≥32 caractères)
2. Lancer l'API (les migrations s'appliquent automatiquement au démarrage, ainsi que le seed de démonstration en environnement `Development`) :

```bash
dotnet run --project src/GestionScolaire.Api
```

Swagger disponible sur `/swagger`, dashboard Hangfire sur `/hangfire`.

### Comptes de démonstration (seed, environnement Development uniquement)

| Rôle      | Email                    | Mot de passe   |
|-----------|---------------------------|----------------|
| Directeur | directeur@ecole.mg        | Password123!   |
| Professeur| prof.math@ecole.mg        | Password123!   |
| Professeur| prof.francais@ecole.mg    | Password123!   |
| Parent    | parent1@ecole.mg … parent8@ecole.mg | Password123! |

Le seed crée 2 classes, 5 matières, 8 élèves, leurs notes du Trimestre 1, paiements et présences du jour.

## Démarrage — Frontend

```bash
cd client
npm install
npm run dev
```

Configurer `client/.env` → `VITE_API_URL` pointant vers l'API (ex: `https://localhost:7000/api`).

## Démarrage — Docker (recommandé pour une démo rapide)

Toute la stack (PostgreSQL, Redis, API, client) tourne en conteneurs, aucune installation locale de .NET/Node/PostgreSQL requise.

```bash
docker compose up -d --build
```

- Client : http://localhost:5240
- API : http://localhost:5230 (Swagger sur `/swagger`, healthcheck sur `/health`, dashboard Hangfire sur `/hangfire`)
- Les migrations EF Core et le seed de démonstration s'exécutent automatiquement au premier démarrage du conteneur `api`.

```bash
docker compose logs -f api      # suivre les logs de l'API
docker compose down             # arrêter (conserve les données PostgreSQL)
docker compose down -v          # arrêter et supprimer les données (reset complet + nouveau seed)
```

Ports 5230/5240 choisis pour éviter les conflits avec d'autres projets locaux ; à ajuster dans `docker-compose.yml` si besoin. Les identifiants de démo sont les mêmes que ci-dessous.

## Tests

```bash
dotnet test tests/GestionScolaire.Application.Tests   # unitaires — calcul des moyennes
dotnet test tests/GestionScolaire.Api.Tests            # intégration — nécessite Docker (Testcontainers)
```

Les tests d'intégration démarrent un vrai PostgreSQL éphémère et hébergent l'API en mémoire (`WebApplicationFactory`) : login, filtrage des données par rôle (Director/Teacher/Parent), contrôle d'accès sur les notes/paiements/bulletins (403 attendu hors périmètre), génération de bulletin PDF. Chaque exécution recrée sa propre base et son propre jeu de données via le `DbSeeder`.

## Fonctionnalités livrées (MVP)

- **Modèle de données** : User, Teacher, Student, SchoolClass, Subject, Grade, Attendance, Payment, StudentParent (relation parent-élève many-to-many).
- **Authentification JWT** : inscription, connexion, refresh token, hashing BCrypt (`AuthController`), rafraîchissement automatique côté client via intercepteur axios (401 → refresh → retry).
- **Notes & moyennes** : saisie sécurisée par rôle (Teacher/Director), calcul pondéré par coefficient, moyenne générale par élève (`GradesController`, `GradeAverageCalculator`, couvert par 15 tests unitaires).
- **Bulletin PDF** : génération via QuestPDF avec rang de classe et mention (`BulletinsController`, `BulletinPdfService`), téléchargeable depuis la page Notes.
- **Frontend applicatif complet** :
  - Page de connexion + routes protégées (`AuthContext`, `ProtectedRoute`)
  - Layout avec sidebar de navigation (`AppLayout`)
  - **Dashboard** : en-tête de bienvenue, 4 cartes de statistiques, tableau des derniers paiements avec badges de statut
  - **Élèves** : liste filtrable/recherchable
  - **Notes** : sélection d'élève, saisie de note, moyennes par matière + générale, téléchargement bulletin PDF
  - **Paiements** : liste avec total encaissé/dû et badges de statut
- **Design system** : palette Slate/Indigo demandée (`#F8FAFC` / `#1E293B` / `#3B82F6`), composants réutilisables typés (`StatCard`, `StatusBadge`, `RecentActivityTable`, `DashboardHeader`).
- **Données de démonstration** : `DbSeeder` peuplant automatiquement la base en développement.
- **Accès restreint par rôle** (`IStudentAccessPolicy`) :
  - **Parent** : ne voit que les élèves qui lui sont rattachés via `StudentParent` ; pas de tableau de bord ni de saisie de notes (lecture seule) ; page Paiements limitée à ses propres enfants.
  - **Teacher** : ne voit que les élèves de la classe dont il est titulaire (`SchoolClass.HomeroomTeacher`) ; ne peut consulter/saisir/modifier/supprimer une note que pour ses propres élèves (`403 Forbidden` sinon) ; pas de tableau de bord ni de page Paiements (hors de son périmètre).
  - **Director** : accès complet, seul rôle à voir le tableau de bord et la liste globale des paiements.
- **Devise** : montants affichés en Ariary malgache (MGA), sans décimales.

## Prochaines étapes suggérées

- Écrans CRUD complets pour les classes et la gestion des absences.
- Pagination sur les listes (élèves, paiements) au-delà du MVP.
- Gérer le cas d'un professeur non-titulaire (enseignant plusieurs classes sans en être responsable) — l'accès Teacher est aujourd'hui limité à sa classe de titulariat.
- Sécuriser le dashboard Hangfire (`/hangfire`) et sortir le secret JWT vers un vrai secret manager avant toute mise en production.
