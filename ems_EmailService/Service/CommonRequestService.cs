using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using ModalLayer.Modal;

namespace EmailRequest.Service
{
    public class CommonRequestService
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        public CommonRequestService(IDb db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
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
        public async Task SendEmailNotification(CommonFields commonFields)
        {
            // validate request modal
            var logoPath = GetCompanyLogo(commonFields.CompanyId);
            if (string.IsNullOrEmpty(logoPath))
                throw HiringBellException.ThrowBadRequest("Logo path not found");

            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailSenderModal.Title = "Exception from live server";
            emailSenderModal.Subject = "Exception message";
            emailSenderModal.To = new List<string> {"marghub12@gmail.com" };
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.CommonException;
            html = html.Replace("__BODY__", commonFields.Body)
                .Replace("__COMPANYLOGO__", logoPath);
            emailSenderModal.Body = html;

            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }
    }
}
