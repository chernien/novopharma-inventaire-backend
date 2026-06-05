using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace tunisair_back.Models
{
    public class FDocLigne
    {

        public short? DO_Domaine { get; set; }
        public short? DO_Type { get; set; }
        public int DE_No { get; set; }
        public short? DL_MvtStock { get; set; }

        public string? DO_Piece { get; set; }

        public string? AR_REF { get; set; }

        public string? DL_Design { get; set; }

        public DateTime? DO_Date { get; set; }
        public decimal DL_QTE { get; set; }

    }

}
