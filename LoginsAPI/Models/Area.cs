using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginsAPI.Models
{
	[Table("ccRIACat_Areas")]
	public class Area
	{
		[Key]
		public int IDArea { get; set; }

		[Required]
		[MaxLength(30)]
		public string AreaName { get; set; } = string.Empty;

		public int StatusArea { get; set; }
		
		[Required]
		public DateTime CreateDate { get; set; }

		public ICollection<Usuario> Usuarios { get; set; }

	}
}