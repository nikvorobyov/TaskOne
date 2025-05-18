using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TextProcessor.Core
{
    public class TextProcessor
    {
        private readonly int _numberOfThreads;
        private readonly int _minWordLength;
        private readonly bool _removePunctuation;
        private const int CHUNK_SIZE = 1000000; // Number of lines per chunk for parallel processing

        //for testing
        public long ProcessingTime { get; private set; }

        public TextProcessor(
            int minWordLength,
            bool removePunctuation,
            int numberOfThreads = 1)
        {
            if (minWordLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minWordLength), "Minimum word length must be at least 1.");
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads), "Number of threads must be at least 1.");
            _minWordLength = minWordLength;
            _removePunctuation = removePunctuation;
            _numberOfThreads = numberOfThreads;
        }
        public async Task ProcessStreamAsync(Stream inputStream, Stream outputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream), "Input stream cannot be null.");
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream), "Output stream cannot be null.");
            try
            {
                var totalStopwatch = Stopwatch.StartNew();
                using var reader = new StreamReader(inputStream);
                using var writer = new StreamWriter(outputStream, leaveOpen: true);


                var chunk = new List<string>(CHUNK_SIZE);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    chunk.Add(line);
                    if (chunk.Count >= CHUNK_SIZE)
                    {
                        await ProcessAndWriteChunkAsync(chunk, writer);
                        chunk.Clear();
                    }
                }
                // Process remaining lines
                if (chunk.Count > 0)
                {
                    await ProcessAndWriteChunkAsync(chunk, writer);
                }
                await writer.FlushAsync();
                totalStopwatch.Stop();
                ProcessingTime = totalStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing stream: {ex.Message}", ex);
            }
        }
        public async Task ProcessFileAsync(string inputFilePath, string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path cannot be null or empty.", nameof(inputFilePath));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFilePath));

            // Check if input and output files are the same
            string inputFullPath = Path.GetFullPath(inputFilePath);
            string outputFullPath = Path.GetFullPath(outputFilePath);
            if (string.Equals(inputFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Input and output file paths must not be the same. Please select a different output file or directory.");

            try
            {
                using var reader = new StreamReader(inputFilePath);
                using var writer = new StreamWriter(outputFilePath, false);
                await ProcessStreamAsync(reader.BaseStream, writer.BaseStream);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing file: {ex.Message}", ex);
            }
        }

        // Process a chunk of lines in parallel and return processed lines
        private async Task<string[]> ProcessLinesChunkAsync(List<string> lines)
        {
            int linesPerThread = (int)Math.Ceiling((double)lines.Count / _numberOfThreads);
            var tasks = new List<Task<string[]>>();
            for (int i = 0; i < _numberOfThreads; i++)
            {
                int startIndex = i * linesPerThread;
                int endIndex = Math.Min(startIndex + linesPerThread, lines.Count);
                if (startIndex >= endIndex)
                {
                    break;
                }
                var chunk = lines.GetRange(startIndex, endIndex - startIndex);
                tasks.Add(Task.Run(() => ProcessGroupOfLines(chunk.ToArray())));
            }
            var results = await Task.WhenAll(tasks);
            return results.SelectMany(x => x).ToArray();
        }

        private string[] ProcessGroupOfLines(in string[] lines)
        {
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

            // Pattern to match whole words (sequences of word characters)
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

        // Helper method to process a chunk and write non-empty lines to the writer
        private async Task ProcessAndWriteChunkAsync(List<string> chunk, StreamWriter writer)
        {
            var processed = await ProcessLinesChunkAsync(chunk);
            foreach (var processedLine in processed)
            {
                if (!string.IsNullOrEmpty(processedLine))
                {
                    await writer.WriteLineAsync(processedLine);
                }
            }
        }
    }
}
