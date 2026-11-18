using System.ComponentModel.DataAnnotations;

namespace StudyRats.Api.Dtos;

/// <summary>Formulário multipart do registro de estudo. A foto é sempre obrigatória.</summary>
public class CreateLogForm
{
    [Required(ErrorMessage = "A foto é obrigatória.")]
    public IFormFile? Photo { get; set; }

    public DateOnly? Date { get; set; }

    public int? QuestionsAnswered { get; set; }
    public int? QuestionsCorrect { get; set; }
    public int? StudyMinutes { get; set; }

    [StringLength(80)] public string? Subject { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}
