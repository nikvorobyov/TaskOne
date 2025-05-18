using TextProcessor.Core;


namespace TextProcessor.Tests
{
    public class TextProcessorTests : IDisposable
    {
        private const string OutputDirectory = "..\\..\\..\\..\\TextProcessor.Tests\\Output";
        private static readonly string TestDirectory = "..\\..\\..\\..\\TextProcessor.Tests\\TestFiles";

        public TextProcessorTests()
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

        private bool compareStrings(in string[] expectedLines, in string[] processedLines)
        {
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
        private async Task<bool> CheckProcessFile(int numberOfThreads, int minWordLength, bool removePunctuation, string inputFilePath, string outputFilePath, string[] expectedLines)
        {
            var processorThread = new Core.TextProcessor(
                numberOfThreads: numberOfThreads,
                minWordLength: minWordLength,
                removePunctuation: removePunctuation);

            await processorThread.ProcessFileAsync(inputFilePath, outputFilePath);
            var processedLines = ReadTextFile(outputFilePath);
            return compareStrings(expectedLines, processedLines);
        }
        private async Task<bool> CheckProcessLines(string[] inputLines, string[] expectedLines, int minWordLength, bool removePunctuation, int numberOfThreads = 1)
        {
            // Prepare input stream from inputLines
            var inputText = string.Join(Environment.NewLine, inputLines);
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(inputText);
            using var inputStream = new MemoryStream(inputBytes);

            // Prepare output stream
            using var outputStream = new MemoryStream();

            var processor = new Core.TextProcessor(
                minWordLength,
                removePunctuation,
                numberOfThreads);
            await processor.ProcessStreamAsync(inputStream, outputStream);

            // Read processed lines from output stream
            outputStream.Position = 0;
            using var reader = new StreamReader(outputStream);
            var processedText = await reader.ReadToEndAsync();
            var processedLines = processedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            // // Remove possible trailing empty line due to WriteLineAsync
            if (processedLines.Length > 0 && processedLines[^1] == "")
                processedLines = processedLines[..^1];

            return compareStrings(expectedLines, processedLines);
        }

        [Fact]
        public async Task ProcessFileAsync_EmptyFile()
        {
            string inputPath = Path.Combine(TestDirectory, "empty.txt");
            string outputPath = Path.Combine(OutputDirectory, "output_empty.txt");
            string[] empty = Array.Empty<string>();
            CreateFileWithLines(inputPath, empty);
            var processor1Thread = new Core.TextProcessor(
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync(inputPath, outputPath);

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
        public async Task ProcessFileAsync_Test_Hellow_World()
        {
            string[] lines = new string[] { "Hellow, World!" };

            string inputPath = Path.Combine(TestDirectory, "input_hellow_world.txt");
            string outputPath = Path.Combine(OutputDirectory, "output_hellow_world.txt");

            CreateFileWithLines(inputPath, lines);

            for (int i = 0; i < 6; i++)
            {
                Assert.True(await CheckProcessFile(1, i, false, inputPath, outputPath, lines));
                Assert.True(await CheckProcessFile(4, i, false, inputPath, outputPath, lines));

                Assert.True(await CheckProcessFile(1, i, true, inputPath, outputPath, new string[] { "Hellow World" }));
                Assert.True(await CheckProcessFile(4, i, true, inputPath, outputPath, new string[] { "Hellow World" }));
            }

            Assert.True(await CheckProcessFile(1, 6, false, inputPath, outputPath, new string[] { "Hellow, !" }));
            Assert.True(await CheckProcessFile(4, 6, false, inputPath, outputPath, new string[] { "Hellow, !" }));
            Assert.True(await CheckProcessFile(1, 6, true, inputPath, outputPath, new string[] { "Hellow " }));
            Assert.True(await CheckProcessFile(4, 6, true, inputPath, outputPath, new string[] { "Hellow " }));

            Assert.True(await CheckProcessFile(1, 7, false, inputPath, outputPath, new string[] { ", !" }));
            Assert.True(await CheckProcessFile(4, 7, false, inputPath, outputPath, new string[] { ", !" }));

            Assert.True(await CheckProcessFile(1, 7, true, inputPath, outputPath, new string[] { " " }));
            Assert.True(await CheckProcessFile(4, 7, true, inputPath, outputPath, new string[] { " " }));

            Assert.True(await CheckProcessFile(1, 7, false, inputPath, outputPath, new string[] { ", !" }));
            Assert.True(await CheckProcessFile(4, 7, false, inputPath, outputPath, new string[] { ", !" }));

            Assert.True(await CheckProcessFile(1, 7, true, inputPath, outputPath, new string[] { " " }));
            Assert.True(await CheckProcessFile(4, 7, true, inputPath, outputPath, new string[] { " " }));
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

        private string[] getArrayOfRepeatedLines(in string line, in int numberOfLines)
        {
            string[] lines = new string[numberOfLines];
            for (int i = 0; i < numberOfLines; i++)
            {
                lines[i] = line;
            }
            return lines;
        }

        [Fact]
        public async Task ProcessFileAsync_Diff_Len_Words()
        {
            string repeatedLine = "a  aa    aaa!? & aaaa aaa?aa";

            string[] lines = getArrayOfRepeatedLines(repeatedLine, 50);

            string inputPath = Path.Combine(TestDirectory, "input_diff_len_words.txt");
            string outputPath = Path.Combine(OutputDirectory, "output_diff_len_words.txt");

            CreateFileWithLines(inputPath, lines);

            string[] expectedLines1Punct = getArrayOfRepeatedLines("a  aa    aaa!? & aaaa aaa?aa", 50);
            string[] expectedLines1NonPunct = getArrayOfRepeatedLines("a  aa    aaa  aaaa aaaaa", 50);

            string[] expectedLines2Punct = getArrayOfRepeatedLines("  aa    aaa!? & aaaa aaa?aa", 50);
            string[] expectedLines2NonPunct = getArrayOfRepeatedLines("  aa    aaa  aaaa aaaaa", 50);

            string[] expectedLines3Punct = getArrayOfRepeatedLines("      aaa!? & aaaa aaa?", 50);
            string[] expectedLines3NonPunct = getArrayOfRepeatedLines("      aaa  aaaa aaa", 50);

            string[] expectedLines4Punct = getArrayOfRepeatedLines("      !? & aaaa ?", 50);
            string[] expectedLines4NonPunct = getArrayOfRepeatedLines("        aaaa ", 50);

            string[] expectedLines5Punct = getArrayOfRepeatedLines("      !? &  ?", 50);
            string[] expectedLines5NonPunct = getArrayOfRepeatedLines("         ", 50);

            // Also check different number of threads
            Assert.True(await CheckProcessFile(1, 1, false, inputPath, outputPath, expectedLines1Punct));
            Assert.True(await CheckProcessFile(4, 1, false, inputPath, outputPath, expectedLines1Punct));

            Assert.True(await CheckProcessFile(1, 1, true, inputPath, outputPath, expectedLines1NonPunct));
            Assert.True(await CheckProcessFile(4, 1, true, inputPath, outputPath, expectedLines1NonPunct));

            Assert.True(await CheckProcessFile(1, 2, false, inputPath, outputPath, expectedLines2Punct));
            Assert.True(await CheckProcessFile(4, 2, false, inputPath, outputPath, expectedLines2Punct));

            Assert.True(await CheckProcessFile(1, 2, true, inputPath, outputPath, expectedLines2NonPunct));
            Assert.True(await CheckProcessFile(4, 2, true, inputPath, outputPath, expectedLines2NonPunct));

            Assert.True(await CheckProcessFile(1, 3, false, inputPath, outputPath, expectedLines3Punct));
            Assert.True(await CheckProcessFile(4, 3, false, inputPath, outputPath, expectedLines3Punct));

            Assert.True(await CheckProcessFile(1, 3, true, inputPath, outputPath, expectedLines3NonPunct));
            Assert.True(await CheckProcessFile(4, 3, true, inputPath, outputPath, expectedLines3NonPunct));

            Assert.True(await CheckProcessFile(1, 4, false, inputPath, outputPath, expectedLines4Punct));
            Assert.True(await CheckProcessFile(4, 4, false, inputPath, outputPath, expectedLines4Punct));

            Assert.True(await CheckProcessFile(1, 4, true, inputPath, outputPath, expectedLines4NonPunct));
            Assert.True(await CheckProcessFile(4, 4, true, inputPath, outputPath, expectedLines4NonPunct));

            Assert.True(await CheckProcessFile(1, 5, false, inputPath, outputPath, expectedLines5Punct));
            Assert.True(await CheckProcessFile(4, 5, false, inputPath, outputPath, expectedLines5Punct));

            Assert.True(await CheckProcessFile(1, 5, true, inputPath, outputPath, expectedLines5NonPunct));
            Assert.True(await CheckProcessFile(4, 5, true, inputPath, outputPath, expectedLines5NonPunct));

            Assert.True(await CheckProcessFile(1, 10, false, inputPath, outputPath, expectedLines5Punct));
            Assert.True(await CheckProcessFile(4, 10, false, inputPath, outputPath, expectedLines5Punct));

            Assert.True(await CheckProcessFile(1, 10, true, inputPath, outputPath, expectedLines5NonPunct));
            Assert.True(await CheckProcessFile(4, 10, true, inputPath, outputPath, expectedLines5NonPunct));
        }

        [Fact]
        public async Task ProcessFileAsync_Check_Performance_HugeFile_100mb()
        {
            string inputPath = Path.Combine(TestDirectory, "100mb.txt");
            CreateHugeTestFileWithLines(inputPath, "Hel lo, worl d!", 100);
            string outputPath1 = Path.Combine(OutputDirectory, "output_100mb_1.txt");
            string outputPath8 = Path.Combine(OutputDirectory, "output_100mb_8.txt");

            var processor1Thread = new Core.TextProcessor(
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync(inputPath, outputPath1);

            long time1Thread = processor1Thread.ProcessingTime;

            var processor8Threads = new Core.TextProcessor(
                numberOfThreads: 8,
                minWordLength: 4,
                removePunctuation: true);

            await processor8Threads.ProcessFileAsync(inputPath, outputPath8);

            long time8Threads = processor8Threads.ProcessingTime;

            Assert.True(time1Thread > time8Threads);
        }

        public async Task ProcessFileAsync_Check_Performance_HugeFile_1gb()
        {
            string inputPath = Path.Combine(TestDirectory, "1gb.txt");
            CreateHugeTestFileWithLines(inputPath, "Hel lo, worl d!", 1024);
            string outputPath = Path.Combine(OutputDirectory, "output_1gb.txt");

            var processor1Thread = new Core.TextProcessor(
                numberOfThreads: 1,
                minWordLength: 4,
                removePunctuation: true);

            await processor1Thread.ProcessFileAsync(inputPath, outputPath);

            long time1Thread = processor1Thread.ProcessingTime;

            var processor8Threads = new Core.TextProcessor(
                numberOfThreads: 8,
                minWordLength: 4,
                removePunctuation: true);

            await processor8Threads.ProcessFileAsync(inputPath, outputPath);

            long time8Threads = processor8Threads.ProcessingTime;

            Assert.True(time1Thread > time8Threads);
        }

        private string createStringWithRepeatingSymbols(in string symbols, in int numberOfSymbols)
        {
            string result = "";
            for (int i = 0; i < numberOfSymbols; i++)
            {
                result += symbols;
            }
            return result;
        }

        private async Task<bool> CheckProcessString(string inputString, string expectedString, int minWordLength, bool removePunctuation, int numberOfThreads = 1)
        {

            var inputBytes = System.Text.Encoding.UTF8.GetBytes(inputString);
            using var inputStream = new MemoryStream(inputBytes);

            // Prepare output stream
            using var outputStream = new MemoryStream();

            var processor = new Core.TextProcessor(
                minWordLength,
                removePunctuation,
                numberOfThreads);
            await processor.ProcessStreamAsync(inputStream, outputStream);

            // Read processed lines from output stream
            outputStream.Position = 0;
            using var reader = new StreamReader(outputStream);
            var processedText = await reader.ReadToEndAsync();

            return compareStrings(new string[] { expectedString }, new string[] { processedText });
        }

        [Fact]
        public async Task ProcessFileAsync_Diff_Len_Words_Repeated_Symbols_In_One_Line()
        {
            string repeatedLine = "a  aa    aaa!? & aaaa aaa?aa ";

            string line = createStringWithRepeatingSymbols(repeatedLine, 10000);

            string expectedLine = createStringWithRepeatingSymbols("      !? &  ? ", 10000);

            Assert.True(await CheckProcessString(line, expectedLine, 10, false, 1));
            Assert.True(await CheckProcessString(line, expectedLine, 10, false, 4));
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
