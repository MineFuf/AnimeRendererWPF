using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NReco;
using NReco.VideoConverter;
using NReco.VideoInfo;


namespace AnimeRenderer
{
    class FileObj
    {
        public string Path;
        public string Name;

        public DirObj Parent;
        public bool hasParent { get => Parent != null; }

        public bool isDir { get; protected set; } = false;
    }

    class OutputFileObj : FileObj
    {
        public string OutPath;

        public OutputFileObj(FileObj old)
        {
            Path = old.Path;
            Name = old.Name;
            Parent = old.Parent;
        }
        public OutputFileObj() { }
    }

    class DirObj : FileObj
    {
        public List<FileObj> allFiles = new List<FileObj>();
        public List<FileObj> Contains = new List<FileObj>();
        public bool hasChilds { get => Contains.Count == 0 ? false : true; }

        public DirObj()
        {
            isDir = true;
        }

        public bool renderFolder()
        {
            if (!System.IO.Directory.Exists(Path))
                return false;

            string[] contentFiles;
            string[] contentDirs;
            try
            {
                contentFiles = System.IO.Directory.GetFiles(Path);
                contentDirs = System.IO.Directory.GetDirectories(Path);
            } catch
            {
                return false;
            }

            foreach (string file in contentFiles)
            {
                FileObj File = new FileObj
                {
                    Name = System.IO.Path.GetFileName(file),
                    Path = file,
                    Parent = this
                };
                addFile(File);
                Contains.Add(File);
            }

            foreach (string dir in contentDirs)
            {
                DirObj Dir = new DirObj
                {
                    Name = System.IO.Path.GetFileName(dir),
                    Path = dir,
                    Parent = this
                };
                Dir.renderFolder();
                Contains.Add(Dir);
            }
            return true;
        }

        public void addFile(FileObj file)
        {
            allFiles.Add(file);
            if (hasParent)
                Parent.addFile(file);
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] videoExtensions = new string[] { ".mp4", ".avi", ".mkv" };

        System.Windows.Media.Color _folderColor = Colors.Blue;
        System.Windows.Media.Color _fileColor = Colors.Black;

        SolidColorBrush folderColor;
        SolidColorBrush fileColor;

        runnerThread runner;

        ObservableCollection<CheckableItem> items;
        public MainWindow()
        {
            folderColor = new SolidColorBrush(_folderColor);
            fileColor = new SolidColorBrush(_fileColor);

            InitializeComponent();

            videoCodecCombo.ItemsSource = Enum.GetValues(typeof(CodecOption)).Cast<CodecOption>();
            videoCodecCombo.SelectedIndex = 0;

            videoTypeCombo.ItemsSource = Enum.GetValues(typeof(VideoTypeOption)).Cast<VideoTypeOption>();
            videoTypeCombo.SelectedIndex = 0;

            videoResolutionCombo.ItemsSource = new string[] { "copy", "1080p", "720p", "480p", "240p" };
            videoResolutionCombo.SelectedIndex = 0;

            videoQualityCombo.ItemsSource = Enum.GetValues(typeof(QualityOption)).Cast<QualityOption>();
            videoQualityCombo.SelectedIndex = 2;

            audioCodecCombo.ItemsSource = Enum.GetValues(typeof(AudioOption)).Cast<AudioOption>();
            audioCodecCombo.SelectedIndex = 0;
        }

        private ImageSource getIcoForFile(string p)
        {
            ImageSource img;
            using (Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(p))
                img = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            return img;
        }

