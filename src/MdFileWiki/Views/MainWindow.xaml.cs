using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using MdFileWiki.ViewModels;
using MyToolkit.Mvvm;

namespace MdFileWiki
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon _trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            ViewModelHelper.RegisterViewModel(Model, this);

            Loaded += OnLoaded;
            Closed += OnClosed;
            StateChanged += OnStateChanged;
        }

        private void OnStateChanged(object sender, EventArgs eventArgs)
        {
            if (WindowState == WindowState.Minimized)
                Hide();
            else
                Show();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
#if !DEBUG
            WindowState = WindowState.Minimized;
#endif

            _trayIcon = new NotifyIcon();
            _trayIcon.DoubleClick += TrayIconOnDoubleClick;
            _trayIcon.Text = "MdFileWiki";
            _trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            _trayIcon.Visible = true;
        }

        private void TrayIconOnDoubleClick(object sender, EventArgs eventArgs)
        {
            Show();

            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;

            Activate();
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            _trayIcon.Dispose();
            Model.CallOnUnloaded();
        }

        public MainWindowModel Model
        {
            get { return (MainWindowModel)Resources["ViewModel"]; }
        }
    }
}
