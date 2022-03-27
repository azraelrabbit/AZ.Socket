using System;
using System.Net;

namespace AZ.TcpNet.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class AzConnectEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        ///  
        /// </summary>
        public AzSocketStateEnum Status { get; set; }

        /// <summary>
        ///  
        /// </summary>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AzSocketConnectEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        ///  
        /// </summary>
        public AzSocketStateEnum Status { get; set; }

        /// <summary>
        ///  
        /// </summary>
        public Exception Exception { get; set; }
    }
}