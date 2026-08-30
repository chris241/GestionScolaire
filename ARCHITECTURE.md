# Architecture multi-établissements

Ce document décrit le modèle multi-tenant introduit dans GestionScolaire (un `Directeur` peut posséder
plusieurs `School`) et les règles à respecter pour ne pas régresser sur l'isolation des données entre
écoles. Il complète le `README.md` (installation, démarrage) sans le remplacer.

## Modèle

- Une `School` appartient à **un seul `Directeur` propriétaire** (`School.DirectorId`) — pas de
  co-direction.
- Un `Teacher` peut être rattaché à **plusieurs écoles** du même directeur, via la table de jonction
  `TeacherSchool` (many-to-many).
- `Student` et `Parent` restent implicitement mono-école : un élève appartient à une seule classe, donc à
  une seule école ; un parent n'a pas de claim école du tout (voir plus bas).

## Contexte école dans le JWT

Le token d'accès porte une claim `schoolId` optionnelle (`ICurrentUserService.SchoolId`), qui définit
« l'école active » de la requête :

- **Directeur / Enseignant** : `schoolId` est toujours renseigné (résolu à la connexion via
  `User.LastActiveSchoolId`, ou la première école accessible). `POST /api/auth/switch-school` réémet un
  token avec une nouvelle claim, sans re-authentification.
- **Élève** (portail) : résolu une fois à la connexion via `Student.ClassId → SchoolClass.SchoolId` (pas
  de bascule possible — un élève n'a qu'une école).
- **Parent** : **aucune claim école**. Son accès reste scopé élève par élève via `StudentParent`
  (`IStudentAccessPolicy`), jamais via `SchoolId`. C'est une source récurrente de bugs si on l'oublie (voir
  plus bas).

## Isolation des données : filtres globaux EF Core

L'isolation entre écoles est une **frontière de sécurité** (fuite de données), pas une autorisation de
rôle — contrairement au reste de l'application qui vérifie manuellement par contrôleur
(`IStudentAccessPolicy`, `CanAccessClassAsync`, etc.), un `.Where()` oublié dans un seul contrôleur serait
une fuite de données entre deux écoles. Le mécanisme choisi est donc un filtre global appliqué une seule
fois, centralement, dans `AppDbContext.OnModelCreating` :

```csharp
modelBuilder.Entity<SchoolClass>().HasQueryFilter(c => c.SchoolId == _currentUser.SchoolId);
```

`AppDbContext` reçoit `ICurrentUserService` par injection de constructeur (`Scoped`, pas de captive
dependency). **Si `SchoolId` est `null` (Parent, ou aucune école active), le filtre ne renvoie aucune
ligne** — comportement sûr par défaut plutôt que « tout afficher ».

Ce filtre s'applique automatiquement à **toute requête** contre l'entité concernée, y compris quand elle
est atteinte via une navigation (`.Include(x => x.Student)` applique le filtre de `Student`) — c'est ce qui
permet à des entités non scopées elles-mêmes d'hériter d'une protection transitive (voir « enfants purs »
ci-dessous), mais c'est aussi la source du bug le plus fréquent de cette migration : un `.Where()` sur une
entité JOINT à une entité filtrée peut silencieusement perdre des lignes pour tout appelant dont le
`SchoolId` ne correspond pas — d'où la nécessité fréquente d'un `.IgnoreQueryFilters()` ciblé (voir plus
bas).

## Trois stratégies de scoping, par entité

Chaque entité scopée suit l'une de ces trois stratégies, choisie au cas par cas :

### 1. Colonne `SchoolId` dénormalisée (racine sans ancrage fiable)

Utilisée quand l'entité n'a **aucune** clé étrangère existante menant en un seul saut à une entité déjà
scopée par sa propre colonne. C'est l'approche standard multi-tenant (`tenant_id` dénormalisé) — plus
robuste et plus rapide qu'une chaîne de navigation profonde.

`SchoolClass`, `Teacher` (via `TeacherSchool`), `AcademicYear`, `AcademicTerm`, `AcademicProgram`, `Room`,
`StudentCategory`, `StudentBatch`, `StudentGroup`, `StudentApplicant`, `AdmissionCampaign`, `Subject`,
`Course`, `CourseSchedule`, `ProgramEnrollment`, `CourseEnrollment`, `StudentLeaveApplication`,
`GradingScale`, `AssessmentGroup`, `AssessmentPlan`, `FeeCategory`, `Invoice`, `Payment`, `Guardian`,
`StudentLog`, `TeacherLog`.

### 2. Filtre de navigation à un seul niveau (ancrage existant réutilisé)

Utilisée quand l'entité a déjà une clé étrangère **non nullable** vers une entité qui, elle, porte sa
propre colonne `SchoolId` (ou un filtre équivalent). Pas de nouvelle colonne — un seul `JOIN` implicite.

| Entité | Filtre | Ancrage |
|---|---|---|
| `Student` | `s => s.Class.SchoolId == _currentUser.SchoolId` | `ClassId` (colonne existante) |
| `Attendance` | `a => a.Class.SchoolId == ...` | `ClassId` (colonne existante) |
| `Grade` | `g => g.Class.SchoolId == ...` | `ClassId` (colonne existante) |
| `FeeStructure` | `s => s.AcademicYear.SchoolId == ...` | `AcademicYearId` (colonne existante) |
| `FeeSchedule` | `s => s.AcademicTerm.SchoolId == ...` | `AcademicTermId` (colonne existante) |
| `StudentGuardian` | `sg => sg.Guardian.SchoolId == ...` | `GuardianId` (colonne existante) |

