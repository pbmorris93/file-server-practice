namespace CodeSignalPractice.Lib;

public class FileServer
{
    private List<File> Files { get; } = [];
    
    public void FileUpload(string fileName, int size)
    {
        var exists = Files.Exists(x => x.Name == fileName);

        if (exists)
        {
            throw new InvalidOperationException($"File '{fileName}' already exists");
        }
        
        Files.Add(new File(name: fileName, size: size));
    }

    public int? FileGet(string fileName)
    {
        return Files.FirstOrDefault(x => x.Name == fileName)?.Size;
    }

    public void FileCopy(string source, string destination)
    {
        var file = Files.FirstOrDefault(x => x.Name == source);

        if (file is null)
        {
            throw new FileNotFoundException($"File '{source}' not found");
        }
        
        var index = Files.IndexOf(file);
        Files[index].UpdateName(destination);
    }

    public List<File> FileSearch(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentNullException(nameof(prefix), "prefix cannot be null or empty");
        }
        return Files.Where(x => x.Name.StartsWith(prefix))
            .OrderByDescending(x => x.Size)
            .ThenByDescending(x => x.Name)
            .Take(10)
            .ToList();
    }
}

public class File
{
    public File(string name, int size)
    {
        Name = name;
        Size = size;
    }

    public void UpdateName(string name)
    {
        Name = name;
    }
    
    public string Name { get; private set; }
    public int Size { get; }
    
    public override string ToString() => $"{Name} ({Size})";

    public override bool Equals(object? obj)
    {
        var other = obj as File;

        return Name == other?.Name && Size == other.Size;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Size);
    }
}