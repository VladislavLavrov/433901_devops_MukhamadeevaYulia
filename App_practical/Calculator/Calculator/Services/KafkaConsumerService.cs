using System.Text.Json;
using Calculator.Data;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Calculator.Services;

public sealed class KafkaConsumerService : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(IOptions<KafkaSettings> options, ILogger<KafkaConsumerService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => Consume(stoppingToken), stoppingToken);

    private void Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_settings.Topic);
        _logger.LogInformation("Kafka consumer подписан на топик {Topic}", _settings.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var record = consumer.Consume(stoppingToken);
                var calculation = JsonSerializer.Deserialize<DataInputVariant>(record.Message.Value);
                _logger.LogInformation(
                    "Kafka consumer получил расчёт #{Id}: {Operand1} {Operation} {Operand2} = {Result}; partition {Partition}, offset {Offset}",
                    calculation?.ID_DataInputVariant,
                    calculation?.Operand_1,
                    calculation?.Type_operation,
                    calculation?.Operand_2,
                    calculation?.Result,
                    record.Partition.Value,
                    record.Offset.Value);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Ошибка получения сообщения из Kafka");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Не удалось разобрать сообщение Kafka");
            }
        }

        consumer.Close();
    }
}
