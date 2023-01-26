using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using TShockAPI;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;

namespace AutoReset.MainPlugin
{
    internal class Utils
    {
        /// <summary>
        /// GZip压缩 256字节以上才有压缩效果
        /// </summary>
        /// <param name="strData">压缩字符串</param>
        /// <returns></returns>
        public static string Compress(string strData)
        {
            try
            {
                byte[] data = Encoding.GetEncoding("UTF-8").GetBytes(strData);
                using (var ms = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                    {
                        zip.Write(data, 0, data.Length);
                    }

                    var buffer = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(buffer, 0, buffer.Length);
                    return Convert.ToBase64String(buffer);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 文件转base64
        /// </summary>
        /// <returns>base64字符串</returns>
        public static string FileToBase64String(string path)
        {
            FileStream fsForRead = new FileStream(path, FileMode.Open);//文件路径
            string base64Str = "";
            try
            {
                fsForRead.Seek(0, SeekOrigin.Begin);
                byte[] bs = new byte[fsForRead.Length];
                int log = Convert.ToInt32(fsForRead.Length);
                fsForRead.Read(bs, 0, log);
                base64Str = Convert.ToBase64String(bs);
                return base64Str;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                Console.ReadLine();
                return base64Str;
            }
            finally
            {
                fsForRead.Close();
            }
        }
        public static void CallAPI()
        {
            if (AutoResetPlugin.config.API)
            {
                try
                {
                    HttpClient client = new();
                    HttpResponseMessage? response = null;

                    client.Timeout = TimeSpan.FromSeconds(180.0);
                    response = client.GetAsync(AutoResetPlugin.config.HttpAPI).Result;
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        TShock.Log.ConsoleWarn($"[自动重置]调用API失败! (状态码: {((int)response.StatusCode)})");
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.Message);
                }
            }

        }
    }
}
