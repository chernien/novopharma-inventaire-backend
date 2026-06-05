using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunisair_back.Models;

namespace tunisair_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticleController : ControllerBase
    {

        private readonly DTHDLGContext _context;

        public ArticleController(DTHDLGContext context)
        {
            _context = context;
        }


        [HttpGet("{ref}")]
        public async Task<IActionResult> GetArticleByRef(string @ref)
        {
            var article = await _context.FArticles
                .FirstOrDefaultAsync(a => a.ArRef == @ref);

            if (article == null)
                return NotFound();

            return Ok(article);
        }

        [HttpGet("check-serie")]
        public IActionResult CheckSerie(string refArticle, string nSerie)
        {
            if (string.IsNullOrEmpty(refArticle) || string.IsNullOrEmpty(nSerie))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Paramètres refArticle et nSerie obligatoires"
                });
            }

            // Vérification dans ProduitInventaire uniquement
            var produit = _context.ProduitsInventaire
                .Where(p => p.Ref == refArticle && p.Nserie == nSerie)
                .Select(p => new
                {
                    refArticle = p.Ref,
                    nSerie = p.Nserie,
                    gamme1 = p.Gamme1,
                    datePeremption = p.DatePeremption   // 🔥🔥🔥 AJOUT ICI

                })
                .FirstOrDefault();

            if (produit == null)
            {
                return Ok(new
                {
                    status = false,
                    message = "Numéro de série introuvable dans ProduitInventaire",
                    refArticle,
                    nSerie,
                    gammeValide = false,
                    gamme1 = (string)null
                });
            }

            return Ok(new
            {
                status = true,
                message = "Numéro de série trouvé et valide",
                produit.refArticle,
                produit.nSerie,
                produit.gamme1,
                produit.datePeremption,
                gammeValide = !string.IsNullOrEmpty(produit.gamme1)
            });
        }


        [HttpGet("check/{value}")]
        public async Task<IActionResult> CheckArticle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest(new { message = "Valeur invalide." });

            string lowerValue = value.ToLower();

            // 1️⃣ Recherche directe dans F_ARTICLE
            var article = await _context.FArticles
                .Where(a =>
                    a.ArRef.ToLower() == lowerValue ||
                    a.ArCodeBarre == value ||
                    a.ArDesign.ToLower().Contains(lowerValue))
                .Select(a => new
                {
                    a.ArRef,
                    a.ArDesign,
                    a.ArPunet,
                    a.ArCodeBarre,
                    a.ArGamme1,
                    a.ArSuiviStock
                })
                .FirstOrDefaultAsync();

            // 2️⃣ Si non trouvé → recherche dans F_ARTENUMREF
            if (article == null)
            {
                var enumRef = await _context.FArtEnumRefs
                    .Where(e => e.AE_CodeBarre == value)
                    .Select(e => e.AR_Ref)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(enumRef))
                {
                    article = await _context.FArticles
                        .Where(a => a.ArRef == enumRef)
                        .Select(a => new
                        {
                            a.ArRef,
                            a.ArDesign,
                            a.ArPunet,
                            a.ArCodeBarre,
                            a.ArGamme1,
                            a.ArSuiviStock
                        })
                        .FirstOrDefaultAsync();
                }
            }

            // 3️⃣ Toujours introuvable
            if (article == null)
                return NotFound(new { message = "Article introuvable." });

            // 4️⃣ Détermination du type de gestion
            bool gereLot = article.ArSuiviStock == 1 || article.ArSuiviStock == 5;
            bool gereGamme = article.ArGamme1 == 1;

            string typeGestion =
                gereLot && gereGamme ? "lot+gamme" :
                gereLot ? "lot" :
                gereGamme ? "gamme" :
                "normal";

            return Ok(new
            {
                article,
                typeGestion
            });
        }

        [HttpGet]

        // GET: api/Article/CheckGamme?arRef=XXX&gamme=YYY
        [HttpGet("CheckGamme")]
        public async Task<IActionResult> CheckGamme(string arRef, string gamme)
        {
            if (string.IsNullOrEmpty(arRef) || string.IsNullOrEmpty(gamme))
                return BadRequest("Référence et gamme sont obligatoires.");

            // Récupérer toutes les gammes de l'article
            var gammesArticle = await _context.FArtGammes
                .Where(f => f.AR_Ref == arRef)
                .Select(f => f.EG_Enumere)
                .ToListAsync();

            if (gammesArticle == null || !gammesArticle.Any())
                return NotFound("Aucune gamme trouvée pour cet article.");

            // Vérifier si la gamme saisie par l'utilisateur existe (ignore la casse et trim)
            var gammeValide = gammesArticle
                .Any(g => string.Equals(g.Trim(), gamme.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!gammeValide)
                return NotFound("La gamme saisie est introuvable pour cet article.");

            // Retourne la ref et la gamme validée
            return Ok(new { ArRef = arRef, Gamme = gamme });
        }

        [HttpGet("suggest-gamme")]
        public IActionResult SuggestGamme([FromQuery] string arRef, [FromQuery] string? query = "")
        {
            // Base query : toutes les gammes de l'article
            var gammeQuery = _context.FArtGammes
                .Where(g => g.AR_Ref.ToLower() == arRef.ToLower());

            // Si utilisateur tape un texte → filtrer
            if (!string.IsNullOrWhiteSpace(query))
            {
                gammeQuery = gammeQuery
                    .Where(g => g.EG_Enumere.ToLower().StartsWith(query.ToLower()));
            }

            // Plus de Take(10) → retour complet
            var suggestions = gammeQuery
                .Select(g => g.EG_Enumere)
                .Distinct()
                .ToList();

            return Ok(suggestions);
        }




        // DTO mis à jour pour inclure ArSuiviStock
        public class ArticleCheckDto
        {
            public string ArRef { get; set; }
            public string ArDesign { get; set; }
            public decimal? ArPunet { get; set; }
            public string ArCodeBarre { get; set; }

            public short? ArGamme1 { get; set; }  // <-- nouveau champ
        }

    }
}
