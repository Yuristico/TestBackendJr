using Humanizer;
using LoginsAPI.Data;
using LoginsAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginsAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LoginsController : ControllerBase
	{
		private readonly AppDbContext _context;

		public LoginsController(AppDbContext context)
		{
			_context = context;
		}

		// GET: api/logins
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Login>>> GetLogins()
		{
			return await _context.ccloglogin.ToListAsync();
		}

		// POST: api/logins
		[HttpPost]
		public async Task<ActionResult<Login>> PostLogin([FromForm] PutLoginRequest login)
		{
			// Verificar si el usuario existe
			var userExists = await _context.ccUsers.AnyAsync(u => u.User_id == login.User_id);
			if (!userExists)
				return BadRequest("El usuario no existe.");

			// Validar que la fecha no sea en el futuro
			if (login.Fecha > DateTime.Now)
				return BadRequest("La fecha del registro no puede estar en el futuro.");

			// Obtener el último registro del usuario
			var ultimo = await _context.ccloglogin
				.Where(l => l.User_id == login.User_id)
				.OrderByDescending(l => l.Fecha)
				.FirstOrDefaultAsync();

			// Si hay un registro previo, validar contra él
			if (ultimo != null)
			{
				if (login.Fecha <= ultimo.Fecha)
					return BadRequest("La fecha del nuevo registro debe ser posterior al último registro del usuario.");

				if (ultimo.TipoMov == login.TipoMov)
				{
					if (login.TipoMov)
						return BadRequest("Ya hay una sesión activa sin logout.");
					else
						return BadRequest("No puedes registrar dos logouts seguidos sin login.");
				}
			}
			else
			{
				// No hay ningún registro anterior, solo se permite login como primer registro
				if (!login.TipoMov)
					return BadRequest("No puedes registrar un logout sin un login previo.");
			}

			// Crear el nuevo registro
			var nuevoRegistro = new Login
			{
				User_id = login.User_id,
				Extension = login.Extension,
				TipoMov = login.TipoMov,
				Fecha = login.Fecha
			};

			_context.ccloglogin.Add(nuevoRegistro);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetLogin), new { id = nuevoRegistro.Log_id }, nuevoRegistro);
		}

		// GET: api/logins/id
		[HttpGet("{id}")]
		public async Task<ActionResult<Login>> GetLogin(int id)
		{
			var login = await _context.ccloglogin.FindAsync(id);

			if (login == null)
			{
				return NotFound();
			}

			return login;
		}

		// PUT: api/logins/id
		[HttpPut("{id}")]
		public async Task<IActionResult> PutLogin(int id, [FromForm] UpdateLoginRequest request)
		{
			var existingLogin = await _context.ccloglogin.FindAsync(id);
			if (existingLogin == null)
				return NotFound("Registro no encontrado.");

			var userId = existingLogin.User_id;

			// Validar cronología si se quiere cambiar la Fecha
			if (request.Fecha.HasValue)
			{
				var nuevaFecha = request.Fecha.Value;

				if (existingLogin.TipoMov) // Es un login
				{
					var logoutPosterior = await _context.ccloglogin
						.Where(r => r.User_id == userId && !r.TipoMov && r.Fecha > existingLogin.Fecha && r.Log_id != existingLogin.Log_id)
						.OrderBy(r => r.Fecha)
						.FirstOrDefaultAsync();

					if (logoutPosterior != null && nuevaFecha >= logoutPosterior.Fecha)
						return BadRequest("La nueva fecha del login no puede ser posterior o igual al logout más cercano.");
				}
				else // Es un logout
				{
					var loginAnterior = await _context.ccloglogin
						.Where(r => r.User_id == userId && r.TipoMov && r.Fecha < existingLogin.Fecha && r.Log_id != existingLogin.Log_id)
						.OrderByDescending(r => r.Fecha)
						.FirstOrDefaultAsync();

					if (loginAnterior != null && nuevaFecha <= loginAnterior.Fecha)
						return BadRequest("La nueva fecha del logout no puede ser anterior o igual al login más cercano.");
				}

				existingLogin.Fecha = nuevaFecha;
			}

			if (request.Extension.HasValue)
			{
				existingLogin.Extension = request.Extension.Value;
			}

			await _context.SaveChangesAsync();
			return Ok("Registro actualizado correctamente.");
		}


		// DELETE: api/logins/id
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteLogPair(int id)
		{
			var registro = await _context.ccloglogin.FindAsync(id);

			if (registro == null)
				return NotFound("Registro no encontrado.");

			var userId = registro.User_id;
			var fecha = registro.Fecha;
			var tipoMov = registro.TipoMov; // true = login, false = logout

			Login? registroPar = null;

			if (tipoMov) // Es un login, busca el logout posterior
			{
				registroPar = await _context.ccloglogin
					.Where(r => r.User_id == userId && r.TipoMov == false && r.Fecha > fecha)
					.OrderBy(r => r.Fecha)
					.FirstOrDefaultAsync();
			}
			else // Es un logout, busca el login anterior
			{
				registroPar = await _context.ccloglogin
					.Where(r => r.User_id == userId && r.TipoMov == true && r.Fecha < fecha)
					.OrderByDescending(r => r.Fecha)
					.FirstOrDefaultAsync();
			}

			_context.ccloglogin.Remove(registro);

			if (registroPar != null)
				_context.ccloglogin.Remove(registroPar);

			await _context.SaveChangesAsync();

			return Ok("Registro y su par eliminados correctamente.");
		}

	}
}
