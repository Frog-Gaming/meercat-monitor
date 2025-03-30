using System.Net.Sockets;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("Server starting…");

using var s = TcpListener.Create(1337);

s.Start();

using TcpClient handler = await s.AcceptTcpClientAsync();
await using NetworkStream stream = handler.GetStream();

var message = $"📅 {DateTime.Now} 🕛";
var dateTimeBytes = Encoding.UTF8.GetBytes(message + "\n");
await stream.WriteAsync(dateTimeBytes);

Console.WriteLine($"Sent message: '{message}'");

Console.WriteLine("end");
Console.ReadLine();
