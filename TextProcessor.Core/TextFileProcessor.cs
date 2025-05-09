
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TextProcessor.Core
{
    public class TextFileProcessor
    {
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly int _numberOfThreads;
        private readonly int _minWordLength;
        private readonly bool _removePunctuation;

        //for testing
        public long ProcessingTime { get; private set; }

        public TextFileProcessor(
            string inputFilePath,
            string outputFilePath,
            int minWordLength,
            bool removePunctuation,
            int numberOfThreads = 1)
        {
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
            _minWordLength = minWordLength;
            _removePunctuation = removePunctuation;
            _numberOfThreads = numberOfThreads;
        }

        private async Task<string[]> ReadAllLinesFromFile()
        {
            try
            {
                var readingStopwatch = Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine($"Start reading file {_inputFilePath}.");

                // Read all lines from the input file
                string[] allLines = await File.ReadAllLinesAsync(_inputFilePath);

                if (allLines.Length < 1)
                {
                    return Array.Empty<string>();
                }
                readingStopwatch.Stop();
                Console.WriteLine($"File reading completed in {readingStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();
                return allLines;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading file: {_inputFilePath}", ex);
            }

        }

        private async Task WriteStringsToFile(string[] processedLines)
        {
            try
            {
                var writingStopwatch = Stopwatch.StartNew();
                Console.WriteLine($"Start writing file {_outputFilePath}.");
                await File.WriteAllLinesAsync(_outputFilePath, processedLines);
                writingStopwatch.Stop();
                Console.WriteLine($"File writing completed in {writingStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing file: {_outputFilePath}", ex);
            }
        }
        public async Task ProcessFileAsync()
        {
            try
            {
                var totalStopwatch = Stopwatch.StartNew();

                // Read all lines from the input file
                string[] allLines = await ReadAllLinesFromFile();

                if (allLines.Length < 1)
                {
                    await WriteStringsToFile(Array.Empty<string>());
                    return;
                }

                var processedLines = await ProcessLines(allLines);

                await WriteStringsToFile(processedLines);

            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing file: {ex.Message}", ex);
            }
        }

        //It's public for testing
        public async Task<string[]> ProcessLines(string[] lines)
        {

            var processingStopwatch = Stopwatch.StartNew();

            // Calculate lines per thread
            int linesPerThread = (int)Math.Ceiling((double)lines.Length / _numberOfThreads);

            // Calculate actual number of threads needed
            int actualThreads = (int)Math.Ceiling((double)lines.Length / linesPerThread);
            Console.WriteLine($"Processing {lines.Length} lines using {actualThreads} threads");

            // Process parts of lines in different threads

            // Create tasks for parallel processing
            var tasks = new List<Task<string[]>>(actualThreads);

            for (int i = 0; i < actualThreads; i++)
            {
                int startIndex = i * linesPerThread;
                int endIndex = Math.Min(startIndex + linesPerThread, lines.Length);

                var groupOfLines = lines[startIndex..endIndex];
                tasks.Add(Task.Run(() => ProcessGroupOfLines(groupOfLines)));
            }
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            processingStopwatch.Stop();
            ProcessingTime = processingStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Processing completed in {ProcessingTime}ms");
            Console.WriteLine();

            return tasks.SelectMany(t => t.Result).OrderBy(x => x).ToArray();


        }
        private string[] ProcessGroupOfLines(in string[] lines)
        {
            Console.WriteLine($"Process lines in thread {Thread.CurrentThread.ManagedThreadId}.");

            var processedLines = new string[lines.Length];
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string processedLine = ProcessLine(line);
                if (!string.IsNullOrEmpty(processedLine))
                {
                    processedLines[i] = processedLine;
                }
            }
            return processedLines;
        }

        private string ProcessLine(in string line)
        {
            // Return empty string if input line is null or empty
            if (string.IsNullOrEmpty(line))
                return string.Empty;

            // Sequences of word characters
            string pattern = @"\b\w+\b";

            // Process each word in the line
            string result = Regex.Replace(line, pattern, match =>
            {
                string word = match.Value;

                // Remove words that are shorter than minimum length
                if (word.Length < _minWordLength)
                {
                    return "";
                }
                else
                {
                    return word;
                }
            });

            // Remove all punctuation marks if _removePunctuation is true
            if (_removePunctuation)
            {
                result = Regex.Replace(result, @"[^\w\s]", "");
            }

            return result;
        }
    }
}
