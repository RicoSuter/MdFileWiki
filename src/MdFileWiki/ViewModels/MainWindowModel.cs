using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MdFileWiki.Models;
using MyToolkit.Command;
using MyToolkit.Mvvm;
using MyToolkit.Serialization;
using MyToolkit.Storage;
using MyToolkit.Utilities;

namespace MdFileWiki.ViewModels
{
    public class MainWindowModel : ViewModelBase
    {
        private WikiConfiguration _selectedConfiguration;

        public MainWindowModel()
        {
            UpdateCommand = new AsyncRelayCommand<WikiConfiguration>(GenerateHtmlFilesAsync);
            AddCommand = new RelayCommand(Add);
            RemoveCommand = new RelayCommand<WikiConfiguration>(Remove);
            ApplyCommand = new RelayCommand<WikiConfiguration>(Apply);
        }

        /// <summary>Gets or sets the configurations. </summary>
        public ObservableCollection<WikiConfiguration> Configurations { get; private set; }

        /// <summary>Gets or sets the selected configuration. </summary>
        public WikiConfiguration SelectedConfiguration
        {
            get { return _selectedConfiguration; }
            set { Set(ref _selectedConfiguration, value); }
        }

        /// <summary>Gets the application version with build time. </summary>
        public string ApplicationVersion
        {
            get { return GetType().Assembly.GetVersionWithBuildTime(); }
        }

        public ICommand UpdateCommand { get; private set; }

        public ICommand AddCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand ApplyCommand { get; private set; }

        protected override void OnLoaded()
        {
            if (Configurations != null)
            {
                foreach (var configuration in Configurations)
                    configuration.Dispose();
            }

            var xml = ApplicationSettings.GetSetting("Configurations", string.Empty);
            Configurations = xml != string.Empty ?
                new ObservableCollection<WikiConfiguration>(XmlSerialization.Deserialize<WikiConfiguration[]>(xml)) :
                new ObservableCollection<WikiConfiguration>();

            SelectedConfiguration = Configurations.FirstOrDefault();

            RaisePropertyChanged(() => Configurations);

            foreach (var configuration in Configurations)
                configuration.RegisterFileWatcher();
        }

        protected override void OnUnloaded()
        {
            var xml = XmlSerialization.Serialize(Configurations.ToArray());
            ApplicationSettings.SetSetting("Configurations", xml);
        }

        private void Apply(WikiConfiguration configuration)
        {
            configuration.Apply();
        }

        private void Remove(WikiConfiguration configuration)
        {
            Configurations.Remove(configuration);
            SelectedConfiguration = Configurations.FirstOrDefault();
        }

        private void Add()
        {
            var configuration = new WikiConfiguration { Name = string.Empty };
            Configurations.Add(configuration);
            SelectedConfiguration = configuration;
        }

        private async Task GenerateHtmlFilesAsync(WikiConfiguration configuration)
        {
            await configuration.GenerateHtmlFilesAsync();
        }
    }
}
