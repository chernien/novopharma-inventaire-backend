<div align="center">

# 🔧 Novo Inventaire — API Backend

### API REST de gestion d'inventaire pharmaceutique (ASP.NET Core 8)

Back-end commun à l'application mobile de saisie et au dashboard d'administration. Gère l'authentification, le **double comptage**, la réconciliation des écarts, l'import/export Excel et l'intégration avec l'ERP **SAGE**.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8-512BD4)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)](https://jwt.io/)

</div>

---

## 📋 Table des matières

- [Présentation](#-présentation)
- [Architecture](#-architecture)
- [Fonctionnalités](#-fonctionnalités)
- [Technologies utilisées](#-technologies-utilisées)
- [Endpoints de l'API](#-endpoints-de-lapi)
- [Logique métier : double comptage](#-logique-métier--double-comptage)
- [Structure du projet](#-structure-du-projet)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Sécurité](#-sécurité)
- [Écosystème du projet](#-écosystème-du-projet)
- [Améliorations futures](#-améliorations-futures)
- [Auteur](#-auteur)

---

## 🎯 Présentation

**Novo Inventaire API** est le cœur applicatif d'une solution complète d'**inventaire physique** en environnement pharmaceutique. Elle fait le **pont entre les opérations terrain** (comptage des articles) et le **système ERP SAGE** (données maîtres articles & dépôts).

Le principe métier central est le **double comptage** : chaque article est compté indépendamment par deux superviseurs ; l'API réconcilie les deux saisies, détecte les écarts et calcule une quantité finale fiable.

| | |
|---|---|
| **Objectif** | Fiabiliser les inventaires via une validation à deux comptages et un audit des écarts |
| **Consommateurs** | Application mobile (saisie) + dashboard web (administration) |
| **Type** | API REST sécurisée par JWT |
| **Base de données** | SQL Server (base `NOVOPHARMA`, schéma SAGE intégré) |

> 🔒 **Note** : ce dépôt est une version *portfolio*. Les secrets réels (chaîne de connexion, clé JWT) ont été retirés du suivi Git et remplacés par un modèle — voir [Configuration](#-configuration).

---

## 🏗 Architecture

```
┌─────────────────┐     ┌─────────────────┐
│  App Mobile     │     │  Dashboard Web  │
│  (Ionic)        │     │  (Angular)      │
└────────┬────────┘     └────────┬────────┘
         │      HTTP / JWT       │
         └───────────┬───────────┘
                     ▼
        ┌──────────────────────────┐
        │   API ASP.NET Core 8     │
        │  Controllers · JWT · DTO │
        └────────────┬─────────────┘
                     │ EF Core (Database-First)
                     ▼
        ┌──────────────────────────┐
        │      SQL Server          │
        │  Tables app + SAGE (F_…) │
        └──────────────────────────┘
```

- **Approche Database-First** : le `DbContext` et les entités SAGE sont **scaffoldés** depuis la base existante via *EF Core Power Tools* (`efpt.config.json`).
- **Deux familles d'entités** : tables **SAGE** en lecture (`FArticle`, `FDepot`, `FArtGamme`…) et tables **applicatives** en lecture/écriture (`Inventaire`, `ProduitInventaire`, `User`…).

---

## ✨ Fonctionnalités

- 🔐 **Authentification JWT** : inscription, connexion, gestion des mots de passe (hachage **BCrypt**), tokens valides 7 jours avec claims (nom, email, rôle).
- 📦 **Gestion des inventaires** : création par dépôt, changement de statut (ouvert / en cours / clôturé), suppression.
- 🔢 **Comptage** : endpoint unique `modifier-quantite` qui aiguille la saisie vers le comptage 1 ou 2 selon le superviseur connecté.
- ⚖️ **Réconciliation & écarts** : calcul automatique des écarts théorique/physique, de la quantité finale et du statut.
- 🔎 **Recherche d'articles SAGE** : par référence, code-barres ou désignation, avec résolution du **type de gestion** (lot / gamme / lot+gamme / normal) et vérification des séries/gammes.
- 👥 **Affectations** : association des utilisateurs aux inventaires (max 2) et des dépôts aux utilisateurs.
- 📥 **Import Excel** : chargement des produits d'un inventaire depuis `.xls` / `.xlsx` (NPOI).
- 📤 **Rapports** : rapport de comptage 1, comptage 2 et **export Excel complet** (écarts, statut, justification) via EPPlus.
- 🌐 **Fallback SPA** : sert le front Angular depuis `wwwroot/`.
- 📜 **Swagger / OpenAPI** activé en développement.

---

## 🛠 Technologies utilisées

| Technologie | Usage |
|-------------|-------|
| **.NET 8 / ASP.NET Core** | Framework de l'API REST |
| **C# 12** | Langage (nullable + implicit usings) |
| **Entity Framework Core 8** | ORM (Database-First, SQL Server) |
| **SQL Server** | Base de données (schéma applicatif + SAGE) |
| **JWT Bearer** | Authentification par token |
| **BCrypt.Net-Next** | Hachage des mots de passe |
| **EPPlus** | Génération de rapports Excel |
| **NPOI** | Lecture de fichiers Excel à l'import |
| **Swashbuckle (Swagger)** | Documentation interactive de l'API |

---

## 📡 Endpoints de l'API

Toutes les routes utilisent le préfixe `api/[controller]`.

### 🔐 `api/Auth`
| Méthode | Route | Description |
|---------|-------|-------------|
| `POST` | `/login` | Connexion → JWT + utilisateur |
| `POST` | `/register` | Création d'un compte |
| `GET` | `/` | Liste des utilisateurs |
| `GET` | `/{idOrEmail}` | Utilisateur par ID ou email |
| `PUT` | `/{id}/ChangePassword` | Changement de mot de passe |
| `DELETE` | `/{id}` | Suppression d'un compte |

### 📦 `api/Inventaire`
| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/` · `/{id}` | Liste / détail des inventaires |
| `POST` | `/` | Créer un inventaire |
| `PUT` | `/{id}/statut` | Changer le statut |
| `POST` | `/import` | Importer les produits (Excel) |
| `POST` | `/modifier-quantite` | Saisir un comptage |
| `GET` | `/produit-inventaire/{id}` | Produits d'un inventaire |
| `GET` | `/lignes-erronees-strict/{id}` | Écarts vs théorique |
| `GET` | `/lignes-erronees-physique/{id}` | Écarts entre comptages |
| `GET` | `/rapport1/{id}` · `/rapport2/{id}` | Rapports de comptage |
| `GET` | `/rapportExcel/{id}` | Export Excel complet |
| `PUT` | `/{id}/quantite-finale` | Correction de la quantité finale |

### 🔎 `api/Article`
| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/{ref}` | Article par référence |
| `GET` | `/check/{value}` | Recherche réf / code-barres / désignation + type de gestion |
| `GET` | `/check-serie` | Vérifier un numéro de série/lot |
| `GET` | `/CheckGamme` | Valider une gamme |
| `GET` | `/suggest-gamme` | Autocomplétion des gammes |

### 👥 `api/Affectation` · 🏬 `api/FDepot` · 📝 `api/LigneInventaire`
Affectation utilisateurs↔inventaires et utilisateurs↔dépôts, données maîtres dépôts, et interface de lignes de comptage.

> ℹ️ `FDocLigneController` (lignes de documents SAGE) est présent mais **désactivé** (endpoints commentés).

---

## ⚖️ Logique métier : double comptage

Chaque produit inventorié (`ProduitInventaire`) porte deux comptages indépendants :

| Champ | Signification |
|-------|---------------|
| `QuantiteTheorique` | Stock attendu (importé / ERP) |
| `QuantiteComptage1` / `QuantiteComptage2` | Comptages des deux superviseurs |
| `EcartTheorique` | `Comptage1 − QuantiteTheorique` |
| `EcartPhysique` | `Comptage1 − Comptage2` |
| `QuantiteFinale` | Quantité retenue après réconciliation |
| `Statut` | `validé` · `écart` · `en attente` |

Le endpoint `modifier-quantite` identifie le produit par `Ref` (+ `Gamme1` / `NumLot` éventuels), aiguille la saisie vers le bon comptage selon le superviseur, puis recalcule écarts, quantité finale et statut. Les lignes en écart sont exposées via les endpoints `lignes-erronees-*` pour arbitrage par l'administrateur.

---

## 📁 Structure du projet

```
novo-inventaire-back/
├── novo-invnt-back.sln
└── tunisair-back/
    ├── Controllers/          # Auth, Inventaire, Article, Affectation, FDepot, LigneInventaire, Fallback
    ├── Models/               # Entités EF (app + SAGE) + DTHDLGContext (scaffoldé)
    ├── DTO/                  # Objets de transfert (Login, Register, Rapport…)
    ├── wwwroot/              # Front Angular servi en fallback
    ├── Program.cs            # Bootstrap : EF, JWT, CORS, Swagger
    ├── appsettings.json      # ⚠️ Config & secrets (git-ignoré)
    └── appsettings.example.json  # Modèle versionné
```

> ⚠️ Les fichiers `Models/DTHDLGContext.cs` et les entités SAGE (`F…`) sont **auto-générés** (EF Core Power Tools). Les régénérer plutôt que les modifier à la main.

---

## 🚀 Installation

### Prérequis

- **.NET 8 SDK**
- **SQL Server** (accès à la base `NOVOPHARMA` ou équivalente)
- *(optionnel)* **EF Core Power Tools** pour re-scaffolder le modèle

### Étapes

```bash
# 1. Cloner le dépôt
git clone https://github.com/<votre-compte>/novo-inventaire-back.git
cd novo-inventaire-back

# 2. Configurer les secrets (voir section suivante)
cp tunisair-back/appsettings.example.json tunisair-back/appsettings.json
# puis éditer la chaîne de connexion et la clé JWT

# 3. Restaurer & lancer
dotnet restore
dotnet run --project tunisair-back
```

---

## ⚙️ Configuration

La configuration se trouve dans `tunisair-back/appsettings.json` :

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Key": "<clé secrète longue et aléatoire>",
    "Issuer": "votre-issuer",
    "Audience": "votre-audience"
  }
}
```

> ⚠️ `appsettings.json` et `appsettings.*.json` sont **ignorés par Git** (`.gitignore`) car ils contiennent la chaîne de connexion et la clé JWT. Copiez `appsettings.example.json` après chaque clone. Pour le développement local, privilégiez les **User Secrets** (`dotnet user-secrets`) ou des variables d'environnement.

---

## 🔐 Sécurité

- **Mots de passe** hachés avec **BCrypt** (jamais stockés en clair).
- **JWT** signé en HS256, expiration 7 jours.
- **Secrets hors du dépôt** via `.gitignore` + modèle d'exemple.

**Pistes de durcissement** (voir [Améliorations futures](#-améliorations-futures)) : appliquer `[Authorize]` sur les endpoints sensibles, restreindre la politique **CORS** (actuellement `AllowAllOrigins`), désactiver `EnableSensitiveDataLogging` en production, et externaliser totalement les secrets.

---



<div align="center">

⭐️ Si ce projet vous a plu, n'hésitez pas à laisser une étoile !

</div>
