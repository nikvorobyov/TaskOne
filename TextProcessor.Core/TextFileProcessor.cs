using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextProcessor.Core
{
    public class TextFileProcessor
    {
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly int _numberOfThreads;
        private readonly int _minWordLength;
        private readonly bool _removePunctuation;
        private readonly FileProcessingStatistics _statistics;

        public TextFileProcessor(
            string inputFilePath,
            string outputFilePath,
            int numberOfThreads,
            int minWordLength,
            bool removePunctuation)
        {
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
            _numberOfThreads = numberOfThreads;
            _minWordLength = minWordLength;
            _removePunctuation = removePunctuation;
            _statistics = new FileProcessingStatistics();
        }

        public FileProcessingStatistics Statistics => _statistics;

        public async Task ProcessFileAsync()
        {
            try
            {
                // Read all lines from the input file
                string[] allLines = await File.ReadAllLinesAsync(_inputFilePath);

                // Calculate lines per thread
                int linesPerThread = (int)Math.Ceiling((double)allLines.Length / _numberOfThreads);

                // Create a concurrent bag to store processed lines
                var processedLines = new ConcurrentBag<string>();

                // Create tasks for parallel processing
                var tasks = new List<Task>();
                
                // Calculate actual number of threads needed
                int actualThreads = (int)Math.Ceiling((double)allLines.Length / linesPerThread);
                
                for (int i = 0; i < actualThreads; i++)
                {
                    int startIndex = i * linesPerThread;
                    int endIndex = Math.Min(startIndex + linesPerThread, allLines.Length);
                    
                    var lines = allLines[startIndex..endIndex];
                    tasks.Add(Task.Run(() => ProcessLines(lines, processedLines)));
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);

                // Write processed lines to output file
                await File.WriteAllLinesAsync(_outputFilePath, processedLines.OrderBy(x => x));

                // Complete statistics
                _statistics.CompleteProcessing();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing file: {ex.Message}", ex);
            }
        }

        private void ProcessLines(string[] lines, ConcurrentBag<string> processedLines)
        {
            foreach (var line in lines)
            {
                _statistics.IncrementLines();
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    _statistics.IncrementEmptyLines();
                    continue;
                }

                string processedLine = ProcessLine(line);
                if (!string.IsNullOrWhiteSpace(processedLine))
                {
                    processedLines.Add(processedLine);
                }
            }
        }

        private string ProcessLine(string line)
        {
            // Remove punctuation if specified
            string processedLine = _removePunctuation 
                ? Regex.Replace(line, @"[^\w\s]", " ")
                : line;

            // Split into words and filter by length
            string[] words = processedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredWords = words.Where(word => word.Length >= _minWordLength).ToArray();

            // Update statistics
            _statistics.AddWords(words.Length, words.Length - filteredWords.Length);

            return string.Join(" ", filteredWords);
        }
    }
}
