using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunisair_back.Models;

namespace tunisair_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AffectationController : ControllerBase
    {
        private readonly DTHDLGContext _context;

        public AffectationController(DTHDLGContext context)
        {
            _context = context;
        }
        // GET: api/Affectation/Utilisateurs/{userId}/Inventaires
        [HttpGet("Utilisateurs/{userId}/Inventaires")]
        public async Task<IActionResult> GetInventairesPourUtilisateur(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Utilisateur introuvable.");

            var inventaires = await _context.UserInventaires
                .Where(ui => ui.UserId == userId)
                .Include(ui => ui.Inventaire)
                .Select(ui => new
                {
                    ui.Inventaire.Id,
                    ui.Inventaire.Depot,
                    ui.Inventaire.DateCreation,
                    ui.Inventaire.Statut
                })
                .ToListAsync();

            return Ok(inventaires);
        }
        // DELETE: api/Affectation/Utilisateurs/{userId}/Inventaires/{inventaireId}
        [HttpDelete("Utilisateurs/{userId}/Inventaires/{inventaireId}")]
        public async Task<IActionResult> DesaffecterInventaireUtilisateur(Guid userId, int inventaireId)
        {
            var userInventaire = await _context.UserInventaires
                .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.InventaireId == inventaireId);

            if (userInventaire == null)
                return NotFound("Affectation introuvable pour cet utilisateur et cet inventaire.");

            _context.UserInventaires.Remove(userInventaire);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inventaire désaffecté avec succès." });
        }

        public class AffectationRequest
        {
            public Guid UserId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AffecterUtilisateur(int inventaireId, [FromBody] AffectationRequest request)
        {
            var userId = request.UserId;

            var inventaire = await _context.Inventaire.FindAsync(inventaireId);
            if (inventaire == null)
                return NotFound("Inventaire introuvable.");

            var utilisateur = await _context.Users.FindAsync(userId);
            if (utilisateur == null)
                return NotFound("Utilisateur introuvable.");

            // Vérifie si l'utilisateur est déjà affecté
            bool dejaAffecte = await _context.UserInventaires
                .AnyAsync(ui => ui.UserId == userId && ui.InventaireId == inventaireId);

            if (dejaAffecte)
                return BadRequest(new { message = "L'utilisateur est déjà affecté à cet inventaire." });

            // Vérifie le nombre d'utilisateurs déjà affectés à cet inventaire
            int nombreUtilisateurs = await _context.UserInventaires
                .CountAsync(ui => ui.InventaireId == inventaireId);

            if (nombreUtilisateurs >= 2)
                return BadRequest(new { message = "Un inventaire ne peut être affecté qu'à deux utilisateurs au maximum." });

            // Ajout de l'affectation
            _context.UserInventaires.Add(new UserInventaire
            {
                UserId = userId,
                InventaireId = inventaireId
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur Affecté avec succès !" });
        }
        [HttpGet("with-affectation/{userId}")]
        public async Task<IActionResult> GetDepotsWithAffectation(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Utilisateur introuvable.");

            var depotsWithAffectation = await (
                from depot in _context.FDepots
                join userDepot in _context.UserDepots
                    on depot.DeNo equals userDepot.DeNo into joined
                from sub in joined.Where(j => j.UserId == userId).DefaultIfEmpty()
                select new DepotAffectationDto
                {
                    DeNo = (int)depot.DeNo!,
                    DeIntitule = depot.DeIntitule,
                    Affecte = sub != null
                }).ToListAsync();

            return Ok(depotsWithAffectation);
        }


        [HttpPost("affecter-depot")]
        public async Task<IActionResult> AffecterDepot([FromBody] AffectationDepotRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound("Utilisateur introuvable.");

            // On récupère tous les dépôts (en mémoire) pour filtrer sans Contains
            var allDepots = await _context.FDepots.ToListAsync();

            // Créer un dictionnaire pour éviter Contains
            var depotIdsMap = request.DepotIds.ToDictionary(id => id, id => true);

            var selectedDepots = new List<FDepot>();
            foreach (var depot in allDepots)
            {
                if (depotIdsMap.ContainsKey((int)depot.DeNo))
                {
                    selectedDepots.Add(depot);
                }
            }

            if (selectedDepots.Count != request.DepotIds.Count)
                return BadRequest("Un ou plusieurs dépôts sont introuvables.");

            // Récupérer les dépôts déjà affectés à l'utilisateur
            var affectationsExistantes = await _context.UserDepots
                .Where(ud => ud.UserId == request.UserId)
                .ToListAsync();

            // Construire une table des nouveaux IDs
            var selectedDepotIdsMap = selectedDepots.ToDictionary(d => d.DeNo, d => true);

            // Déterminer ceux à supprimer
            var affectationsASupprimer = new List<UserDepot>();
            foreach (var aff in affectationsExistantes)
            {
                if (!selectedDepotIdsMap.ContainsKey(aff.DeNo))
                {
                    affectationsASupprimer.Add(aff);
                }
            }

            _context.UserDepots.RemoveRange(affectationsASupprimer);

            // Supprimer aussi les affectations des lignes
            //foreach (var aff in affectationsASupprimer)
            //{
            //    var lignesAffectation = await _context.FDocLigneAffectations
            //        .Where(l => l.UserId == request.UserId && l.DE_No == aff.DeNo)
            //        .ToListAsync();

            //    _context.FDocLigneAffectations.RemoveRange(lignesAffectation);
            //}

            // Ajouter les nouveaux dépôts s’ils n’étaient pas déjà affectés
            var existingDepotIds = affectationsExistantes.Select(a => a.DeNo).ToHashSet();

            foreach (var depot in selectedDepots)
            {
                if (!existingDepotIds.Contains((int)depot.DeNo))
                {
                    _context.UserDepots.Add(new UserDepot
                    {
                        UserId = request.UserId,
                        DeNo = (int)depot.DeNo,
                        DeIntitule = depot.DeIntitule
                    });

                    //var lignes = await _context.FDocLignes
                    //    .Where(d => d.DE_No == depot.DeNo &&
                    //                d.DO_Domaine == 2 &&
                    //                d.DO_Type == 23 &&
                    //                d.DL_MvtStock == 1)
                    //    .ToListAsync();

                    //foreach (var ligne in lignes)
                    //{
                    //    // Vérifier si la ligne d'affectation existe déjà (sans tenir compte de l'utilisateur)
                    //    var affectationExistante = await _context.FDocLigneAffectations.FirstOrDefaultAsync(a =>
                    //        a.DO_Domaine == ligne.DO_Domaine &&
                    //        a.DO_Type == ligne.DO_Type &&
                    //        a.DE_No == ligne.DE_No &&
                    //        a.DL_MvtStock == ligne.DL_MvtStock &&
                    //        a.DO_Piece == ligne.DO_Piece &&
                    //        a.AR_REF == ligne.AR_REF);

                    //    int ligneAffectationId;

                    //    //if (affectationExistante == null)
                    //    //{
                    //    //    var nouvelleAffectation = new FDocLigneAffectation
                    //    //    {
                    //    //        DO_Domaine = ligne.DO_Domaine,
                    //    //        DO_Type = ligne.DO_Type,
                    //    //        DE_No = ligne.DE_No,
                    //    //        DL_MvtStock = ligne.DL_MvtStock,
                    //    //        DO_Piece = ligne.DO_Piece,
                    //    //        AR_REF = ligne.AR_REF,
                    //    //        DL_Design = ligne.DL_Design,
                    //    //        DO_Date = ligne.DO_Date,
                    //    //        DL_QTE = ligne.DL_QTE,
                    //    //        QuantiteRecu = 0,
                    //    //        Statut = "En attente"
                    //    //    };

                    //    //    _context.FDocLigneAffectations.Add(nouvelleAffectation);
                    //    //    await _context.SaveChangesAsync(); // pour récupérer l'ID

                    //    //    ligneAffectationId = nouvelleAffectation.Id;
                    //    //}
                    //    //else
                    //    //{
                    //    //    ligneAffectationId = affectationExistante.Id;
                    //    //}

                    //    // Vérifier si ce user est déjà lié à cette ligne
                    //    //var existeDeja = await _context.FDocLigneAffectationUsers.AnyAsync(u =>
                    //    //    u.LigneAffectationId == ligneAffectationId && u.UserId == request.UserId);

                    //    //if (!existeDeja)
                    //    //{
                    //    //    _context.FDocLigneAffectationUsers.Add(new FDocLigneAffectationUser
                    //    //    {
                    //    //        LigneAffectationId = ligneAffectationId,
                    //    //        UserId = request.UserId
                    //    //    });
                    //    //}
                    //}

                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Les affectations ont été mises à jour avec succès." });
        }

        [HttpPost("desaffecter-depot")]
        public async Task<IActionResult> DesaffecterDepot([FromBody] AffectationDepotRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound("Utilisateur introuvable.");
            // Liste des dépôts affectés à ce user
            var depotsAEnlever = new List<UserDepot>();
            foreach (var depotId in request.DepotIds)
            {
                var affectation = await _context.UserDepots
                    .FirstOrDefaultAsync(ud => ud.UserId == request.UserId && ud.DeNo == depotId);

                if (affectation != null)
                {
                    depotsAEnlever.Add(affectation);
                }
            }

            if (depotsAEnlever.Count == 0)
                return BadRequest("Aucun des dépôts spécifiés n’est affecté à cet utilisateur.");

            // Supprimer les affectations
            _context.UserDepots.RemoveRange(depotsAEnlever);

            // Supprimer aussi les lignes d'affectation liées à ces dépôts et à cet utilisateur
            //foreach (var depot in depotsAEnlever)
            //{
            //    var lignesAEnlever = await _context.FDocLigneAffectations
            //        .Where(l => l.UserId == request.UserId && l.DE_No == depot.DeNo)
            //        .ToListAsync();

            //    _context.FDocLigneAffectations.RemoveRange(lignesAEnlever);
            //}

            await _context.SaveChangesAsync();

            return Ok(new { message = "Dépôts désaffectés avec succès." });
        }



        // GET: api/Affectation/5
        [HttpGet("{inventaireId}")]
        public async Task<IActionResult> GetUtilisateursAffectes(int inventaireId)
        {
            var affectations = await _context.UserInventaires
                .Where(ui => ui.InventaireId == inventaireId)
                .Include(ui => ui.User)
                .Select(ui => new
                {
                    ui.User.Id,
                    ui.User.Username,
                    ui.User.Name,
                    ui.User.Email
                })
                .ToListAsync();

            return Ok(affectations);
        }


        public class DepotAffectationDto
        {
            public int DeNo { get; set; }
            public string DeIntitule { get; set; }
            public bool Affecte { get; set; } // true si déjà affecté
        }
        public class AffectationDepotRequestSimple
        {
            public Guid UserId { get; set; }
            public int DeNo { get; set; }
        }
    }
}

