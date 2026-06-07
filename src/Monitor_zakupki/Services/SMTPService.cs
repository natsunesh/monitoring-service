using Microsoft.Extensions.Logging;
using Monitor_zakupki.Models;
using Monitor_zakupki.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Monitor_zakupki.Services;
using htmlparse;

namespace ConsoleApp10.src.Monitor_zakupki.Services
{
    internal class SMTPService
    {
        private readonly ILogger<NotificationService> Logger;
        private readonly string FilePathToAppConfig, FilePathToLogs;

        List<ProcurementItem> _pi = new List<ProcurementItem>();

        ProcurementParserService _parserService;
        public SMTPService(ILogger<NotificationService> logger, string filePathToAppConfig, string filePathToLogs, ProcurementParserService ParseService)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            FilePathToAppConfig = filePathToAppConfig ?? throw new ArgumentNullException(nameof(filePathToAppConfig));
            FilePathToLogs = filePathToLogs ?? throw new ArgumentNullException(nameof(filePathToLogs));
            _parserService = ParseService ?? throw new ArgumentNullException(nameof(ParseService));
        }


        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            string _userSettings = File.ReadAllText(FilePathToAppConfig);
            var root = JsonSerializer.Deserialize<RootSettings>(_userSettings);

            string smtpServer = root.MainSettings.email.SmtpServer;
            int smtpPort = root.MainSettings.email.SmtpPort;
            string smtpName = root.MainSettings.email.SmtpLogin;
            string smtpPassword = root.MainSettings.email.SmtpPassword;

            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpName, smtpPassword);
                smtpClient.EnableSsl = true;

                _pi = await _parserService.GetNewProcurementsAsync(cancellationToken);

                if (_pi.Count != null)
                {
                    foreach (var pi in _pi)
                    {
                        using (MailMessage mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress(smtpName);
                            mailMessage.To.Add(root.MainSettings.email.SmtpFrom.ToString());
                            mailMessage.Subject = $"Поступила новая закупка от {pi.Name}";
                            mailMessage.Body = $"Наименование организации: {pi.Name}\n ИНН: {pi.Inn}\n Номер закупки: {pi.Number}\n Ссылка: {pi.Url}\n  Дата размещения: {pi.Date}";

                            try
                            {
                                smtpClient.Send(mailMessage);
                                File.AppendAllText(FilePathToLogs, $"{DateTime.Now.ToString()} Сообщение отправлено на {root.MainSettings.email.SmtpTo}");
                            }
                            catch(Exception ex) 
                            {
                                Logger.LogError($"Ошибка отправки сообщения: {ex}", ex);
                                File.AppendAllText(FilePathToLogs, $"{DateTime.Now.ToString()} Ошибка отправки сообщения: {ex}");
                            }
                        }
                    }
                }    
                    return;
                }
            }
        }
    }
