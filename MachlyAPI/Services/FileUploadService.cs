namespace MachlyAPI.Services;

public class FileUploadService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public FileUploadService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        // Validar tamaño
        var maxSizeMB = int.Parse(_configuration["Upload:MaxFileSizeMB"] ?? "5");
        if (file.Length > maxSizeMB * 1024 * 1024)
        {
            throw new InvalidOperationException($"File size exceeds {maxSizeMB}MB limit");
        }

        // Validar extensión
        var allowedExtensions = _configuration.GetSection("Upload:AllowedExtensions").Get<string[]>() ?? new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type {extension} is not allowed");
        }

        // Crear carpeta si no existe
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadPath);

        // Generar nombre único
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        // Guardar archivo
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Retornar URL relativa
        return $"/uploads/{folder}/{fileName}";
    }

    public async Task<List<string>> SaveMultipleFilesAsync(List<IFormFile> files, string folder)
    {
        var urls = new List<string>();
        foreach (var file in files)
        {
            var url = await SaveFileAsync(file, folder);
            urls.Add(url);
        }
        return urls;
    }

    public void DeleteFile(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        var filePath = Path.Combine(_environment.WebRootPath, url.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
