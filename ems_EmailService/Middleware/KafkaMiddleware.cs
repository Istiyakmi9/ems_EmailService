using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.Model;
using EmailRequest.Service;

namespace EmailRequest.Middleware
{
    public class KafkaService : IHostedService
    {
        private ILogger<KafkaService> _logger;
        private readonly IKafkaConsumerService _kafkaConsumerService;
        private readonly KafkaGreetingJobManagerService _greetingJobManagerService;
        private readonly KafkaDailyJobManagerService _dailyJobManagerService;
        private readonly KafkaUnhandleExceptionService _kafkaUnhandleExceptionService;
        public KafkaService(ILogger<KafkaService> logger,
                             IKafkaConsumerService kafkaConsumerService,
                             KafkaGreetingJobManagerService greetingJobManagerService,
                             KafkaDailyJobManagerService dailyJobManagerService,
                             KafkaUnhandleExceptionService kafkaUnhandleExceptionService)
        {
            _logger = logger;
            _kafkaConsumerService = kafkaConsumerService;
            _greetingJobManagerService = greetingJobManagerService;
            _dailyJobManagerService = dailyJobManagerService;
            _kafkaUnhandleExceptionService = kafkaUnhandleExceptionService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Kafka] Kafka listener registered successfully.");

            _kafkaConsumerService.SubscribeTopic(_dailyJobManagerService.SendEmailNotification, nameof(KafkaTopicNames.DAILY_JOBS_MANAGER));
            _kafkaConsumerService.SubscribeTopic(_greetingJobManagerService.SendEmailNotification, nameof(KafkaTopicNames.ATTENDANCE_REQUEST_ACTION));
            _kafkaConsumerService.SubscribeTopic(_kafkaUnhandleExceptionService.SendEmailNotification, nameof(KafkaTopicNames.EXCEPTION_MESSAGE_BROKER));

            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping service.");
            return Task.CompletedTask;
        }
    }
}
