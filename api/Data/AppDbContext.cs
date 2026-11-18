using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Domain;

namespace StudyRats.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<StudyLog> StudyLogs => Set<StudyLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(200);
        });

        b.Entity<Group>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.Code).HasMaxLength(12);
        });

        b.Entity<Member>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.UserId });
            e.HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<StudyLog>(e =>
        {
            e.HasIndex(x => new { x.GroupId, x.UserId, x.Date });
            e.HasIndex(x => new { x.GroupId, x.CreatedAt });
            e.Property(x => x.PhotoFileName).HasMaxLength(100);
            e.Property(x => x.Subject).HasMaxLength(80);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);

            // Defesa em profundidade: acertos nunca maiores que questões.
            e.ToTable(t => t.HasCheckConstraint("ck_log_correct_le_answered",
                "\"QuestionsCorrect\" IS NULL OR (\"QuestionsAnswered\" IS NOT NULL AND \"QuestionsCorrect\" <= \"QuestionsAnswered\")"));
        });
    }
}
