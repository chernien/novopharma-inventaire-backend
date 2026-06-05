using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunisair_back.DTO;
using tunisair_back.Models;
using OfficeOpenXml;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace tunisair_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventaireController : ControllerBase
    {
        private readonly DTHDLGContext _context;

        public InventaireController(DTHDLGContext context)
        {
            _context = context;
        }

        // GET: api/Inventaire
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventaire>>> GetInventaires()
        {
            var inventaires = await _context.Inventaire
                .AsNoTracking()
                .ToListAsync();

            return Ok(inventaires);
        }

        [HttpGet("produit-inventaire/{id}")]
        public async Task<ActionResult<IEnumerable<Inventaire>>> GetLignesInventaires(int id)
        {
            var inventaires = await _context.ProduitsInventaire.Where(p => p.InventaireId == id)
                .AsNoTracking()
                .ToListAsync();

            return Ok(inventaires);
        }
        // GET: api/Inventaire/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inventaire>> GetInventaire(int id)
        {
            var inventaire = await _context.Inventaire.FindAsync(id);

            if (inventaire == null)
                return NotFound();

            return inventaire;
        }

        // POST: api/Inventaire
        [HttpPost]
        public async Task<ActionResult<Inventaire>> PostInventaire(CreateInventaireDto dto)
        {
            // Vérifier s’il existe un inventaire ouvert pour ce dépôt
            bool existeInventaireOuvert = await _context.Inventaire
                .AnyAsync(i => i.Depot == dto.Depot && (i.Statut == StatutInventaire.Ouvert || i.Statut == StatutInventaire.EnCours));

            if (existeInventaireOuvert)
            {
                return Conflict(new { message = $"Un inventaire est déjà ouvert pour le dépôt \"{dto.Depot}\"." });
            }

            var inventaire = new Inventaire
            {
                Depot = dto.Depot,
                DateInventaire = dto.DateInventaire,
                DateCreation = DateTime.Now,
                Statut = StatutInventaire.Ouvert,
            };

            _context.Inventaire.Add(inventaire);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInventaire), new { id = inventaire.Id }, inventaire);
        }



        // DELETE: api/Inventaire/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventaire(int id)
        {
            var inventaire = await _context.Inventaire.FindAsync(id);

            if (inventaire == null)
                return NotFound();

            _context.Inventaire.Remove(inventaire);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpPut("{id}/statut")]
        public async Task<IActionResult> ChangeStatut(int id, [FromBody] StatutUpdateDto dto)
        {
            var inventaire = await _context.Inventaire.FindAsync(id);
            if (inventaire == null)
                return NotFound();

            inventaire.Statut = dto.Statut;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("rapport1/{id}")]
        public async Task<ActionResult<IEnumerable<RapportInventaireDto>>> GetRapportInventaire1(int id)
        {
            var rapport = await _context.ProduitsInventaire
                .Where(p => p.InventaireId == id)
                .AsNoTracking()
                .Select(p => new RapportInventaireDto
                {
                    Id = p.Id,
                    Ref = p.Ref,
                    Designation = p.Designation,
                    nserie = p.Nserie,
                    gamme1 = p.Gamme1,
                    Depot = p.Depot,
                    QuantiteTheorique = p.QuantiteTheorique,
                    QuantiteComptage1 = p.QuantiteComptage1,
                    Superviseur1 = p.Superviseur1,
                    QuantiteFinale = p.QuantiteFinale
                })
                .ToListAsync();

            return Ok(rapport);
        }


        [HttpGet("rapport2/{id}")]
        public async Task<ActionResult<IEnumerable<RapportInventaireDto>>> GetRapportInventaire2(int id)
        {
            var rapport = await _context.ProduitsInventaire
                .Where(p => p.InventaireId == id)
                .AsNoTracking()
                .Select(p => new RapportInventaireDto
                {
                    Id = p.Id,
                    Ref = p.Ref,
                    Designation = p.Designation,
                    nserie = p.Nserie,
                    gamme1 = p.Gamme1,
                    Depot = p.Depot,
                    QuantiteTheorique = p.QuantiteTheorique,
                    QuantiteComptage2 = p.QuantiteComptage2,
                    Superviseur2 = p.Superviseur2,
                    QuantiteFinale = p.QuantiteFinale
                })
                .ToListAsync();

            return Ok(rapport);
        }


        [HttpGet("rapportExcel/{id}")]
        public async Task<ActionResult<IEnumerable<RapportExcelDto>>> GetRapportExcel(int id)
        {
            var rapport = await _context.ProduitsInventaire
                .Where(p => p.InventaireId == id)
                .AsNoTracking()
                .Select(p => new RapportExcelDto
                {
                    Id = p.Id,
                    Ref = p.Ref,
                    Designation = p.Designation,
                    nserie = p.Nserie,
                    gamme1 = p.Gamme1,
                    Depot = p.Depot,
                    QuantiteTheorique = p.QuantiteTheorique,
                    QuantiteComptage1 = p.QuantiteComptage1,
                    QuantiteComptage2 = p.QuantiteComptage2,
                    Superviseur1 = p.Superviseur1,
                    Superviseur2 = p.Superviseur2,
                    QuantiteFinale = p.QuantiteFinale,
                    EcartPhysique = p.EcartPhysique,
                    EcartTheorique = p.EcartTheorique,
                    Justification = p.Justification,
                    Statut = p.Statut
                })
                .ToListAsync();

            return Ok(rapport);
        }


        [HttpPost("import")]
