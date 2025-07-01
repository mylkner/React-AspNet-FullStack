using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class AppDbContext(DbContextOptions<DbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
}
