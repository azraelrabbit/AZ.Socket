// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using AZ.TcpNet.Base;
using NLog;
using TestNet.Command;

 ILogger LogHelper=LogManager.GetCurrentClassLogger();


Console.WriteLine("Hello, World!");

var ackChar = "ACK".ToCharArray();

var server=new AzTcpServer(listenIp:IPAddress.Any);

server.ClientConnect += Server_ClientConnect;

server.ClientDisConnect += Server_ClientDisConnect;

server.ReceiveData += Server_ReceiveData;

void Server_ReceiveData(AzReceiveEventArgs obj)
{
    var cmdType = (AzCommandType)obj.CmdCode;
    switch (cmdType)
    {
        case AzCommandType.TestRequest:
            OnReceivedTestReq(obj);
            break;
        default:
            break;
            
    }
}

void OnReceivedTestReq(AzReceiveEventArgs obj)
{
    try
    {

        var cmd = obj.ReceivedData.ToCommand<TestRequestCommand>();
        if (cmd == null)
        {

        }
        else
        {
            //var timeSpan = DateTime.Now - cmd.ReqTime;
            //LogHelper.Info(cmd.TestValue+$"Req Cost Time :{timeSpan.Milliseconds}:{timeSpan.Ticks}");

            LogHelper.Info(cmd.TestValue);
            var resp = new TestResponseCommand()
            {
                Code = (int)AzCommandType.TestResponse,
                TestValue = "[OK]" + cmd.TestValue.TrimStart(ackChar),
                ReqTime = cmd.ReqTime,
                Content = cmd.Content
            };
            //if (obj.WorkClient.IsConnected())
            //{
            obj.WorkClient.SendAsync(resp.ToBytes());
            //}

        }

    }
    catch (Exception ex)
    {
        LogHelper.Error(ex);
    }
}

void Server_ClientDisConnect(AzConnectEventArgs obj)
{
    var clientIp = obj.RemoteEndPoint.ToIPv4String();

    LogHelper.Info($"Client:{clientIp} Disconnectd.");
    if (obj?.Exception != null)
    {
        if (obj.Exception is SocketException)
        {
            var se = obj.Exception as SocketException;
            LogHelper.Error(se.SocketErrorCode.ToString());
        }
        else
        {
            LogHelper.Error(obj.Exception);
        }
 
    }
}

void Server_ClientConnect(AzConnectEventArgs obj)
{
    var clientIp = obj.RemoteEndPoint.ToIPv4String();

    LogHelper.Info($"Client:{clientIp} connected.");
}



server.Start();
LogHelper.Info("Server started");

Console.ReadLine();