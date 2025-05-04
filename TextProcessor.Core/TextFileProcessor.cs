using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        private async Task<string[]> readAllLinesFromFile()
        {
            var readingStopwatch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine($"Start reading file {_inputFilePath}.");

            // Read all lines from the input file
            string[] allLines = await File.ReadAllLinesAsync(_inputFilePath);

            if (allLines.Length < 1)
            {
                return new string[] { "" };
            }
            readingStopwatch.Stop();
            Console.WriteLine($"File reading completed in {readingStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();

        }
        public async Task ProcessFileAsync()
        {
            try
            {
                var totalStopwatch = Stopwatch.StartNew();
                var readingStopwatch = Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine($"Start reading file {_inputFilePath}.");
                
                // Read all lines from the input file
                string[] allLines = await File.ReadAllLinesAsync(_inputFilePath);

                if ( allLines.Length < 1 )
                {
                    return;
                }
                readingStopwatch.Stop();
                Console.WriteLine($"File reading completed in {readingStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();
                var processingStopwatch = Stopwatch.StartNew();

                // Calculate lines per thread
                int linesPerThread = (int)Math.Ceiling((double)allLines.Length / _numberOfThreads);

                // Calculate actual number of threads needed
                int actualThreads = (int)Math.Ceiling((double)allLines.Length / linesPerThread);
                Console.WriteLine($"Processing {allLines.Length} lines using {actualThreads} threads");

                if (actualThreads > 1)
                {
                    // Create tasks for parallel processing
                    var tasks = new List<Task<string[]>>(actualThreads - 1);

                    for (int i = 0; i < actualThreads - 1; i++)
                    {
                        int startIndex = i * linesPerThread;
                        int endIndex = Math.Min(startIndex + linesPerThread, allLines.Length);

                        var lines = allLines[startIndex..endIndex];
                        tasks.Add(Task.Run(() => ProcessLines(lines)));
                    }

                    int lastStartIndex = (actualThreads - 1) * linesPerThread;
                    int lastEndIndex = Math.Min(lastStartIndex + linesPerThread, allLines.Length);
                    var lastSpan = allLines[lastStartIndex..lastEndIndex];
                    string[] lastLines = ProcessLines(lastSpan);

                    // Wait for all tasks to complete
                    await Task.WhenAll(tasks);

                    processingStopwatch.Stop();
                    ProcessingTime = processingStopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Processing completed in {ProcessingTime}ms");
                    Console.WriteLine();


                    var writingStopwatch = Stopwatch.StartNew();
                    Console.WriteLine($"Start writing file {_outputFilePath}.");
                    // Write processed lines to output file
                    await File.WriteAllLinesAsync(_outputFilePath, tasks.SelectMany(t => t.Result).OrderBy(x => x).Concat(lastLines));

                    writingStopwatch.Stop();
                    Console.WriteLine($"File writing completed in {writingStopwatch.ElapsedMilliseconds}ms");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Process started in thread {Thread.CurrentThread.ManagedThreadId}.");
                    string[] processedLines = ProcessLines(allLines);
                    processingStopwatch.Stop();
                    ProcessingTime = processingStopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Processing completed in {ProcessingTime}ms");
                    Console.WriteLine();

                    var writingStopwatch = Stopwatch.StartNew();
                    Console.WriteLine($"Start writing file {_outputFilePath}.");
                    await File.WriteAllLinesAsync(_outputFilePath, processedLines);
                    writingStopwatch.Stop();
                    Console.WriteLine($"File writing completed in {writingStopwatch.ElapsedMilliseconds}ms");
                }

                totalStopwatch.Stop();
                Console.WriteLine($"Total processing time: {totalStopwatch.ElapsedMilliseconds}ms");

            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing file: {ex.Message}", ex);
            }
        }

        private string[] ProcessLines(in string[] lines)
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
                if (!string.IsNullOrWhiteSpace(processedLine))
                {
                    processedLines[i] = processedLine;
                }
            }
            return processedLines;
        }

        private string ProcessLine(in string line)
        {
            // Split into words
            string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Remove punctuation if specified and filter by length
            var processedWords = words
                .Select(word => _removePunctuation ? Regex.Replace(word, @"[^\w\s]", "") : word)
                .Where(word => word.Length >= _minWordLength)
                .ToArray();

            return string.Join(_removePunctuation? "" : " ", processedWords);
        }
    }
}
