
using TextProcessor.Tests;

class Program
{
    static async Task Main(string[] args)
    {
        using var test = new TextFileProcessorTests();
        try
        {
            await test.ProcessFileAsync_HugeFile_1gb();
            Console.WriteLine("Test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
        }
    }
}

