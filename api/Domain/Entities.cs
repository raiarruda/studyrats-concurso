namespace StudyRats.Api.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Métricas escolhidas na criação. Não há endpoint de edição: ficam imutáveis.
    public bool TrackQuestions { get; set; }
    public bool TrackCorrect { get; set; }
    public bool TrackTime { get; set; }

    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Member> Members { get; set; } = [];
}

public enum MemberRole { Member = 0, Owner = 1 }

public class Member
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class StudyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    public string PhotoFileName { get; set; } = "";   // obrigatório

    public int? QuestionsAnswered { get; set; }       // null se o grupo não usa
    public int? QuestionsCorrect { get; set; }        // null se o grupo não usa
    public int? StudyMinutes { get; set; }            // null se o grupo não usa

    public string? Subject { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
