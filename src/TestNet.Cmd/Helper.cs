using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AZ.TcpNet.Base;
using Mapster;

namespace TestNet.Command
{
    /// <summary>
    /// 助手类
    /// </summary>
    public static class Helper
    {

 
        /// <summary>
        /// 获取枚举变量值的 Description 属性
        /// </summary>
        /// <param name="obj">枚举变量</param>
        /// <param name="isTop">是否改变为返回该类、枚举类型的头 Description 属性，而不是当前的属性或枚举变量值的 Description 属性</param>
        /// <returns>如果包含 Description 属性，则返回 Description 属性的值，否则返回枚举变量值的名称</returns>
        public static string GetEnumDescription<TEnum>(this TEnum obj, bool isTop = false)
        {
            if (obj == null)
            {
                return String.Empty;
            }

            var enumType = obj.GetType();
            DescriptionAttribute dna;
            if (isTop)
            {
                dna = (DescriptionAttribute)Attribute.GetCustomAttribute(enumType, typeof(DescriptionAttribute));
            }
            else
            {
                enumType = obj.GetType();
                Attribute[] dnas;
                {
                    var fi = enumType.GetField(Enum.GetName(enumType, obj));
                    dnas = Attribute.GetCustomAttributes(fi, typeof(DescriptionAttribute), true);
                }
                dna = (DescriptionAttribute)dnas.FirstOrDefault(p => p.GetType() == typeof(DescriptionAttribute));
                if (dna != null && String.IsNullOrEmpty(dna.Description) == false)
                    return dna.Description;
            }

            if (dna != null && String.IsNullOrEmpty(dna.Description) == false)
                return dna.Description;

            return obj.ToString();
        }

 
        /// <summary>
        /// 将父类转化成子类,并赋值
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        [Obsolete]
        public static TOut ToChild<TOut>(this object parent) where TOut : new()
        {
            // Edit by Cheery 2017/11/15: 处理null
            if (parent == null)
            {
                return default(TOut);
            }

            var t = new TOut();
            var proplist = parent.GetType().GetProperties();
            foreach (var p in proplist.Where(p => p.CanRead && p.CanWrite))
            //foreach (var p in proplist.Where(p => p.CanRead))
            {
                if (t.GetType().GetProperty(p.Name) != null)
                {
                    p.SetValue(t, p.GetValue(parent, null), null);
                }
                else
                {
                   
                }
            }
            return t;
        }

 
        /// <summary>
        /// 将字符串转化为Enum
        /// </summary>
        /// <typeparam name="T">Enumerate</typeparam>
        /// <param name="value">字符串值</param>
        /// <returns></returns>
        public static dynamic GetEnum<T>(string value)
            where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            Enum.TryParse(value, true, out T t);
            return t;
        }

        /// <summary>
        /// 将Int转化Enum对象
        /// 如果输入为空，则输出为空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code"></param>
        /// <returns></returns>
        public static dynamic GetEnum<T>(int? code)
            where T : struct
        {
            if (code == null)
            {
                return null;
            }
            else
            {
                var i = (int)code;
                return (T)Enum.ToObject(typeof(T), i);
            }
        }

