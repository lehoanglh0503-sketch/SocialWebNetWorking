using Microsoft.EntityFrameworkCore;

namespace Social_Website.Models
{
    public class SocialDbContext : DbContext
    {
        public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options) { }
        
        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<PostLike> PostLikes => Set<PostLike>();
        public DbSet<PostReport> PostReports => Set<PostReport>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<BannedWord> BannedWords => Set<BannedWord>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requestor)
                .WithMany()
                .HasForeignKey(f => f.RequestorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Receiver)
                .WithMany()
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.User)
                .WithMany()
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostReport>()
                .HasOne(pr => pr.Post)
                .WithMany()
                .HasForeignKey(pr => pr.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostReport>()
                .HasOne(pr => pr.Reporter)
                .WithMany()
                .HasForeignKey(pr => pr.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
