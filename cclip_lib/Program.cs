using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
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
        public static void Main(bool list, bool all, bool outputJson, string clipboardFormat, string output)
        {
            // 出力パス
            var outPath = output;
            if (outPath != "" && !System.IO.File.Exists(outPath))
            {
                Path.Combine(Directory.GetCurrentDirectory(), output);
            }
            // 出力文字列の取得
            object resultData;
            if (list)
            {
                resultData = FormatMode();
            }
            else if(outputJson)
            {
                resultData = JsonMode(!all);
            }
            else
            {
                resultData = TextMode(clipboardFormat);
            }
            // 出力
            if (resultData != null)
            {
                if (output == "")
                {
                    if (resultData is string resultStr)
                    {
                        Console.WriteLine(resultStr);
                    }
                    else if (resultData is byte[] resultBytes)
                    {
                        Console.WriteLine(System.Convert.ToBase64String(resultBytes));
                    }
                }
                else
                {
                    if (resultData is string resultStr)
                    {
                        File.WriteAllText(outPath, resultStr);
                    }
                    else if (resultData is byte[] resultBytes)
                    {
                        File.WriteAllBytes(outPath, resultBytes);
                    }
                }
            }

        }
        private static string FormatMode()
        {
            var dataObj = Clipboard.GetDataObject();
            var formats = dataObj.GetFormats();
            var formatsStr = string.Join("\n", formats);
            return formatsStr;
        }
        private static string JsonMode(bool onFilter)
        {
            var normalFormats = new string[] {
                "Text",
                "Bitmap",
                "FileDrop",
                "HTML Format",
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
            return json;
        }
        private static object TextMode(string clipboardFormat)
        {
            var normalFormats = new string[] {
                clipboardFormat.ToLower(),
            };
            var allClipData = GetClipData();
            IEnumerable<ClipData> clipData = allClipData.Where(x => normalFormats.Contains(x.Format.ToLower()));
            if (clipData.ToArray().Length == 1)
            {
                return clipData.First().Data;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// クリップボードからデータを取得し、一般的な形式に直してリターンする。
        /// </summary>
        /// <returns></returns>
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
        

        /// <summary>
        /// クリップボードから取得したデータを、一般的な形式
        /// （文字列・バイト列・文字列の配列）に変換し、CぃｐDataオブジェクトとして
        /// リターンする。
        /// </summary>
        /// <param name="sourceData"></param>
        /// <returns></returns>
        static object ConvertDataForOutput(object sourceData)
        {
            static byte[] InteropBitmapToBytes(BitmapSource bmp)
            {
                using var stream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
                var result = stream.ToArray();
                return result;
            }
            object convertedData = sourceData switch
            {
                // Byte列に変換する
                MemoryStream stream => stream.ToArray(),
                // Byte列に変換する
                InteropBitmap bmp => InteropBitmapToBytes(bmp),
                // 文字列に変換する
                string str => str,
                // 文字列の配列に変換する
                string[] strArray => strArray,
                _ => null,
            };
            return convertedData;
        }
        ///
        /// クリップデータの列をJsonに変換する。
        static string ToJson(IEnumerable<ClipData> data)
        {
            var clipList = new List<Dictionary<string, object>>();
            var jsonOption = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
            };

            foreach (var clip in data)
            {
                object jsonData = clip.Data switch
                {
                    byte[] bytes => System.Convert.ToBase64String(bytes),
                    _ => clip.Data,
                };
                var oneData = new Dictionary<string, object>()
                {
                    {"format", clip.Format },
                    {"data", jsonData},
                };
                clipList.Add(oneData);
            }
            
            var json = JsonSerializer.Serialize(clipList, jsonOption);
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
