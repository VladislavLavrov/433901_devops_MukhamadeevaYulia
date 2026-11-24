using System.Text.Json;
using Calculator.Data;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Calculator.Services;

public sealed class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;

    public KafkaProducerService(IOptions<KafkaSettings> options)
    {
        _settings = options.Value;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All
        }).Build();
    }

    public async Task ProduceAsync(DataInputVariant calculation, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(calculation);
        await _producer.ProduceAsync(
            _settings.Topic,
            new Message<string, string>
            {
                Key = calculation.ID_DataInputVariant.ToString(),
                Value = payload
            },
            cancellationToken);
    }

    public void Dispose() => _producer.Dispose();
}
