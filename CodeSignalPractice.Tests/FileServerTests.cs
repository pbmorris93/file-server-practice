using CodeSignalPractice.Lib;
using File = CodeSignalPractice.Lib.File;

namespace CodeSignalPractice.Tests;

[TestFixture]
public class FileServerTests
{
    private FileServer _fileServer;
    
    [SetUp]
    public void Setup()
    {
        _fileServer = new FileServer();
    }
    
    [Test]
    public void FileUpload_NewFile_ShouldUploadSuccessfully()
    {
        // Arrange
        const string fileName = "testFile.txt";
        const int size = 1024;

        // Act
        Assert.DoesNotThrow(() => _fileServer.FileUpload(fileName, size));

        // Assert
        var retrievedSize = _fileServer.FileGet(fileName);
        Assert.IsNotNull(retrievedSize);
        Assert.That(retrievedSize.Value, Is.EqualTo(size));
    }

    [Test]
    public void FileUpload_DuplicateFile_ShouldThrowException()
    {
        const string fileName = "duplicateFile.txt";
        const int size = 2048;
        _fileServer.FileUpload(fileName, size);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _fileServer.FileUpload(fileName, size));
        Assert.That(ex.Message, Is.EqualTo($"File '{fileName}' already exists"));
    }

    [Test]
    public void FileGet_ExistingFile_ShouldReturnSize()
    {
        const string fileName = "existingFile.txt";
        const int size = 4096;
        _fileServer.FileUpload(fileName, size);

        // Act
        var result = _fileServer.FileGet(fileName);

        // Assert
        Assert.IsNotNull(result);
        Assert.That(result.Value, Is.EqualTo(size));
    }

    [Test]
    public void FileGet_NonExistingFile_ShouldReturnNull()
    {
        // Arrange
        const string fileName = "nonExistingFile.txt";

        // Act
        var result = _fileServer.FileGet(fileName);

        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void FileCopy_ExistingSourceAndNewDestination_ShouldCopySuccessfully()
    {
        // Arrange
        const string source = "sourceFile.txt";
        const string dest = "destFile.txt";
        const int size = 512;
        _fileServer.FileUpload(source, size);

        // Act
        Assert.DoesNotThrow(() => _fileServer.FileCopy(source, dest));

        // Assert
        var destSize = _fileServer.FileGet(dest);
        Assert.That(destSize, Is.Not.Null);
        Assert.That(destSize.Value, Is.EqualTo(size));
    }

    [Test]
    public void FileCopy_ExistingSourceAndExistingDestination_ShouldOverwriteSuccessfully()
    {
        // Arrange
        const string source = "sourceFile.txt";
        const string dest = "destFile.txt";
        const int sourceSize = 1024;
        const int destOriginalSize = 2048;
        _fileServer.FileUpload(source, sourceSize);
        _fileServer.FileUpload(dest, destOriginalSize);

        // Act
        Assert.DoesNotThrow(() => _fileServer.FileCopy(source, dest));

        // Assert
        var destSize = _fileServer.FileGet(dest);
        Assert.That(destSize, Is.Not.Null);
        Assert.That(destSize.Value, Is.EqualTo(sourceSize));
    }

    [Test]
    public void FileCopy_NonExistingSource_ShouldThrowException()
    {
        // Arrange
        const string source = "nonExistingSource.txt";
        const string dest = "destFile.txt";

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => _fileServer.FileCopy(source, dest));
        Assert.That(ex.Message, Is.EqualTo($"File '{source}' not found"));
    }
    
    
    [Test]
    public void FileSearch_PrefixMatchesMultipleFiles_ShouldReturnTop10OrderedCorrectly()
    {
        // Arrange
        _fileServer.FileUpload("alpha1.txt", 500);
        _fileServer.FileUpload("alpha2.txt", 1500);
        _fileServer.FileUpload("alpha3.txt", 1500);
        _fileServer.FileUpload("beta1.txt", 2000);
        _fileServer.FileUpload("beta2.txt", 2500);
        _fileServer.FileUpload("gamma1.txt", 3000);
        _fileServer.FileUpload("gamma2.txt", 3000);
        _fileServer.FileUpload("delta1.txt", 3500);
        _fileServer.FileUpload("delta2.txt", 3500);
        _fileServer.FileUpload("delta3.txt", 3500);
        _fileServer.FileUpload("epsilon1.txt", 4000);
        _fileServer.FileUpload("epsilon2.txt", 4000);
        _fileServer.FileUpload("zeta1.txt", 4500);
        _fileServer.FileUpload("zeta2.txt", 4500);
        _fileServer.FileUpload("eta1.txt", 5000);
        
        const string prefix = "a";

        // Act
        var result = _fileServer.FileSearch(prefix);

        // Assert
        var expected = new List<File>
        {
            new ("alpha3.txt", 1500),
            new ("alpha2.txt", 1500),
            new ("alpha1.txt", 500)
        };
        
        CollectionAssert.AreEqual(expected, result);
    }
    
    [Test]
        public void Search_NoFiles_ReturnsEmptyList()
        {
            // Act
            var result = _fileServer.FileSearch("anyPrefix");

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null.");
            Assert.That(result, Is.Empty, "Result should be empty when no files are present.");
        }

        [Test]
        public void Search_NoMatchingFiles_ReturnsEmptyList()
        {
            // Arrange
            _fileServer.FileUpload("file1.txt", 100);
            _fileServer.FileUpload("file2.doc", 200);

            // Act
            var result = _fileServer.FileSearch("prefix");

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null.");
            Assert.That(result, Is.Empty, "Result should be empty when no files match the prefix.");
        }

        [Test]
        public void Search_SingleMatchingFile_ReturnsCorrectFile()
        {
            // Arrange
            _fileServer.FileUpload("prefix_file1.txt", 150);
            _fileServer.FileUpload("file2.doc", 200);

            // Act
            var result = _fileServer.FileSearch("prefix");

            // Assert
            Assert.That(result, Has.Count.EqualTo(1), "Should return exactly one matching file.");
            Assert.That(result[0], Is.EqualTo(new File("prefix_file1.txt", 150)), "Returned file does not match the expected file.");
        }

        [Test]
        public void Search_MultipleMatchingFiles_ReturnsCorrectFilesInOrder()
        {
            // Arrange
            _fileServer.FileUpload("prefix_file1.txt", 100);
            _fileServer.FileUpload("prefix_file2.doc", 200);
            _fileServer.FileUpload("prefix_file3.pdf", 150);
            _fileServer.FileUpload("other_file4.txt", 250);

            // Act
            var result = _fileServer.FileSearch("prefix");

            // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3), "Should return three matching files.");
            Assert.That(result[0], Is.EqualTo(new File("prefix_file2.doc", 200)), "First file should be prefix_file2.doc with size 200.");
            Assert.That(result[1], Is.EqualTo(new File("prefix_file3.pdf", 150)), "Second file should be prefix_file3.pdf with size 150.");
            Assert.That(result[2], Is.EqualTo(new File("prefix_file1.txt", 100)), "Third file should be prefix_file1.txt with size 100.");
        });
    }

        [Test]
        public void Search_MoreThanTenMatchingFiles_ReturnsTopTenFiles()
        {
            // Arrange
            for (var i = 1; i <= 15; i++)
            {
                _fileServer.FileUpload($"prefix_file{i}.txt", i * 10);
            }

            // Act
            var result = _fileServer.FileSearch("prefix");

            // Assert
            Assert.That(result, Has.Count.EqualTo(10), "Should return only the top ten matching files.");

            // Verify that the files are the top ten largest
            for (var i = 0; i < 10; i++)
            {
                Assert.That(result[i], Is.EqualTo(new File($"prefix_file{15 - i}.txt", (15 - i) * 10)),
                    $"File at position {i} does not match the expected file.");
            }
        }

        [Test]
        public void Search_OrderIsDescendingSizeThenDescendingName()
        {
            // Arrange
            _fileServer.FileUpload("prefix_alpha.txt", 300);
            _fileServer.FileUpload("prefix_beta.txt", 300);
            _fileServer.FileUpload("prefix_gamma.txt", 200);
            _fileServer.FileUpload("prefix_delta.txt", 400);
            _fileServer.FileUpload("prefix_epsilon.txt", 200);

            // Act
            var result = _fileServer.FileSearch("prefix");

            // Assert
            Assert.That(result, Has.Count.EqualTo(5), "Should return five matching files.");

        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo(new File("prefix_delta.txt", 400)), "First file should be prefix_delta.txt with size 400.");
            Assert.That(result[1], Is.EqualTo(new File("prefix_beta.txt", 300)), "Second file should be prefix_beta.txt with size 300.");
            Assert.That(result[2], Is.EqualTo(new File("prefix_alpha.txt", 300)), "Third file should be prefix_alpha.txt with size 300.");
            Assert.That(result[3], Is.EqualTo(new File("prefix_gamma.txt", 200)), "Fourth file should be prefix_gamma.txt with size 200.");
            Assert.That(result[4], Is.EqualTo(new File("prefix_epsilon.txt", 200)), "Fifth file should be prefix_epsilon.txt with size 200.");
        });
    }

        [Test]
        public void Search_PrefixIsCaseSensitive_ReturnsCorrectFiles()
        {
            // Arrange
            _fileServer.FileUpload("Prefix_File1.txt", 100);
            _fileServer.FileUpload("prefix_file2.txt", 200);
            _fileServer.FileUpload("PREFIX_file3.txt", 300);
            _fileServer.FileUpload("prefix_File4.txt", 400);

            // Act
            var result = _fileServer.FileSearch("prefix");

            // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return two files matching the lowercase prefix.");
            Assert.That(result[0], Is.EqualTo(new File("prefix_File4.txt", 400)), "First file should be prefix_File4.txt with size 400.");
            Assert.That(result[1], Is.EqualTo(new File("prefix_file2.txt", 200)), "Second file should be prefix_file2.txt with size 200.");
        });
    }

        [Test]
        public void Search_NullPrefix_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _fileServer.FileSearch(null!),
                "Search should throw ArgumentNullException when prefix is null.");
        }

        [Test]
        public void Search_PrefixMatchesEntireFileName_ReturnsExactMatch()
        {
            // Arrange
            _fileServer.FileUpload("exactmatch", 500);
            _fileServer.FileUpload("exactmatch_extra", 300);
            _fileServer.FileUpload("exact", 400);

            // Act
            var result = _fileServer.FileSearch("exactmatch");

            // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return two files that start with 'exactmatch'.");
            Assert.That(result[1], Is.EqualTo(new File("exactmatch_extra", 300)), "Second file should be exactmatch_extra with size 300.");
            Assert.That(result[0], Is.EqualTo(new File("exactmatch", 500)), "First file should be exactmatch with size 500.");
        });
    }

        [Test]
        public void Search_PrefixWithSpecialCharacters_ReturnsCorrectFiles()
        {
            // Arrange
            _fileServer.FileUpload("prefix_@file1.txt", 100);
            _fileServer.FileUpload("prefix_#file2.txt", 200);
            _fileServer.FileUpload("prefix_$file3.txt", 150);
            _fileServer.FileUpload("prefix_file4.txt", 250);

            // Act
            var result = _fileServer.FileSearch("prefix_@");

            // Assert
            Assert.That(result, Has.Count.EqualTo(1), "Should return one file that starts with 'prefix_@'.");
            Assert.That(result[0], Is.EqualTo(new File("prefix_@file1.txt", 100)), "Returned file does not match the expected file.");
        }

        [Test]
        public void Search_PrefixWithWhitespace_ReturnsCorrectFiles()
        {
            // Arrange
            _fileServer.FileUpload("prefix file1.txt", 100);
            _fileServer.FileUpload("prefix file2.txt", 200);
            _fileServer.FileUpload("prefix_file3.txt", 150);

            // Act
            var result = _fileServer.FileSearch("prefix ");

            // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return two files that start with 'prefix ' (with space).");
            Assert.That(result[0], Is.EqualTo(new File("prefix file2.txt", 200)), "First file should be prefix file2.txt with size 200.");
            Assert.That(result[1], Is.EqualTo(new File("prefix file1.txt", 100)), "Second file should be prefix file1.txt with size 100.");
        });
    }
}