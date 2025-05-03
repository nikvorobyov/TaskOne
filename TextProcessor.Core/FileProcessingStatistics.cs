using System;

namespace TextProcessor.Core
{
    /// <summary>
    /// Tracks statistics for file processing operations
    /// </summary>
    public class FileProcessingStatistics
    {
        /// <summary>
        /// Total number of processed lines
        /// </summary>
        public int TotalLines { get; private set; }

        /// <summary>
        /// Number of lines that were empty or contained only whitespace
        /// </summary>
        public int EmptyLines { get; private set; }

        /// <summary>
        /// Total number of words processed
        /// </summary>
        public int TotalWords { get; private set; }

        /// <summary>
        /// Number of words that were filtered out due to length constraints
        /// </summary>
        public int FilteredWords { get; private set; }

        /// <summary>
        /// Processing start time
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Processing end time
        /// </summary>
        public DateTime EndTime { get; private set; }

        public FileProcessingStatistics()
        {
            StartTime = DateTime.Now;
        }

        public void IncrementLines()
        {
            TotalLines++;
        }

        public void IncrementEmptyLines()
        {
            EmptyLines++;
        }

        public void AddWords(int totalInLine, int filteredInLine)
        {
            TotalWords += totalInLine;
            FilteredWords += filteredInLine;
        }

        public void CompleteProcessing()
        {
            EndTime = DateTime.Now;
        }

        public TimeSpan GetProcessingTime()
        {
            return EndTime - StartTime;
        }

        public override string ToString()
        {
            return $@"File Processing Statistics:
- Total lines processed: {TotalLines}
- Empty lines: {EmptyLines}
- Total words: {TotalWords}
- Filtered words: {FilteredWords}
- Processing time: {GetProcessingTime().TotalSeconds:F2} seconds";
        }
    }
} 