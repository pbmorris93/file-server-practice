namespace CodeSignalPractice.Lib;

public class FileServerWithTtl : FileServer
{
    private List<FileWithTtl> FilesWithTtl { get; set; } = [];
    private List<FileWithTtl> History { get; set; } = [];
    
    public void FileUploadAt(string fileName, int size, DateTimeOffset timestamp, int? ttl = null)
    {
        var exists = FilesWithTtl.Exists(x => x.Name == fileName);

        if (exists)
        {
            throw new InvalidOperationException($"File '{fileName}' already exists");
        }

        var file = new FileWithTtl(name: fileName, size: size, timestamp: timestamp, ttl: ttl);
        
        FilesWithTtl.Add(file);
        History.Add(file);
    }
    
    public int? FileGetAt(string fileName, DateTimeOffset timestamp)
    {
        return FilesWithTtl.FirstOrDefault(x => x.Name == fileName && x.Timestamp == timestamp)?.Size;
    }
    
    public void FileCopyAt(string source, string destination, DateTimeOffset timestamp)
    {
        var file = FilesWithTtl.FirstOrDefault(x => x.Name == source && x.Timestamp == timestamp);

        if (file is null)
        {
            throw new FileNotFoundException($"File '{source}' not found");
        }
        
        var index = FilesWithTtl.IndexOf(file);
        FilesWithTtl[index].UpdateName(destination);
    }
    
    public List<FileWithTtl> FileSearchAt(string prefix, DateTimeOffset timestamp)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentNullException(nameof(prefix), "prefix cannot be null or empty");
        }
        
        return FilesWithTtl.Where(x => x.Name.StartsWith(prefix) && x.Timestamp == timestamp && x.FileIsAlive)
            .OrderByDescending(x => x.Size)
            .ThenByDescending(x => x.Name)
            .Take(10)
            .ToList();
    }
    
    public void Rollback(DateTimeOffset timestamp)
    {
        // Clear current state
        FilesWithTtl.Clear();

        // Rollback to the specified timestamp
        foreach (var entry in History.Where(h => h.Timestamp <= timestamp).ToList())
        {
            var exists = FilesWithTtl.Exists(x => x.Equals(entry));

            if (!exists)
            {
                FileUploadAt(fileName: entry.Name, size: entry.Size, timestamp: entry.Timestamp, ttl: entry.Ttl);
            }
        }
    }
}

public class FileWithTtl : File
{
    public FileWithTtl(string name, int size, DateTimeOffset timestamp, int? ttl = null) 
        : base(name, size)
    {
        Timestamp = timestamp;
        Ttl = ttl;
    }
    
    public DateTimeOffset Timestamp { get; set; }
    public int? Ttl { get; set; }

    public bool FileIsAlive => DateTimeOffset.UtcNow.Subtract(Timestamp).TotalSeconds < Ttl;
    
    public override string ToString() => $"File Name: {Name} | Size: ({Size}) | Timestamp: ({Timestamp}) | TTL: {Ttl} | IsAlive: {FileIsAlive}";
}