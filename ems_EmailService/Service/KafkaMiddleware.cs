using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Confluent.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using EmailRequest.Service.Interface;
using EmailRequest.Service.TemplateService;
using Microsoft.Extensions.Options;
using ModalLayer;
using Newtonsoft.Json;

namespace EmailRequest.Service
{
    public class KafkaService : IHostedService
    {
        private readonly KafkaServiceConfig _kafkaServiceConfig;
        private ILogger<KafkaService> _logger;
        private IServiceProvider _serviceProvider;

        public KafkaService(IServiceProvider serviceProvider, ILogger<KafkaService> logger, IOptions<KafkaServiceConfig> options)
        {
            _serviceProvider = serviceProvider;
            _kafkaServiceConfig = options.Value;
            _logger = logger;
        }

        public KafkaServiceConfig KafkaServiceConfig => _kafkaServiceConfig;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Kafka] Kafka listener registered successfully.");

            Task.Run(() =>
            {
                SubscribeKafkaTopic();
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping service.");
            return Task.CompletedTask;
        }

        private void SubscribeKafkaTopic()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var config = new ConsumerConfig
                {
                    GroupId = "gid-consumers",
                    BootstrapServers = $"{KafkaServiceConfig.ServiceName}:{KafkaServiceConfig.Port}"
                };

                _logger.LogInformation($"[Kafka] Start listning kafka topic: {KafkaServiceConfig.AttendanceEmailTopic}");
                using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
                {
                    consumer.Subscribe(KafkaServiceConfig.AttendanceEmailTopic);
                    while (true)
                    {
                        _logger.LogInformation($"[Kafka] Waiting on topic: {KafkaServiceConfig.AttendanceEmailTopic}");
                        var message = consumer.Consume();

                        HandleMessageSendEmail(message, scope);
                    }
                }
            }
        }

        private void HandleMessageSendEmail(ConsumeResult<Null, string> result, IServiceScope scope)
        {
            if (string.IsNullOrWhiteSpace(result.Message.Value))
                throw new Exception("[Kafka] Received invalid object from producer.");

            _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");


            CommonFields? commonFields = JsonConvert.DeserializeObject<CommonFields>(result.Message.Value);
            if (commonFields == null)
                throw new Exception("[Kafka] Received invalid object from producer.");

            IEmailServiceRequest? emailServiceRequest = null;
            switch (commonFields?.kafkaServiceName)
            {
                case KafkaServiceName.Attendance:
                    AttendanceRequestModal? attendanceTemplateModel = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
                    if (attendanceTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                    switch (attendanceTemplateModel.ActionType)
                    {
                        case AppConstants.Submitted:
                            emailServiceRequest = scope.ServiceProvider.GetRequiredService<AttendanceRequested>();
                            break;
                        case AppConstants.Approved:
                        case AppConstants.Rejected:
                            emailServiceRequest = scope.ServiceProvider.GetRequiredService<AttendanceAction>();
                            break;
                    }

                    _logger.LogInformation($"[Kafka] Starting sending request.");
                    emailServiceRequest!.SendEmailNotification(attendanceTemplateModel);
                    break;
                case KafkaServiceName.Billing:
                    BillingTemplateModel? billingTemplateModel = JsonConvert.DeserializeObject<BillingTemplateModel>(result.Message.Value);
                    if (billingTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for billing template modal from producer.");

                    var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();
                    billingService?.SendEmailNotification(billingTemplateModel);
                    break;
                case KafkaServiceName.Leave:
                    LeaveTemplateModel? leaveTemplateModel = JsonConvert.DeserializeObject<LeaveTemplateModel>(result.Message.Value);
                    if (leaveTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for leave template modal from producer.");

                    var leaveRequested = scope.ServiceProvider.GetRequiredService<LeaveRequested>();
                    leaveRequested.SetupEmailTemplate(leaveTemplateModel);
                    break;
            }
        }
    }
}
