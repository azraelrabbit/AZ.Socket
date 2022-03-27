using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AZ.TcpNet.Base
{
    /// <summary>
    /// 
    /// </summary>
    public static class SocketExtends
    {
        /// <summary>
        /// 设置socket keepalive 实现连接断开检测
        /// </summary>
        /// <param name="client"></param>
        /// <param name="startDelay">连接成功后多久开始探测,单位ms,默认10秒</param>
        /// <param name="detectInterval">每次探测的时间间隔,单位ms,默认5秒</param>
        public static void SetKeepAlive(this System.Net.Sockets.Socket client, int startDelay = 10000, int detectInterval = 5000)
        {
            //int startDetectionDelay = 5000;//ms,开始探测延时时间
            //int detectionInterval = 5000;//ms, 探测间隔时间

            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0); // 开启keeplive 模式
            BitConverter.GetBytes((uint)startDelay).CopyTo(inOptionValues, Marshal.SizeOf(dummy)); //连接成功多久开始探测
            BitConverter.GetBytes((uint)detectInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2); //每次探测的时间间隔

            client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool IsConnected(this TcpClient client)
        {
            try
            {
                if (client?.Client == null)
                {
                    return false;
                }
                //var isconnect = client.Connected;
                // return !(client.Client.Poll(1, SelectMode.SelectRead) && client.Client.Available == 0);
                return !((client.Client.Poll(1000, SelectMode.SelectRead) && (client.Client.Available == 0)) || !client.Client.Connected);
            }
            catch (Exception)
            {

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool IsConnected(this System.Net.Sockets.Socket client)
        {
            try
            {
                if (client == null)
                {
                    return false;
                }
                //var isconnect = client.Connected;
                return !((client.Poll(1000, SelectMode.SelectRead) && client.Available == 0) || !client.Connected);
            }
            catch (SocketException)
            {

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static IPEndPoint GetRemoteIpEndPoint(this System.Net.Sockets.Socket client)
        {
            var remote = client.RemoteEndPoint;
            return remote as IPEndPoint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static IPEndPoint GetRemoteIpEndPoint(this TcpClient client)
        {
            return client.Client.GetRemoteIpEndPoint();
        }


        ///// <summary>
        ///// 获取本机Ipv4地址
        ///// </summary>
        ///// <returns></returns>
        //public static IPAddress GetLocalInterIpAddress()
        //{
        //    return Dns.GetHostEntry(Dns.GetHostName())
        //        .AddressList.FirstOrDefault<IPAddress>(p => p.AddressFamily == AddressFamily.InterNetwork);
        //}

        ///// <summary>
        ///// 获取本机Ipv6地址
        ///// </summary>
        ///// <returns></returns>
        //public static IPAddress GetLocalInterIpAddressV6()
        //{
        //    return Dns.GetHostEntry(Dns.GetHostName())
        //        .AddressList.FirstOrDefault<IPAddress>(p => p.AddressFamily == AddressFamily.InterNetworkV6);
        //}



        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static IPAddress GetLocalIpAddressByInterface()
        //{
        //    var adapter = GetLocalInterface();

        //    if (adapter != null)
        //    {
        //        var ipProp = adapter.GetIPProperties();
        //        var ip4AddrList = ipProp.UnicastAddresses.ToList().FindAll(p => p.Address.AddressFamily == AddressFamily.InterNetwork);

        //        var ipAddressInformation = ip4AddrList.FirstOrDefault();
        //        if (ipAddressInformation != null)
        //        {
        //            return ipAddressInformation.Address;
        //        }
        //    }

        //    return GetLocalInterIpAddress();
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static NetworkInterface GetLocalInterface()
        //{
        //    var adapter = GetLocalInterfaceList().FirstOrDefault(p => p.OperationalStatus == OperationalStatus.Up);

        //    return adapter;
        //}
        //Microsoft Hyper-V Network Adapter #3
        static readonly List<string> virtualNetworkKeyWords = new List<string>() { "虚拟", "vm", "virtual", "vpn", "software", "wan", "bluetooth", "teredo" };
        /// <summary>
        /// 
        /// </summary>
        ///// <returns></returns>
        public static List<NetworkInterface> GetLocalInterfaceList()
        {
            var list = NetworkInterface.GetAllNetworkInterfaces()
                .ToList();


            var locallistStr = string.Join("|", list.Select(p => p.Description).ToList());
            Console.WriteLine(locallistStr);
            return list.FindAll(p => !virtualNetworkKeyWords.Exists(v => p.Description.ToLower().Contains(v)));
        }

        ///// <summary>
        ///// 检测本地网络是否正常连接
        ///// </summary>
        ///// <returns></returns>
        //public static bool LocalNetworkAvaliable()
        //{
        //    return GetLocalInterface()?.OperationalStatus == OperationalStatus.Up;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipaddr"></param>
        /// <returns></returns>
        public static bool LocalNetworkAvaliable(this IPAddress ipaddr)
        {
            if (ipaddr.Equals(IPAddress.Any) || ipaddr.Equals(IPAddress.Parse("127.0.0.1")))
            {
                return true;
            }
            var localList = GetLocalInterfaceList();
            var locallistStr = string.Join("|", localList.Select(p => p.Description).ToList());
            Console.WriteLine(locallistStr);
            var ipInterface = localList.FirstOrDefault(p => p.GetIPProperties().UnicastAddresses.ToList().Exists(m => m.Address != null && m.Address.ToString() == ipaddr.ToString()));
            return ipInterface?.OperationalStatus == OperationalStatus.Up;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static IPAddress GetLocalIpAddressV6ByInterface()
        //{
        //    var adapter = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(p => !virtualNetworkKeyWords.Exists(v => p.Description.ToLower().Contains(v)));

        //    if (adapter != null)
        //    {
        //        var ipProp = adapter.GetIPProperties();
        //        var ip6AddrList = ipProp.UnicastAddresses.ToList().FindAll(p => p.Address.AddressFamily == AddressFamily.InterNetworkV6);

        //        var ipAddressInformation = ip6AddrList.FirstOrDefault();
        //        if (ipAddressInformation != null)
        //        {
        //            return ipAddressInformation.Address;
        //        }
        //    }

        //    return GetLocalInterIpAddressV6();
        //}

        /// <summary>
        /// Ping
        /// </summary>
        /// <param name="remoteIp">远程地址</param>
        /// <returns></returns>
        public static bool Ping(IPAddress remoteIp)
        {
            using (var p = new Ping())
            {
                PingReply pr = null;
                try
                {
                    pr = p.Send(remoteIp);
                }
                catch
                {
                    // ignored
                }
                return pr != null && pr.Status == IPStatus.Success;
            }
        }

        /// <summary>
        /// Ping
        /// </summary>
        /// <param name="remoteHost">远程地址</param>
        /// <returns></returns>
        public static bool Ping(string remoteHost)
        {
            using (var p = new Ping())
            {
                PingReply pr = null;
                try
                {
                    pr = p.Send(remoteHost);
                }
                catch
                {
                    // ignored
                }
                return pr != null && pr.Status == IPStatus.Success;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uncPath"></param>
        /// <returns></returns>
        public static bool CheckUncPathAccessable(string uncPath)
        {
            var realIp = uncPath.Split('\\').FirstOrDefault(p => !string.IsNullOrEmpty(p));
            return Ping(realIp);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uncPath"></param>
        /// <returns></returns>
        public static bool UncPathAccessable(this string uncPath)
        {
            return CheckUncPathAccessable(uncPath);
        }


        /// <summary>
        /// 检测路径是否为UNC Path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CheckIsUncPath(string path)
        {
            var rootpath = Path.GetPathRoot(path);


            if ((Environment.OSVersion.Platform == PlatformID.MacOSX ||
                 Environment.OSVersion.Platform == PlatformID.Unix))
            {
                rootpath = path;
            }


            return rootpath != null && rootpath.StartsWith("\\");
        }

        /// <summary>
        /// 检测路径是否为UNC Path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsUncPath(this string path)
        {
            return CheckIsUncPath(path);
        }

        /// <summary>
        /// IPv4地址转化为形如0.0.0.0:0的字符串
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string ToIPv4String(this IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                return String.Empty;
            }
            return String.Format("{0}:{1}", endpoint.Address, endpoint.Port);
        }
    }
}
