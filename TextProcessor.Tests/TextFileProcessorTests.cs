using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using TextProcessor.Core;

namespace TextProcessor.Tests
{
    public class TextFileProcessorTests : IDisposable
    {
        private const string TestDirectory = "TestFiles";

        public TextFileProcessorTests()
        {
            // Create test directory if it doesn't exist
            if (!Directory.Exists(TestDirectory))
            {
                Directory.CreateDirectory(TestDirectory);
            }
        }

        [Fact]
        public async Task ProcessFileAsync_RemovesShortWords_Success()
        {
            // Arrange
            string inputPath = Path.Combine(TestDirectory, "input_short_words.txt");
            string outputPath = Path.Combine(TestDirectory, "output_short_words.txt");
            string inputText = "The quick brown fox jumps over a lazy dog";
            await File.WriteAllTextAsync(inputPath, inputText);

            var processor = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 2,
                minWordLength: 4,
                removePunctuation: false);

            // Act
            await processor.ProcessFileAsync();

            // Assert
            string result = await File.ReadAllTextAsync(outputPath);
            var resultWords = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.DoesNotContain(resultWords, word => word.Equals("The", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(resultWords, word => word.Equals("a", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("quick", result);
            Assert.Contains("brown", result);
            Assert.Contains("jumps", result);
        }

        [Fact]
        public async Task ProcessFileAsync_RemovesPunctuation_Success()
        {
            // Arrange
            string inputPath = Path.Combine(TestDirectory, "input_punctuation.txt");
            string outputPath = Path.Combine(TestDirectory, "output_punctuation.txt");
            string inputText = "Hello, world! How are you? This is a test.";
            await File.WriteAllTextAsync(inputPath, inputText);

            var processor = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 2,
                minWordLength: 1,
                removePunctuation: true);

            // Act
            await processor.ProcessFileAsync();

            // Assert
            string result = await File.ReadAllTextAsync(outputPath);
            Assert.DoesNotContain(",", result);
            Assert.DoesNotContain("!", result);
            Assert.DoesNotContain("?", result);
            Assert.DoesNotContain(".", result);
        }

        [Fact]
        public async Task ProcessFileAsync_MultipleThreads_ProcessesAllLines()
        {
            // Arrange
            string inputPath = Path.Combine(TestDirectory, "input_multiple_lines.txt");
            string outputPath = Path.Combine(TestDirectory, "output_multiple_lines.txt");
            string[] inputLines = new[]
            {
                "First line with some text",
                "Second line with other text",
                "Third line here",
                "Fourth line present",
                "Fifth line exists"
            };
            await File.WriteAllLinesAsync(inputPath, inputLines);

            var processor = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 3,
                minWordLength: 4,
                removePunctuation: false);

            // Act
            await processor.ProcessFileAsync();

            // Assert
            string[] resultLines = await File.ReadAllLinesAsync(outputPath);
            Assert.True(resultLines.Length > 0);
            foreach (var line in resultLines)
            {
                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Assert.True(words.All(word => word.Length >= 4));
            }
        }

        [Fact]
        public async Task ProcessFileAsync_EmptyFile_CreatesEmptyOutput()
        {
            // Arrange
            string inputPath = Path.Combine(TestDirectory, "input_empty.txt");
            string outputPath = Path.Combine(TestDirectory, "output_empty.txt");
            await File.WriteAllTextAsync(inputPath, "");

            var processor = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 2,
                minWordLength: 3,
                removePunctuation: false);

            // Act
            await processor.ProcessFileAsync();

            // Assert
            string result = await File.ReadAllTextAsync(outputPath);
            Assert.Empty(result.Trim());
        }

        [Fact]
        public async Task ProcessFileAsync_InvalidInput_ThrowsException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(TestDirectory, "non_existent.txt");
            string outputPath = Path.Combine(TestDirectory, "output_error.txt");

            var processor = new TextFileProcessor(
                nonExistentPath,
                outputPath,
                numberOfThreads: 2,
                minWordLength: 3,
                removePunctuation: false);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => processor.ProcessFileAsync());
        }

        [Fact]
        public async Task ProcessFileAsync_TracksStatistics_Correctly()
        {
            // Arrange
            string inputPath = Path.Combine(TestDirectory, "input_statistics.txt");
            string outputPath = Path.Combine(TestDirectory, "output_statistics.txt");
            string[] inputLines = new[]
            {
                "The quick brown fox",
                "jumps over a lazy dog",
                "",
                "Some short words: a an the",
                "Multiple     spaces    here"
            };

            await File.WriteAllLinesAsync(inputPath, inputLines);

            var processor = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 2,
                minWordLength: 4,
                removePunctuation: false);

            // Act
            await processor.ProcessFileAsync();

            // Assert
            Assert.Equal(5, processor.Statistics.TotalLines);
            Assert.Equal(1, processor.Statistics.EmptyLines);
            Assert.True(processor.Statistics.TotalWords > 0);
            Assert.True(processor.Statistics.FilteredWords > 0);
            Assert.True(processor.Statistics.GetProcessingTime().TotalMilliseconds > 0);
        }

        public void Dispose()
        {
            // Cleanup test files after tests
            if (Directory.Exists(TestDirectory))
            {
                Directory.Delete(TestDirectory, true);
            }
        }
    }
} 