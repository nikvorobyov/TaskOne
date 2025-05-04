
using TextProcessor.Tests;

class Program
{
    static async Task Main(string[] args)
    {
        using var test = new TextFileProcessorTests();
        try
        {
            await test.ProcessLinesAsync_Test_Hellow_World();
            Console.WriteLine("Test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
        }
    }
}