public async Task<IActionResult> Import(IFormFile file, [FromForm] int inventaireId)
{
    var inventaire = _context.Inventaire.FirstOrDefault(i => i.Id == inventaireId);
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "Fichier manquant." });

    using var stream = file.OpenReadStream();
    IWorkbook workbook;

    if (file.FileName.EndsWith(".xls"))
        workbook = new HSSFWorkbook(stream);
    else if (file.FileName.EndsWith(".xlsx"))
        workbook = new XSSFWorkbook(stream);
    else
        return BadRequest(new { message = "Format de fichier non supporté." });

    var sheet = workbook.GetSheetAt(0);
    var produitsTemp = new List<ProduitInventaire>();

    // Superviseurs
    var superviseurs = await _context.UserInventaires
        .Where(ui => ui.InventaireId == inventaireId)
        .Include(ui => ui.User)
        .ToListAsync();

    var depotAssocie = inventaire?.Depot?.Trim().ToLower();
    var superviseur1 = superviseurs.ElementAtOrDefault(0);
    var superviseur2 = superviseurs.ElementAtOrDefault(1);

    // Parcours des lignes
    for (int row = 1; row <= sheet.LastRowNum; row++) // Skip Header
    {
        var excelRow = sheet.GetRow(row);

        // Ligne réellement vide → ignorer
        if (excelRow == null) continue;

        bool isEmptyRow = excelRow.Cells.All(c =>
            c == null || string.IsNullOrWhiteSpace(c.ToString())
        );
        if (isEmptyRow) continue;

        // Lire dépôt
        var depotExcel = excelRow.GetCell(10)?.ToString()?.Trim().ToLower() ?? "";

        // Validation dépôt
        if (depotExcel != depotAssocie)
        {
            return BadRequest(new
            {
                message = $"Erreur à la ligne {row + 1} : le dépôt saisi '{excelRow.GetCell(10)?.ToString()}' ne correspond pas au dépôt autorisé pour l'inventaire : '{inventaire.Depot}'"
            });
        }

                // 🔥 Lecture de la date de péremption (colonne 11)
                DateTime? datePeremption = null;
                var cellDate = excelRow.GetCell(11);

                if (cellDate != null)
                {
                    if (cellDate.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cellDate))
                        datePeremption = cellDate.DateCellValue;
                    else
                    {
                        // Tentative parsing si format texte
                        if (DateTime.TryParse(cellDate.ToString(), out var parsed))
                            datePeremption = parsed;
                    }
                }

                // Construire objet temporaire
                var produit = new ProduitInventaire
        {
            Ref = excelRow.GetCell(0)?.ToString(),
            Designation = excelRow.GetCell(1)?.ToString(),
            Famille = excelRow.GetCell(2)?.ToString(),
            Gamme1 = excelRow.GetCell(3)?.ToString(),
            Gamme2 = excelRow.GetCell(4)?.ToString(),
            Nserie = excelRow.GetCell(5)?.ToString(),
            Quantite = (int)(excelRow.GetCell(6)?.NumericCellValue ?? 0),
            QuantiteTheorique = (decimal)(excelRow.GetCell(6)?.NumericCellValue ?? 0),
            PrixUnitaire = (decimal)(excelRow.GetCell(7)?.NumericCellValue ?? 0),
            MontantTotal = (decimal)(excelRow.GetCell(8)?.NumericCellValue ?? 0),
            SuiviStock = excelRow.GetCell(9)?.ToString(),
            Depot = excelRow.GetCell(10)?.ToString(),
            DatePeremption = datePeremption,   // ✅ AJOUTÉ
            InventaireId = inventaireId,
            Superviseur1 = superviseur1?.User.Username ?? "Superviseur1 par défaut",
            Superviseur2 = superviseur2?.User.Username ?? "Superviseur2 par défaut"
        };

        produitsTemp.Add(produit);
    }

            foreach (var ligne in produitsTemp)
            {
                var produitFinal = new ProduitInventaire
                {
                    Ref = ligne.Ref,
                    Designation = ligne.Designation,
                    Famille = ligne.Famille,
                    Gamme1 = ligne.Gamme1,
                    Gamme2 = ligne.Gamme2,
                    Nserie = ligne.Nserie,
                    PrixUnitaire = ligne.PrixUnitaire,
                    MontantTotal = ligne.MontantTotal,
                    SuiviStock = ligne.SuiviStock,
                    Depot = ligne.Depot,
                    DatePeremption = ligne.DatePeremption,
                    InventaireId = inventaireId,

                    QuantiteTheorique = ligne.QuantiteTheorique ?? 0,
                    QuantiteComptage1 = 0,
                    QuantiteComptage2 = 0,
                    EcartTheorique = 0 - (ligne.QuantiteTheorique ?? 0),
                    EcartPhysique = 0,
                    QuantiteFinale = 0,
                    Statut = "en attente",
                    Justification = "à compter",

                    Superviseur1 = superviseur1?.User.Name ?? "Superviseur 1",
                    Superviseur2 = superviseur2?.User.Name ?? "Superviseur 2"
                };

                _context.ProduitsInventaire.Add(produitFinal);
            }


            inventaire.IsImport = true;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Fichier importé avec succès" });
}



        [HttpDelete("lignes/{inventaireId}")]
        public async Task<IActionResult> SupprimerProduitsParInventaireId(int inventaireId)
        {
            var inventaire = await _context.Inventaire.FirstOrDefaultAsync(i => i.Id == inventaireId);
            var produits = await _context.ProduitsInventaire
                .Where(p => p.InventaireId == inventaireId)
                .ToListAsync();

            if (produits == null || produits.Count == 0)
            {
                return NotFound(new { message = "Aucun produit trouvé pour cet inventaire." });
            }

            _context.ProduitsInventaire.RemoveRange(produits);
            inventaire.IsImport = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Produits supprimés avec succès." });





        }
        [HttpPost("modifier-quantite")]
        public async Task<IActionResult> ModifierQuantiteComptage([FromBody] QuantiteUpdateRequest request)
        {
            // Vérifier utilisateur
            var utilisateur = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (utilisateur == null)
                return NotFound("Utilisateur non reconnu.");

            ProduitInventaire produit = null;

            // CAS 1 : LOT + GAMME
            if (!string.IsNullOrWhiteSpace(request.NumLot) && !string.IsNullOrWhiteSpace(request.Gamme1))
            {
                produit = await _context.ProduitsInventaire.FirstOrDefaultAsync(p =>
                    p.InventaireId == request.InventaireId &&
                    p.Ref == request.Ref &&
                    p.Nserie == request.NumLot &&
                    p.Gamme1 == request.Gamme1 &&
                    p.Depot == request.Depot);
            }
            // CAS 2 : LOT
            else if (!string.IsNullOrWhiteSpace(request.NumLot))
            {
                produit = await _context.ProduitsInventaire.FirstOrDefaultAsync(p =>
                    p.InventaireId == request.InventaireId &&
                    p.Ref == request.Ref &&
                    p.Nserie == request.NumLot &&
                    p.Depot == request.Depot);
            }
            // CAS 3 : GAMME
            else if (!string.IsNullOrWhiteSpace(request.Gamme1))
            {
                produit = await _context.ProduitsInventaire.FirstOrDefaultAsync(p =>
                    p.InventaireId == request.InventaireId &&
                    p.Ref == request.Ref &&
                    p.Gamme1 == request.Gamme1 &&
                    p.Depot == request.Depot);
            }
            // CAS 4 : NORMAL
            else
            {
                produit = await _context.ProduitsInventaire.FirstOrDefaultAsync(p =>
                    p.InventaireId == request.InventaireId &&
                    p.Ref == request.Ref &&
                    p.Depot == request.Depot);
            }

            bool nouveauProduit = false;

            // ➤ CRÉATION SI ABSENT
            if (produit == null)
            {
                // 🔥🔥🔥 RÉCUPÉRATION DES SUPERVISEURS DEPUIS UserInventaire 🔥🔥🔥
                var superviseurUserIds = await _context.UserInventaires
                    .Where(ui => ui.InventaireId == request.InventaireId)
                    .OrderBy(ui => ui.UserId) // ordre = superviseur 1 puis 2
                    .Select(ui => ui.UserId)
                    .Take(2)
                    .ToListAsync();

                var superviseur1Id = superviseurUserIds.ElementAtOrDefault(0);
                var superviseur2Id = superviseurUserIds.ElementAtOrDefault(1);

                var superviseurs = await _context.Users
                    .Where(u => u.Id == superviseur1Id || u.Id == superviseur2Id)
                    .ToListAsync();

                var superviseur1Username = superviseurs
                    .FirstOrDefault(u => u.Id == superviseur1Id)?.Username;

                var superviseur2Username = superviseurs
                    .FirstOrDefault(u => u.Id == superviseur2Id)?.Username;
                // 🔥🔥🔥 FIN RÉCUPÉRATION SUPERVISEURS 🔥🔥🔥


                produit = new ProduitInventaire
                {
                    InventaireId = request.InventaireId,
                    Ref = request.Ref,
                    Designation = request.Designation,
                    Depot = request.Depot,

                    Nserie = request.NumLot,
                    Gamme1 = request.Gamme1,

                    SuiviStock = request.SuiviStock,
                    Superviseur1 = superviseur1Username,
                    Superviseur2 = superviseur2Username,
                    QuantiteTheorique = 0,
                    QuantiteComptage1 = 0,
                    QuantiteComptage2 = 0,
                    Statut = "en attente",
                    Justification = "ligne créée",
                    DateAjout = DateTime.Now,
                    // 🔥 Ajouter la date de péremption uniquement si NumLot est renseigné
                    DatePeremption = !string.IsNullOrWhiteSpace(request.NumLot) ? request.DatePeremption : null

                };

                // 🔥🔥🔥 AJOUT : REMPLIR SUPERVISEUR 2 AUTOMATIQUEMENT 🔥🔥🔥

                //var autresLignes = await _context.ProduitsInventaire
                //    .Where(p => p.InventaireId == request.InventaireId && p.Superviseur2 != null)
                //    .ToListAsync();

                //if (autresLignes.Any())
                //{
                //    produit.Superviseur2 = autresLignes
                //        .GroupBy(p => p.Superviseur2)
                //        .OrderByDescending(g => g.Count())
                //        .Select(g => g.Key)
                //        .FirstOrDefault();
                //}

                ////// Si vraiment aucun superviseur 2 existant → valeur par défaut
                ////if (string.IsNullOrWhiteSpace(produit.Superviseur2))
                ////    produit.Superviseur2 = "Superviseur 2";

                //// 🔥🔥🔥 FIN AJOUT 🔥🔥🔥


                _context.ProduitsInventaire.Add(produit);
                nouveauProduit = true;
            }

            else
            {
                // 🔥 Mettre à jour date de péremption si c'est un lot et que la date est fournie
                if (!string.IsNullOrWhiteSpace(request.NumLot) && request.DatePeremption.HasValue)
                {
                    produit.DatePeremption = request.DatePeremption.Value;
                }
            }

            // ➤ VERIFICATION AUTORISATION
            bool isSuperviseur1 =
                string.Equals(produit.Superviseur1?.Trim(), utilisateur.Name.Trim(),
                    StringComparison.OrdinalIgnoreCase);

            bool isSuperviseur2 =
                string.Equals(produit.Superviseur2?.Trim(), utilisateur.Name.Trim(),
                    StringComparison.OrdinalIgnoreCase);

            if (!isSuperviseur1 && !isSuperviseur2)
                return BadRequest("Vous n'êtes pas autorisé à modifier ce produit.");

            // ➤ AJOUT QUANTITÉ
            if (isSuperviseur1)
                produit.QuantiteComptage1 = (produit.QuantiteComptage1 ?? 0) + request.Quantite;

            if (isSuperviseur2)
                produit.QuantiteComptage2 = (produit.QuantiteComptage2 ?? 0) + request.Quantite;

            // 🔥🔥🔥 AJOUT ICI — CALCUL DES ÉCARTS 🔥🔥🔥

            // Ecart Théorique
            produit.EcartTheorique =
                (produit.QuantiteComptage1 ?? 0) - (produit.QuantiteTheorique ?? 0);

            // Ecart Physique
            produit.EcartPhysique =
                (produit.QuantiteComptage1 ?? 0) - (produit.QuantiteComptage2 ?? 0);


            // 🔥🔥🔥 NOUVEAU : CALCUL QUANTITÉ FINALE 🔥🔥🔥
            if ((produit.QuantiteComptage1 ?? 0) > 0 && (produit.QuantiteComptage2 ?? 0) > 0)
            {
                if (produit.QuantiteComptage1 == produit.QuantiteComptage2)
                {
                    produit.QuantiteFinale = produit.QuantiteComptage1;
                    produit.Statut = "validé";
                }
                else
                {
                    produit.QuantiteFinale = null;
                    produit.Statut = "écart";
                }
            }

            // 🔥🔥🔥 FIN AJOUT 🔥🔥🔥

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = nouveauProduit
                    ? "Nouvelle ligne créée + quantité ajoutée."
                    : "Quantité ajoutée.",
                produit
            });
        }







        [HttpPut("{id}/quantite-finale")]
        public async Task<IActionResult> UpdateQuantiteFinale(int id, [FromBody] decimal nouvelleQuantiteFinale)
        {
            var produit = await _context.ProduitsInventaire.FindAsync(id);

            if (produit == null)
                return NotFound(new { message = "Produit non trouvé" });

            produit.QuantiteFinale = nouvelleQuantiteFinale;

            // Mise à jour des écarts et statut en fonction de la nouvelle quantité finale et quantité théorique
            var qTheo = produit.QuantiteTheorique ?? 0m;

            produit.EcartTheorique = qTheo - produit.QuantiteComptage1;

            // Comme la quantité finale est mise directement, on considère pas d'écart physique ici
            produit.EcartPhysique = 0;

            // Détermination du statut et justification selon différence avec quantité théorique
            var diff = Math.Abs(produit.EcartTheorique.Value!);

            if (diff == 0)
            {
                produit.Statut = "validé";
                produit.Justification = "quantité validée";
            }
            else if (diff <= 2)
            {
                produit.Statut = "à vérifier";
                produit.Justification = "écart léger, à vérifier";
            }
            else
            {
                produit.Statut = "écart grave";
                produit.Justification = "écart grave, à saisir";
            }

            _context.ProduitsInventaire.Update(produit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quantité finale mise à jour avec succès", produit });
        }

        [HttpPut("{id}/quantite-finale/{comptage}")]
        public async Task<IActionResult> UpdateQuantiteComptage(int id, string comptage, [FromBody] decimal nouvelleQuantite)
        {
            var produit = await _context.ProduitsInventaire.FindAsync(id);

            if (produit == null)
                return NotFound(new { message = "Produit non trouvé" });

            switch (comptage.ToLower())
            {
                case "comptage1":
                    produit.QuantiteComptage1 = nouvelleQuantite;
                    break;
                case "comptage2":
                    produit.QuantiteComptage2 = nouvelleQuantite;
                    break;
                default:
                    return BadRequest(new { message = "Type de comptage invalide. Utilisez 'comptage1' ou 'comptage2'." });
            }

            var qTheo = produit.QuantiteTheorique ?? 0m;
            bool comptage1Rempli = produit.QuantiteComptage1.HasValue;
            bool comptage2Rempli = produit.QuantiteComptage2.HasValue;

            if (comptage1Rempli && comptage2Rempli)
            {
                var q1 = produit.QuantiteComptage1.Value;
                var q2 = produit.QuantiteComptage2.Value;

                var ecartPhys = Math.Abs(q1 - q2);
                var qFinale = Math.Round((q1 + q2) / 2);

                if (ecartPhys > 2)
                {
                    produit.Statut = "écart grave";
                    produit.Justification = "écart grave, à saisir";
                    qFinale = qTheo;
                }
                else if (ecartPhys > 0)
                {
                    produit.Statut = "à vérifier";
                    produit.Justification = "écart léger, à vérifier";
                }
                else
                {
                    produit.Statut = "validé";
                    produit.Justification = "quantité validée";
                }

                produit.EcartTheorique = q1 - qTheo;
                produit.EcartPhysique = q1 - q2;
                produit.QuantiteFinale = qFinale;
            }
            else
            {
                produit.QuantiteFinale = produit.QuantiteComptage1 ?? produit.QuantiteComptage2;
                produit.EcartTheorique = qTheo - (produit.QuantiteComptage1 ?? 0);
                produit.Statut = "en attente";
                produit.Justification = "un seul comptage saisi";
                produit.EcartPhysique = 0;
            }

            _context.ProduitsInventaire.Update(produit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quantité de comptage mise à jour avec succès", produit });
        }
       
        [HttpGet("lignes-erronees-strict/{inventaireId}")]
        public async Task<ActionResult<List<ProduitInventaire>>> GetLignesErroneesStrict(int inventaireId)
        {
            var lignesErronees = await _context.ProduitsInventaire
                .Where(p => p.InventaireId == inventaireId &&

                            (
                             p.QuantiteComptage1 != p.QuantiteTheorique ||
                             p.QuantiteComptage2 != p.QuantiteTheorique))
                .ToListAsync();

            return Ok(lignesErronees);
        }
     
        [HttpGet("lignes-erronees-physique/{inventaireId}")]
        public async Task<ActionResult<List<ProduitInventaire>>> GetLignesErroneesStrictPhysique(int inventaireId)
        {
            var lignesErronees = await _context.ProduitsInventaire
                .Where(p => p.InventaireId == inventaireId &&
                            p.QuantiteComptage1 != p.QuantiteComptage2)
                .ToListAsync();

            return Ok(lignesErronees);
        }
        public class QuantiteUpdateRequest
        {
            public int InventaireId { get; set; }
            public string Ref { get; set; }
            public string Designation { get; set; }
            public decimal Quantite { get; set; }
            public string Username { get; set; }
            public string Depot { get; set; }
            public string? Gamme1 { get; set; }
            public string? NumLot { get; set; } // 👈 ajouté
            public string SuiviStock { get; set; }
            public DateTime? DatePeremption { get; set; }

        }
        public class StatutUpdateDto
        {
            public StatutInventaire Statut { get; set; }
        }
    }
}
