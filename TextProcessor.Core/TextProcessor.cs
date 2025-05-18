using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

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

        private static readonly char[] Separators = { ' ', '.', ',', ';', ':', '!', '?', '\n', '\r', '\t' };

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
        // Reads a block from the stream, ensuring it ends at a separator or throws if a word exceeds maxWordSize
        private async Task<string?> ReadBlockAsync(StreamReader reader, int size = CHUNK_SIZE, int maxWordSize = 1024)
        {
            var buffer = new char[size];
            int read = await reader.ReadAsync(buffer, 0, size);
            if (read == 0)
            {
                return null;
            }

            var chunk = new StringBuilder(new string(buffer, 0, read));

            // If the last character is a separator, return the block as is
            if (Separators.Contains(chunk[chunk.Length - 1]))
            {
                return chunk.ToString();
            }

            int additionallyRead = 0;
            // Read additional characters until a separator is found or maxWordSize is reached
            while (additionallyRead < maxWordSize)
            {
                int nextChar = await reader.ReadAsync(buffer, 0, 1);
                if (nextChar == 0)
                {
                    // End of stream
                    break;
                }

                chunk.Append(buffer[0]);
                additionallyRead++;

                if (Separators.Contains(buffer[0]))
                {
                    return chunk.ToString();
                }
            }

            if (additionallyRead >= maxWordSize)
            {
                throw new Exception($"Word exceeded maxWordSize ({maxWordSize} chars) without separator.");
            }

            // If we reached the end of the stream without finding a separator, return everything we have
            return chunk.ToString();
        }

        // Split block into N sub-blocks, each ending at a separator
        private List<string> SplitBlockForParallel(string block, int numBlocks)
        {
            var subBlocks = new List<string>();
            if (string.IsNullOrEmpty(block) || numBlocks <= 1)
            {
                subBlocks.Add(block);
                return subBlocks;
            }
            int approxSize = block.Length / numBlocks;
            int start = 0;
            for (int i = 1; i < numBlocks; i++)
            {
                int end = Math.Min(start + approxSize, block.Length - 1);
                // Move right until the next separator is found
                while (end < block.Length - 1 && !Separators.Contains(block[end]))
                {
                    end++;
                }
                // If no separator is found — everything until the end of the block
                if (end >= block.Length - 1)
                {
                    end = block.Length - 1;
                }
                subBlocks.Add(block.Substring(start, end - start + 1));
                start = end + 1;
            }
            // The last block — everything that is left
            if (start < block.Length)
                subBlocks.Add(block.Substring(start));
            return subBlocks;
        }

        // Asynchronously processes a block and writes the result
        private async Task ProcessAndWriteBlockAsync(string block, StreamWriter writer)
        {
            var subBlocks = SplitBlockForParallel(block, _numberOfThreads);
            var tasks = new List<Task<string>>();
            foreach (var subBlock in subBlocks)
            {
                tasks.Add(Task.Run(() => ProcessString(subBlock)));
            }
            var results = await Task.WhenAll(tasks);
            var processed = string.Concat(results.SelectMany(x => x));
            if (!string.IsNullOrEmpty(processed))
                await writer.WriteAsync(processed);
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

                string block;
                while ((block = await ReadBlockAsync(reader, CHUNK_SIZE, 1024)) != null)
                {
                    await ProcessAndWriteBlockAsync(block, writer);
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

        private string ProcessString(in string line)
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
    }
}
