using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunisair_back.Models;

namespace tunisair_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LigneInventaireController : ControllerBase
    {
        private readonly DTHDLGContext _context;

        public LigneInventaireController(DTHDLGContext context)
        {
            _context = context;
        }

        // GET: api/LigneInventaire
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LigneInventaire>>> GetLigneInventaires()
        {
            return await _context.LigneInventaires.ToListAsync();
        }

        // GET: api/LigneInventaire/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LigneInventaire>> GetLigneInventaire(int id)
        {
            var ligneInventaire = await _context.LigneInventaires.FindAsync(id);

            if (ligneInventaire == null)
            {
                return NotFound();
            }

            return ligneInventaire;
        }

        // POST: api/LigneInventaire
        [HttpPost]
        public async Task<ActionResult<LigneInventaire>> PostLigneInventaire(LigneInventaire ligneInventaire)
        {
            // Validation des données d'entrée, si nécessaire
            if (ligneInventaire == null)
            {
                return BadRequest("Les données sont invalides.");
            }

            _context.LigneInventaires.Add(ligneInventaire);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLigneInventaire), new { id = ligneInventaire.Id }, ligneInventaire);
        }

        // PUT: api/LigneInventaire/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLigneInventaire(int id, LigneInventaire ligneInventaire)
        {
            if (id != ligneInventaire.Id)
            {
                return BadRequest();
            }

            _context.Entry(ligneInventaire).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LigneInventaireExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/LigneInventaire/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLigneInventaire(int id)
        {
            var ligneInventaire = await _context.LigneInventaires.FindAsync(id);
            if (ligneInventaire == null)
            {
                return NotFound();
            }

            _context.LigneInventaires.Remove(ligneInventaire);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // GET: api/LigneInventaire/ByInventaire/{inventaireId}

        [HttpGet("ByInventaire/{inventaireId}")]
        public async Task<ActionResult<IEnumerable<LigneInventaireGrouped>>> GetLigneInventairesByInventaire(int inventaireId)
        {
            var ligneInventaires = await _context.LigneInventaires
                .Where(l => l.InventaireId == inventaireId)
                .GroupBy(l => l.ArRef)
                .Select(group => new LigneInventaireGrouped
                {
                    ArRef = group.Key,
                    Articles = group.Select(g => new ArticleDetails
                    {
                        ArRef = g.ArRef,
                        ArDesign = g.ArDesign,
                        Quantite = g.Quantite,
                        Username = g.Username,
                        Depot = g.Depot,
                        NumSerie = g.NumSerie,
                        Gamme1 = g.Gamme1,
                        Gamme2 = g.Gamme2,
                        TypeDocument = g.TypeDocument,
                        DateInventaire = g.DateInventaire,
                        Domaine = g.Domaine
                    }).ToList()
                })
        .ToListAsync();

            if (ligneInventaires == null || ligneInventaires?.Count == 0)
            {
                return NotFound();
            }

            return Ok(ligneInventaires);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AjouterLigneInventaire([FromBody] LigneInventaire ligne)
        {
            if (ligne == null)
                return BadRequest("Données manquantes.");

            // Validation simple : vérifie que la référence n'est pas vide et que la quantité > 0
            if (string.IsNullOrWhiteSpace(ligne.ArRef) || ligne.Quantite <= 0)
                return BadRequest(new { erreur = "Référence vide ou quantité invalide." });

            try
            {
                ligne.DateSaisie = DateTime.Now;
                _context.LigneInventaires.Add(ligne);
                await _context.SaveChangesAsync();

                // Recherche toutes les lignes d'inventaire pour le même article dans cet inventaire, triées par ordre d'insertion (par exemple par Id)
                var lignes = await _context.LigneInventaires
                    .Where(l => l.InventaireId == ligne.InventaireId && l.ArRef == ligne.ArRef)
                    .OrderBy(l => l.Id)
                    .ToListAsync();

                // Récupère le produit dans la table ProduitInventaire correspondant à cet article et inventaire
                var produit = await _context.ProduitsInventaire
                    .FirstOrDefaultAsync(p => p.InventaireId == ligne.InventaireId && p.Ref == ligne.ArRef);

                if (produit != null && lignes.Any())
                {
                    produit.QuantiteTheorique = produit.Quantite;
                    // Le premier ajout sera toujours le comptage 1, le deuxième le comptage 2.
                    var ligne1 = lignes.ElementAtOrDefault(0);
                    var ligne2 = lignes.ElementAtOrDefault(1); // Peut être null si seul le premier comptage existe.

                    // Mise à jour du comptage 1
                    decimal q1 = ligne1?.Quantite ?? 0;
                    produit.QuantiteComptage1 = q1;
                    produit.Superviseur1 = ligne1?.Username ?? "";

                    // Si le deuxième comptage existe, on le traite et on effectue le calcul complet.
                    if (ligne2 != null)
                    {
                        produit.QuantiteTheorique = produit.Quantite;
                        decimal q2 = ligne2.Quantite;
                        produit.QuantiteComptage2 = q2;
                        produit.Superviseur2 = ligne2?.Username ?? "";

                        decimal? qTheo = produit.QuantiteTheorique; // On suppose que ce champ a déjà été défini (par défaut ou importé)
                        var ecartTheo = qTheo - q1;
                        var ecartPhys = q1 - q2;
                        decimal? qFinale = (q1 + q2) / 2;

                        string statut = "validé";
                        string justification = "quantité validée";

                        if (Math.Abs(ecartPhys) > 2)
                        {
                            statut = "écart grave";
                            justification = "écart grave, à saisir";
                            qFinale = qTheo;
                        }
                        else if (Math.Abs(ecartPhys) > 0)
                        {
                            statut = "à vérifier";
                            justification = "écart léger, à vérifier";
                        }
                        produit.EcartTheorique = ecartTheo;
                        produit.EcartPhysique = ecartPhys;
                        produit.QuantiteFinale = qFinale;
                        produit.Statut = statut;
                        produit.Justification = justification;
                    }
                    else
                    {
                        // Si seul le premier comptage est saisi, on peut éventuellement ne mettre à jour que le comptage 1
                        // et laisser les autres champs non renseignés ou avec des valeurs par défaut.
                        produit.QuantiteComptage2 = 0;
                        produit.EcartTheorique = 0;
                        produit.EcartPhysique = 0;
                        produit.QuantiteFinale = q1;
                        produit.Statut = "en attente";
                        produit.Justification = "un seul comptage saisi";
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "Ligne d'inventaire ajoutée et synchronisée avec succès.",
                    ligne
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de l'ajout ou de la synchronisation.",
                    details = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("{id}/commentaire")]
        public async Task<IActionResult> UpdateCommentaire(int id, [FromBody] CommentaireDto dto)
        {
            var produit = await _context.ProduitsInventaire.FindAsync(id);
            if (produit == null) return NotFound();

            produit.Commentaire = dto.Commentaire;
            await _context.SaveChangesAsync();

            return NoContent();
        }

      
        // Classe pour retourner les résultats groupés par ArRef
        [HttpPost("bulk")]
        public async Task<IActionResult> AjouterLignesInventaire([FromBody] List<LigneInventaire> lignes)
        {
            if (lignes == null || !lignes.Any())
                return BadRequest("Aucune ligne d'inventaire reçue.");

            var lignesInserees = new List<LigneInventaire>();
            var erreurs = new List<object>();

            foreach (var ligne in lignes)
            {
                try
                {
                    // Validation légère (tu peux enrichir)
                    if (string.IsNullOrEmpty(ligne.ArRef) || ligne.Quantite <= 0)
                    {
                        erreurs.Add(new { ligne.ArRef, erreur = "Référence vide ou quantité invalide." });
                        continue;
                    }

                    ligne.DateSaisie = DateTime.Now;

                    _context.LigneInventaires.Add(ligne);
                    lignesInserees.Add(ligne);
                }
                catch (Exception ex)
                {
                    erreurs.Add(new
                    {
                        ligne.ArRef,
                        ex.Message,
                        Inner = ex.InnerException?.Message
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la sauvegarde des données.",
                    details = ex.Message
                });
            }

            return Ok(new
            {
                success = true,
                lignesAjoutees = lignesInserees.Count,
                erreurs = erreurs
            });
        }
        [HttpGet("inventaire/{inventaireId}")]
        public async Task<ActionResult<List<LigneInventaireResult>>> CalculerInventaire(int inventaireId)
        {
            // Récupérer les lignes d'inventaire correspondant à cet inventaire
            var lignesInventaire = await _context.LigneInventaires
                .Where(li => li.InventaireId == inventaireId)
                .ToListAsync();

            if (lignesInventaire == null || !lignesInventaire.Any())
            {
                return NotFound("Aucune ligne d'inventaire trouvée pour cet inventaire.");
            }

            // Simuler ou récupérer les quantités théoriques
            // Ici je suppose qu'on peut avoir une table pour les théoriques ou sinon c'est à récupérer manuellement
            // Pour l'exemple, on suppose que la quantité théorique est 10 pour tous
            var quantitesTheoriques = lignesInventaire
                .GroupBy(li => li.ArRef)
                .ToDictionary(g => g.Key, g => 10m); // valeur théorique = 10 pour tous (à adapter si besoin)

            var result = CalculerInventaireResult(lignesInventaire, quantitesTheoriques);

            return Ok(result);
        }

        private List<LigneInventaireResult> CalculerInventaireResult(List<LigneInventaire> lignesInventaire, Dictionary<string, decimal> quantitesTheoriques)
        {
            var result = new List<LigneInventaireResult>();

            // Grouper par référence article
            var groupes = lignesInventaire.GroupBy(l => l.ArRef);

            foreach (var groupe in groupes)
            {
                var lignes = groupe.Take(2).ToList(); // Prendre max 2 lignes par article

                var ligne1 = lignes.ElementAtOrDefault(0);
                var ligne2 = lignes.ElementAtOrDefault(1);

                decimal quantite1 = ligne1?.Quantite ?? 0;
                decimal quantite2 = ligne2?.Quantite ?? 0;

                quantitesTheoriques.TryGetValue(groupe.Key, out var quantiteTheorique);

                var ecartTheorique = quantiteTheorique - quantite1;
                var ecartPhysique = quantite1 - quantite2;

                decimal quantiteFinale = (quantite1 + quantite2) / 2;

                string statut = "validé";
                string justification = "quantité validée";

                if (Math.Abs(ecartPhysique) > 2)
                {
                    statut = "écart grave";
                    justification = "écart grave, à saisir";
                    quantiteFinale = quantiteTheorique;
                }
                else if (Math.Abs(ecartPhysique) > 0)
                {
                    statut = "à vérifier";
                    justification = "écart léger, à vérifier";
                }

                result.Add(new LigneInventaireResult
                {
                    ArRef = groupe.Key,
                    ArDesign = ligne1?.ArDesign ?? ligne2?.ArDesign ?? "",
                    Depot = ligne1?.Depot ?? ligne2?.Depot ?? "",
                    QuantiteTheorique = quantiteTheorique,
                    QuantiteComptage1 = quantite1,
                    QuantiteComptage2 = quantite2,
                    EcartTheorique = ecartTheorique,
                    EcartPhysique = ecartPhysique,
                    Statut = statut,
                    QuantiteFinale = quantiteFinale,
                    Justification = justification,
                    Superviseur1 = ligne1?.Username ?? "",
                    Superviseur2 = ligne2?.Username ?? ""
                });
            }

            return result;
        }

        private bool LigneInventaireExists(int id)
        {
            return _context.LigneInventaires.Any(e => e.Id == id);
        }
        public class ArticleDetails
        {
            public string ArRef { get; set; }
            public string ArDesign { get; set; }
            public int Quantite { get; set; }
            public string Username { get; set; }
            public string Depot { get; set; }
            public string NumSerie { get; set; }
            public string Gamme1 { get; set; }
            public string Gamme2 { get; set; }
            public string TypeDocument { get; set; }
            public DateTime? DateInventaire { get; set; }
            public string Domaine { get; set; }
        }

        public class LigneInventaireGrouped
        {
            public string ArRef { get; set; }
            public List<ArticleDetails> Articles { get; set; }
        }
        public class CommentaireDto
        {
            public string Commentaire { get; set; }
        }

    }
}

