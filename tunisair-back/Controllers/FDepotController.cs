using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunisair_back.Models;

namespace tunisair_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FDepotController : ControllerBase
    {
        private readonly DTHDLGContext _context;

        public FDepotController(DTHDLGContext context)
        {
            _context = context;
        }

        // GET: api/Depot
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FDepotDto>>> GetDepots()
        {
            var depots = await _context.FDepots
                .AsNoTracking()
                .Select(d => new FDepotDto
                {
                    DeNo = d.DeNo,
                    DeIntitule = d.DeIntitule
                })
                .ToListAsync();

            return Ok(depots);
        }


        // GET: api/Depot/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FDepot>> GetDepot(int id)
        {
            var depot = await _context.FDepots.FindAsync(id);

            if (depot == null)
            {
                return NotFound();
            }

            return depot;
        }

        // POST: api/Depot
        [HttpPost]
        public async Task<ActionResult<FDepot>> PostDepot(FDepot depot)
        {
            _context.FDepots.Add(depot);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDepot), new { id = depot.DeNo }, depot);
        }



    }
}
