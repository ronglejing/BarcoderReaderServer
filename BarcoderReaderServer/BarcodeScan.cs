using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Dynamsoft.Barcode;
using Newtonsoft.Json;
using System.Drawing;
using System.Threading;

using System.Management;
using System.Management.Instrumentation;
using System.Security.Cryptography;

/*
* 方法：string GetResult(); //返回json格式string结果
*/

namespace BarcoderReaderServer
{
    class ScanObj
    {
        public int scanContentLen { get; set; }
        public List<scanContentWordsinfo> scanContent = new List<scanContentWordsinfo>();
    }

    class scanContentWordsinfo
    {
        public string word { get; set; }
    }

    class MyBarcodeReader
    {
        string productKey = "t0173ZQYAALXkmNagYswn85OLvnzR9XGsOWRrkDCBCWRz2PxpnxvmJkHATWpoIVaT4fMvsLgns3Vzd3TpkOkpJUFLUh5931xx36z3ptE7+9tk9E2BRqAxaC7DAoYFDAsYFjAsYFjAsIBhAcMChgKGAoYChgKGAoYChgKGAoYChgaGBoYGhgaGBoYGhgaGBob97uun9oagcd9kdI0+VWfYNAUagca/m6m5j5w+AQVCTAo= ";

        public bool isIdle { get; set; }
        public string textResult { get; set; }
        public BarcodeReader barcodeReader = null;

        public MyBarcodeReader()
        {
            isIdle = true;
            textResult = null;
            barcodeReader = new BarcodeReader(productKey);
        }
    }

    class BarcodeScan
    {
        int mBarcodeReaderNumber = 0;
        public List<MyBarcodeReader> myBarcodeReaderList;

        public BarcodeScan()
        {
            string sError;
            string runtimeSettingsFilePath = Directory.GetCurrentDirectory() + @"\runtimesettings.json";

            myBarcodeReaderList = new List<MyBarcodeReader>();

            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                mBarcodeReaderNumber += int.Parse(item["NumberOfCores"].ToString());
            }
            Console.WriteLine("Number Of Cores: {0}", mBarcodeReaderNumber);
            if (mBarcodeReaderNumber == 0)
            {
                mBarcodeReaderNumber = 4;
            }

            for (int i = 0; i < mBarcodeReaderNumber; i++)
            {
                myBarcodeReaderList.Add(new MyBarcodeReader());
                myBarcodeReaderList[i].barcodeReader.InitRuntimeSettingsWithFile(runtimeSettingsFilePath, EnumConflictMode.CM_OVERWRITE, out sError);
            }
        }

        public string GetResult(Stream stream)
        {
            int index = 0;
            int retryCount = 0;

            try
            {
                do
                {
                    if (myBarcodeReaderList[index].isIdle)
                    {
                        lock (myBarcodeReaderList[index])
                        {
                            Console.WriteLine("Threa ID: " + Thread.CurrentThread.ManagedThreadId.ToString() + "  BarcodeReader" + index + ": 开始");
                            myBarcodeReaderList[index].isIdle = false;
                            Bitmap bitMap = new Bitmap(stream);
                            ScanObj scanObj = new ScanObj();

                            TextResult[] textResults = myBarcodeReaderList[index].barcodeReader.DecodeBitmap(bitMap, "");
                            bitMap.Dispose();
                            scanObj.scanContentLen = textResults.Length;
                            for (var i = 0; i < textResults.Length; i++)
                            {
                                scanObj.scanContent.Add(new scanContentWordsinfo() { word = textResults[i].BarcodeText });
                            }
                            
                            Console.WriteLine("Threa ID: " + Thread.CurrentThread.ManagedThreadId.ToString() + "  BarcodeReader" + index + ": 完成");
                            myBarcodeReaderList[index].isIdle = true;

                            string result = JsonConvert.SerializeObject(scanObj);
                            return result;
                        }
                    }
                    else
                    {
                        index++;
                        if (index == myBarcodeReaderList.Count)
                        {
                            Thread.Sleep(10);
                            index = 0;
                            retryCount++;
                            if (retryCount >= 3000)//等待30S后服务器超时
                            {
                                return null;
                            }
                        }
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Threa ID: " + Thread.CurrentThread.ManagedThreadId.ToString() + "  BarcodeReader" + index + ": 异常");
                myBarcodeReaderList[index].isIdle = true;
                throw ex;
            }
        }

        public string Md5Verify(Stream fileStream)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] md5Byte = md5.ComputeHash(fileStream);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < md5Byte.Length; i++)
            {
                stringBuilder.Append(md5Byte[i].ToString("x2"));
            }

            md5.Dispose();
            return stringBuilder.ToString();
        }
    }
}
