using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AZ.TcpNet.Base;
using NLog;
using TestNet.Command;


namespace TestNet
{
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
            client = new AzTcpClient();
            client.KeepAlive = true;

            client.ReceivedData += Client_ReceiveData;
            client.ClientConnected += Client_ClientConnected;
            client.ClientDisconnected += Client_ClientDisconnected;
            client.ConnectTimeout += Client_ConnectTimeout;


            //host = new IPEndPoint(IPAddress.Parse("150.158.170.196"), 54321);

            host = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54321);

            LogHelper.Info($"Connecting to {host.ToIPv4String()}");
            client.Connect(host,3000);



            StartAsyncSending(client);

            Console.ReadLine();
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
             TaskEx.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t =>
             {
                 LogHelper.Info($"Retry Connecting to Host:{host.ToIPv4String()}");
                 client.Connect(host);

             });
        }

        static void StartAsyncSending(AzTcpClient client)
        {
            TaskEx.Delay(TimeSpan.FromMilliseconds(5000)).ContinueWith(t =>
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
              
            }).ContinueWith(t => { StartAsyncSending(client);});
        }

        private static void OnReceivedTestResponse(AzReceiveEventArgs e)
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

        private static void Client_ClientDisconnected(object sender, AzConnectEventArgs e)
        {
            _connected = false;

            //client.EndReceiveData();

            var clientIp = e.RemoteEndPoint.ToIPv4String();

            LogHelper.Info($"Client:{clientIp} Disconnectd.");
        }

        private static void Client_ClientConnected(object sender, AzConnectEventArgs e)
        {
            _connected = true;
            var clientIp = e.RemoteEndPoint.ToIPv4String();
            
            LogHelper.Info($"Client:{clientIp} connected.");

            client.BeginReceiveData();
        }
    }
}
