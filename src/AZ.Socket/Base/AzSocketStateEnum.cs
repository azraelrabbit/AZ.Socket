using System.ComponentModel;

namespace AZ.TcpNet.Base
{
    /// <summary>
    /// 
    /// </summary>
    public enum AzSocketStateEnum
    {
        /// <summary>
        /// 正常连接状态
        /// </summary>
        [Description("Connected")]
        Normal,
        /// <summary>
        /// Socket 读写异常
        /// </summary>
        [Description("Socket Exception")]
        SocketError,
        /// <summary>
        /// 连接超时
        /// </summary>
        [Description("Connection Timeout")]
        ConnectTimeout,

        /// <summary>
        /// Remote端关闭连接
        /// </summary>
        [Description("Connection Closed by Remote")]
        RemoteClose
    }
}
