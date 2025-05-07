using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WorkerRabbit.Models;
using WorkerRabbit.Services.Interfaces;

namespace WorkerRabbit.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(NotificationEvent notification)
        {
            try
            {
                // Obter configurações de e-mail do appsettings.json
                var smtpServer = _configuration["Email:SmtpServer"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                var smtpUsername = _configuration["Email:Username"];
                var smtpPassword = _configuration["Email:Password"];
                var senderEmail = _configuration["Email:SenderEmail"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
                {
                    _logger.LogError("Configurações de e-mail incompletas");
                    return false;
                }

                // Preparar cliente SMTP
                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                // Configurar e-mail com base no tipo de notificação
                var mailMessage = CreateEmailMessage(notification, senderEmail);

                if (mailMessage == null)
                {
                    _logger.LogError($"Não foi possível criar e-mail para notificação tipo {notification.Type}");
                    return false;
                }

                // Enviar e-mail
                await client.SendMailAsync(mailMessage);

                _logger.LogInformation($"E-mail enviado com sucesso para {mailMessage.To[0]} - Tipo: {notification.Type}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar e-mail para notificação {notification.Id}");
                return false;
            }
        }

        private MailMessage CreateEmailMessage(NotificationEvent notification, string senderEmail)
        {
            // Verifica se os dados necessários estão presentes
            if (!notification.Data.ContainsKey("UserEmail"))
            {
                _logger.LogError("E-mail do usuário não fornecido nos dados da notificação");
                return null;
            }

            string recipientEmail = notification.Data["UserEmail"];
            string userName = notification.Data.ContainsKey("UserName") ? notification.Data["UserName"] : "Cliente";

            string subject;
            string body;

            // Configuração do e-mail com base no tipo de notificação
            switch (notification.Type)
            {
                case NotificationType.UserRegistration:
                    subject = "Bem-vindo ao nosso sistema!";
                    body = $@"
                        <html>
                        <body>
                            <h2>Olá {userName}!</h2>
                            <p>Seu cadastro foi realizado com sucesso em nosso sistema.</p>
                            <p>Agora você pode aproveitar todos os nossos recursos.</p>
                            <br/>
                            <p>Atenciosamente,<br/>Equipe de Notificações</p>
                        </body>
                        </html>";
                    break;

                case NotificationType.OrderCreated:
                    if (!notification.Data.ContainsKey("OrderId"))
                    {
                        _logger.LogError("ID do pedido não fornecido nos dados da notificação");
                        return null;
                    }

                    subject = $"Pedido #{notification.Data["OrderId"]} Criado";
                    body = $@"
                        <html>
                        <body>
                            <h2>Olá {userName}!</h2>
                            <p>Seu pedido #{notification.Data["OrderId"]} foi criado com sucesso.</p>
                            <p>Acompanhe o status do seu pedido em nossa plataforma.</p>
                            <br/>
                            <p>Atenciosamente,<br/>Equipe de Notificações</p>
                        </body>
                        </html>";
                    break;

                //case NotificationType.PaymentReceived:
                //    if (!notification.Data.ContainsKey("OrderId"))
                //    {
                //        _logger.LogError("ID do pedido não fornecido nos dados da notificação");
                //        return null;
                //    }

                //    subject = $"Pagamento Confirmado - Pedido #{notification.Data["OrderId"]}";
                //    body = $@"
                //        <html>
                //        <body>
                //            <h2>Olá {userName}!</h2>
                //            <p>O pagamento do seu pedido #{notification.Data["OrderId"]} foi confirmado.</p>
                //            <p>Seu pedido está sendo processado e em breve será enviado.</p>
                //            <br/>
                //            <p>Atenciosamente,<br/>Equipe de Notificações</p>
                //        </body>
                //        </html>";
                //    break;

                default:
                    _logger.LogError($"Tipo de notificação não suportado: {notification.Type}");
                    return null;
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "Sistema de Notificações"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true // Habilitando HTML para mensagens mais atrativas
            };

            mailMessage.To.Add(recipientEmail);
            return mailMessage;
        }
    }
}
