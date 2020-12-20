using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace cclip_lib
{

    public struct ClipData
    {
        public readonly string Format;
        public readonly object Source;
        public readonly object Data;

        public ClipData(string format, object source, object data) : this()
        {
            this.Format = format;
            this.Source = source;
            this.Data = data;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {

            var allFlag = args.Contains("--all");

            if (args.Contains("--formats"))
            {
                var outText = FormatMode();
                Console.WriteLine(outText);
            }
            else if( args.Contains("--xml"))
            {
                XmlMode(!allFlag);
            }
            else if( args.Contains("--json"))
            {
                JsonMode(!allFlag);
            }
            else
            {
                TextMode();
            }

        }
        private static string FormatMode()
        {
            var dataObj = Clipboard.GetDataObject();
            var formats = dataObj.GetFormats();
            var formatsStr = string.Join("\n", formats);
            return formatsStr;
        }
        private static void XmlMode(bool onFilter)
        {
            var normalFormats = new string[] {
                "Text",
                "Bitmap",
                "FileDrop",
                "HTML Format",
                "Csv",
                "Rich Text Format",
            };
            IEnumerable<ClipData> clipData;
            if (onFilter)
            {
                clipData = GetClipData().Where(x => normalFormats.Contains(x.Format));
            }
            else
            {
                clipData = GetClipData();
            }
            var json = ToXml(clipData);
            Console.WriteLine(json);
            

        }
        private static void JsonMode(bool onFilter)
        {
            var normalFormats = new string[] {
                "Text",
                "Bitmap",
                "FileDrop",
                "HTML Format",
                "Csv",
                "Rich Text Format",
            };
            IEnumerable<ClipData> clipData;
            if (onFilter)
            {
                clipData = GetClipData().Where(x => normalFormats.Contains(x.Format));
            }
            else
            {
                clipData = GetClipData();
            }
            var json = ToJson(clipData);
            Console.WriteLine(json);
        }
        private static void TextMode()
        {
            var normalFormats = new string[] {
                "Text",
            };
            IEnumerable<ClipData> clipData = clipData = GetClipData().Where(x => normalFormats.Contains(x.Format));
            if (clipData.ToArray().Length == 1)
            {
                Console.WriteLine(clipData.First().Data.ToString());
            }
        }
        public static ClipData[] GetClipData()
        {
            var dataObj = Clipboard.GetDataObject();
            var formats = dataObj.GetFormats();
            static ClipData getData(string f)
            {
                try {
                    object data = Clipboard.GetData(f);
                    if (data != null)
                    {
                        return new ClipData(f, data, ConvertDataForOutput(data));
                    }
                    else
                    {
                        return new ClipData(f, null, null);
                    }
                }
                catch (COMException)
                {
                    return new ClipData(f, null, null);
                }
            }
            var clipDict = formats.Select((f) => getData(f));
            return clipDict.ToArray();

        }
        

        static object ConvertDataForOutput(object sourceData)
        {
            static byte[] InteropBitmapToBytes(BitmapSource bmp)
            {
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Save(stream);
                    var result = stream.ToArray();
                    return result;
                }
            }
            object dataForJson = sourceData.GetType().Name switch
            {
                "MemoryStream" => Convert.ToBase64String(((MemoryStream)sourceData).ToArray()).ToString(),
                "InteropBitmap" => Convert.ToBase64String(InteropBitmapToBytes((InteropBitmap)sourceData)).ToString(),
                //"Bitmap" => Convert.ToBase64String(BitmapToBytes((Bitmap)sourceData)).ToString(),
                "String" => sourceData.ToString(),
                "String[]" => (string[])sourceData,
                _ => null,
            };
            return dataForJson;
        }
        ///
        /// クリップデータの列をJsonに変換する。
        static string ToJson(IEnumerable<ClipData> data)
        {
            var clipList = new List<Dictionary<string, object>>();
            foreach (var clipData in data)
            {
                var oneData = new Dictionary<string, object>()
                {
                    {"format", clipData.Format },
                    {"type", clipData.Source?.GetType().Name },
                    {"data", clipData.Data},
                };
                clipList.Add(oneData);
            }
            var json = JsonSerializer.Serialize(clipList);
            return json;
        }
        struct XmlData
        {
            public string Format;
            public string Type;
            public object Data;
        }
        struct XmlData2
        {
            public XmlData[] ClipList;
        }
        static string ToXml(IEnumerable<ClipData> data)
        {
            var clipList = new XmlData[] { };
            foreach (var clipData in data)
            {
                var oneData = new XmlData()
                {
                    Format = clipData.Format,
                    Type = clipData.Source?.GetType().Name,
                    Data = clipData.Data,
                };
                _ = clipList.Append(oneData);
            }
            var xmlData = new XmlData2() { ClipList = clipList };
            var serializer = new XmlSerializer(typeof(XmlData2));
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, xmlData);
                var result = stream.ToArray();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
