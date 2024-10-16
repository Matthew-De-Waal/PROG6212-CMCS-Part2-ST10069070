using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AccountRecovery> AccountRecovery { get; set; }
    }
}
