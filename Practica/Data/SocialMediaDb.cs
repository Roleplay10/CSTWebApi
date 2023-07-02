using Microsoft.EntityFrameworkCore;
using Practica.Data.Entities;

namespace Practica.Data
{
    public class SocialMediaDb : DbContext
    {
        public SocialMediaDb(DbContextOptions<SocialMediaDb> options) : base(options)
        {
            
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Reaction> Reactions { get; set; }
    }
}
