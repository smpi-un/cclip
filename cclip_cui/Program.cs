using System;
using System.Collections.Generic;
using System.Linq;

namespace cclip_cui
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var argsLow = args.Select(x => x.ToLower());
            var queue = new Queue<string>(argsLow);

            // 複数出力の場合に全フォーマットを出力するか
            bool all = false;
            // 出力先パス(空白の場合は標準出力)
            var output = "";
            // リスト表示をするか
            var list = false;
            // 単体出力の場合のクリップボード取得元タイプ
            var clipboardFormat = "Text";
            // json形式で出力するか
            var outputJson = false;

            while(queue.Count > 0)
            {
                var data = queue.Dequeue();
                if( data[0] == '-' && data[1] != '-')
                {
                    switch (data)
                    {
                        case "-l":
                            list = true;
                            break;
                        case "-a":
                            all = true;
                            break;
                        case "-j":
                            outputJson = true;
                            break;
                        case "-o":
                            if (queue.Count > 0)
                            {
                                output = queue.Dequeue();
                            }
                            break;
                        case "-f":
                            if (queue.Count > 0)
                            {
                                clipboardFormat = queue.Dequeue();
                            }
                            break;
                        default:
                            // do nothing
                            break;
                    }

                }
                else if (data[0] == '-' && data[1] == '-')
                {
                    switch (data)
                    {
                        case "--list":
                            list = true;
                            break;
                        case "--all":
                            all = true;
                            break;
                        case "--json":
                            outputJson = true;
                            break;
                        case "--output":
                            output = queue.Dequeue();
                            break;
                        case "--format":
                            clipboardFormat = queue.Dequeue();
                            break;
                        default:
                            // do nothing
                            break;
                    }

                }
            }
           
            cclip_lib.Program.Main(list, all, outputJson, clipboardFormat, output);
        }
    }
}
