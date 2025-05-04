using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using TextProcessor.Core;
using System.Reflection;

namespace TextProcessor.Tests
{
    public class TextFileProcessorTests : IDisposable
    {
        private const string OutputDirectory = "..\\..\\..\\..\\TextProcessor.Tests\\Output";
        private static readonly string TestDirectory = "..\\..\\..\\..\\TextProcessor.Tests\\TestFiles";

        public TextFileProcessorTests()
        {
            // Create test directory if it doesn't exist
            if (!Directory.Exists(TestDirectory))
            {
                Directory.CreateDirectory(TestDirectory);
            }
        }

        private string[] readTextFile(in string filePath)
        {
            return File.ReadAllLines(filePath);
        }

        private void createHugeTestFileWithLines(in string filePath, in string stringToRepeat, in int sizeInMb)
        {
            int stringUtf8Length = System.Text.Encoding.UTF8.GetByteCount(stringToRepeat + "\n");
            int numberOfStrings = sizeInMb * 1024 * 1024 / stringUtf8Length;

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new StreamWriter(fileStream))
            {
                for (int i = 0; i < numberOfStrings; i++)
                {
                    writer.Write(stringToRepeat + "\n");
                }
            }
        }

        private void createFileWithLines(in string filePath, in string[] strings)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var str in strings)
                {
                    writer.WriteLine(str);
                }
            }
        }
        private async Task<bool> checkProcessLines(string[] inputLines, string[] expectedLines, int minWordLength, bool removePunctuation, int numberOfThreads = 1)
        {
            var processor = new TextFileProcessor(
                string.Empty,
                string.Empty,
                minWordLength,
                removePunctuation,
                numberOfThreads);
            var processedLines = await processor.ProcessLines(inputLines);

            if (expectedLines.Length != processedLines.Length)
            {
                return false;
            }
            for (int i = 0; i < processedLines.Length; i++)
            {
                if (!expectedLines[i].Equals(processedLines[i]))
                {
                    Console.WriteLine("expected *" + expectedLines[i] + "* is not equal to *" + processedLines[i] + "*");
                    return false;
                }
            }
            return true;
        }

        [Fact]
        public async Task ProcessFileAsync_EmptyFile()
        {
            string inputPath = Path.Combine(TestDirectory, "empty.txt");
            string outputPath = Path.Combine(OutputDirectory, "output_empty.txt");
            string[] empty = Array.Empty<string>();
            createFileWithLines(inputPath, empty);
            var processor1Thread = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync();

            var processedLines = readTextFile(outputPath);
            Assert.Empty(processedLines);
        }

        [Fact]
        public async Task ProcessLinesAsync_Test_Hellow_World()
        {
            string[] lines = new string[] { "Hellow, World!" };

            for (int i = 0; i < 6; i++)
            {
                Assert.True(await checkProcessLines(lines, lines, i, false));
                Assert.True(await checkProcessLines(lines, new string[] { "Hellow World" }, i, true));
            }

            Assert.True(await checkProcessLines(lines, new string[] { "Hellow, !" }, 6, false));
            Assert.True(await checkProcessLines(lines, new string[] { "Hellow " }, 6, true));

            Assert.True(await checkProcessLines(lines, new string[] { ", !" }, 7, false));
            Assert.True(await checkProcessLines(lines, new string[] { " " }, 7, true));
        }

        [Fact]
        public async Task ProcessLinesAsync_Diff_Len_Words()
        {
            string[] lines = new string[] { "a  aa    aaa!? & aaaa aaa?aa" };

            Assert.True(await checkProcessLines(lines, new string[] { "a  aa    aaa!? & aaaa aaa?aa" }, 1, false));
            Assert.True(await checkProcessLines(lines, new string[] { "a  aa    aaa  aaaa aaaaa" }, 1, true));

            Assert.True(await checkProcessLines(lines, new string[] { "  aa    aaa!? & aaaa aaa?aa" }, 2, false));
            Assert.True(await checkProcessLines(lines, new string[] { "  aa    aaa  aaaa aaaaa" }, 2, true));

            Assert.True(await checkProcessLines(lines, new string[] { "      aaa!? & aaaa aaa?" }, 3, false));
            Assert.True(await checkProcessLines(lines, new string[] { "      aaa  aaaa aaa" }, 3, true));

            Assert.True(await checkProcessLines(lines, new string[] { "      !? & aaaa ?" }, 4, false));
            Assert.True(await checkProcessLines(lines, new string[] { "        aaaa " }, 4, true));

            Assert.True(await checkProcessLines(lines, new string[] { "      !? &  ?" }, 5, false));
            Assert.True(await checkProcessLines(lines, new string[] { "         " }, 5, true));

            Assert.True(await checkProcessLines(lines, new string[] { "      !? &  ?" }, 10, false));
            Assert.True(await checkProcessLines(lines, new string[] { "         " }, 10, true));
        }

        [Fact]
        public async Task ProcessFileAsync_HugeFile_100mb()
        {
            string inputPath = Path.Combine(TestDirectory, "100mb.txt");
            createHugeTestFileWithLines(inputPath, "Hel lo, worl d!", 100);
            string outputPath = Path.Combine(OutputDirectory, "output_100mb.txt");

            var processor1Thread = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync();

            long time1Thread = processor1Thread.ProcessingTime;

            var processor8Threads = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 8,
                minWordLength: 4,
                removePunctuation: true);

            await processor8Threads.ProcessFileAsync();

            long time8Threads = processor8Threads.ProcessingTime;

            Assert.True(time1Thread > time8Threads);
        }

        public async Task ProcessFileAsync_HugeFile_1gb()
        {
            string inputPath = Path.Combine(TestDirectory, "1gb.txt");
            createHugeTestFileWithLines(inputPath, "Hel lo, worl d!", 1024);
            string outputPath = Path.Combine(OutputDirectory, "output_1gb.txt");

            var processor1Thread = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync();

            long time1Thread = processor1Thread.ProcessingTime;

            var processor8Threads = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 8,
                minWordLength: 4,
                removePunctuation: true);

            await processor8Threads.ProcessFileAsync();

            long time8Threads = processor8Threads.ProcessingTime;

            Assert.True(time1Thread > time8Threads);
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
