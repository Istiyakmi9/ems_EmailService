using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
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
        private readonly IDb _db;

        public KafkaService(IServiceProvider serviceProvider, ILogger<KafkaService> logger, IOptions<KafkaServiceConfig> options, IDb db)
        {
            _serviceProvider = serviceProvider;
            _kafkaServiceConfig = options.Value;
            _logger = logger;
            _db = db;
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
                        try
                        {
                            _logger.LogInformation($"[Kafka] Waiting on topic: {KafkaServiceConfig.AttendanceEmailTopic}");
                            var message = consumer.Consume();

                            HandleMessageSendEmail(message, scope);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Kafka Error]: Got exception - {ex.Message}");
                        }
                    }
                }
            }
        }

        private void HandleMessageSendEmail(ConsumeResult<Null, string> result, IServiceScope scope)
        {
            if (string.IsNullOrWhiteSpace(result.Message.Value))
                throw new Exception("[Kafka] Received invalid object from producer.");

            _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");


            CommonFields commonFields = JsonConvert.DeserializeObject<CommonFields>(result.Message.Value);
            if (commonFields == null)
                throw new Exception("[Kafka] Received invalid object from producer.");

            IEmailServiceRequest emailServiceRequest = null;
            switch (commonFields.kafkaServiceName)
            {
                case KafkaServiceName.Attendance:
                    AttendanceRequestModal attendanceTemplateModel = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
                    if (attendanceTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                    SetDbConnection(attendanceTemplateModel.LocalConnectionString);

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
                    emailServiceRequest.SendEmailNotification(attendanceTemplateModel);
                    break;
                case KafkaServiceName.Billing:
                    BillingTemplateModel billingTemplateModel = JsonConvert.DeserializeObject<BillingTemplateModel>(result.Message.Value);
                    if (billingTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for billing template modal from producer.");

                    SetDbConnection(billingTemplateModel.LocalConnectionString);

                    var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();
                    billingService?.SendEmailNotification(billingTemplateModel);
                    break;
                case KafkaServiceName.Leave:
                    LeaveTemplateModel leaveTemplateModel = JsonConvert.DeserializeObject<LeaveTemplateModel>(result.Message.Value);
                    if (leaveTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for leave template modal from producer.");

                    SetDbConnection(leaveTemplateModel.LocalConnectionString);

                    var leaveRequested = scope.ServiceProvider.GetRequiredService<LeaveRequested>();
                    leaveRequested.SetupEmailTemplate(leaveTemplateModel);
                    break;
                case KafkaServiceName.Payroll:
                    PayrollTemplateModel payrollTemplateModel = JsonConvert.DeserializeObject<PayrollTemplateModel>(result.Message.Value);
                    if (payrollTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for payroll template modal from producer.");

                    SetDbConnection(payrollTemplateModel.LocalConnectionString);

                    var payrollService = scope.ServiceProvider.GetRequiredService<PayrollService>();
                    payrollService?.SendEmailNotification(payrollTemplateModel);
                    break;
                case KafkaServiceName.BlockAttendance:
                    _logger.LogInformation($"[Kafka] Message received: Block attendance get");
                    AttendanceRequestModal attendanceTemplate = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
                    if (attendanceTemplate == null)
                        throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                    SetDbConnection(attendanceTemplate.LocalConnectionString);

                    _logger.LogInformation($"[Kafka] Message received: {attendanceTemplate.ActionType}");
                    switch (attendanceTemplate.ActionType)
                    {
                        case AppConstants.Submitted:
                            emailServiceRequest = scope.ServiceProvider.GetRequiredService<BlockAttendanceActionRequested>();
                            break;
                        case AppConstants.Approved:
                        case AppConstants.Rejected:
                            emailServiceRequest = scope.ServiceProvider.GetRequiredService<BlockAttendanceAction>();
                            break;
                    }

                    _logger.LogInformation($"[Kafka] Starting sending request.");
                    emailServiceRequest!.SendEmailNotification(attendanceTemplate);
                    break;
                case KafkaServiceName.ForgotPassword:
                    _logger.LogInformation($"[Kafka] Message received: Forgot password get");
                    ForgotPasswordTemplateModel forgotPasswordTemplateModel = JsonConvert.DeserializeObject<ForgotPasswordTemplateModel>(result.Message.Value);
                    if (forgotPasswordTemplateModel == null)
                        throw new Exception("[Kafka] Received invalid object for forgot password template modal from producer.");

                    SetDbConnection(forgotPasswordTemplateModel.LocalConnectionString);

                    var forgotpasswordService = scope.ServiceProvider.GetRequiredService<ForgotPasswordRequested>();
                    forgotpasswordService?.SendEmailNotification(forgotPasswordTemplateModel);
                    break;
                case KafkaServiceName.Timesheet:
                    _logger.LogInformation($"[Kafka] Message received: Timesheet get");
                    TimesheetApprovalTemplateModel? timesheetSubmittedTemplate = JsonConvert.DeserializeObject<TimesheetApprovalTemplateModel>(result.Message.Value);
                    if (timesheetSubmittedTemplate == null)
                        throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                    SetDbConnection(timesheetSubmittedTemplate.LocalConnectionString);

                    _logger.LogInformation($"[Kafka] Message received: {timesheetSubmittedTemplate.ActionType}");
                    switch (timesheetSubmittedTemplate.ActionType)
                    {
                        case AppConstants.Submitted:
                            var timesheetRequestedService = scope.ServiceProvider.GetRequiredService<TimesheetRequested>();
                            _logger.LogInformation($"[Kafka] Starting sending request.");
                            timesheetRequestedService?.SendEmailNotification(timesheetSubmittedTemplate);
                            break;
                        case AppConstants.Approved:
                        case AppConstants.Rejected:
                            var timesheetActionService = scope.ServiceProvider.GetRequiredService<TimesheetAction>();
                            _logger.LogInformation($"[Kafka] Starting sending request.");
                            timesheetActionService?.SendEmailNotification(timesheetSubmittedTemplate);
                            break;
                    }

                    break;
            }
        }

        private void SetDbConnection(string cs)
        {
            _logger.LogInformation($"[Kafka] Setting connection: {cs}");
            _db.SetupConnectionString(cs);
        }
    }
}
