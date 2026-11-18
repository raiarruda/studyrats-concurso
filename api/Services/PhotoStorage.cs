using Microsoft.Extensions.Options;

namespace StudyRats.Api.Services;

public class PhotoStorageOptions
{
    public string Directory { get; set; } = "uploads";
    public long MaxBytes { get; set; } = 8 * 1024 * 1024;
}

public interface IPhotoStorage
{
    /// <summary>Valida e grava a foto. Retorna o nome do arquivo ou uma mensagem de erro.</summary>
    Task<(string? FileName, string? Error)> SaveAsync(IFormFile file, CancellationToken ct);
    void Delete(string fileName);
}

public class LocalPhotoStorage(IOptions<PhotoStorageOptions> options) : IPhotoStorage
{
    private readonly PhotoStorageOptions _o = options.Value;

    public async Task<(string? FileName, string? Error)> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return (null, "A foto está vazia.");
        if (file.Length > _o.MaxBytes) return (null, $"A foto excede {_o.MaxBytes / 1024 / 1024} MB.");

        // Não confia no Content-Type nem na extensão enviados: confere os bytes iniciais.
        var header = new byte[12];
        await using (var s = file.OpenReadStream())
        {
            var read = await s.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            if (read < header.Length) return (null, "Arquivo de imagem inválido.");
        }

        var ext = DetectExtension(header);
        if (ext is null) return (null, "Formato não suportado. Use JPEG, PNG ou WebP.");

        Directory.CreateDirectory(_o.Directory);
        var name = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(_o.Directory, name);
        await using var dst = File.Create(path);
        await using var src = file.OpenReadStream();
        await src.CopyToAsync(dst, ct);
        return (name, null);
    }

    public void Delete(string fileName)
    {
        // O nome vem do banco, mas garante que nunca sai do diretório de uploads.
        var safe = Path.GetFileName(fileName);
        var path = Path.Combine(_o.Directory, safe);
        if (File.Exists(path)) File.Delete(path);
    }

    private static string? DetectExtension(byte[] h)
    {
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";
        if (h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F'
            && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";
        return null;
    }
}
