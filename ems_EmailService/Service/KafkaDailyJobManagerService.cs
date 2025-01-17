using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.Common.Service.Model;
using Bt.Lib.Common.Service.Services;
using Confluent.Kafka;
using EmailRequest.Modal;
using Newtonsoft.Json;

namespace EmailRequest.Service
{
    public class KafkaDailyJobManagerService(IServiceProvider _serviceProvider,
                                        ILogger<KafkaDailyJobManagerService> _logger,
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
                _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");

                KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);
                if (kafkaPayload == null)
                    throw new Exception("[Kafka] Received invalid object from producer.");

                if (kafkaPayload.kafkaServiceName == KafkaServiceName.DailyGreetingJob)
                {
                    var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<DatabaseConfiguration>(_microserviceRegistry.DatabaseConfigurationUrl);
                    _db.SetupConnectionString(DatabaseConfiguration.BuildConnectionString(masterDatabse));

                    var commonNotificationRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                    _ = commonNotificationRequestService.SendDailyDigestEmailNotification(kafkaPayload);
                    _logger.LogInformation($"[Kafka] Message send: ");
                }
            }
        }
    }
}
