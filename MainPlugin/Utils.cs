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

namespace AutoReset.MainPlugin
{
    internal class Utils
    {

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
                Task.Run(delegate
                {
                    try
                    {
                        HttpClient client =new();
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
                });
            }    
        }
    }
}
