using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AZ.TcpNet.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class AzSocketServer
    {
        #region Socket通讯全局变量
        /// <summary>
        /// 包头-消息体长度
        /// </summary>
        public readonly int HEADER_LENGTH = 4;

        /// <summary>
        /// 包头-消息code[唯一]
        /// </summary>
        public readonly int CMDCODE_LENGTH = 4;


        /// <summary>
        /// 消息接收缓冲buffer长度
        /// </summary>
        private readonly int BUFFER_SIZE = 8 * 1024;//如果是 8Kb 缓冲区,如果1w个连接,则需要 4k*10000 约等于78Mb内存的缓冲区

        #endregion

        private System.Net.Sockets.Socket _listener;

        /// <summary>
        /// 
        /// </summary>
        public event Action<AzSocketReceiveEventArgs> ReceiveData;


        /// <summary>
        /// 客户端已连接
        /// </summary>
        public event Action<AzSocketConnectEventArgs> ClientConnect;

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        public event Action<AzSocketConnectEventArgs> ClientDisConnect;

        /// <summary>
        /// 
        /// </summary>
        public int Port = 54321;

        /// <summary>
        /// 
        /// </summary>
        public IPAddress ListenIp { get; set; }

        private bool _isActive;
        private readonly ConcurrentDictionary<Guid, AzSocketClient> _clientPool = new ConcurrentDictionary<Guid, AzSocketClient>();

        private Timer _clientCheckTimer;


        private IAsyncResult acceptAsyncResult;

        /// <summary>
        /// 
        /// </summary>
        public AzSocketServer(int bodyHeadLength=4, int cmdCodeHeadLength=4, int receiveBufferSize = 8192,int localPort = 54321, IPAddress listenIp = null )
        {
            if (localPort > 0 && localPort < 65535)
            {
                Port = localPort;
            }
            ListenIp = listenIp;

            HEADER_LENGTH = bodyHeadLength;
            CMDCODE_LENGTH = cmdCodeHeadLength;

            BUFFER_SIZE = receiveBufferSize;
            //AzCommandHelper.InitCommands();
        }
 
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            _isActive = true;

            if (_listener == null)
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Any, Port);

                if (ListenIp != null)
                {
                    ipEndPoint = new IPEndPoint(ListenIp, Port);
                }

                _listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

                _listener.Bind(ipEndPoint);

                _listener.Listen(5000);

                acceptAsyncResult= _listener.BeginAccept(OnAcceptTcpClient, _listener);
                //_listener.BeginAcceptTcpClient(OnAcceptTcpClient, _listener);
            }

            if (_clientCheckTimer == null)
            {
                _clientCheckTimer = new Timer(ClientCheckCallback);
            }

            _clientCheckTimer.Change(10000, 5000);
        }

        /// <summary>
        /// stop TcpServer
        /// </summary>
        public void Stop()
        {
            _isActive = false;
            _clientCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            if (_listener != null)
            {
                try
                {
                    _listener.Shutdown(SocketShutdown.Both);
                    _listener.Dispose();
                }
                catch (Exception)
                {
                    // ignored
                }

                try
                {
                    foreach (var tcpClientObject in _clientPool)
                    {
                        try
                        {
                            tcpClientObject.Value.Close();
                        }
                        catch
                        {
                            // ignored
                        }
 
                        try
                        {
                            tcpClientObject.Value.Dispose();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                catch (Exception ex)
                {
                   // LogHelper.Error(ex);
                   //ignore
                }
                finally
                {
                    _clientPool.Clear();
                }

                _listener = null;
            }

        }

        private void ClientCheckCallback(object state)
        {
            if (_clientPool.Count > 0)
            {
                var disconnClients = _clientPool.Where(p => !p.Value.IsConnected()).ToList();
                foreach (var disconnClient in disconnClients)
                {
                    ClearDisconnected(disconnClient.Key);
                }
            }
        }



        private void OnAcceptTcpClient(IAsyncResult ar)
        {
            if (_isActive)
            {
                try
                {
                    var server = ar.AsyncState as System.Net.Sockets.Socket;

                    if (server != null)
                    {
                        var client = server.EndAccept(acceptAsyncResult);// EndAcceptTcpClient(ar);
                        acceptAsyncResult= server.BeginAccept(OnAcceptTcpClient, server);
                        NewClientConnected(client);
                       
                    }
                }
                catch (Exception ex)
                {
                    _isActive = false;
                    //LogHelper.Error(ex);
                }
            }
        }

        private void NewClientConnected(System.Net.Sockets.Socket client)
        {
            var workclient = new AzSocketClient(client,HEADER_LENGTH,CMDCODE_LENGTH,BUFFER_SIZE);

            workclient.ReceivedData += OnReceivedData;
            workclient.ClientDisconnected += Workclient_ClientDisconnected;
            workclient.BeginReceiveData();

            _clientPool.TryAdd(workclient.Id, workclient);

            OnClientConnect(new AzSocketConnectEventArgs() { ClientId = workclient.Id, RemoteEndPoint = workclient.GetRemoteIpEndPoint(), Status = AzSocketStateEnum.Normal });
            //LogHelper.Trace($"Client {workclient.GetRemoteIpEndPoint().ToIPv4String()} Connected.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void OnReceivedData(AzSocketReceiveEventArgs e)
        {
            //ReceiveData?.BeginInvoke(e, null, null); //?.Invoke(e);
            Task.Factory.StartNew(() => { ReceiveData?.Invoke(e); });
        }

        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="buffer"></param>
        public virtual void SendAsync(Guid clientId, byte[] buffer)
        {
            AzSocketClient client;
            _clientPool.TryGetValue(clientId, out client);
            if (client != null)
            {
                client.SendAsync(buffer);
            }
            else
            {
                throw new NullReferenceException("Client ID:" + clientId.ToString() + " does not exist.");
            }
        }
        
        private void Workclient_ClientDisconnected(object sender, AzSocketConnectEventArgs e)
        {
            //连接断开

            OnClientDisConnect(e);
            ClearDisconnected(e.ClientId);

            OnError(e);
        }


        /// <summary>
        /// 当发生异常时
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(AzSocketConnectEventArgs e)
        {
            //if (e.Exception != null)
            //{
            //    LogHelper.Error($"Client {e.RemoteEndPoint.ToIPv4String()} Disconnected with Exception:{e.Exception.Message}.", e.Exception);
            //}
            //else
            //{
            //    LogHelper.Trace($"Client {e.RemoteEndPoint.ToIPv4String()} Disconnected: {e.Status.GetEnumDescription()}.");
            //}
        }

        private void ClearDisconnected(Guid clientId)
        {
            AzSocketClient closedClient;
            _clientPool.TryRemove(clientId, out closedClient);
            if (closedClient == null) return;
            closedClient.ReceivedData -= OnReceivedData;
            closedClient.ClientDisconnected -= Workclient_ClientDisconnected;
            closedClient.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void OnClientConnect(AzSocketConnectEventArgs obj)
        {

            AsyncHelper.Run(() => ClientConnect?.Invoke(obj));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void OnClientDisConnect(AzSocketConnectEventArgs obj)
        {

            AsyncHelper.Run(() => ClientDisConnect?.Invoke(obj));
        }
    }
}
