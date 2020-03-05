using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcoderReaderServer
{
    class Program
    {
        HttpServer mHttpServer;
        static void Main(string[] args)
        {
            HttpServer mHttpServer = new HttpServer();
            Console.ReadKey(false);
        }
    }
}
