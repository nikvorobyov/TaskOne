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
        private const string OutputDirectory = "..\\..\\..\\..\\TextProcessor.Tests\\Output";
        private static readonly string TestDirectory = "..\\..\\..\\..\\TextProcessor.Tests\\TestFiles";

        public TextFileProcessorTests()
        {
            // Create test directory if it doesn't exist
            if (!Directory.Exists(TestDirectory))
            {
                Directory.CreateDirectory(TestDirectory);
            }
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }
        }

        private static string[] ReadTextFile(in string filePath)
        {
            return File.ReadAllLines(filePath);
        }

        private static void CreateHugeTestFileWithLines(in string filePath, in string stringToRepeat, in int sizeInMb)
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

        private static void CreateFileWithLines(in string filePath, in string[] strings)
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
        private async Task<bool> CheckProcessLines(string[] inputLines, string[] expectedLines, int minWordLength, bool removePunctuation, int numberOfThreads = 1)
        {
            string inputPath = Path.Combine(TestDirectory, "temp.txt");
            string outputPath = Path.Combine(OutputDirectory, "output_temp.txt");

            CreateFileWithLines(inputPath, inputLines);
            var processor = new TextFileProcessor(
                inputPath,
                outputPath,
                minWordLength,
                removePunctuation,
                numberOfThreads);
            await processor.ProcessFileAsync();

            var processedLines = ReadTextFile(outputPath);
            
            //remove files after test
            File.Delete(inputPath);
            File.Delete(outputPath);

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
            CreateFileWithLines(inputPath, empty);
            var processor1Thread = new TextFileProcessor(
                inputPath,
                outputPath,
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync();

            var processedLines = ReadTextFile(outputPath);
            Assert.Empty(processedLines);
        }

        [Fact]
        public async Task ProcessLinesAsync_Test_Hellow_World()
        {
            string[] lines = new string[] { "Hellow, World!" };

            for (int i = 0; i < 6; i++)
            {
                Assert.True(await CheckProcessLines(lines, lines, i, false));
                Assert.True(await CheckProcessLines(lines, new string[] { "Hellow World" }, i, true));
            }

            Assert.True(await CheckProcessLines(lines, new string[] { "Hellow, !" }, 6, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "Hellow " }, 6, true));

            Assert.True(await CheckProcessLines(lines, new string[] { ", !" }, 7, false));
            Assert.True(await CheckProcessLines(lines, new string[] { " " }, 7, true));
        }

        [Fact]
        public async Task ProcessLinesAsync_Diff_Len_Words()
        {
            string[] lines = new string[] { "a  aa    aaa!? & aaaa aaa?aa" };

            Assert.True(await CheckProcessLines(lines, new string[] { "a  aa    aaa!? & aaaa aaa?aa" }, 1, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "a  aa    aaa  aaaa aaaaa" }, 1, true));

            Assert.True(await CheckProcessLines(lines, new string[] { "  aa    aaa!? & aaaa aaa?aa" }, 2, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "  aa    aaa  aaaa aaaaa" }, 2, true));

            Assert.True(await CheckProcessLines(lines, new string[] { "      aaa!? & aaaa aaa?" }, 3, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "      aaa  aaaa aaa" }, 3, true));

            Assert.True(await CheckProcessLines(lines, new string[] { "      !? & aaaa ?" }, 4, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "        aaaa " }, 4, true));

            Assert.True(await CheckProcessLines(lines, new string[] { "      !? &  ?" }, 5, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "         " }, 5, true));

            Assert.True(await CheckProcessLines(lines, new string[] { "      !? &  ?" }, 10, false));
            Assert.True(await CheckProcessLines(lines, new string[] { "         " }, 10, true));
        }

        [Fact]
        public async Task ProcessFileAsync_HugeFile_100mb()
        {
            string inputPath = Path.Combine(TestDirectory, "100mb.txt");
            CreateHugeTestFileWithLines(inputPath, "Hel lo, worl d!", 100);
            string outputPath1 = Path.Combine(OutputDirectory, "output_100mb_1.txt");
            string outputPath8 = Path.Combine(OutputDirectory, "output_100mb_8.txt");

            var processor1Thread = new TextFileProcessor(
                inputPath,
                outputPath1,
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync();

            long time1Thread = processor1Thread.ProcessingTime;

            var processor8Threads = new TextFileProcessor(
                inputPath,
                outputPath8,
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
            CreateHugeTestFileWithLines(inputPath, "Hel lo, worl d!", 1024);
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
            if (Directory.Exists(OutputDirectory))
            {
                Directory.Delete(OutputDirectory, true);
            }
        }
    }
}
