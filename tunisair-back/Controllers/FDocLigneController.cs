//using System.Linq;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using tunisair_back.Models;

//namespace tunisair_back.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class FDocLigneController : ControllerBase
//    {
//        private readonly DTHDLGContext _context;

//        public FDocLigneController(DTHDLGContext context)
//        {
//            _context = context;
//        }

//        // GET: api/FDocligne
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<FDocLigne>>> GetFilteredDocLignes()
//        {
//            return await _context.FDocLignes
//                .Where(d => d.DO_Domaine == 2 && d.DO_Type == 23)
//                .ToListAsync();
//        }
//        [HttpGet("{userId}")]
//        public async Task<ActionResult> GetDocLignesByUser(Guid userId)
//        {
//            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
//            if (!userExists)
//                return NotFound("Utilisateur introuvable.");

//            var result = await (
//                from lien in _context.FDocLigneAffectationUsers
//                join doc in _context.FDocLigneAffectations on lien.LigneAffectationId equals doc.Id
//                join depot in _context.FDepots on doc.DE_No equals depot.DeNo
//                where lien.UserId.ToString() == userId.ToString() // CORRECTION IMPORTANTE
//                select new FDocLigneAffectationDto
//                {
//                    Id = doc.Id,
//                    DO_Domaine = doc.DO_Domaine,
//                    DO_Type = doc.DO_Type,
//                    DO_Piece = doc.DO_Piece,
//                    AR_REF = doc.AR_REF,
//                    DL_Design = doc.DL_Design,
//                    DO_Date = doc.DO_Date,
//                    DL_QTE = doc.DL_QTE,
//                    DE_No = doc.DE_No,
//                    QuantiteRecu = doc.QuantiteRecu,
//                    Statut = doc.Statut,
//                    Justification = doc.Justification,
//                    DepotNom = depot.DeIntitule,
//                }
//            ).ToListAsync();

//            return Ok(result);
//        }

//        [HttpGet("{userId}/do_piece")]
//        public async Task<ActionResult> GetFilteredDocLignesdopiece(Guid userId)
//        {
//            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
//            if (!userExists)
//                return NotFound("Utilisateur introuvable.");

//            var filteredDocs = await (
//                from doc in _context.FDocLigneAffectations
//                join ud in _context.UserDepots on doc.DE_No equals ud.DeNo
//                join depot in _context.FDepots on doc.DE_No equals depot.DeNo
//                where ud.UserId == userId
//                      && doc.DO_Domaine == 2
//                      && doc.DO_Type == 23
//                      && doc.DL_MvtStock == 1
//                select new
//                {
//                    Ligne = doc,
//                    doc.DO_Piece,
//                    doc.DO_Date,
//                    doc.DE_No,
//                    DepotNom = depot.DeIntitule
//                }
//            ).ToListAsync();

//            var grouped = filteredDocs
//                .GroupBy(d => d.DO_Piece)
//                .Select(g =>
//                {
//                    // Lignes avec leur statut propre
//                    var lignes = g.Select(x => new
//                    {
//                        x.Ligne.Id,
//                        x.Ligne.AR_REF,
//                        x.Ligne.DL_QTE,
//                        x.Ligne.DE_No,
//                        StatutLigne = x.Ligne.Statut
//                    }).ToList();

//                    // Statut global de la pièce
//                    string statutPiece;
//                    if (lignes.Any(l => l.StatutLigne?.Trim().ToLower() == "quantité manquante"))
//                        statutPiece = "Non valide";
//                    else if (lignes.Any(l => l.StatutLigne?.Trim().ToLower() == "en attente"))
//                        statutPiece = "En attente";
//                    else
//                        statutPiece = "Validé";

//                    return new
//                    {
//                        DO_Piece = g.Key,
//                        DO_Date = g.First().DO_Date,
//                        DepotNom = g.First().DepotNom,
//                        StatutPiece = statutPiece,
//                        Lignes = lignes
//                    };
//                })
//                .ToList();

