using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;

namespace BadgeSwipeApp
{
    class EmailAgent
    {
        public void MissingBadgeEmail(int badgeID)
        {
            MailMessage mail = new MailMessage("BadgeSwipeApp@gestamp.donotreply.com", "mfennell@us.gestamp.com");
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "192.168.77.247";
            mail.Priority = MailPriority.High;
            mail.Subject = "Unregistered Badge ID - " + badgeID;
            mail.Body = "During operation, BadgeSwipeApp encountered an unregistered Badge ID (" + badgeID +
                ") and was forced to add a new entry to its Workers table. Please update this entry as soon as possible.";
            client.Send(mail);
        }

        public void NewReferenceAdded()
        {
            MailMessage mail = new MailMessage("BadgeSwipeApp@gestamp.donotreply.com", "mfennell@us.gestamp.com");
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "192.168.77.247";
            mail.Priority = MailPriority.High;
            mail.Subject = "New Reference Alert";
            mail.Body = "During operation, BadgeSwipeApp was forced to generate a new Reference Entry. Here's what I've got: ";
            client.Send(mail);
        }
        
        public void ErrorEmail()
        {
            MailMessage mail = new MailMessage("BadgeSwipeApp@gestamp.donotreply.com", "mfennell@us.gestamp.com");
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "192.168.77.247";
            mail.Priority = MailPriority.High;
            mail.Subject = "Something Has Gone Wrong";
            mail.Body = "During operation, BadgeSwipeApp encountered an error. Here's what I've got: ";
            client.Send(mail);
        }

    }
}
