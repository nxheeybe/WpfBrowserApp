using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace AudioPlayer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<string> _playHistory = new ObservableCollection<string>();
        private string _selectedFolder;
        private string[] _audioFiles;
        private MediaPlayer _player = new MediaPlayer();
        private CancellationTokenSource _cancellationTokenSource;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> PlayHistory
        {
            get { return _playHistory; }
            set { _playHistory = value; OnPropertyChanged(nameof(PlayHistory)); }
        }

        public string TimeInfo { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _selectedFolder = dialog.FileName;
                LoadAudioFiles();
                PlayAudio(0);
            }
        }

        private void LoadAudioFiles()
        {
            _audioFiles = Directory.GetFiles(_selectedFolder)
                                   .Where(file => file.EndsWith(".mp3")  file.EndsWith(".m4a"))
                                   .ToArray();
        }

        private void PlayAudio(int index)
        {
            _player.Open(new Uri(_audioFiles[index]));
            _player.Play();

            AddToHistory(_audioFiles[index]);
            StartPlaybackWatcher();
        }

        private void AddToHistory(string audioFile)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PlayHistory.Add(audioFile);
            });
        }

        private void StartPlaybackWatcher()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (_player.Position < _player.NaturalDuration.TimeSpan)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TimeInfo = $"{_player.Position:mm\\:ss} / {_player.NaturalDuration.TimeSpan:mm\\:ss}";
                        OnPropertyChanged(nameof(TimeInfo));
                    });

                    Thread.Sleep(1000);
                }
            }, _cancellationTokenSource.Token);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = Array.IndexOf(_audioFiles, _player.Source.LocalPath);
            int previousIndex = currentIndex > 0 ? currentIndex - 1 : _audioFiles.Length - 1;
            _player.Stop();
            PlayAudio(previousIndex);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Position == TimeSpan.Zero)
                _player.Play();
            else if (_player.Position == _player.NaturalDuration.TimeSpan)
                _player.Stop();
            else
                _player.Pause();
        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = Array.IndexOf(_audioFiles, _player.Source.LocalPath);
            int nextIndex = currentIndex < _audioFiles.Length - 1 ? currentIndex + 1 : 0;
            _player.Stop();
            PlayAudio(nextIndex);
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            _player.MediaEnded += RepeatMediaEnded;
        }

        private void RepeatMediaEnded(object sender, EventArgs e)
        {
            _player.Position = TimeSpan.Zero;
            _player.Play();
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            Random rng = new Random();
            _audioFiles = _audioFiles.OrderBy(x => rng.Next()).ToArray();
            PlayAudio(0);
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _player.Position = TimeSpan.FromSeconds(e.NewValue * _player.NaturalDuration.TimeSpan.TotalSeconds / 100);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _player.Volume = e.NewValue / 100;
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow.Visibility = Visibility.Visible;
        }

        private void CloseHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow.Visibility = Visibility.Collapsed;
        }
    }
}
