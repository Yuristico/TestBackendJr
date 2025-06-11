using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginsAPI.Models
{
	[Table("ccloglogin")]
	public class Login
	{
		[Key]
		public int Log_id { get; set; }
		public int User_id { get; set; } // Relación con Usuario
		[ForeignKey("User_id")]
		public Usuario UsuarioRef { get; set; }
		public int Extension { get; set; }
		public bool TipoMov { get; set; } // 1 = login, 0 = logout	
		public DateTime Fecha { get; set; }
	}
}
