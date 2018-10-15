namespace Serverless.Simulator
{
    using Microsoft.Azure.EventHubs;
    using Serverless.Serialization.Models;
    using Serverless.Serialization;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    class Program
    {
        public const int SimulatedDelayBetweenMessages = 1000;
        private static CancellationTokenSource cts;

        private const string DevicesPrefix = "drone-";

        private static async Task GenerateTelemetryAsync<T>(Func<T, string, bool, T> factory,
            ObjectPool<EventHubClient> pool, TelemetrySerializer<T> serializer, int randomSeed, AsyncConsole console, int generateKeyframeGap, int simulatedDelayInMs, int waittime, string deviceId) where T : class
        {

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (console == null)
            {
                throw new ArgumentNullException(nameof(console));
            }

            if (waittime > 0)
            {
                TimeSpan span = TimeSpan.FromMilliseconds(waittime);
                await Task.Delay(span);
            }

            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            Random random = new Random(randomSeed);

            // buffer block that holds the messages . consumer will fetch records from this block asynchronously.
            BufferBlock<T> buffer = new BufferBlock<T>(new DataflowBlockOptions()
            {
                BoundedCapacity = 100000
            });

            // consumer that sends the data to event hub asynchronoulsy.
            var consumer = new ActionBlock<T>(
                action: (t) =>
                {
                    using (var client = pool.GetObject())
                    {
                        return client.Value.SendAsync(new EventData(serializer.Serialize(t))).ContinueWith(
                            async task =>
                            {
                                cts.Cancel();
                                await console.WriteLine(task.Exception.InnerException.Message);
                                await console.WriteLine($"event hub client failed for {deviceId}");
                            }, TaskContinuationOptions.OnlyOnFaulted
                        );

                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 100000,
                    CancellationToken = cts.Token,
                    MaxDegreeOfParallelism = 100,
                }
            );

            // link the buffer to consumer .
            buffer.LinkTo(consumer, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            long messages = 0;

            List<Task> taskList = new List<Task>();
            T telemetryObject = null;
            T lastKeyFrameTelemetryObject = null;
            bool keyFrame = true;
            var generateTask = Task.Factory.StartNew(
                async () =>
                {
                    // generate telemetry records and send them to buffer block
                    for (; ; )
                    {
                        telemetryObject = factory(lastKeyFrameTelemetryObject, deviceId, keyFrame);
                        await buffer.SendAsync(telemetryObject).ConfigureAwait(false);

                        // Save the last key frame for stateful generation
                        if (keyFrame)
                        {
                            lastKeyFrameTelemetryObject = telemetryObject;

                            // Turn key frame off after sending a key frame
                            keyFrame = false;
                        }

                        if (++messages % generateKeyframeGap == 0)
                        {
                            await console.WriteLine($"Created records for {deviceId} - generating key frame").ConfigureAwait(false);

                            // since rec is changing, makes sense to generate a key frame
                            keyFrame = true;
                        }
                        else
                        {
                            await console.WriteLine($"Created {messages} records for {deviceId}").ConfigureAwait(false);

                            // Wait for given number of milliseconds after each messaage
                            await Task.Delay(simulatedDelayInMs).ConfigureAwait(false);
                        }

                        // Every few messages, send a key frame randomly
                        if (messages % random.Next(10, 100) == 0) keyFrame = true;


                        if (cts.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    buffer.Complete();
                    await Task.WhenAll(buffer.Completion, consumer.Completion);
                    await console.WriteLine($"Created total {messages} records for {deviceId}").ConfigureAwait(false);
                }
            ).Unwrap().ContinueWith(
                async task =>
                {
                    cts.Cancel();
                    await console.WriteLine($"failed to generate telemetry for {deviceId}").ConfigureAwait(false);
                    await console.WriteLine(task.Exception.InnerException.Message).ConfigureAwait(false);
                }, TaskContinuationOptions.OnlyOnFaulted
            );

            // await on consumer completion. Incase if sending is failed at any moment ,
            // exception is thrown and caught . This is used to signal the cancel the reading operation and abort all activity further

            try
            {
                await Task.WhenAll(consumer.Completion, generateTask);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                await console.WriteLine(ex.Message).ConfigureAwait(false);
                await console.WriteLine($"failed to generate telemetry").ConfigureAwait(false);
                throw;
            }

        }

        private static (string EventHubConnectionString,
            int MillisecondsToRun, int GenerateKeyframeGap, int NumberOfDevices) ParseArguments()
        {
            var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTION_STRING");
            var numberOfMillisecondsToRun = (int.TryParse(Environment.GetEnvironmentVariable("SECONDS_TO_RUN"), out int outputSecondToRun) ? outputSecondToRun : 0) * 1000;
            var generateKeyframeGap = int.TryParse(Environment.GetEnvironmentVariable("GENERATE_KEYFRAME_GAP"), out int genKeyframeGap) ? genKeyframeGap : 100;
            var numberOfDevices = int.TryParse(Environment.GetEnvironmentVariable("NUMBER_OF_DEVICES"), out int numDevices) ? numDevices : 1000;

            if (string.IsNullOrWhiteSpace(eventHubConnectionString))
            {
                throw new ArgumentException("eventHubConnectionString must be provided");
            }

            return (eventHubConnectionString, numberOfMillisecondsToRun, generateKeyframeGap, numberOfDevices);
        }

        // blocking collection that helps to print to console the messages on progress on the generation/send to event hub.
        private class AsyncConsole
        {
            private BlockingCollection<string> _blockingCollection = new BlockingCollection<string>();
            private CancellationToken _cancellationToken;
            private Task _writerTask;

            public AsyncConsole(CancellationToken cancellationToken = default(CancellationToken))
            {
                _cancellationToken = cancellationToken;
                _writerTask = Task.Factory.StartNew((state) =>
                {
                    var token = (CancellationToken)state;
                    string msg;
                    while (!token.IsCancellationRequested)
                    {
                        if (_blockingCollection.TryTake(out msg, 500))
                        {
                            Console.WriteLine(msg);
                        }
                    }

                    while (_blockingCollection.TryTake(out msg, 100))
                    {
                        Console.WriteLine(msg);
                    }
                }, _cancellationToken, TaskCreationOptions.LongRunning);
            }

            public Task WriteLine(string toWrite)
            {
                _blockingCollection.Add(toWrite);
                return Task.FromResult(0);
            }

            public Task WriterTask
            {
                get { return _writerTask; }
            }
        }

        //  start of the telemetry generation task
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var (EventHubConnectionString, MillisecondsToRun, GenerateKeyframeGap, NumberOfDevices) = ParseArguments();
                var eventHubClient = EventHubClient.CreateFromConnectionString(EventHubConnectionString);
                cts = MillisecondsToRun == 0 ? new CancellationTokenSource() : new CancellationTokenSource(MillisecondsToRun);

                Console.CancelKeyPress += (s, e) =>
                {
                    Console.WriteLine("Cancelling data generation");
                    cts.Cancel();
                    e.Cancel = true;
                };

                AsyncConsole console = new AsyncConsole(cts.Token);

                var eventHubClientPool = new ObjectPool<EventHubClient>(() => EventHubClient.CreateFromConnectionString(EventHubConnectionString), 100);

                var tasks = new List<Task>();
                for (int i = 0; i < NumberOfDevices; i++)
                {
                    tasks.Add(GenerateTelemetryAsync(TelemetryGenerator.GetTimeElapsedTelemetry, eventHubClientPool, new TelemetrySerializer<DroneState>(), 100, console, GenerateKeyframeGap, SimulatedDelayBetweenMessages, 1000, DevicesPrefix+i));
                }

                tasks.Add(console.WriterTask);

                await Task.WhenAll(tasks.ToArray());
                Console.WriteLine("Data generation complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Data generation failed");
                return 1;
            }

            return 0;
        }
    }
}