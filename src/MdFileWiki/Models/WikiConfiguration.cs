using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using CommonMark;
using MyToolkit.Model;

namespace MdFileWiki.Models
{
    public class WikiConfiguration : ObservableObject, IDisposable
    {
        private readonly object _lock = new object();

        private string _name;
        private string _inputPath;
        private string _outputPath;
        private bool _autoCreateNewFiles;
        private bool _askToOpenNewFiles;
        private string _htmlTemplate;
        private FileSystemWatcher _watcher;

        public WikiConfiguration()
        {
            Logs = new ObservableCollection<string>();
        }

        /// <summary>Gets or sets the name. </summary>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        /// <summary>Gets or sets the input path. </summary>
        public string InputPath
        {
            get { return _inputPath; }
            set { Set(ref _inputPath, value); }
        }

        /// <summary>Gets or sets the output path. </summary>
        public string OutputPath
        {
            get { return _outputPath; }
            set { Set(ref _outputPath, value); }
        }

        /// <summary>Gets or sets a value indicating whether to automatically create new files when a wiki link is detected. </summary>
        public bool AutoCreateNewFiles
        {
            get { return _autoCreateNewFiles; }
            set { Set(ref _autoCreateNewFiles, value); }
        }

        /// <summary>Gets or sets a value indicating whether to ask the user to open newly created wiki files. </summary>
        public bool AskToOpenNewFiles
        {
            get { return _askToOpenNewFiles; }
            set { Set(ref _askToOpenNewFiles, value); }
        }

        [XmlIgnore]
        public ObservableCollection<string> Logs { get; private set; }

        public void AddLog(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                Logs.Insert(0, string.Format("{0}: {1}", DateTime.Now, message));
            }));
        }

        public void RegisterFileWatcher()
        {
            UnregisterFileWatcher();

            if (Directory.Exists(InputPath))
            {
                _watcher = new FileSystemWatcher();
                _watcher.Path = InputPath;
                _watcher.Changed += OnFileChanged;
                _watcher.Created += OnFileCreated;
                _watcher.Deleted += OnFileDeleted;
                _watcher.EnableRaisingEvents = true;

                AddLog(string.Format("Now watching: {0}", InputPath));
            }
            else
                AddLog(string.Format("Directory Path does not exist: {0}", InputPath));
        }

        private void UnregisterFileWatcher()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public async Task GenerateHtmlFilesAsync()
        {
            await GenerateHtmlFilesAsync(InputPath);
        }

        private async Task GenerateHtmlFilesAsync(string path)
        {
            foreach (var file in await Task.Run(() => Directory.GetFiles(path, "*.md")))
                await GenerateHtmlFileAsync(file);
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs args)
        {
            if (args.FullPath.EndsWith(".md"))
                await GenerateHtmlFileAsync(args.FullPath);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs args)
        {
            if (args.FullPath.EndsWith(".md"))
            {
                var htmlPath = GetHtmlPath(args.FullPath);
                if (File.Exists(htmlPath))
                    File.Delete(htmlPath);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs args)
        {
            if (args.FullPath.EndsWith(".md"))
                GenerateHtmlFileAsync(args.FullPath);
        }

        private string GetHtmlPath(string file)
        {
            if (!Directory.Exists(OutputPath))
                throw new InvalidOperationException("Output path not found. ");

            var htmlFileName = Path.GetFileNameWithoutExtension(file) + ".html";
            return Path.Combine(OutputPath, htmlFileName);
        }

        public async Task GenerateHtmlFileAsync(string file)
        {
            try
            {
                await Task.Delay(1000);
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        LoadHtmlTemplate();

                        var htmlPath = GetHtmlPath(file);
                        using (var stream = new StreamReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
                        {
                            var data = stream.ReadToEnd();
                            data = ReplaceWikiLinks(data);

                            var output = _htmlTemplate.Replace("{CONTENT}", CommonMarkConverter.Convert(data));
                            output = output.Replace("{TITLE}", Path.GetFileNameWithoutExtension(file));

                            File.WriteAllText(htmlPath, output);
                        }
                    }
                });

                AddLog(string.Format("Updated: {0}", file));
            }
            catch (Exception exception)
            {
                AddLog(string.Format("Error: {0}", exception.Message));
            }
        }

        private string ReplaceWikiLinks(string data)
        {
            data = Regex.Replace(data, @"\[\[(.*?)\]\]", m =>
            {
                var array = m.Groups[1].Value.Split('|');
                var title = array[0];
                var link = array.Length > 1 ? array[1] : array[0];

                if (AutoCreateNewFiles)
                    CreateMdFile(link);

                return string.Format("[{0}]({1}.html)", title, link);
            });

            return data;
        }

        private void CreateMdFile(string file)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                var path = Path.Combine(InputPath, file + ".md");
                if (!File.Exists(path))
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    File.WriteAllText(path, "# " + fileName + "\n\nTODO");

                    if (AskToOpenNewFiles)
                    {
                        Application.Current.MainWindow.Activate();

                        var message = string.Format("Do you want to open the newly created MD file '{0}`?", fileName);
                        if (MessageBox.Show(message, "Open MD File", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            Process.Start(path, string.Empty);
                    }
                }
            }));
        }

        private void LoadHtmlTemplate()
        {
            var customTemplate = Path.Combine(InputPath, "Templates/Layout.html");
            if (File.Exists(customTemplate))
                _htmlTemplate = File.ReadAllText(customTemplate);
            else
                _htmlTemplate = File.ReadAllText("Templates/Layout.html");
        }

        public void Dispose()
        {
            UnregisterFileWatcher();
        }

        public void Apply()
        {
            RegisterFileWatcher();
        }
    }
}