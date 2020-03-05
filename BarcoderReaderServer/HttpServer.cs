using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BarcoderReaderServer
{
    class HttpServer
    {
        static HttpListener httpListener;
        BarcodeScan barcodeScan;

        public HttpServer()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://+:8080/");
                httpListener.Start();
                httpListener.BeginGetContext(HttpListenerAsyncCallbackHadle, null);
                IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());

                barcodeScan = new BarcodeScan();

                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        Console.WriteLine(i + "  " + IpEntry.AddressList[i].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HttpServer 启动失败：" + ex.ToString());
            }
        }

        public void HttpListenerAsyncCallbackHadle(IAsyncResult ar)
        {
            httpListener.BeginGetContext(HttpListenerAsyncCallbackHadle, null);
            var context = httpListener.EndGetContext(ar);
            if (context.Request.HttpMethod == "GET")
            {
                Console.WriteLine("Threa ID: " + Thread.CurrentThread.ManagedThreadId.ToString());
            }
            else if (context.Request.HttpMethod == "POST")
            {
                try
                {
                    string result = barcodeScan.GetResult(context.Request.InputStream);
                    if (result == null)
                    {
                        return;
                    }
                    var returnByteArr = Encoding.UTF8.GetBytes(result);
                    context.Response.ContentType = "application/json; charset=UTF-8";
                    using (var stream = context.Response.OutputStream)
                    {
                        context.Response.OutputStream.Write(returnByteArr, 0, returnByteArr.Length);
                    }
                }
                catch (Exception ex)
                {
                    context.Response.ContentType = "application/json; charset=UTF-8";
                    var returnByteArr = Encoding.UTF8.GetBytes(ex.ToString());
                    using (var stream = context.Response.OutputStream)
                    {
                        context.Response.OutputStream.Write(returnByteArr, 0, returnByteArr.Length);
                    }
                }
            }
        }
    }
}