        private void chooseOutputClick(object sender, RoutedEventArgs e)
        {
            string selectedPath = "";
            var t = new Thread((ThreadStart)(() => {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.RootFolder = System.Environment.SpecialFolder.MyComputer;
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                selectedPath = fbd.SelectedPath;
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            Console.WriteLine(selectedPath);

            outputDirTextBox.Text = selectedPath;
        }

        public void chooseInputClick(object sender, RoutedEventArgs e)
        {
            string selectedPath = "";
            var t = new Thread((ThreadStart)(() => {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.RootFolder = System.Environment.SpecialFolder.MyComputer;
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                selectedPath = fbd.SelectedPath;
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            Console.WriteLine(selectedPath);

            inputDirTextBox.Text = selectedPath;
            items = new ObservableCollection<CheckableItem>();
            filesTreeView.ItemsSource = items;
            listDir(selectedPath);
        }

        void listDir(string p)
        {
            string[] Files;
            string[] Folders;
            try
            {
                Files = Directory.GetFiles(p);
                Folders = Directory.GetDirectories(p);
            } catch
            {
                return;
            }

            foreach (string dir in Folders)
            {
                var item = new CheckableItem();
                //item.ico = getIcoForFile(dir);
                item.Path = dir;
                item.IsFolder = true;
                item.TextColor = folderColor;
                item.Value = System.IO.Path.GetFileName(dir);
                items.Add(item);
                listDir(System.IO.Path.Combine(p, dir), item);
            }
            Files.ToList().ForEach(i => {
                var it = new CheckableItem();
                it.Path = i;
                it.TextColor = fileColor;
                //it.ico = getIcoForFile(i);
                it.Value = System.IO.Path.GetFileName(i);
                items.Add(it);
            });
        }
        void listDir(string p, CheckableItem parent)
        {
            string[] Files = Directory.GetFiles(p);
            string[] Folders = Directory.GetDirectories(p);

            foreach (string dir in Folders)
            {
                var item = new CheckableItem();
                item.Path = dir;
                item.Parent = parent;
                item.IsFolder = true;
                item.TextColor = folderColor;
                //item.ico = getIcoForFile(dir);
                item.Value = System.IO.Path.GetFileName(dir);
                parent.Children.Add(item);
                listDir(System.IO.Path.Combine(p, dir), item);
            }

            Files.ToList().ForEach(i => {
                var it = new CheckableItem();
                //it.ico = getIcoForFile(i);
                it.Path = i;
                it.TextColor = fileColor;
                it.Parent = parent;
                it.Value = System.IO.Path.GetFileName(i);
                parent.Children.Add(it);
            });
        }

        private void filesTreeView_Selected(object sender, RoutedEventArgs e)
        {
            CheckableItem item = filesTreeView.SelectedItem as CheckableItem;
            Console.WriteLine(System.IO.Path.GetExtension(item.Path).ToLower());
            if (!videoExtensions.Contains(System.IO.Path.GetExtension(item.Path).ToLower()))
                return;
            var video = new MediaFile { Filename = item.Path };
            using (Engine engine = new Engine())
            {
                engine.GetMetadata(video);
            }

            FilenametextBlock.Text = item.Value;
            FrameSizetextBlock.Text = video.Metadata.VideoData.FrameSize;
            FPStextBlock.Text = video.Metadata.VideoData.Fps.ToString();
            CodectextBlock.Text = video.Metadata.VideoData.Format;
            DurationtextBlock.Text = video.Metadata.Duration.ToString();
        }

        private void RunRenderButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputDirTextBox.Text))
            {
                inputDirTextBox.Background = new SolidColorBrush(Colors.Red);
                return;
            }
            else
                inputDirTextBox.Background = new SolidColorBrush(Colors.White);

            if (string.IsNullOrWhiteSpace(outputDirTextBox.Text))
            {
                outputDirTextBox.Background = new SolidColorBrush(Colors.Red);
                return;
            }
            else
                outputDirTextBox.Background = new SolidColorBrush(Colors.White);

            DirObj dir = new DirObj
            {
                Path = inputDirTextBox.Text
            };

            bool dirError = !dir.renderFolder();
            if (dirError)
            {
                Console.WriteLine("We wasn't able to render folder");
                return;
            }

            dir.allFiles.ForEach(file => Console.WriteLine("Path: \"" + file.Path + "\""));
            List<OutputFileObj> videoFiles = new List<OutputFileObj>();
            List<OutputFileObj> otherFiles = new List<OutputFileObj>();

            Func<FileObj, ObservableCollection<CheckableItem>, CheckableItem> findCorrespondingItem = null;
            findCorrespondingItem = (file, items) => {
                foreach (CheckableItem item in items)
                {
                    if (item.Path == file.Path)
                        return item;
                    var foundItem = findCorrespondingItem(file, item.Children);
                    if (foundItem != null)
                        return foundItem;
                }
                return null;
            };

            foreach (FileObj file in dir.allFiles)
            {
                if (findCorrespondingItem(file, items).IsChecked)
                {
                    string relPath = new Uri(inputDirTextBox.Text).MakeRelativeUri(new Uri(file.Path)).ToString();
                    string outPath = System.IO.Path.Combine(outputDirTextBox.Text, relPath);
                    outPath = Uri.UnescapeDataString(outPath);

                    if (videoExtensions.Contains(System.IO.Path.GetExtension(file.Name)))
                        videoFiles.Add(new OutputFileObj(file) { OutPath = outPath });
                    else
                        otherFiles.Add(new OutputFileObj(file) { OutPath = outPath });
                }
            }

            bool to1Dir = videoTo1Dir.IsChecked == true;
            bool CopyAllFiles = copyAllFiles.IsChecked == true;

            CodecOption codec = (CodecOption)videoCodecCombo.SelectedItem;
            AudioOption audioCodec = (AudioOption)audioCodecCombo.SelectedItem;
            ResolutionOption res = Enum.GetValues(typeof(ResolutionOption)).Cast<ResolutionOption>().ToArray()[videoResolutionCombo.SelectedIndex];
            QualityOption quality = (QualityOption)videoQualityCombo.SelectedItem;
            VideoTypeOption type = (VideoTypeOption)videoTypeCombo.SelectedItem;

            runner = new runnerThread();

            Thread t = new Thread(() => { runner.run(
                                            to1Dir,
                                            CopyAllFiles,
                                            videoFiles,
                                            otherFiles,
                                            this,
                                            codec,
                                            audioCodec,
                                            res,
                                            quality,
                                            type); });
            t.Start();
        }

        #region options
        enum CodecOption
        {
            copy,
            h264,
            HEVC
        }
        enum AudioOption
        {
            copy,
            aac,
            flac,
            mp3,
            opus
        }
        enum ResolutionOption
        {
            copy,
            _1080,
            _720,
            _480,
            _240
        }
        enum QualityOption
        {
            Ultraslow = 1,
            Ultrafast = 51,
            Normal = 19,
            Faster = 23,
            Slower = 15
        }
        enum VideoTypeOption
        {
            copy,
            mp4,
            avi,
            mkv
        }
        #endregion

        class runnerThread
        {

            FFMpegConverter Converter;
            public void run(bool to1dir, bool allFiles, List<OutputFileObj> videos, List<OutputFileObj> others, MainWindow window,
                CodecOption codec, AudioOption audioOp, ResolutionOption res, QualityOption quality, VideoTypeOption typeOp)
            {
                Console.WriteLine(
                    "Running Configurartion:" +
                    "\n  Codec: " + Enum.GetName(typeof(CodecOption), codec) +
                    "\n  Audio Codec: " + Enum.GetName(typeof(AudioOption), audioOp) + 
                    "\n  Video Resolution: " + Enum.GetName(typeof(ResolutionOption), res) + 
                    "\n Video Type: " + Enum.GetName(typeof(VideoTypeOption), typeOp));

                int totalVideos = videos.Count * 100;
                window.Dispatcher.Invoke(() => window.allVideoFilesProgressBar.Maximum = 100);

                Converter = new FFMpegConverter();
                int index = 0;
                MainWindow _window = window;
                Thread _t = Thread.CurrentThread;
                Converter.ConvertProgress += (sender, e) =>
                {
                    try
                    {
                        _window.Dispatcher.Invoke(() =>
                        {
                            Console.WriteLine("Processed: " + e.Processed);
                            _window.currFileProgressBar.Value = (float)e.Processed.TotalSeconds / (float)e.TotalDuration.TotalSeconds * 100;
                            Console.WriteLine((float)e.Processed.TotalSeconds / (float)e.TotalDuration.TotalSeconds * 100);
                            Console.WriteLine(_window.currFileProgressBar.Value);
                            _window.allVideoFilesProgressBar.Value = 100 / (float)videos.Count * index +
                                    (float)e.Processed.TotalSeconds / (float)e.TotalDuration.TotalSeconds * 100 / (float)videos.Count;
                        });
                    }
                    catch (TaskCanceledException tce)
                    {
                        _t.Abort();
                    }
                };
                foreach (OutputFileObj video in videos)
                {
                    var videoMedia = new NReco.VideoInfo.FFProbe().GetMediaInfo(video.Path);
                    window.Dispatcher.Invoke(() =>
                    {
                        window.currFileProgressBar.Maximum = 100;
                    });

                    if (typeOp != VideoTypeOption.copy)
                    {
                        video.OutPath = System.IO.Path.ChangeExtension(video.OutPath, "." + Enum.GetName(typeof(VideoTypeOption), typeOp));
                    }

                    if (audioOp == AudioOption.flac &&
                        System.IO.Path.GetExtension(video.Path).ToLower() ==
                        "." + Enum.GetName(typeof(VideoTypeOption), VideoTypeOption.mp4).ToLower())
                        audioOp = AudioOption.copy;

                    string scaleString =
                        res == ResolutionOption.copy ? "" :
                        res == ResolutionOption._1080 ? "-vf scale=-2:1080" :
                        res == ResolutionOption._720 ? "-vf scale=-2:720" :
                        res == ResolutionOption._480 ? "-vf scale=-2:480" :
                        res == ResolutionOption._240 ? "-vf scale=-2:240" : "";

                    Func<string> getCopyCodec = () =>
                    {
                        string videoCodec = videoMedia.Streams.ToList().Find(
                            (item) => item.CodecType.ToLower() == "video").CodecName;
                        if (videoCodec.Contains("h264"))
                            return "libx264";
                        if (videoCodec.Contains("hevc"))
                            return "libx265";
                        return "libx264";
                    };

                    string codecString =
                        codec == CodecOption.copy ? getCopyCodec() :
                        codec == CodecOption.h264 ? "libx264" :
                        codec == CodecOption.HEVC ? "libx265" : getCopyCodec();

                    string Command = string.Format(
                        "-c:v {3} {0} -crf {1} -c:a {2} -stats -y",
                        scaleString, (int)quality,
                        Enum.GetName(typeof(AudioOption), (int)audioOp), codecString);

                    Console.WriteLine("Input Path: '" + video.Path + "'");
                    Console.WriteLine("Output Path: '" + video.OutPath + "'");

                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(video.OutPath));
                    Console.WriteLine("Created direcotory: '" + System.IO.Path.GetDirectoryName(video.OutPath) + "'");
                    Console.WriteLine("Running Command: '" + Command + "'");
                    Converter.ConvertMedia(video.Path, null, video.OutPath, null, new ConvertSettings()
                    {
                        CustomOutputArgs = Command
                    });
                    Console.WriteLine("Video converted");

                    index++;
                }
            }
        }

        private void videoTo1Dir_Click(object sender, RoutedEventArgs e)
        {
            copyAllFiles.IsEnabled = videoTo1Dir.IsChecked == true ? false : true;
        }

        private void copyAllFiles_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!(runner is null))
            {
                runner._Engine.();
                Console.WriteLine("Engine disposed");
                MessageBox.Show("Engine Dispoded");
            }
        }
    }
}
