using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginsAPI.Models
{
	[Table("ccUsers")]
	public class Usuario
	{
		[Key]
		public int User_id { get; set; }

		[Required]
		[MaxLength(30)]
		public string Login { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Nombres { get; set; } = string.Empty;

		[Required]
		[MaxLength(30)]
		public string ApellidoPaterno { get; set; } = string.Empty;

		[MaxLength(30)]
		public string ApellidoMaterno { get; set; } = string.Empty;

		[Required]
		[MaxLength(64)]
		public string Password { get; set; } = string.Empty;

		public int TipoUser_id { get; set; }

		public int Status { get; set; }

		[Required]
		public DateTime FCreate { get; set; }

		public DateTime? LastLoginAttempt { get; set; }

		// Relación con Area
		public int IDArea { get; set; }

		[ForeignKey("IDArea")]
		public Area Area { get; set; }
		public ICollection<Login> Logins { get; set; }

	}
}
