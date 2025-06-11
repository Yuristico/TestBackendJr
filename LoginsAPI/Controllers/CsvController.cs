using LoginsAPI.Data;
using LoginsAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LoginsAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CsvController : ControllerBase
	{
		private readonly AppDbContext _context;

		public CsvController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("export/csv")]
		public async Task<IActionResult> ExportCsv()
		{
			var logins = await _context.ccloglogin
				.Include(l => l.UsuarioRef)
				.ThenInclude(u => u.Area)
				.OrderBy(l => l.UsuarioRef.User_id)
				.ThenBy(l => l.Fecha)
				.ToListAsync();

			var userSessions = new Dictionary<int, List<(DateTime login, DateTime logout)>>();

			foreach (var log in logins)
			{
				var userId = log.UsuarioRef.User_id;

				if (!userSessions.ContainsKey(userId))
					userSessions[userId] = new List<(DateTime, DateTime)>();

				if (log.TipoMov) // Login
				{
					userSessions[userId].Add((log.Fecha, DateTime.MinValue));
				}
				else // Logout
				{
					var session = userSessions[userId].LastOrDefault(s => s.logout == DateTime.MinValue);
					if (session.login != DateTime.MinValue)
					{
						var index = userSessions[userId].IndexOf(session);
						userSessions[userId][index] = (session.login, log.Fecha);
					}
				}
			}

			var sb = new StringBuilder();
			sb.AppendLine("Login,NombreCompleto,Area,HorasTrabajadas");

			foreach (var userId in userSessions.Keys)
			{
				var user = await _context.ccUsers
					.Include(u => u.Area)
					.FirstOrDefaultAsync(u => u.User_id == userId);

				if (user == null) continue;

				double totalHoras = 0;

				foreach (var (login, logout) in userSessions[userId])
				{
					if (logout != DateTime.MinValue)
					{
						totalHoras += (logout - login).TotalHours;
					}
				}

				string linea = $"{user.Login}," +
							   $"{user.Nombres} {user.ApellidoPaterno} {user.ApellidoMaterno}," +
							   $"{user.Area?.AreaName}," +
							   $"{Math.Round(totalHoras, 2)}";

				sb.AppendLine(linea);
			}

			var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
			return File(csvBytes, "text/csv", "reporte_logins.csv");
		}

		[HttpPost("upload")]
		public async Task<IActionResult> UploadCsv([FromForm] CsvUploadRequest request)
		{
			if (request.File == null || request.File.Length == 0)
				return BadRequest("No se envió ningún archivo.");

			if (string.IsNullOrWhiteSpace(request.TargetTable))
				return BadRequest("Debes indicar la tabla destino.");

			using var reader = new StreamReader(request.File.OpenReadStream());
			var csvContent = await reader.ReadToEndAsync();

			try
			{
				switch (request.TargetTable.ToLower())
				{
					case "ccusers":
						var users = ParseUsersCsv(csvContent);
						_context.ccUsers.AddRange(users);
						break;

					case "ccriacat_areas":
						var areas = ParseAreasCsv(csvContent);
						_context.ccRIACat_Areas.AddRange(areas);
						break;

					case "ccloglogin":
						var logins = ParseLoginsCsv(csvContent);
						_context.ccloglogin.AddRange(logins);
						break;

					default:
						return BadRequest("Nombre de tabla inválido.");
				}

				await _context.SaveChangesAsync();
				return Ok("Datos insertados correctamente.");
			}
			catch (Exception ex)
			{
				return BadRequest("Error al procesar el archivo: " + ex.Message);
			}
		}

		private List<Usuario> ParseUsersCsv(string csv)
		{
			var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1); // omitir encabezado
			var list = new List<Usuario>();

			foreach (var line in lines)
			{
				var parts = line.Split(',');

				var user = new Usuario
				{
					Login = parts[0].Trim(),
					Nombres = parts[1].Trim(),
					ApellidoPaterno = parts[2].Trim(),
					ApellidoMaterno = parts[3].Trim(),
					Password = parts[4].Trim(),
					TipoUser_id = int.Parse(parts[5]),
					Status = int.Parse(parts[6]),
					FCreate = DateTime.Parse(parts[7]),
					IDArea = int.Parse(parts[8]),
					LastLoginAttempt = DateTime.Parse(parts[9])
				};

				list.Add(user);
			}

			return list;
		}

		private List<Area> ParseAreasCsv(string csv)
		{
			var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1);
			var list = new List<Area>();

			foreach (var line in lines)
			{
				var parts = line.Split(',');

				var area = new Area
				{
					AreaName = parts[0].Trim(),
					StatusArea = int.Parse(parts[1]),
					CreateDate = DateTime.Parse(parts[2])
				};

				list.Add(area);
			}

			return list;
		}

		private List<Login> ParseLoginsCsv(string csv)
		{
			var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1);
			var list = new List<Login>();

			foreach (var line in lines)
			{
				var parts = line.Split(',');

				var login = new Login
				{
					User_id = int.Parse(parts[0]),
					Extension = int.Parse(parts[1]),
					TipoMov = Convert.ToBoolean(int.Parse(parts[2])), // true para login, false para logout
					Fecha = DateTime.Parse(parts[3])
					
				};

				list.Add(login);
			}

			return list;
		}

	}
}
