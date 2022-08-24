using System.CommandLine;
using System.Diagnostics;

var endpointOption = new Option<string>(name: "--endpoint", description: "The endpoint to poll.") { IsRequired = true };
var expectedResponseOption = new Option<string>(name: "--response", description: "The expected resonse from the endpoint to wait for.") { IsRequired = true };
var timeoutOption = new Option<int>(name: "--timeout", description: "The timeout in seconds.", getDefaultValue: () => 5 * 60);

var rootCommand = new RootCommand("Poll an endpoint until it returns the specified response")
{
    endpointOption,
    expectedResponseOption,
    timeoutOption
};

rootCommand.SetHandler(
    async (endpoint, expectedResponse, timeoutSeconds) =>
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        using var cts = new CancellationTokenSource(timeout);
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"Started polling {endpoint} for {expectedResponse}");

        try
        {
            await PollEndpoint(cts.Token);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Received {expectedResponse} from {endpoint} after {stopwatch.Elapsed.TotalSeconds}s.");
            Console.ResetColor();
        }
        catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Timed out waiting for {expectedResponse} from {endpoint} after {timeout.TotalSeconds}s.");
            Console.ResetColor();

            throw;
        }

        async Task PollEndpoint(CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await httpClient.GetStringAsync(endpoint, cancellationToken);

                    if (result == expectedResponse)
                    {
                        return;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
                catch (Exception)
                {
                }

                await Task.Delay(3000, cancellationToken);
            }

            throw new TimeoutException();
        }
    },
    endpointOption,
    expectedResponseOption,
    timeoutOption);

return await rootCommand.InvokeAsync(args);
