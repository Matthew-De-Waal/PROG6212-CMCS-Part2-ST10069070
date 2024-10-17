using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.DBContext
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Master constructor
        /// </summary>
        /// <param name="options"></param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary>
        /// AccountRecovery table
        /// </summary>
        public DbSet<AccountRecovery> AccountRecovery { get; set; }

        /// <summary>
        /// Document table
        /// </summary>
        public DbSet<Document> Document { get; set; }
    }
}
