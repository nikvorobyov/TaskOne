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

        private async void writeStringsToFile(string[] processedLines)
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
            catch(Exception ex)
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
                string[] allLines = await readAllLinesFromFile();

                if ( allLines.Length < 1 )
                {
                    writeStringsToFile(Array.Empty<string>());
                }

                var processedLines = await ProcessLines(allLines);

                writeStringsToFile(processedLines);

            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing file: {ex.Message}", ex);
            }
        }

        //It's public for comfortable testing
        public async Task<string[]> ProcessLines(string[] lines)
        {

            var processingStopwatch = Stopwatch.StartNew();

            // Calculate lines per thread
            int linesPerThread = (int)Math.Ceiling((double)lines.Length / _numberOfThreads);

            // Calculate actual number of threads needed
            int actualThreads = (int)Math.Ceiling((double)lines.Length / linesPerThread);
            Console.WriteLine($"Processing {lines.Length} lines using {actualThreads} threads");

            // Process parts of lines in different threads
            if (actualThreads > 1)
            {
                // Create tasks for parallel processing
                var tasks = new List<Task<string[]>>(actualThreads - 1);

                for (int i = 0; i < actualThreads - 1; i++)
                {
                    int startIndex = i * linesPerThread;
                    int endIndex = Math.Min(startIndex + linesPerThread, lines.Length);

                    var groupOfLines = lines[startIndex..endIndex];
                    tasks.Add(Task.Run(() => ProcessGroupOfLines(groupOfLines)));
                }

                int lastStartIndex = (actualThreads - 1) * linesPerThread;
                int lastEndIndex = Math.Min(lastStartIndex + linesPerThread, lines.Length);
                var lastSpan = lines[lastStartIndex..lastEndIndex];
                string[] lastLines = ProcessGroupOfLines(lastSpan);

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);

                processingStopwatch.Stop();
                ProcessingTime = processingStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Processing completed in {ProcessingTime}ms");
                Console.WriteLine();

                return tasks.SelectMany(t => t.Result).OrderBy(x => x).Concat(lastLines).ToArray();
            }
            else
            {
                string[] processedLines = ProcessGroupOfLines(lines);


                processingStopwatch.Stop();
                ProcessingTime = processingStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Processing completed in {ProcessingTime}ms");
                Console.WriteLine();

                return processedLines;
            }
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
            if (string.IsNullOrEmpty(line))
                return string.Empty;

            // Найдём все слова (любая буквенно-цифровая последовательность)
            string pattern = @"\b\w+\b";

            string result = Regex.Replace(line, pattern, match =>
            {
                string word = match.Value;

                if (word.Length < _minWordLength)
                {
                    // Удаляем слово полностью
                    return "";
                }
                else
                {
                    // Оставляем слово как есть
                    return word;
                }
            });

            if (_removePunctuation)
            {
                // Убираем знаки препинания, но пробелы оставляем
                result = Regex.Replace(result, @"[^\w\s]", "");
            }

            return result;
            //if (string.IsNullOrEmpty(line))
            //    return string.Empty;

            //// Удаляем слова длиной <= minWordLength
            //string pattern = $@"\b\w{{,{_minWordLength + 1}}}\b";
            //string result = Regex.Replace(line, pattern, "");

            //if (_removePunctuation)
            //{
            //    // Убираем знаки препинания, оставляем пробелы
            //    result = Regex.Replace(result, @"[^\w\s]", "");
            //}

            //// Опционально: убрать лишние пробелы (если нужно красивое оформление)
            ////result = Regex.Replace(result, @"\s+", " ").Trim();

            //return result;
        }
    }
}
