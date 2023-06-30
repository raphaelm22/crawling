using Crawling;
using var cts = new CancellationTokenSource();


while (!args.Any())
{
    Console.Write("Type Crawler name and parameters: ");
    var debugArgs = Console.ReadLine() ?? "";
    args = debugArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

await Setup.Perform()
    .RunAsync(args, cts.Token);
