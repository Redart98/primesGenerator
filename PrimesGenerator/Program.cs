using Confluent.Kafka;
using PrimesGenerator;
using System.Diagnostics;
using System.Text.Json;

var topicName = "test";
var bootstrapServers = "kafka:29092";

Console.WriteLine("Starting");

var initialPrime = 1m;

var options = new JsonSerializerOptions { Converters = { new NoTimezoneConverter() } };
var consumerConfig = new ConsumerConfig { BootstrapServers = bootstrapServers, GroupId = "PrimesGenerator" };
using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
var topicPartition = new TopicPartition(topicName, new Partition(0));
var watermarkOffsets
    = consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(10));
if (watermarkOffsets.High.Value != watermarkOffsets.Low.Value)
{
    var tpo = new TopicPartitionOffset(topicPartition, new Offset(watermarkOffsets.High.Value - 1));
    consumer.Assign(tpo);
    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
    if (consumeResult?.Message != null)
    {
        initialPrime = JsonSerializer.Deserialize<PrimeDto>(consumeResult.Message.Value, options)!.Number;
    }
}

var generator = new PrimesGenerator.PrimesGenerator(initialPrime);

var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

using var hiresTimer = new HiresTimer();

try
{
    for(int i = 0; i < 20;  i++)
    {
        var next = generator.GetNext();
        producer.Produce(topicName, new Message<string, string>
        {
            Value = JsonSerializer.Serialize(new PrimeDto(next, DateTime.Now), options)
        });
        Console.WriteLine($"Prime: {next}");
        var timestamp = Stopwatch.GetTimestamp();
        var delay = next % 17;
        hiresTimer.Wait(TimeSpan.FromMilliseconds((int)delay));
        Console.WriteLine($"{Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds}ms elapsed");
    }
    var now = DateTime.UtcNow;
    Thread.Sleep(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerSecond));
    Console.WriteLine($"{now} - {DateTime.UtcNow}");
}
finally
{
    producer.Flush();
}

record PrimeDto (decimal Number, DateTime GeneratedAt);