//            return Ok(grouped);
//        }


//        // GET: api/FDocligne/{domaine}/{type}/{piece}/{deNo}
//        [HttpGet("{domaine}/{type}/{piece}/{deNo}")]
//        public async Task<ActionResult<FDocLigne>> GetFDocligne(short domaine, short type, string piece, short deNo)
//        {
//            var docligne = await _context.FDocLignes.FindAsync(domaine, type, piece, deNo);

//            if (docligne == null)
//                return NotFound();

//            return docligne;
//        }

//        // POST: api/FDocligne
//        [HttpPost]
//        public async Task<ActionResult<FDocLigne>> PostFDocligne(FDocLigne docligne)
//        {
//            _context.FDocLignes.Add(docligne);
//            await _context.SaveChangesAsync();

//            return CreatedAtAction(nameof(GetFDocligne), new
//            {
//                domaine = docligne.DO_Domaine,
//                type = docligne.DO_Type,
//                piece = docligne.DO_Piece,
//                deNo = docligne.DE_No
//            }, docligne);
//        }
//        [HttpPut("{id}/quantite")]
//        public async Task<IActionResult> UpdateQuantiteRecu(int id, [FromBody] int quantiteRecu)
//        {
//            var ligne = await _context.FDocLigneAffectations.FindAsync(id);
//            if (ligne == null)
//                return NotFound("Ligne non trouvée.");

//            ligne.QuantiteRecu = quantiteRecu;

//            if (ligne.QuantiteRecu == ligne.DL_QTE)
//                ligne.Statut = "Validé";
//            else if (ligne.QuantiteRecu < ligne.DL_QTE)
//                ligne.Statut = "Quantité manquante";
//            else
//                ligne.Statut = "Quantité en excès";

//            await _context.SaveChangesAsync();

//            return NoContent();
//        }
//        [HttpPut("{id}/justification/{justif}")]
//        public async Task<IActionResult> UpdateJustification(int id, string justif)
//        {
//            if (string.IsNullOrWhiteSpace(justif))
//                return BadRequest("Justification invalide.");

//            var ligne = await _context.FDocLigneAffectations.FindAsync(id);
//            if (ligne == null)
//                return NotFound("Ligne non trouvée.");

//            ligne.Justification = justif;
//            await _context.SaveChangesAsync();

//            return NoContent();
//        }

//        // PUT: api/FDocligne/{domaine}/{type}/{piece}/{deNo}
//        [HttpPut("{domaine}/{type}/{piece}/{deNo}")]
//        public async Task<IActionResult> PutFDocligne(short domaine, short type, string piece, short deNo, FDocLigne updatedDocligne)
//        {
//            if (domaine != updatedDocligne.DO_Domaine ||
//                type != updatedDocligne.DO_Type ||
//                piece != updatedDocligne.DO_Piece ||
//                deNo != updatedDocligne.DE_No)
//            {
//                return BadRequest("Les clés primaires ne correspondent pas.");
//            }

//            _context.Entry(updatedDocligne).State = EntityState.Modified;

//            try
//            {
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                var exists = await _context.FDocLignes.AnyAsync(e =>
//                    e.DO_Domaine == domaine &&
//                    e.DO_Type == type &&
//                    e.DO_Piece == piece &&
//                    e.DE_No == deNo);

//                if (!exists)
//                    return NotFound();

//                throw;
//            }

//            return NoContent();
//        }

//        // DELETE: api/FDocligne/{domaine}/{type}/{piece}/{deNo}
//        [HttpDelete("{domaine}/{type}/{piece}/{deNo}")]
//        public async Task<IActionResult> DeleteFDocligne(short domaine, short type, string piece, short deNo)
//        {
//            var docligne = await _context.FDocLignes.FindAsync(domaine, type, piece, deNo);
//            if (docligne == null)
//                return NotFound();

//            _context.FDocLignes.Remove(docligne);
//            await _context.SaveChangesAsync();

//            return NoContent();
//        }
//    }
//}
