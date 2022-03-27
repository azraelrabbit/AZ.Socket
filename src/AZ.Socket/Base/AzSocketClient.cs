using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AZ.TcpNet.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class AzSocketClient : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private IPEndPoint _ipEndPoint;

        private readonly Socket _workClient;
        /// <summary>
        /// 连接成功标识
        /// </summary>
        private bool _isConnectionSuccessful;

        private bool _keepAlive;

        /// <summary>
        /// 获取或设置是否KeepAlive,即长连接
        /// </summary>
        public bool KeepAlive
        {
            get { return _keepAlive; }
            set
            {
                if (!_keepAlive && value)
                {
                    _workClient.SetKeepAlive();
                }

                _keepAlive = value;
            }
        }

        #region Socket通讯全局变量
        /// <summary>
        /// 包头-消息体长度
        /// </summary>
        public readonly int HEADER_LENGTH = 4;

        /// <summary>
        /// 包头-消息code[唯一]
        /// </summary>
        public readonly  int CMDCODE_LENGTH = 4;

        /// <summary>
        /// 消息接收缓冲buffer长度
        /// </summary>
        private readonly int BUFFER_SIZE = 8 * 1024;//如果是 8Kb 缓冲区,如果1w个连接,则需要 4k*10000 约等于78Mb内存的缓冲区

        private   int HEAD_LEN
        {
            get
            {
                return HEADER_LENGTH + CMDCODE_LENGTH;
            }
        }

        #endregion


        private Stopwatch st;

        /// <summary>
        /// 异常信息
        /// </summary>
        private Exception _socketException;
        /// <summary>
        /// 线程开关
        /// </summary>
        private readonly ManualResetEvent _timeoutObject = new ManualResetEvent(false);

       
        private byte[] _buffer;// = new byte[BUFFER_SIZE];

 

        private List<byte> _receiveData = new List<byte>();
        private int _remainLength = -9;
        private int _msgBodyLength;
        private IAsyncResult _receiveCallback;
        private int _cmdType;

        #region Events
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event Action<AzSocketReceiveEventArgs> ReceivedData;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyHeadLength">消息体长度在header中的字节数</param>
        /// <param name="cmdCodeHeadLength">消息编号在header中的字节数</param>
        /// <param name="receiveBufferSize">消息接收缓冲buffer长度</param>
        public AzSocketClient(int bodyHeadLength=4,int cmdCodeHeadLength=4,int receiveBufferSize=8192)
        {
            Id = Guid.NewGuid();

            _workClient = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            _workClient.NoDelay = true; 

            _isConnectionSuccessful = true;

            HEADER_LENGTH = bodyHeadLength;
            CMDCODE_LENGTH = cmdCodeHeadLength;

            BUFFER_SIZE = receiveBufferSize;
            _buffer = new byte[BUFFER_SIZE];

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bodyHeadLength">消息体长度在header中的字节数</param>
        /// <param name="cmdCodeHeadLength">消息编号在header中的字节数</param>
        /// <param name="receiveBufferSize">消息接收缓冲buffer长度</param>
        public AzSocketClient(Socket client, int bodyHeadLength = 4, int cmdCodeHeadLength = 4, int receiveBufferSize = 8192)
        {
            _workClient = client;
            _workClient.NoDelay = true;
            Id = Guid.NewGuid();

            _isConnectionSuccessful = true;
            _ipEndPoint = client.GetRemoteIpEndPoint();//.GetRemotIpEndPoint();

            HEADER_LENGTH = bodyHeadLength;
            CMDCODE_LENGTH = cmdCodeHeadLength;
            BUFFER_SIZE = receiveBufferSize;
            _buffer = new byte[BUFFER_SIZE];
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <param name="bodyHeadLength">消息体长度在header中的字节数</param>
        /// <param name="cmdCodeHeadLength">消息编号在header中的字节数</param>
        /// <param name="receiveBufferSize">消息接收缓冲buffer长度</param>
        /// <param name="timeoutMSec"></param>
        public AzSocketClient(IPEndPoint ipEndPoint, int bodyHeadLength = 4, int cmdCodeHeadLength = 4, int receiveBufferSize = 8192, int timeoutMSec = 1000)
        {
            Id = Guid.NewGuid();
            _workClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
            _ipEndPoint = ipEndPoint;
            _isConnectionSuccessful = true;
            _workClient.NoDelay = true;
 

            HEADER_LENGTH = bodyHeadLength;
            CMDCODE_LENGTH = cmdCodeHeadLength;
            BUFFER_SIZE = receiveBufferSize;
            _buffer = new byte[BUFFER_SIZE];

            Connect(_ipEndPoint, timeoutMSec);

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        protected void OnReceivedData(int cmdType,byte[] data)
        {
 
            var args = new AzSocketReceiveEventArgs()
            {
 
                WorkClient = this,
                ClientId = Id,
                CmdCode = cmdType,
                ReceivedData = data
            };

            AsyncHelper.Run(() => ReceivedData?.Invoke(args));
 
        }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AzSocketConnectEventArgs> ClientDisconnected;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClientDisconnected(AzSocketConnectEventArgs e)
        {
 
            if (ClientDisconnected == null)
            {
                //当使用同步方法时不订阅事件,直接抛出异常
                if (e.Exception != null)
                {
                    throw e.Exception;
                    //LogHelper.Error(e.Exception);
                }
            }
            else
            {
                ClientDisconnected.Invoke(this, e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AzSocketConnectEventArgs> ClientConnected;
        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnClientConnected(AzSocketConnectEventArgs e)
        {
            try
            {
                KeepAlive = true;
            }
            catch (Exception)
            {
                // ignored
            }


            AsyncHelper.Run(()=>ClientConnected?.Invoke(this, e)).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AzSocketConnectEventArgs> ConnectTimeout;
        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnConnectTimeout(AzSocketConnectEventArgs e)
        {


            AsyncHelper.Run(() => ConnectTimeout?.Invoke(this, e));
        }
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AzSocketConnectEventArgs> OccurredSocketErrors;
        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnOccurredSocketErrors(AzSocketConnectEventArgs e)
        {

            AsyncHelper.Run(() => OccurredSocketErrors?.Invoke(this, e));
        }
        #endregion


        /// <summary>
        /// 获取连接的远程IpEndPoint
        /// </summary>
        /// <returns></returns>
        public IPEndPoint GetRemoteIpEndPoint()
        {
            // todo 需要注意
            if (_workClient != null && _workClient.IsConnected())
            {
                return _workClient.GetRemoteIpEndPoint();
            }
            return _ipEndPoint;
        }



        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="remoteEndPoint">客户端</param>
        /// <param name="timeoutMSec">超时时间</param>
        public void Connect(IPEndPoint remoteEndPoint, int timeoutMSec = 1000)
        {
            _ipEndPoint = remoteEndPoint;

            _timeoutObject.Reset();
            _socketException = null;

            // 不启用Timeout
            if (timeoutMSec == 0)
            {
                try
                {
                    _workClient.Connect(_ipEndPoint);
                    //连接成功,触发已连接事件
                    OnClientConnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.Normal });
                }
                catch (Exception ex)
                {
                    OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.SocketError, Exception = ex });
                }
            }
            else
            {
                try
                {
                    _workClient.BeginConnect(_ipEndPoint.Address, _ipEndPoint.Port, CallBackMethod, this);
                }
                catch (Exception ex)
                {
                    //LogHelper.Error(ex);
                    _socketException = ex;
                    //throw;
                }

                if (_timeoutObject.WaitOne(timeoutMSec, false))
                {
                    if (_isConnectionSuccessful)
                    {
                        //连接成功,触发已连接事件
                        OnClientConnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.Normal });

                        return;
                    }

                    OnClientDisconnected(_socketException == null
                        ? new AzSocketConnectEventArgs()
                        {
                            ClientId = Id,
                            RemoteEndPoint = GetRemoteIpEndPoint(),
                            Status = AzSocketStateEnum.SocketError,
                            Exception = null
                        }
                        : new AzSocketConnectEventArgs()
                        {
                            ClientId = Id,
                            RemoteEndPoint = GetRemoteIpEndPoint(),
                            Status = AzSocketStateEnum.SocketError,
                            Exception = _socketException
                        });
                }
                else
                {
                    //OnConnectTimeout(new AzConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.ConnectTimeout });
                    OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.ConnectTimeout, Exception = null });
                }
            }
        }


        private void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                _isConnectionSuccessful = false;
                var tcpclient = asyncresult.AsyncState as AzSocketClient;

                if (tcpclient?._workClient == null) return;
                tcpclient._workClient.EndConnect(asyncresult);
                _isConnectionSuccessful = true;
            }
            catch (Exception ex)
            {
                _isConnectionSuccessful = false;
                _socketException = ex;
            }
            finally
            {
                _timeoutObject.Set();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            var ret = _workClient.IsConnected() && _isConnectionSuccessful;
            if (!ret)
            {
                OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.Normal });
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        public void BeginReceiveData()
        {
            try
            {

                _receiveCallback = _workClient.BeginReceive(_buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceivedCallback, _workClient);
                //_receiveCallback = _workClient.Client.BeginReceive(buffer,SocketFlags.None, ReceivedCallback, _workClient);
            }
            catch (Exception ex)
            {
                OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.SocketError, Exception = ex });
            }
        }

 
        private void ReceivedCallback(IAsyncResult ar)
        {
            //实现异分段接收数据
            try
            {
                st=Stopwatch.StartNew();

                var client = ar.AsyncState as Socket;
                var receivedCount = 0;
                if (client != null)
                {
                    if (!client.IsConnected())
                    {
                        //连接断开
                        OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.RemoteClose });
                        return;
                    }

                    receivedCount = client.EndReceive(ar);
                }
                else
                {
                    OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.RemoteClose, Exception = new IOException("client is null") });
                }

                //var bf = buffer.FirstOrDefault();

                //接收数据
                if (_remainLength <= 0)
                {//新消息
                    if (receivedCount < HEADER_LENGTH + CMDCODE_LENGTH)
                    {
                        //无效消息 
                        //继续等待接收数据                      
                        BeginReceiveData();
                        return;
                    }

                    //_msgBodyLength = BitConverter.ToInt32(bf.Array.Take(Global.HEADER_LENGTH).ToArray(), 0);
                    _msgBodyLength = BitConverter.ToInt32(_buffer.Take(HEADER_LENGTH).ToArray(), 0);
                    _cmdType = BitConverter.ToInt32(
                        _buffer.Skip(HEADER_LENGTH).Take(CMDCODE_LENGTH).ToArray(), 0);
                    _remainLength = _msgBodyLength;
                }

                if (_msgBodyLength + HEADER_LENGTH+CMDCODE_LENGTH <= receivedCount)
                {
                    //若当前缓冲区已经包括完整的消息
                    var msgbuffer = _buffer.Skip(HEADER_LENGTH+ CMDCODE_LENGTH).Take(_msgBodyLength).ToArray();


                    //var msgbuffer = bf.Array.Skip(Global.HEADER_LENGTH).Take(_msgBodyLength).ToArray();
                    _receiveData = msgbuffer.ToList();
                    _remainLength = 0;
                }
                else
                {//消息体长度大于收到的消息长度
                    if (_remainLength > 0)
                    {
                        if (_remainLength == _msgBodyLength)
                        {
                            //第一块
                            var tmpBody = _buffer.Skip(HEADER_LENGTH+CMDCODE_LENGTH).Take(receivedCount - (HEADER_LENGTH+ CMDCODE_LENGTH)).ToArray();
                            //var tmpBody=bf.Array.Skip(Global.HEADER_LENGTH).Take(receivedCount - Global.HEADER_LENGTH).ToArray();
                            _receiveData.AddRange(tmpBody);
                            _remainLength -= tmpBody.Length;
                        }
                        else
                        {
                            if (_remainLength <= receivedCount)
                            {
                                //最后一块
                                var tmpBody = _buffer.Take(_remainLength).ToArray();

                                //var tmpBody = bf.Array.Take(_remainLength).ToArray();
                                _receiveData.AddRange(tmpBody);
                                _remainLength = 0;
                            }
                            else
                            {
                                var tmpBody = _buffer.Take(receivedCount).ToArray();
                                //var tmpBody = bf.Array.Take(receivedCount).ToArray();
                                _receiveData.AddRange(tmpBody);
                                _remainLength -= tmpBody.Length;
                            }
                        }
                    }
                }

                if (_remainLength == 0 && _receiveData.Count > 4)
                {
                    var receivedData = _receiveData;

                    //重置状态和变量
                    _receiveData = new List<byte>();
                    _remainLength = -9;
                    _msgBodyLength = 0;
                  
                    st.Stop();
                    Console.WriteLine($"RECV Coast {st.ElapsedMilliseconds}|tks: {st.ElapsedTicks}");
                    //消息读取完毕
                    OnReceivedData(_cmdType,receivedData.ToArray());

                    _cmdType = -9;
                }


                //Array.Clear(_buffer,0,BUFFER_SIZE);

                //_buffer = new byte[BUFFER_SIZE];
                //继续等待接收数据
                BeginReceiveData();
            }
            catch (Exception ex)
            {
                try
                {
                    //_buffer = new byte[BUFFER_SIZE];
                    //重置状态和变量
                    _receiveData = new List<byte>();
                    _remainLength = -9;
                    _msgBodyLength = 0;
                    _cmdType = -9;
                }
                catch
                {
                    // ignored
                }

                var msg = ex.Message;
                if (msg.Contains("强迫关闭"))
                {
                    OnClientDisconnected(new AzSocketConnectEventArgs()
                    {
                        ClientId = Id,
                        Status = AzSocketStateEnum.RemoteClose,
                        RemoteEndPoint = GetRemoteIpEndPoint(),
                        Exception = ex
                    });
                }
                else
                {
                    OnClientDisconnected(new AzSocketConnectEventArgs()
                    {
                        ClientId = Id,
                        Status = AzSocketStateEnum.SocketError,
                        RemoteEndPoint = GetRemoteIpEndPoint(),
                        Exception = ex
                    });
                }
            }
        }
 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        public void SendAsync(byte[] buffer)
        {
            try
            {
                if (_workClient == null || !_workClient.IsConnected())
                {
                    //连接断开
                    OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.RemoteClose });
                    return;
                }

                _workClient.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, asyncCallback, this);
            }
            catch (Exception ex)
            {
                OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.SocketError, Exception = ex });
            }
        }

        private void asyncCallback(IAsyncResult ar)
        {
            //LogHelper.Trace("async send :" + ar.IsCompleted.ToString());

        }
 

        /// <summary>
        /// 同步发送接收数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public byte[] Send(byte[] buffer)
        {
            try
            {
                if (_workClient != null && _workClient.IsConnected())
                {
                    var outputStream = new NetworkStream(_workClient, FileAccess.Write);// _workClient.GetStream();

                    ////发送数据
                    outputStream.Write(buffer, 0, buffer.Length);

                    ////开始接收数据,若服务器暂时未返回,则会等待
                    var inPutStream = new NetworkStream(_workClient, FileAccess.Read); //_workClient.GetStream();

                    var receiveBodyLength = new byte[HEADER_LENGTH];

                    inPutStream.Read(receiveBodyLength, 0, HEADER_LENGTH);
                    var bodyLength = BitConverter.ToInt32(receiveBodyLength, 0);

                    var receivedData = new List<byte>();
                    var remainLength = bodyLength;

                    while (remainLength > 0)
                    {
                        var recBuffer = new byte[_workClient.ReceiveBufferSize];

                        var recCount = inPutStream.Read(recBuffer, 0, recBuffer.Length);

                        if (remainLength < recCount)
                        {
                            //若已接收到的数据已经包括完整的消息
                            var msgbuffer = recBuffer.Take(bodyLength).ToArray();
                            receivedData = msgbuffer.ToList();
                            remainLength = 0;
                        }
                        else
                        {
                            if (remainLength == bodyLength)
                            {
                                //第一块
                                var tmpBody = recBuffer.Take(recCount).ToArray();
                                receivedData.AddRange(tmpBody);
                                remainLength -= tmpBody.Length;
                            }
                            else
                            {
                                if (remainLength <= recCount)
                                {
                                    //最后一块
                                    var tmpBody = recBuffer.Take(remainLength).ToArray();
                                    receivedData.AddRange(tmpBody);
                                    remainLength = 0;
                                }
                                else
                                {
                                    var tmpBody = recBuffer.Take(recCount).ToArray();
                                    receivedData.AddRange(tmpBody);
                                    remainLength -= tmpBody.Length;
                                }
                            }
                        }
                    }
                    return receivedData.ToArray();
                }
                //连接断开
                OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.RemoteClose });
            }
            catch (Exception ex)
            {
                OnClientDisconnected(new AzSocketConnectEventArgs() { ClientId = Id, RemoteEndPoint = GetRemoteIpEndPoint(), Status = AzSocketStateEnum.SocketError, Exception = ex });
            }
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            try
            {
                _workClient.Close();
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            try
            {
                Close();

#if NET40||NET45
               
#else
             _workClient.Dispose();
#endif
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _buffer = null;
                _receiveData = null;
            }
        }
    }
}
