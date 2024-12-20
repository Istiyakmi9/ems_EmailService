﻿using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.Common.Service.KafkaService.interfaces;
using Confluent.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using EmailRequest.Modal.Common;
using EmailRequest.Service;
using EmailRequest.Service.Interface;
using EmailRequest.Service.TemplateService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EmailRequest.Middleware
{
    public class KafkaService : IHostedService
    {
        private ILogger<KafkaService> _logger;
        private IServiceProvider _serviceProvider;
        private readonly MasterDatabase _masterDatabase;
        private readonly IKafkaConsumerService _kafkaConsumerService;
        private readonly IDb _db;

        public KafkaService(IServiceProvider serviceProvider, ILogger<KafkaService> logger,
            IDb db, IOptions<MasterDatabase> masterConfigOptions, IKafkaConsumerService kafkaConsumerService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _masterDatabase = masterConfigOptions.Value;
            _db = db;
            _kafkaConsumerService = kafkaConsumerService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Kafka] Kafka listener registered successfully.");

            await _kafkaConsumerService.SubscribeKafkaService(SendEmailNotification);

            await Task.CompletedTask;
        }

        private async Task SendEmailNotification(ConsumeResult<Null, string> result)
        {
            if (string.IsNullOrWhiteSpace(result.Message.Value))
                throw new Exception("[Kafka] Received invalid object from producer.");

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");

                KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);
                if (kafkaPayload == null)
                    throw new Exception("[Kafka] Received invalid object from producer.");

                _db.SetupConnectionString(MasterDatabase.BuildConnectionString(_masterDatabase));

                IEmailServiceRequest emailServiceRequest = null;
                switch (kafkaPayload.kafkaServiceName)
                {
                    case KafkaServiceName.Attendance:
                        AttendanceRequestModal attendanceTemplateModel = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
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

                        await emailServiceRequest.SendEmailNotification(attendanceTemplateModel);
                        break;
                    case KafkaServiceName.Billing:
                        BillingTemplateModel billingTemplateModel = JsonConvert.DeserializeObject<BillingTemplateModel>(result.Message.Value);
                        if (billingTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for billing template modal from producer.");

                        var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();
                        _ = billingService.SendEmailNotification(billingTemplateModel);
                        break;
                    case KafkaServiceName.Leave:
                        LeaveTemplateModel leaveTemplateModel = JsonConvert.DeserializeObject<LeaveTemplateModel>(result.Message.Value);
                        if (leaveTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for leave template modal from producer.");

                        var leaveRequested = scope.ServiceProvider.GetRequiredService<LeaveRequested>();
                        leaveRequested.SetupEmailTemplate(leaveTemplateModel);
                        break;
                    case KafkaServiceName.Payroll:
                        PayrollTemplateModel payrollTemplateModel = JsonConvert.DeserializeObject<PayrollTemplateModel>(result.Message.Value);
                        if (payrollTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for payroll template modal from producer.");

                        var payrollService = scope.ServiceProvider.GetRequiredService<PayrollService>();
                        payrollService.SendEmailNotification(payrollTemplateModel);
                        break;
                    case KafkaServiceName.BlockAttendance:
                        _logger.LogInformation($"[Kafka] Message received: Block attendance get");
                        AttendanceRequestModal attendanceTemplate = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
                        if (attendanceTemplate == null)
                            throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

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
                        await emailServiceRequest!.SendEmailNotification(attendanceTemplate);
                        break;
                    case KafkaServiceName.ForgotPassword:
                        _logger.LogInformation($"[Kafka] Message received: Forgot password get");
                        ForgotPasswordTemplateModel forgotPasswordTemplateModel = JsonConvert.DeserializeObject<ForgotPasswordTemplateModel>(result.Message.Value);
                        if (forgotPasswordTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for forgot password template modal from producer.");

                        var forgotpasswordService = scope.ServiceProvider.GetRequiredService<ForgotPasswordRequested>();
                        _ = forgotpasswordService.SendEmailNotification(forgotPasswordTemplateModel);
                        break;
                    case KafkaServiceName.Timesheet:
                        _logger.LogInformation($"[Kafka] Message received: Timesheet get");
                        TimesheetApprovalTemplateModel timesheetSubmittedTemplate = JsonConvert.DeserializeObject<TimesheetApprovalTemplateModel>(result.Message.Value);
                        if (timesheetSubmittedTemplate == null)
                            throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                        _logger.LogInformation($"[Kafka] Message received: {timesheetSubmittedTemplate.ActionType}");
                        switch (timesheetSubmittedTemplate.ActionType)
                        {
                            case AppConstants.Submitted:
                                var timesheetRequestedService = scope.ServiceProvider.GetRequiredService<TimesheetRequested>();
                                _logger.LogInformation($"[Kafka] Starting sending request.");
                                _ = timesheetRequestedService.SendEmailNotification(timesheetSubmittedTemplate);
                                break;
                            case AppConstants.Approved:
                            case AppConstants.Rejected:
                                var timesheetActionService = scope.ServiceProvider.GetRequiredService<TimesheetAction>();
                                _logger.LogInformation($"[Kafka] Starting sending request.");
                                _ = timesheetActionService.SendEmailNotification(timesheetSubmittedTemplate);
                                break;
                        }

                        break;
                    case KafkaServiceName.Common:
                        _logger.LogInformation($"[Kafka] Message received: Timesheet get");
                        if (kafkaPayload == null)
                            throw new Exception("[Kafka] Received invalid object. Getting null value.");

                        var commonRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                        _logger.LogInformation($"[Kafka] Starting sending request.");

                        _ = commonRequestService.SendDailyDigestEmailNotification(kafkaPayload);

                        _logger.LogInformation($"[Kafka] Message received: ");

                        break;
                    case KafkaServiceName.UnhandledException:
                        _logger.LogInformation($"[Kafka] Got unhandled exception");

                        if (kafkaPayload == null)
                            throw new Exception("[Kafka] Received invalid object. Getting null value.");

                        var comService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                        _ = comService.SendUnhandledExceptionEmailNotification(kafkaPayload);
                        break;
                    case KafkaServiceName.DailyGreetingJob:
                        _logger.LogInformation($"[Kafka] Message received: DailyGreetingJob");
                        if (kafkaPayload == null)
                            throw new Exception("[Kafka] Received invalid object. Getting null value.");

                        var commonNotificationRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                        _logger.LogInformation($"[Kafka] Starting sending request.");

                        _ = commonNotificationRequestService.SendDailyDigestEmailNotification(kafkaPayload);

                        _logger.LogInformation($"[Kafka] Message received: ");
                        break;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping service.");
            return Task.CompletedTask;
        }
    }
}
