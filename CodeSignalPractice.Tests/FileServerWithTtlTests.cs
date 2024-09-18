using CodeSignalPractice.Lib;

namespace CodeSignalPractice.Tests;

[TestFixture]
public class FileServerWithTtlTests
{
    private FileServerWithTtl _fileServer;
    private DateTimeOffset _fixedTimestamp;

    [SetUp]
    public void SetUp()
    {
        _fileServer = new FileServerWithTtl();
        _fixedTimestamp = DateTimeOffset.UtcNow;
    }

    [Test]
    public void FileUploadAt_ShouldUploadNewFileSuccessfully()
    {
        // Arrange
        const string fileName = "testfile.txt";
        const int size = 1024;
        int? ttl = 3600;

        // Act
        _fileServer.FileUploadAt(fileName, size, _fixedTimestamp, ttl);

        // Assert
        var retrievedSize = _fileServer.FileGetAt(fileName, _fixedTimestamp);
        Assert.That(retrievedSize, Is.EqualTo(size));
    }

    [Test]
    public void FileUploadAt_DuplicateFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string fileName = "duplicate.txt";
        const int size = 2048;
        _fileServer.FileUploadAt(fileName, size, _fixedTimestamp);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _fileServer.FileUploadAt(fileName, size, _fixedTimestamp));
        Assert.That(ex.Message, Is.EqualTo($"File '{fileName}' already exists"));
    }

    [Test]
    public void FileGetAt_ExistingFile_ShouldReturnFileSize()
    {
        // Arrange
        const string fileName = "existing.txt";
        const int size = 4096;
        _fileServer.FileUploadAt(fileName, size, _fixedTimestamp);

        // Act
        var retrievedSize = _fileServer.FileGetAt(fileName, _fixedTimestamp);

        // Assert
        Assert.That(retrievedSize, Is.Not.Null);
        Assert.That(retrievedSize.Value, Is.EqualTo(size));
    }

    [Test]
    public void FileGetAt_NonExistingFile_ShouldReturnNull()
    {
        // Arrange
        const string fileName = "nonexistent.txt";

        // Act
        var retrievedSize = _fileServer.FileGetAt(fileName, _fixedTimestamp);

        // Assert
        Assert.That(retrievedSize, Is.Null);
    }

    [Test]
    public void FileCopyAt_ExistingFile_ShouldCopySuccessfully()
    {
        // Arrange
        const string sourceFile = "source.txt";
        const string destinationFile = "destination.txt";
        const int size = 512;
        _fileServer.FileUploadAt(sourceFile, size, _fixedTimestamp);

        // Act
        _fileServer.FileCopyAt(sourceFile, destinationFile, _fixedTimestamp);

        // Assert
        var copiedSize = _fileServer.FileGetAt(destinationFile, _fixedTimestamp);
        Assert.That(copiedSize, Is.Not.Null);
        Assert.That(copiedSize.Value, Is.EqualTo(size));
    }

    [Test]
    public void FileCopyAt_NonExistingSourceFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        const string sourceFile = "nonexistent_source.txt";
        const string destinationFile = "destination.txt";

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() =>
            _fileServer.FileCopyAt(sourceFile, destinationFile, _fixedTimestamp));
        Assert.That(ex.Message, Is.EqualTo($"File '{sourceFile}' not found"));
    }

    [Test]
    public void FileSearchAt_ValidPrefix_ShouldReturnMatchingFiles()
    {
        // Arrange
        const string prefix = "file";
        var files = new List<(string Name, int Size, DateTimeOffset Timestamp, int? Ttl)>
        {
            ("fileA.txt", 100, _fixedTimestamp, 3600),
            ("fileB.txt", 200, _fixedTimestamp, 3600),
            ("fileC.txt", 150, _fixedTimestamp, 3600),
            ("other.txt", 300, _fixedTimestamp, 3600)
        };

        foreach (var file in files)
        {
            _fileServer.FileUploadAt(file.Name, file.Size, file.Timestamp, file.Ttl);
        }

        // Act
        var result = _fileServer.FileSearchAt(prefix, _fixedTimestamp);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.All(f => f.Name.StartsWith(prefix)), Is.True);
            Assert.That(result[0].Name, Is.EqualTo("fileB.txt")); // Largest size first
            Assert.That(result[1].Name, Is.EqualTo("fileC.txt"));
            Assert.That(result[2].Name, Is.EqualTo("fileA.txt"));
        });
    }

    [Test]
    public void FileSearchAt_EmptyPrefix_ShouldThrowArgumentNullException()
    {
        // Arrange
        const string prefix = "";

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _fileServer.FileSearchAt(prefix, _fixedTimestamp));
        Assert.That(ex.Message, Is.EqualTo("prefix cannot be null or empty (Parameter 'prefix')"));
    }

    [Test]
    public void FileSearchAt_OnlyAliveFiles_ShouldReturnAliveFiles()
    {
        // Arrange
        var aliveTimestamp = _fixedTimestamp.AddSeconds(-1000);
        var expiredTimestamp = _fixedTimestamp.AddSeconds(-4000);
        _fileServer.FileUploadAt("alive1.txt", 100, aliveTimestamp, 2000); // Alive
        _fileServer.FileUploadAt("alive2.txt", 200, aliveTimestamp, 2000); // Alive
        _fileServer.FileUploadAt("expired1.txt", 150, expiredTimestamp, 2000); // Expired

        // Act
        var result = _fileServer.FileSearchAt("alive", aliveTimestamp);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(f => f.Name.StartsWith("alive")), Is.True);
    }

    [Test]
    public void FileSearchAt_NoMatchingFiles_ShouldReturnEmptyList()
    {
        // Arrange
        const string prefix = "nomatch";

        // Act
        var result = _fileServer.FileSearchAt(prefix, _fixedTimestamp);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FileSearchAt_MoreThanTenFiles_ShouldReturnTopTen()
    {
        // Arrange
        const string prefix = "test";
        for (var i = 1; i <= 15; i++)
        {
            _fileServer.FileUploadAt($"testFile{i}.txt", i * 10, _fixedTimestamp, 3600);
        }

        // Act
        var result = _fileServer.FileSearchAt(prefix, _fixedTimestamp);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(10));
            Assert.That(result[0].Name, Is.EqualTo("testFile15.txt"));
            Assert.That(result[9].Name, Is.EqualTo("testFile6.txt"));
        });
    }

    [Test]
    public void Rollback_ToSpecificTimestamp_ShouldRestoreCorrectState()
    {
        // Arrange
        var timestamp1 = _fixedTimestamp.AddMinutes(-10);
        var timestamp2 = _fixedTimestamp.AddMinutes(-5);
        var timestamp3 = _fixedTimestamp;

        // Upload files at different timestamps
        _fileServer.FileUploadAt("file1.txt", 100, timestamp1, 3600);
        _fileServer.FileUploadAt("file2.txt", 200, timestamp2, 3600);
        _fileServer.FileUploadAt("file3.txt", 300, timestamp3, 3600);

        // Act
        // Rollback to timestamp2: should retain file1 and file2
        _fileServer.Rollback(timestamp2);

        // Assert
        var size1 = _fileServer.FileGetAt("file1.txt", timestamp1);
        var size2 = _fileServer.FileGetAt("file2.txt", timestamp2);
        var size3 = _fileServer.FileGetAt("file3.txt", timestamp3);

        Assert.Multiple(() =>
        {
            Assert.That(size1, Is.EqualTo(100));
            Assert.That(size2, Is.EqualTo(200));
            Assert.That(size3, Is.Null);
        });
    }

    [Test]
    public void Rollback_ToTimestampBeforeAllUploads_ShouldClearAllFiles()
    {
        // Arrange
        var timestamp1 = _fixedTimestamp.AddMinutes(-10);
        var timestamp2 = _fixedTimestamp.AddMinutes(-5);

        // Upload files at different timestamps
        _fileServer.FileUploadAt("file1.txt", 100, timestamp1, 3600);
        _fileServer.FileUploadAt("file2.txt", 200, timestamp2, 3600);

        // Act
        // Rollback to timestamp before any uploads
        var rollbackTimestamp = _fixedTimestamp.AddMinutes(-15);
        _fileServer.Rollback(rollbackTimestamp);

        // Assert
        var size1 = _fileServer.FileGetAt("file1.txt", timestamp1);
        var size2 = _fileServer.FileGetAt("file2.txt", timestamp2);

        Assert.Multiple(() =>
        {
            Assert.That(size1, Is.Null);
            Assert.That(size2, Is.Null);
        });
    }

    [Test]
    public void Rollback_ToTimestampAfterAllUploads_ShouldRetainAllFiles()
    {
        // Arrange
        var timestamp1 = _fixedTimestamp.AddMinutes(-10);
        var timestamp2 = _fixedTimestamp.AddMinutes(-5);

        // Upload files at different timestamps
        _fileServer.FileUploadAt("file1.txt", 100, timestamp1, 3600);
        _fileServer.FileUploadAt("file2.txt", 200, timestamp2, 3600);

        // Act
        // Rollback to timestamp after all uploads
        var rollbackTimestamp = _fixedTimestamp.AddMinutes(1);
        _fileServer.Rollback(rollbackTimestamp);

        // Assert
        var size1 = _fileServer.FileGetAt("file1.txt", timestamp1);
        var size2 = _fileServer.FileGetAt("file2.txt", timestamp2);

        Assert.AreEqual(100, size1);
        Assert.AreEqual(200, size2);
    }

    [Test]
    public void Rollback_ShouldClearCurrentStateBeforeRestoring()
    {
        // Arrange
        var initialTimestamp = _fixedTimestamp.AddMinutes(-20);
        var rollbackTimestamp = _fixedTimestamp.AddMinutes(-10);
        var postRollbackTimestamp = _fixedTimestamp;

        // Upload initial files
        _fileServer.FileUploadAt("initial1.txt", 50, initialTimestamp, 3600);
        _fileServer.FileUploadAt("initial2.txt", 150, initialTimestamp, 3600);

        // Upload additional files after rollback timestamp
        _fileServer.FileUploadAt("post1.txt", 100, postRollbackTimestamp, 3600);
        _fileServer.FileUploadAt("post2.txt", 200, postRollbackTimestamp, 3600);

        // Act
        // Perform rollback
        _fileServer.Rollback(rollbackTimestamp);

        // Assert
        // Initial files should remain
        var sizeInitial1 = _fileServer.FileGetAt("initial1.txt", initialTimestamp);
        var sizeInitial2 = _fileServer.FileGetAt("initial2.txt", initialTimestamp);

        Assert.AreEqual(50, sizeInitial1);
        Assert.AreEqual(150, sizeInitial2);

        // Post-rollback files should be removed
        var sizePost1 = _fileServer.FileGetAt("post1.txt", postRollbackTimestamp);
        var sizePost2 = _fileServer.FileGetAt("post2.txt", postRollbackTimestamp);

        Assert.IsNull(sizePost1);
        Assert.IsNull(sizePost2);
    }

    [Test]
    public void Rollback_WithNoHistory_ShouldResultInEmptyState()
    {
        // Arrange
        // No files uploaded

        // Act
        _fileServer.Rollback(_fixedTimestamp);

        // Assert
        var result = _fileServer.FileSearchAt("any", _fixedTimestamp);
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [Test]
    public void Rollback_MultipleTimes_ShouldMaintainCorrectStateEachTime()
    {
        // Arrange
        var timestamp1 = _fixedTimestamp.AddMinutes(-30);
        var timestamp2 = _fixedTimestamp.AddMinutes(-20);
        var timestamp3 = _fixedTimestamp.AddMinutes(-10);

        // Upload files at different timestamps
        _fileServer.FileUploadAt("file1.txt", 100, timestamp1, 3600);
        _fileServer.FileUploadAt("file2.txt", 200, timestamp2, 3600);
        _fileServer.FileUploadAt("file3.txt", 300, timestamp3, 3600);

        // First rollback to timestamp2
        _fileServer.Rollback(timestamp2);

        // Assert after first rollback
        var size1AfterFirstRollback = _fileServer.FileGetAt("file1.txt", timestamp1);
        var size2AfterFirstRollback = _fileServer.FileGetAt("file2.txt", timestamp2);
        var size3AfterFirstRollback = _fileServer.FileGetAt("file3.txt", timestamp3);

        Assert.Multiple(() =>
        {
            Assert.That(size1AfterFirstRollback, Is.EqualTo(100));
            Assert.That(size2AfterFirstRollback, Is.EqualTo(200));
            Assert.That(size3AfterFirstRollback, Is.Null);
        });

        // Second rollback to timestamp1
        _fileServer.Rollback(timestamp1);

        // Assert after second rollback
        var size1AfterSecondRollback = _fileServer.FileGetAt("file1.txt", timestamp1);
        var size2AfterSecondRollback = _fileServer.FileGetAt("file2.txt", timestamp2);
        var size3AfterSecondRollback = _fileServer.FileGetAt("file3.txt", timestamp3);

        Assert.Multiple(() =>
        {
            Assert.That(size1AfterSecondRollback, Is.EqualTo(100));
            Assert.That(size2AfterSecondRollback, Is.Null);
            Assert.That(size3AfterSecondRollback, Is.Null);
        });
    }
}