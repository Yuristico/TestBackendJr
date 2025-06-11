using System.ComponentModel.DataAnnotations;

namespace LoginsAPI.Data
{
    public class CsvUploadRequest
    {
		[Required]
		public IFormFile File { get; set; }

		[Required]
		public string TargetTable { get; set; } = string.Empty;
	}
}
