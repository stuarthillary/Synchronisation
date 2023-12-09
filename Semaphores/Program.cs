// See https://aka.ms/new-console-template for more information

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_,_) => {cts.Cancel();};

var simulation = new DrivingToDisneyLand(cts.Token);
await simulation.Simulate();

public class DrivingToDisneyLand
{
    private readonly CancellationToken _cancellationToken;
    private readonly Road _road;
    private readonly Driver _driver;
    private readonly Passenger _passenger;

    public DrivingToDisneyLand(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _driver = new Driver(this);
        _road = new Road(this);
        _passenger = new Passenger(this);
    }

    class Road
    {
        private readonly DrivingToDisneyLand _context;

        public Road(DrivingToDisneyLand context)
        {
            _context = context;
        }
        
        public async Task Drive()
        {
            while (!_context._cancellationToken.IsCancellationRequested)
            {
                var danger = new[]
                {
                    "passing truck",
                    "cat on the road",
                    "sub shining in the eye",
                    "drunk driver",
                    "ambulance",
                };

                await _context._driver.Alert(
                    danger[Random.Shared.Next(0, danger.Length)],
                    Random.Shared.Next(3,7)
                );

                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 5)), _context._cancellationToken);
            }
        }
    }

    class Driver
    {
        private readonly DrivingToDisneyLand _context;
        private SemaphoreSlim _busy = new SemaphoreSlim(1);
        
        public Driver(DrivingToDisneyLand context)
        {
            _context = context;
        }

        public async Task Alert(string danger, int durationSeconds)
        {
            await _busy.WaitAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"paying attention to {danger}");

            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), _context._cancellationToken);
            
            Console.WriteLine($"finished paying attention to {danger}");
            _busy.Release();
        }

        public async Task<string> AskQuestion(string question)
        {
            await _busy.WaitAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"processing \"{question}\"");

            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 3)), _context._cancellationToken);

            Console.WriteLine($"answering {question}");
            _busy.Release();
            
            return "Pffft";
        }
    }

    class Passenger
    {
        private readonly DrivingToDisneyLand _context;

        public Passenger(DrivingToDisneyLand context)
        {
            _context = context;
        }

        public async Task BeBored()
        {
            while (!_context._cancellationToken.IsCancellationRequested)
            {
                var questions = new[]
                {
                    "are we there yet?",
                    "can we stop at McDonalds?",
                    "I'm hungry",
                    "I'm bored",
                };

                var answer = await _context._driver.AskQuestion(
                    questions[Random.Shared.Next(0, questions.Length)]);

                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 5)), _context._cancellationToken);
            }
        }
    }

    public Task Simulate()
    {
        return Task.WhenAll(_road.Drive(), _passenger.BeBored());
    }
}