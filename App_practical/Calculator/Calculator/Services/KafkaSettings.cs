namespace Calculator.Services;

public sealed class KafkaSettings
{
    public string BootstrapServers { get; set; } = "kafka:9092";
    public string Topic { get; set; } = "calculator-results";
    public string GroupId { get; set; } = "calculator-consumer";
}