**Règle appliquée systématiquement** : ne jamais chaîner sur 2 niveaux ou plus (ex. `Invoice` aurait pu
naviguer `Student.Class.SchoolId`, mais `StudentId` n'est qu'à 2 sauts d'une colonne réelle — `Invoice` a
donc reçu sa propre colonne plutôt qu'une chaîne profonde). Un chaînage à 2+ niveaux est plus fragile et
plus lent ; dès qu'un ancrage à un seul saut n'existe pas, l'entité reçoit sa propre colonne.

### 3. Enfant pur (aucune colonne, aucun filtre)

Utilisée pour les entités qui n'ont de sens que rattachées à un parent déjà scopé, et qui ne sont jamais
listées de façon autonome (toujours navigées depuis leur parent). Elles héritent de la protection
**transitivement**, via le filtre déjà actif du parent quand celui-ci est inclus/joint.

`Topic` (via `Course`), `AssessmentCriteria` (via `AssessmentPlan`), `GradingScaleInterval` (via
`GradingScale`), `FeeStructureItem` (via `FeeStructure`), `AdmissionCampaignQuota` (via
`AdmissionCampaign`), `StudentGroupMember` (via `StudentGroup`), `StudentSibling` (via `Student`, sur ses
**deux** navigations `Student`/`SiblingStudent` — cas particulier : deux FK vers la même entité scopée,
pas un seul parent).

**Piège** : un enfant pur n'a pas de filtre propre. Toute route qui le modifie ou le lit **par son propre
id** (`DELETE /api/x/items/{itemId}`) doit vérifier explicitement que le parent est accessible dans l'école
active — via `FindAsync` sur le parent (qui, lui, est filtré) — avant d'agir. Voir la classe de bug
ci-dessous.

## Deux classes de bug récurrentes (à vérifier sur tout nouveau code)

### A. `IgnoreQueryFilters()` devenu trop large

Un `.IgnoreQueryFilters()` ajouté pour une raison légitime — le plus souvent pour laisser un `Parent` (sans
claim école) accéder à ses propres enfants malgré le filtre — peut, une fois toutes les entités scopées,
masquer le filtre pour **tous les rôles**, y compris un Directeur d'une autre école. Le correctif standard,
appliqué partout dans ce projet :

```csharp
var query = _context.X.Where(x => x.StudentId == studentId);
if (_currentUser.Role == nameof(UserRole.Parent))
    query = query.IgnoreQueryFilters();
```

Avant d'ajouter un `IgnoreQueryFilters()`, ou avant de scoper une nouvelle entité, grepper les appels
existants sur son `DbSet` et re-justifier chacun. Ne jamais bypasser pour un rôle qui porte une vraie claim
école (Director/Teacher/Student ont toujours un `SchoolId` correct — seul Parent ne l'a jamais).

### B. Contrôle d'accès qui ne vérifie pas l'école

`IStudentAccessPolicy.CanAccessStudentAsync` renvoie `true` sans condition pour un `Director` (c'est
voulu : un directeur peut accéder à n'importe quel élève **de son école**, cette policy ne fait qu'un
contrôle de rôle, pas de tenant). Un endpoint qui combine ce laisser-passer avec un
`.IgnoreQueryFilters()` inconditionnel (classe A) laisse alors un Directeur lire les données de
**n'importe quelle école** en devinant un id. Cette combinaison a été trouvée et corrigée dans une dizaine
d'endpoints à travers les phases 5 à 8 (`GradesController`, `AttendanceController`, `BulletinsController`,
`FinalGradesController`, `GuardiansController`, `StudentsController.GetSiblings`, etc.) — c'est la classe
de vulnérabilité la plus sérieuse rencontrée dans cette migration, car directement exploitable par un
compte authentifié légitime, pas seulement un bug de filtrage incidentel.

Une variante existe pour les **enfants purs** (stratégie 3) : une route `{childId}` sans vérification du
parent laisse n'importe quel appelant lire/modifier/supprimer un enfant d'une autre école, même sans
`IgnoreQueryFilters()` explicite, simplement parce que l'enfant n'a pas de filtre du tout. Le correctif :

```csharp
var item = await _context.ChildItems.FindAsync(itemId);
if (item is null) return NotFound();
if (await _context.Parents.FindAsync(item.ParentId) is null) return NotFound(); // parent filtré → 404 si autre école
```

**Checklist pour toute nouvelle route** : grepper `HasAccessAsync`/`CanAccessStudentAsync`/
`CanAccessClassAsync`-style et vérifier la requête juste après ; pour toute route `{id}` sur un enfant pur,
vérifier que le parent est validé via son propre `FindAsync` avant lecture/écriture.

## Tests d'isolation

`tests/GestionScolaire.Api.Tests/SchoolScopingIsolationTests.cs` est la suite dédiée à cette garantie de
sécurité (distincte des tests fonctionnels par rôle). Le patron de base : inscrire un nouveau Directeur,
lui créer une école fraîche (`RegisterDirectorWithFreshSchoolAsync`), puis vérifier qu'il ne voit **jamais**
les données déjà seedées pour Lumière/Génie — y compris en devinant un id d'une autre école
(`NewSchool_DirectorCannotSee...ByGuessingItsXxxId`). Tout nouvel endpoint qui expose ou modifie une entité
scopée devrait avoir un test de ce type, pas seulement un test fonctionnel « ça retourne les bonnes
données ».

## Historique des phases

La migration s'est faite en 8 phases (une par session, un PR par phase, squash-mergées sur `main`) :
0 (fondation School/TeacherSchool/JWT), 1 (structure académique + élèves), 2 (admissions), 3 (programmes &
cours), 4 (présence & congés), 5 (évaluations — découverte de la classe de bug B ci-dessus), 6 (finances),
7 (tuteurs & journaux), 8 (audit final + ce document). Le détail de chaque phase est dans les messages de
commit et les PR correspondantes sur `chris241/GestionScolaire`.
