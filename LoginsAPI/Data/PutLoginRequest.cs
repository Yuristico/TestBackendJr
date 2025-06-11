using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginsAPI.Data
{
	public class PutLoginRequest
	{
		[Required]
		public int User_id { get; set; } // Relación con Usuario
		[ForeignKey("User_id")]
		[Required]
		public int Extension { get; set; }
		[Required]
		public bool TipoMov { get; set; } // 1 = login, 0 = logout	
		[Required]
		public DateTime Fecha { get; set; }
	}
}

