using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.Common.Service.Model;
using Bt.Lib.Common.Service.Services;
using Confluent.Kafka;
using EmailRequest.Modal;
using Newtonsoft.Json;

namespace EmailRequest.Service
{
    public class KafkaGreetingJobManagerService(IServiceProvider _serviceProvider, 
                                        ILogger<KafkaGreetingJobManagerService> _logger,
                                        IDb _db,
                                        MicroserviceRegistry _microserviceRegistry,
                                        GitHubConnector _gitHubConnector)
    {
        public async Task SendEmailNotification(ConsumeResult<Ignore, string> result)
        {
            if (string.IsNullOrWhiteSpace(result.Message.Value))
                throw new Exception("[Kafka] Received invalid object from producer.");

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);
                if (kafkaPayload == null)
                    throw new Exception("[Kafka] Received invalid object from producer.");

                var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<DatabaseConfiguration>(_microserviceRegistry.DatabaseConfigurationUrl);
                _db.SetupConnectionString(DatabaseConfiguration.BuildConnectionString(masterDatabse));

                var commonNotificationRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                _logger.LogInformation($"[Kafka] Starting sending request.");

                _ = commonNotificationRequestService.SendDailyDigestEmailNotification(kafkaPayload);

                _logger.LogInformation($"[Kafka] Message received: ");
            }
        }
    }
}
