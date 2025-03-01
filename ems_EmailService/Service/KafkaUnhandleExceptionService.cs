﻿using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.Services;
using Confluent.Kafka;
using EmailRequest.Modal;
using Newtonsoft.Json;

namespace EmailRequest.Service
{
    public class KafkaUnhandleExceptionService(IServiceProvider _serviceProvider,
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

                var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceRegistry.DatabaseConfigurationUrl);
                _db.SetupConnectionString(masterDatabse);

                _logger.LogInformation($"[Kafka] Got unhandled exception");

                if (kafkaPayload == null)
                    throw new Exception("[Kafka] Received invalid object. Getting null value.");

                var comService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                _ = comService.SendUnhandledExceptionEmailNotification(kafkaPayload);
            }
        }
    }
}
