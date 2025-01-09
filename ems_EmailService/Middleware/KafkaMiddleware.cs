using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.Model;
using EmailRequest.Service;

namespace EmailRequest.Middleware
{
    public class KafkaService : IHostedService
    {
        private ILogger<KafkaService> _logger;
        private readonly IKafkaConsumerService _kafkaConsumerService;
        private readonly KafkaNotificationManagerService _kafkaNotificationManagerService;
        private readonly KafkaDailyJobManagerService _kafkaDailyJobManagerService;
        private readonly KafkaUnhandleExceptionService _kafkaUnhandleExceptionService;
        public KafkaService(ILogger<KafkaService> logger,
                             IKafkaConsumerService kafkaConsumerService,
                             KafkaUnhandleExceptionService kafkaUnhandleExceptionService,
                             KafkaNotificationManagerService kafkaNotificationManagerService,
                             KafkaDailyJobManagerService kafkaDailyJobManagerService)
        {
            _logger = logger;
            _kafkaConsumerService = kafkaConsumerService;
            _kafkaUnhandleExceptionService = kafkaUnhandleExceptionService;
            _kafkaNotificationManagerService = kafkaNotificationManagerService;
            _kafkaDailyJobManagerService = kafkaDailyJobManagerService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Kafka] Kafka listener registered successfully.");

            _kafkaConsumerService.SubscribeTopic(_kafkaDailyJobManagerService.SendEmailNotification, nameof(KafkaTopicNames.DAILY_JOBS_MANAGER));
            _kafkaConsumerService.SubscribeTopic(_kafkaNotificationManagerService.SendEmailNotification, nameof(KafkaTopicNames.ATTENDANCE_REQUEST_ACTION));
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
