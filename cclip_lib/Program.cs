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
    public class ConsoleHelper
    {
        /// <summary>
        /// Allocates a new console for current process.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();

        /// <summary>
        /// Frees the console.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();
    }

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



        // This always writes to the parent console window and also to a redirected stdout if there is one.
        // It would be better to do the relevant thing (eg write to the redirected file if there is one, otherwise
        // write to the console) but it doesn't seem possible.
        public class GUIConsoleWriter
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            private static extern bool AttachConsole(int dwProcessId);

            private const int ATTACH_PARENT_PROCESS = -1;

            StreamWriter _stdOutWriter;

            // this must be called early in the program
            public GUIConsoleWriter()
            {
                // this needs to happen before attachconsole.
                // If the output is not redirected we still get a valid stream but it doesn't appear to write anywhere
                // I guess it probably does write somewhere, but nowhere I can find out about
                var stdout = Console.OpenStandardOutput();
                _stdOutWriter = new StreamWriter(stdout);
                _stdOutWriter.AutoFlush = true;

                AttachConsole(ATTACH_PARENT_PROCESS);
            }

            public void WriteLine(string line)
            {
                _stdOutWriter.WriteLine(line);
                Console.WriteLine(line);
            }
        }
    public class Program
    {

                [DllImport("kernel32.dll")]
        static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);
        [DllImport("kernel32.dll")]
        private static extern SafeFileHandle GetStdHandle(uint nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(uint nStdHandle, SafeFileHandle hHandle);
        [DllImport("kernel32.dll")]
        private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, SafeFileHandle hSourceHandle, IntPtr hTargetProcessHandle,
        out SafeFileHandle lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
        private const uint STD_OUTPUT_HANDLE = 0xFFFFFFF5;
        private const uint STD_ERROR_HANDLE = 0xFFFFFFF4;
        private const uint DUPLICATE_SAME_ACCESS = 2;

        struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }



        static void InitConsoleHandles()
        {
            SafeFileHandle hStdOut, hStdErr, hStdOutDup;
            hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            hStdErr = GetStdHandle(STD_ERROR_HANDLE);

            // Get current process handle
            IntPtr hProcess = Process.GetCurrentProcess().Handle;

            // Duplicate Stdout handle to save initial value
            DuplicateHandle(hProcess, hStdOut, hProcess, out hStdOutDup,
            0, true, DUPLICATE_SAME_ACCESS);

            // Duplicate Stderr handle to save initial value
            DuplicateHandle(hProcess, hStdErr, hProcess, out SafeFileHandle hStdErrDup,
            0, true, DUPLICATE_SAME_ACCESS);

            // Attach to console window – this may modify the standard handles
            AttachConsole(ATTACH_PARENT_PROCESS);

            // Adjust the standard handles
            if (GetFileInformationByHandle(GetStdHandle(STD_OUTPUT_HANDLE), out _))
            {
                SetStdHandle(STD_OUTPUT_HANDLE, hStdOutDup);
            }
            else
            {
                SetStdHandle(STD_OUTPUT_HANDLE, hStdOut);
            }

            if (GetFileInformationByHandle(GetStdHandle(STD_ERROR_HANDLE), out _))
            {
                SetStdHandle(STD_ERROR_HANDLE, hStdErrDup);
            }
            else
            {
                SetStdHandle(STD_ERROR_HANDLE, hStdErr);
            }
        }


        private static void FormatMode()
        {
            var dataObj = Clipboard.GetDataObject();
            var formats = dataObj.GetFormats();
            var formatsStr = string.Join("\n", formats);
            Console.WriteLine(formatsStr);
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
        
        public static void Main(string[] args)
        {
            // InitConsoleHandles();
            // AttachConsole(ATTACH_PARENT_PROCESS);
            // ConsoleHelper.AllocConsole();



            var allFlag = args.Contains("--all");

            if (args.Contains("--formats"))
            {
                FormatMode();
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
