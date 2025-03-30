using System.Net.Sockets;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var timeout = TimeSpan.FromMilliseconds(4);
//var timeout = TimeSpan.FromSeconds(4);

Console.WriteLine("Client Starting…");
using var c = new TcpClient();

var connected = await WithTimeoutAsync(timeout, (cToken) => c.ConnectAsync("localhost", 1337, cToken));
if (!connected)
{
    Console.Write($"Failed to connect; Timeout {timeout}");
    Console.ReadLine();
    return;
}
Console.WriteLine("Connected");

//using var cts = new CancellationTokenSource();
//var connectTask = c.ConnectAsync("localhost", 1337, cts.Token).AsTask();
//var timeoutTask = Task.Delay(timeout, cts.Token);
//var r = await Task.WhenAny(connectTask, timeoutTask);
//await cts.CancelAsync();

//if (r == timeoutTask)
//{
//    Console.WriteLine("Timeout");
//}
//else if (r == connectTask)
//{
//    Console.WriteLine("Connected");
//}

c.SendTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
c.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

using var stream = c.GetStream();
using var sr = new StreamReader(stream);
var line = await sr.ReadLineAsync();
Console.WriteLine($"Received: {line}");
stream.Close();

Console.WriteLine("end");
Console.ReadLine();

static async Task<bool> WithTimeoutAsync(TimeSpan timeout, Func<CancellationToken, ValueTask> fn)
{
    using var cts = new CancellationTokenSource();
    var task = fn(cts.Token).AsTask();
    var timeoutTask = Task.Delay(timeout, cts.Token);
    var r = await Task.WhenAny(task, timeoutTask);
    await cts.CancelAsync();

    return r == task;
}
