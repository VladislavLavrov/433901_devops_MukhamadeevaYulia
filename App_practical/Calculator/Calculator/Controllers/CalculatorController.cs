using System.Globalization;
using Calculator.Data;
using Calculator.Services;
using Microsoft.AspNetCore.Mvc;

namespace Calculator.Controllers
{
    public enum Operation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public class CalculatorController : Controller
    {
        private readonly CalculatorContext _context;
        private readonly KafkaProducerService _producer;
        private readonly ILogger<CalculatorController> _logger;

        public CalculatorController(CalculatorContext context, KafkaProducerService producer, ILogger<CalculatorController> logger)
        {
            _context = context;
            _producer = producer;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(double num1, double num2, Operation operation, CancellationToken cancellationToken)
        {
            if (operation == Operation.Divide && num2 == 0)
            {
                ViewBag.Error = "На ноль делить нельзя.";
                ViewBag.Num1 = num1;
                ViewBag.Num2 = num2;
                return View("Index");
            }

            var result = operation switch
            {
                Operation.Add => num1 + num2,
                Operation.Subtract => num1 - num2,
                Operation.Multiply => num1 * num2,
                Operation.Divide => num1 / num2,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };

            ViewBag.Num1 = num1;
            ViewBag.Num2 = num2;
            ViewBag.Result = result;

            var calculation = new DataInputVariant
            {
                Operand_1 = num1.ToString(CultureInfo.InvariantCulture),
                Operand_2 = num2.ToString(CultureInfo.InvariantCulture),
                Type_operation = operation.ToString(),
                Result = result.ToString(CultureInfo.InvariantCulture)
            };

            _context.DataInputVariants.Add(calculation);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _producer.ProduceAsync(calculation, cancellationToken);
                ViewBag.KafkaStatus = "Расчёт сохранён в MariaDB и отправлен в Kafka.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить расчёт #{Id} в Kafka", calculation.ID_DataInputVariant);
                ViewBag.KafkaStatus = "Расчёт сохранён в MariaDB, но Kafka временно недоступна.";
            }

            return View("Index");
        }
    }
}
