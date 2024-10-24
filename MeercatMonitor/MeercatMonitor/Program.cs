using MailKit.Net.Smtp;
using MimeKit;

using HttpClient c = new HttpClient();
HttpResponseMessage res = await c.GetAsync("https://ite-si.de");
if (res.IsSuccessStatusCode)
{
    // send mail
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Mr. Meercat", "meercat@example.de"));
    message.To.Add(new MailboxAddress("Listening Meercats", "moedl.leonie@gmail.com"));
    message.Subject = "How you doin'?";
    Console.WriteLine("sending e mail uhuhu");

    message.Body = new TextPart("plain")
    {
        Text = """
                Your website is down. lol
                """
    };

    using (var client = new SmtpClient())
    {
        client.Connect("localhost", 25, useSsl: false);

        client.Send(message);
        client.Disconnect(true);
    }
}