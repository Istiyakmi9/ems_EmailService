﻿using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using EmailRequest.Service.Interface;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class BlockAttendanceActionRequested: IEmailServiceRequest
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<BlockAttendanceActionRequested> _logger;

        public BlockAttendanceActionRequested(IDb db,
            IEmailService emailService,
            ILogger<BlockAttendanceActionRequested> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        private void ValidateModal(AttendanceRequestModal attendanceTemplateModel)
        {
            if (attendanceTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(attendanceTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(attendanceTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(attendanceTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

        }

        private EmailTemplate GetEmailTemplate()
        {
            _logger.LogInformation($"[1. Kafka] Trying to read email template from database.");
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.Attendance });

            if (emailTemplate == null)
            {
                _logger.LogError($"[Kafka] Fail to read email template.");
                throw new HiringBellException("Email template not found. Please contact to admin.");
            }

            return emailTemplate;
        }

        private string GetCompanyLogo(int companyId)
        {
            if (companyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company id");

            Files file = _db.Get<Files>("sp_company_primary_logo_get_byid", new
            {
                CompanyId = companyId,
                FileRole = ApplicationConstants.CompanyPrimaryLogo
            });

            if (file == null)
                throw new HiringBellException(" Company primary logo not found. Please contact to admin.");

            string filePath = string.Empty;
            if (file.FileName.Contains("."))
                filePath = $"{AppConstants.BaseImageUrl}{file.FilePath}/{file.FileName}";
            else
                filePath = $"{AppConstants.BaseImageUrl}{file.FilePath}/{file.FileName}.+{file.FileExtension}";

            if (filePath.Contains("\\"))
                filePath = filePath.Replace("\\", "/");

            return filePath;
        }

        public async Task SendEmailNotification(AttendanceRequestModal attendanceTemplateModel)
        {
            try
            {
                ValidateModal(attendanceTemplateModel);
                EmailTemplate emailTemplate = GetEmailTemplate();
                var logoPath = GetCompanyLogo(attendanceTemplateModel.CompanyId);
                if (string.IsNullOrEmpty(logoPath))
                    throw HiringBellException.ThrowBadRequest("Logo path not found");

                EmailSenderModal emailSenderModal = new EmailSenderModal();
                emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", attendanceTemplateModel.CompanyName);
                emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__DATE__", "Block Attendance")
                    .Replace("__DEVELOPERNAME__", attendanceTemplateModel.DeveloperName)
                    .Replace("__REQUESTTYPE__", attendanceTemplateModel!.RequestType);
                emailSenderModal.To = attendanceTemplateModel.ToAddress;
                emailSenderModal.FileLocationDetail = new FileLocationDetail();

                var html = ApplicationResource.AttendanceApplied;

                string statusColor = string.Empty;
                switch (attendanceTemplateModel?.ActionType?.ToLower())
                {
                    case ApplicationConstants.Submitted:
                        statusColor = "#0D6EFD";
                        break;
                    case ApplicationConstants.Approved:
                        statusColor = "#198754";
                        break;
                    case ApplicationConstants.Rejected:
                        statusColor = "#DC3545";
                        break;
                }

                html = html.Replace("__REQUESTTYPE__", attendanceTemplateModel!.RequestType)
                    .Replace("__REVEIVERNAME__", attendanceTemplateModel.ManagerName)
                    .Replace("__DEVELOPERNAME__", attendanceTemplateModel.DeveloperName)
                    .Replace("__DATE__", attendanceTemplateModel.Body)
                    .Replace("[__NOOFDAYS__]", string.Empty)
                    .Replace("__STATUS__", attendanceTemplateModel.ActionType)
                    .Replace("__ACTIONNAME__", "Manager Name")
                    .Replace("__STATUSCOLOR__", statusColor)
                    .Replace("__MESSAGE__", emailTemplate.EmailNote ?? string.Empty)
                    .Replace("__MOBILENO__", emailTemplate.ContactNo)
                    .Replace("__COMPANYNAME__", emailTemplate.SignatureDetail)
                    .Replace("__EMAILNOTE__", attendanceTemplateModel.Note)
                    .Replace("__MANAGENAME__", attendanceTemplateModel.ManagerName)
                    .Replace("__COMPANYLOGO__", logoPath)
                    .Replace("__ENCLOSINGSTATEMENT__", emailTemplate.EmailClosingStatement);

                emailSenderModal.Body = html;

                _logger.LogInformation($"[5. Kafka] Template converted.");
                await Task.Run(() => _emailService.SendEmail(emailSenderModal));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Kafka] Got exception: {ex.Message}");
            }
        }
    }
}
