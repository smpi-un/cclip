using cclip_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ccliplog
{
    class JournalMeta
    {
        public string id { get; set; }
        public long creationDate { get; set; }
        public long modifiedDate { get; set; }
        public string version { get; set; }
        public string[] tags { get; set; }
        public bool starred { get; set; }
    }
    class JournalContents
    {
        public string text { get; set; }
        public int mood { get; set; }
        public string type { get; set; }
    }
    class Journal
    {
        public JournalMeta meta { get; set; }
        public JournalContents contents { get; set; }
        public Journal(string text)
        {
            var now = ToUnixTime(DateTime.Now);
            this.meta = new JournalMeta() {
                id = (now).ToString(),
                creationDate = now,
                modifiedDate = now,
                version = "1.0",
                tags = new string[]{ "ccliplog" },
                starred = false,
            };

            this.contents = new JournalContents()
            {
                text = text,
                mood = 0,
                type = "text/plain",
            };

        }
        public static long ToUnixTime(DateTime dt)
        {
            var dto = new DateTimeOffset(dt.Ticks, new TimeSpan(+09, 00, 00));
            return dto.ToUnixTimeMilliseconds();
        }
        public static DateTime FromUnixTime(long unixTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
        }

    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ClipData[] ClipData;
        public MainWindow()
        {
            InitializeComponent();
            this.ClipData = cclip_lib.Program.GetClipData();

            var textData = ClipData.Where(x => x.Format == "Text");
            var imageData = ClipData.Where(x => x.Format == "Bitmap");
            var fileData = ClipData.Where(x => x.Format == "FileDrop");

            // テキストボックス
            if (textData.Count() == 1)
            {
                this.PostTextBox.Text = textData.First().Data.ToString();
            }
            else if (fileData.Count() == 1)
            {
                this.PostTextBox.Text = string.Join("\n", fileData.First().Data as string[]);
            }
            else
            {
                this.PostTextBox.Text = "";
            }

            // 画像の添付
            if (ClipData.Select(x => x.Format).Contains("Bitmap"))
            {
                this.AttachFileLabel.Content = "画像の添付ファイルがあります。";
            }
            else
            {
                this.AttachFileLabel.Content = "";
            }
        }

        private void PostButton_Click(object sender, RoutedEventArgs e)
        {
            var j = new Journal(this.PostTextBox.Text);
            var json = System.Text.Json.JsonSerializer.Serialize(j);
            MessageBox.Show(json);

        }
    }
}
