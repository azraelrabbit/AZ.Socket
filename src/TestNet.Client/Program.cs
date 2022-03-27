// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using AZ.TcpNet.Base;
using NLog;
using TestNet.Command;


internal class Program
{
    private static AzTcpClient client;

    private static IPEndPoint host;

    private static byte[] LargeContent;

    private static bool _connected = false;

    private static ILogger LogHelper;

    static void Main(string[] args)
    {
        LogHelper = LogManager.GetCurrentClassLogger();

        //var content = 
        //LargeContent= Encoding.UTF8.GetBytes(File.ReadAllText("nlog.config"));
        client = CreateClient();


        //host = new IPEndPoint(IPAddress.Parse("150.158.170.196"), 54321);

        host = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54321);

        LogHelper.Info($"Connecting to {host.ToIPv4String()}");
        client.Connect(host, 3000);
 
        StartAsyncSending(client);

        Console.ReadLine();
    }

    private static AzTcpClient CreateClient()
    {
        var c = new AzTcpClient();
        c.KeepAlive = true;

        c.ReceivedData += Client_ReceiveData;
        c.ClientConnected += Client_ClientConnected;
        c.ClientDisconnected += Client_ClientDisconnected;
        c.ConnectTimeout += Client_ConnectTimeout;
        return c;
    }

    static void ReConnect()
    {
        client?.Dispose();
        client = null;
        client = CreateClient();

        client.Connect(host, 3000);

    }

    private static void Client_ReceiveData(AzReceiveEventArgs obj)
    {
        var cmdType = (AzCommandType)obj.CmdCode;
        switch (cmdType)
        {
            case AzCommandType.TestResponse:
                OnReceivedTestResponse(obj);
                break;
            default:
                break;

        }
    }

    private static void Client_ConnectTimeout(object sender, AzConnectEventArgs e)
    {
        //auto reconnect wait 2 sec.
        _connected = false;
        LogHelper.Warn($"Connecting to Host:{host.ToIPv4String()} timeout. Waiting 2 seconds to retry ...");
        Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t =>
        {
            LogHelper.Info($"Retry Connecting to Host:{host.ToIPv4String()}");
            ReConnect();

        });
    }

    static void StartAsyncSending(AzTcpClient client)
    {
        Task.Delay(TimeSpan.FromMilliseconds(1)).ContinueWith(t =>
        {
            if (_connected)
            {
                var reqCmd = new TestRequestCommand()
                {
                    Code = (int)AzCommandType.TestRequest,
                    Content = LargeContent,
                    ReqTime = DateTime.Now,
                    TestValue = $"ACK:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}"
                };

                client.SendAsync(reqCmd.ToBytes());
            }

        }).ContinueWith(t => { StartAsyncSending(client); });
    }

    private static void OnReceivedTestResponse(AzReceiveEventArgs e)
    {
        try
        {


            var cmd = e.ReceivedData.ToCommand<TestResponseCommand>();
            if (cmd == null)
            {
                LogHelper.Warn($"Resonse is null or empty.");
            }
            else
            {
                var timeSpan = DateTime.Now - cmd.ReqTime;
                //LogHelper.Info(cmd.TestValue + $"Req Cost Time :{timeSpan.Milliseconds}:{timeSpan.Ticks}");
                //var ss =Encoding.UTF8.GetString(cmd.Content);
                LogHelper.Info(cmd.TestValue + $"Resp Cost Time :{timeSpan.Milliseconds}:{timeSpan.Ticks}");

            }
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex);
        }
    }

    private static void Client_ClientDisconnected(object sender, AzConnectEventArgs e)
    {
        _connected = false;

        //client.EndReceiveData();

        var clientIp = e.RemoteEndPoint.ToIPv4String();

        LogHelper.Info($"Client:{clientIp} Disconnectd.");

        
        if (e?.Exception != null)
        {
            if (e.Exception is SocketException)
            {
                var se=e.Exception as SocketException;
                LogHelper.Error(se.SocketErrorCode.ToString());
            }
            else
            {
                LogHelper.Error(e.Exception);
            }
           
        }

        Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t =>
        {
            LogHelper.Info($"Retry Connecting to Host:{host.ToIPv4String()}");

            ReConnect();

        });
    }

    private static void Client_ClientConnected(object sender, AzConnectEventArgs e)
    {
        _connected = true;
        var clientIp = e.RemoteEndPoint.ToIPv4String();

        LogHelper.Info($"Client:{clientIp} connected.");

        client.BeginReceiveData();
    }
}
