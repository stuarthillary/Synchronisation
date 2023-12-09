// See https://aka.ms/new-console-template for more information

using System.Threading.Channels;

var cts = new CancellationTokenSource();
var startColour = Console.ForegroundColor;
Console.CancelKeyPress += (_, _) => { 
    cts.Cancel();
    Console.ForegroundColor = startColour;
};

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

                await _context._driver.Send(
                    new Driver.Alert(
                        danger[Random.Shared.Next(0, danger.Length)],
                        Random.Shared.Next(3,7)
                    )
                );

                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 5)), _context._cancellationToken);
            }
        }
    }

    class Driver
    {
        private Channel<IMessage> _channel = Channel.CreateUnbounded<IMessage>();

        public async Task Listen()
        {
            while (!_context._cancellationToken.IsCancellationRequested)
            {
                var msg = await _channel.Reader.ReadAsync();

                switch (msg)
                {
                    case Alert alert:
                        await ProcessAlert(alert);
                        break;
                    case AskQuestion askQuestion:
                        await ProcessAskQuestion(askQuestion);
                        break;
                }
            }
        }

        public ValueTask Send<T>(T msg) where T: IMessage => _channel.Writer.WriteAsync(msg);

        public interface IMessage { }

        public abstract record AwaitableMessage<TReturn>
        {
            private TaskCompletionSource<TReturn> _resolve = new();
            public Task<TReturn> Result() => _resolve.Task;
            public void Complete(TReturn result) => _resolve.SetResult(result);
        }
        
        public record Alert(string Danger, int DurationSeconds) : IMessage;
        public record AskQuestion(string Question) : AwaitableMessage<string>, IMessage;
        
        private readonly DrivingToDisneyLand _context;

        public Driver(DrivingToDisneyLand context)
        {
            _context = context;
        }

        private async Task ProcessAlert(Alert alert)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"paying attention to {alert.Danger}");

            await Task.Delay(TimeSpan.FromSeconds(alert.DurationSeconds), _context._cancellationToken);
            
            Console.WriteLine($"finished paying attention to {alert.Danger}");
        }

        private async Task ProcessAskQuestion(AskQuestion question)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"processing \"{question.Question}\"");

            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 3)), _context._cancellationToken);

            Console.WriteLine($"answering {question.Question}");
            
            question.Complete("Pffft");
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

                var msg = new Driver.AskQuestion(
                    questions[Random.Shared.Next(0, questions.Length)]
                );
                
                await _context._driver.Send(msg);
                var answer = await msg.Result();

                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 5)), _context._cancellationToken);
            }
        }
    }

    public Task Simulate()
    {
        return Task.WhenAll(_road.Drive(), _driver.Listen(), _passenger.BeBored());
    }
}