namespace NexusCRM.Infrastructure.Storage;

public interface IFileStorage
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(fileName);
        var relative = Path.Combine(DateTime.UtcNow.ToString("yyyy/MM/dd"), $"{Guid.NewGuid():N}_{safeName}");
        var fullPath = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_root, path);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_root, path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