        /// <summary>
        /// 获取文件的MD5值
        /// </summary>
        /// <param name="bytes">文件的字节数组</param>
        /// <returns>无连字符的MD5值</returns>
        public static string ComputeMd5(byte[] bytes)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(bytes)).Replace("-", "");
        }

        /// <summary>
        /// 计算文件的Hash代码
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ComputeFileHash(string filePath)
        {
            return ComputeMd5(File.ReadAllBytes(filePath));
        }

        /// <summary>
        /// 获取字符串的MD5值
        /// </summary>
        /// <param name="s">字符串</param>
        /// <returns>无连字符的MD5值</returns>
        public static string ComputeMd5(string s)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
        }

        /// <summary>
        /// 使用IP地址和端口号获取计算机通信实例的ID
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static string ComputeMd5WithIPEndPoint(IPAddress address, int port)
        {
            return ComputeMd5WithIPEndPoint(new IPEndPoint(address, port));
        }
        /// <summary>
        /// 使用IP地址和端口号获取计算机通信实例的ID
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static string ComputeMd5WithIPEndPoint(IPEndPoint endPoint)
        {
            return ComputeMd5(endPoint.ToIPv4String());
        }

 
        

        /// <summary>
        /// 判断给定的字符串是否是一个IP地址
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsIpAddress(string s)
        {
            return !String.IsNullOrEmpty(s) && Regex.IsMatch(s, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }

       

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] Serialize(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <returns></returns>
        public static object Deserialize(byte[] bytes)
        {
            if (bytes == null || !bytes.Any())
            {
                return null;
            }
            using (var ms = new MemoryStream(bytes))
            {
                var bf = new BinaryFormatter();
                return bf.Deserialize(ms);
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <returns></returns>
        public static object ToCommand(this byte[] bytes)
        {
            return Deserialize(bytes);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <returns></returns>
        public static T ToCommand<T>(this byte[] bytes)
        {
            return Deserialize(bytes).Adapt<T>();
        }


        /// <summary>
        /// 利用MemoryStream进行深拷贝
        /// </summary>
        /// <typeparam name="T">要拷贝的对象类型</typeparam>
        /// <param name="obj">源对象</param>
        /// <returns></returns>
        [Obsolete]
        public static T DeepCopy<T>(T obj)
        {
            object retval;
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);

                ms.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }

 
  

        /// <summary>
        /// 开启cmd.exe进程,并执行cmd命令
        /// </summary>
        /// <param name="strCmd"></param>
        public static void RunCmdProcess(string strCmd)
        {
            var fileName = @"cmd.exe";

            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                fileName = @"bash";
            }

            var prc = new Process
            {
                StartInfo =
                    {
                        FileName = fileName,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = false,
                        CreateNoWindow = false
                    }
            };
            prc.Start();

            prc.StandardInput.WriteLine(strCmd.TrimEnd('&'));
            prc.StandardInput.Close();
            prc.WaitForExit();
        }
 
         
      
        public static bool IsWin
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    //PlatformID.
                    return false;
                }

                return true;
            }
        }

        static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static byte[] Zip(string str, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            var bytes = encoding.GetBytes(str);
            return Zip(bytes);

             
        }

        public static byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes, Encoding encoding)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(Unzip(bytes));
           
        }
        private static bool IsAbsoluteStartChar(char firstChar)
        {
            if ((int) firstChar != (int) Path.DirectorySeparatorChar)
                return (int) firstChar == (int) Path.AltDirectorySeparatorChar;
            return true;
        }
        public static bool IsRelativePath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimStart();
                int length = path.Length;
                if (length >= 1)
                {
                    char firstChar = path[0];
                    if (IsAbsoluteStartChar(firstChar))
                        return false;
                    if (firstChar == '.')
                        return true;
                    if (length >= 2)
                    {
                        char ch = path[1];
                        if ((int) Path.VolumeSeparatorChar != (int) Path.DirectorySeparatorChar &&
                            (int) ch == (int) Path.VolumeSeparatorChar)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
            return false;
        }

        public static string ResolveRelativePath(string path)
        {
            if (IsRelativePath(path))
            {
                var npath = Path.Combine(Environment.CurrentDirectory, path);
                return npath;
            }

            return path;
        }

        /// <summary>
        /// 利用反射的转换
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="origin">原始实例</param>
        /// <returns>目标实例</returns>
        public static T ObjectCast<T>(this object origin)
            where T : new()
        {

            return origin == null ? default(T) : origin.Adapt<T>();
        }

        /// <summary>
        /// 利用反射的转换
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="origin">原始实例</param>
        /// <returns>目标实例</returns>
        public static T ObjectCast<T, TSource>(this TSource origin)
            where T : new() where TSource : new()
        {

            return origin == null ? default(T) : origin.Adapt<T>();
        }

        /// <summary>
        /// 利用mapper的转换
        /// </summary>
        /// <typeparam name="TTarget">目标类型</typeparam>
        /// <typeparam name="T">原始类型</typeparam>
        /// <param name="origin">原始实例</param>
        /// <returns>目标实例</returns>
        public static List<TTarget> ObjectCast<TTarget, T>(this List<T> origin)
            where TTarget : new() where T : new()
        {

            return origin == null ? default(List<TTarget>) : origin.Adapt<List<TTarget>>();
        }

    }
}
