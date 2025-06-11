using Microsoft.EntityFrameworkCore;
using LoginsAPI.Models;

namespace LoginsAPI.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options) { }

		public DbSet<Login> ccloglogin { get; set; }
		public DbSet<Area> ccRIACat_Areas { get; set; }
		public DbSet<Usuario> ccUsers { get; set; }


		// Agrega otras tablas si quieres
		//public DbSet<User> Users { get; set; }
	}
}