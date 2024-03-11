using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NReco.VideoConverter;
using NReco.VideoInfo;
using Syncfusion.Windows.Shared;
using UMediaPlayer.Commands;
using UMediaPlayer.Extensions;
using UMediaPlayer.Models;
using UMediaPlayer.Views;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;

namespace UMediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string[] s_supportedVideoExtensions = new string[] { "*.mp4", "*.avi", "*.mov", "*.mkv" };
        private PlayerState _state;
        private MenuItem _previousSelectedTrackItem;
        private MenuItem _previousSelectedDeviceItem;
        private MenuItem _previousSelectedSubItem;
        private DispatcherTimer _mouseStopTimer;
        private DispatcherTimer _fastForwardTimer;
        private DispatcherTimer _rewindTimer;
        private VlcMediaPlayerInstance _vlcInstance;
        private DirectoryInfo _vlcLibDirectory;
        public PlayerState State => _state;
        private VlcMediaPlayer MediaPlayer => VlcControl.SourceProvider.MediaPlayer;

        public MainWindow()
        {
            LicenseHelper.ValidateLicense();
            _state = new PlayerState();
            GlobalState = _state;
            this.DataContext = this;
            InitializeCommands();
            InitializeComponent();
            InitializeVlcPlayer();
            _mouseStopTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(2),
            };
            _mouseStopTimer.Tick += (_, _) => PlayerGrid.Visibility = Visibility.Hidden;
            _fastForwardTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            _fastForwardTimer.Tick += (_, _) => FastForward();
            _rewindTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            _rewindTimer.Tick += (_, _) => Rewind();
        }

        private void InitializeCommands()
        {
            Open = new RelayCommand(HandleOpen, CanOpen);
            Quit = new RelayCommand(_ => Application.Current.Shutdown(), _ => true);
            ToggleMinimalHUD = new RelayCommand(HandleToggleMinimalHud, _ => _state.IsOpened);
            ToggleFullscreen = new RelayCommand(HandleToggleFullscreen, _ => _state.IsOpened);
            JumpBackward = new RelayCommand(HandleJumpBackward, _ => _state.IsOpened);
            JumpForward = new RelayCommand(HandleJumpForward, _ => _state.IsOpened);
            VolumeUp = new RelayCommand(HandleVolumeUp, _ => _state.IsOpened);
            VolumeDown = new RelayCommand(HandleVolumeDown, _ => _state.IsOpened);
            Mute = new RelayCommand(HandleMute, _ => _state.IsOpened);
            ToggleAlwaysFit = new RelayCommand(HandleToggleAlwaysFit, _ => _state.IsOpened);
            ChangeSpeed = new RelayCommand(HandleSpeedChange, _ => _state.IsOpened);
            OpenJumpTo = new RelayCommand(HandleOpenJumpTo, _ => _state.IsOpened);
            JumpTo = new RelayCommand(HandleJumpTo, _ => _state.IsOpened);
            OpenFolderVideos = new RelayCommand(HandleOpenFolderVideos, _ => _state.IsFolderOpened);
            ChangeSelectedVideo = new RelayCommand(HandleSelectedVideoChanged, _ => _state.IsFolderOpened);
        }

        private void InitializeVlcPlayer()
        {
            Assembly currentAssembly = Assembly.GetEntryAssembly()!;
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName!;
            var libDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            _vlcLibDirectory = libDirectory;
            VlcControl.SourceProvider.CreatePlayer(_vlcLibDirectory);
            VlcControl.SourceProvider.MediaPlayer.MediaChanged += OnMediaOpened;
            VlcControl.SourceProvider.MediaPlayer.TimeChanged += OnTimeChanged;
            MediaPlayer.EndReached += MediaPlayerOnEndReached;
            _vlcInstance = (VlcMediaPlayerInstance)typeof(VlcMediaPlayer)
                .GetField("myMediaPlayerInstance", BindingFlags.NonPublic | BindingFlags.Instance)
                !.GetValue(MediaPlayer);
        }

        private void MediaPlayerOnEndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            if (!_state.IsFolderOpened)
                return;
            if (_state.SelectedVideoIndex == _state.VideoItems.Count - 1)
            {
                Dispatcher.Invoke(() =>
                {
                    PlayPauseButton.IsChecked = false;
                });
                
                return;
            }

            _state.SelectedVideoIndex++;
        }

        private void OnMediaSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VlcControl.SourceProvider.MediaPlayer.Time != (long)e.NewValue)
            {
                VlcControl.SourceProvider.MediaPlayer.Time = (long)e.NewValue;
            }
        }

        private void OnTimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            _state.Current = TimeSpan.FromMilliseconds(VlcControl.SourceProvider.MediaPlayer.Time);
        }

        private async void OnMediaOpened(object sender, VlcMediaPlayerMediaChangedEventArgs e)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            _state.Length = TimeSpan.FromMilliseconds(VlcControl.SourceProvider.MediaPlayer.Length);
            Dispatcher.Invoke(() =>
            {
                PlayPauseButton.IsChecked = true;
                MediaSlider.Maximum = _state.Length.TotalMilliseconds;
            });
            _state.AudioTracks = MediaPlayer.Audio.Tracks.All.Where(t => t.ID != -1).ToList();
            _state.OutputDevices = MediaPlayer.Audio.Outputs.All.First(am => am.Name == "mmdevice").Devices.ToList();
            _state.SubTracks = MediaPlayer.SubTitles.All.ToList();
            _state.CurrentVideo = e.NewMedia.Title;
            _state.Current = TimeSpan.Zero;
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            if (_state.IsPaused)
            {
                VlcControl.SourceProvider.MediaPlayer.Play();
            }
            else
            {
                VlcControl.SourceProvider.MediaPlayer.Pause();
            }
            _state.IsPaused = !_state.IsPaused;
        }

        #region Player Commands

        private void HandleJumpBackward(object parameter)
        {
            if (VlcControl.SourceProvider.MediaPlayer.Time < 10_000)
            {
                VlcControl.SourceProvider.MediaPlayer.Time = 0;
            }
            else
            {
                VlcControl.SourceProvider.MediaPlayer.Time -= 10_000;
            }
        }

        private void HandleJumpForward(object parameter)
        {
            if (VlcControl.SourceProvider.MediaPlayer.Time + 10_000 < VlcControl.SourceProvider.MediaPlayer.Length)
            {
                VlcControl.SourceProvider.MediaPlayer.Time += 10_000;
            }
        }

        private void HandleVolumeUp(object parameter)
        {
            MediaPlayer.Audio.Volume = MediaPlayer.Audio.Volume + 5 > 100 ? 100 : MediaPlayer.Audio.Volume + 5;
        }

        private void HandleVolumeDown(object parameter)
        {
            MediaPlayer.Audio.Volume = MediaPlayer.Audio.Volume - 5 < 0 ? 0 : MediaPlayer.Audio.Volume - 5;
        }

        private void HandleMute(object parameter)
        {
            MediaPlayer.Audio.IsMute = !MediaPlayer.Audio.IsMute;
        }

        private void FastForward()
        {
            if (MediaPlayer.Time + 5_000 <= MediaPlayer.Length)
            {
                MediaPlayer.Time += 5_000;
            }
        }

        private void Rewind()
        {
            if (MediaPlayer.Time - 5_000 >= 0)
            {
                MediaPlayer.Time -= 5_000;
            }
        }

        private void StartRewind(object sender, MouseButtonEventArgs e)
        {
            _rewindTimer.Start();
        }

        private void StopRewind(object sender, MouseButtonEventArgs e)
        {
            _rewindTimer.Stop();
        }

        private void StartFastForward(object sender, MouseButtonEventArgs e)
        {
            _fastForwardTimer.Start();
        }

        private void StopFastForward(object sender, MouseButtonEventArgs e)
        {
            _fastForwardTimer.Stop();
        }

        private void HandleToggleAlwaysFit(object parameter)
        {
            if (!_state.FitWindow)
            {
                MediaPlayer.Video.AspectRatio = $"{VlcControl.SourceProvider.VideoSource.Width}:{VlcControl.SourceProvider.VideoSource.Height}";
                _state.FitWindow = true;
            }
            else
            {
                MediaPlayer.Video.AspectRatio = "";
                _state.FitWindow = false;
            }
        }

        private void HandleSpeedChange(object parameter)
        {
            var speedRate = float.Parse((string)parameter);
            MediaPlayer.Rate = speedRate;
        }

        #endregion

        #region Interface Commands

        private async void HandleSelectedVideoChanged(object parameter)
        {
            MediaPlayer.Pause(); 
            //MediaPlayer.ResetMedia();
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            VlcControl.SourceProvider.CreatePlayer(_vlcLibDirectory);
            VlcControl.SourceProvider.MediaPlayer.MediaChanged += OnMediaOpened;
            VlcControl.SourceProvider.MediaPlayer.TimeChanged += OnTimeChanged;
            MediaPlayer.EndReached += MediaPlayerOnEndReached;
            MediaPlayer.Play(new FileInfo(_state.VideoFiles[_state.SelectedVideoIndex]));
            MediaPlayer.Time = 0;
        }

        private void HandleToggleMinimalHud(object parameter)
        {
            MenuGrid.Visibility = MenuGrid.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
            _state.IsMinimalHUD = MenuGrid.Visibility == Visibility.Hidden;
        }

        private void HandleToggleFullscreen(object parameter)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            WindowStyle = WindowStyle == WindowStyle.None ? WindowStyle.SingleBorderWindow : WindowStyle.None;
            MediaPlayer.Video.FullScreen = WindowState == WindowState.Maximized;
        }

        private void HandleOpenJumpTo(object parameter)
        {
            var window = new JumpWindow();
            window.Owner = this;
            window.DataContext = this;
            window.ShowDialog();
        }

        private void HandleJumpTo(object parameter)
        {
            MediaPlayer.Time = (long)_state.JumpTo.TotalMilliseconds;
        }

        private void HandleOpenFolderVideos(object parameter)
        {
            var window = new FolderWindow();
            window.DataContext = this;
            window.Owner = this;
            window.Show();
        }

        #endregion

        #region Open Command
        private bool CanOpen(object parameter) => true;

        private void HandleOpen(object parameter)
        {
            var openType = (OpenType)Enum.Parse(typeof(OpenType), (string)parameter);
            switch (openType)
            {
                case OpenType.File:
                    HandleOpenFile();
                    break;
                case OpenType.Directory:
                    HandleOpenDirectory();
                    break;
            }
        }

        private void HandleOpenDirectory()
        {
            var dialog = new FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                ShowNewFolderButton = true
            };

            var result = dialog.ShowDialog();
            if (result is System.Windows.Forms.DialogResult.OK)
            {
                var stopwatch = Stopwatch.StartNew();
                var files = new List<string>();
                foreach (var extension in s_supportedVideoExtensions)
                {
                    files.AddRange(Directory.GetFiles(dialog.SelectedPath, extension, SearchOption.TopDirectoryOnly));
                }

                if (!files.Any())
                {
                    MessageBox.Show("Selected directory does not contain any video file.", "No Video Files", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _state.IsFolderOpened = true;
                _state.IsOpened = true;
                _state.VideoFiles.Clear();
                _state.VideoFiles.AddRange(files);
                _state.VideoItems.Clear();
                Task.Run(() =>
                {
                    var ffMpeg = new FFMpegConverter();
                    var ffProbe = new FFProbe();
                    foreach (var file in files)
                    {
                        using var stream = new MemoryStream();
                        var thumbnailPath = Path.GetTempFileName();
                        ffMpeg.GetVideoThumbnail(file, thumbnailPath);
                        var videoInfo = ffProbe.GetMediaInfo(file);

                        var videoItem = new VideoItem()
                        {
                            Thumbnail = thumbnailPath,
                            Title = Path.GetFileName(file),
                            Duration = videoInfo.Duration.ToString(@"hh\:mm\:ss")
                        };
                        _state.VideoItems.Add(videoItem);
                    }
                });
                
                VlcControl.SourceProvider.MediaPlayer.Play(new FileInfo(_state.VideoFiles.First()));
                stopwatch.Stop();
                Debug.WriteLine("Open Directory - {0}", stopwatch.Elapsed);
            }
        }

        private void HandleOpenFile()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Video Files (*.mp4;*.avi;*.mov;.mkv)|*.mp4;*.avi;*.mov;*mkv",
                RestoreDirectory = true,
                Multiselect = false
            };

            var result = dialog.ShowDialog();
            if (result is System.Windows.Forms.DialogResult.OK)
            {
                var stopwatch = Stopwatch.StartNew();
                _state.IsFolderOpened = false;
                _state.IsOpened = true;
                _state.VideoFiles.Clear();
                _state.VideoFiles.Add(dialog.FileName);
                VlcControl.SourceProvider.MediaPlayer.Play(new FileInfo(dialog.FileName));
                stopwatch.Stop();
                Debug.WriteLine("Open File - {0}", stopwatch.Elapsed);
            }
        }

        #endregion

        #region Static Commands
        public static RelayCommand Open { get; set; }
        public static RelayCommand Quit { get; set; }
        public static RelayCommand ToggleMinimalHUD { get; set; }
        public static RelayCommand ToggleFullscreen { get; set; }
        public static RelayCommand JumpForward { get; set; }
        public static RelayCommand JumpBackward { get; set; }
        public static RelayCommand VolumeUp { get; set; }
        public static RelayCommand VolumeDown { get; set; }
        public static RelayCommand Mute { get; set; }
        public static RelayCommand ToggleAlwaysFit { get; set; }
        public static RelayCommand ChangeSpeed { get; set; }
        public static RelayCommand OpenJumpTo { get; set; }
        public static RelayCommand JumpTo { get; set; }
        public static RelayCommand OpenFolderVideos { get; set; }
        public static RelayCommand ChangeSelectedVideo { get; set; }
        public static PlayerState GlobalState { get; set; }
        #endregion

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _mouseStopTimer.Stop();
            if (_state.IsOpened)
            {
                PlayerGrid.Visibility = Visibility.Visible;
            }
            _mouseStopTimer.Start();
        }

        private void OnAudioTrackChanged(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var audioTrack = (string)menuItem.Header;
            var currentTrack = _state.AudioTracks.First(a => a.Name == audioTrack);
            _state.CurrentAudioTrack = currentTrack.Name;
            MediaPlayer.Audio.Tracks.Current = currentTrack;
            menuItem.IsChecked = true;
            if (_previousSelectedTrackItem != null)
            {
                _previousSelectedTrackItem.IsChecked = false;
            }

            _previousSelectedTrackItem = menuItem;
        }

        private void OnDeviceChanged(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var device = (string)menuItem.Header;
            var currentDevice = _state.OutputDevices.First(a => a.Description == device);
            _state.CurrentDevice = currentDevice.Description;
            var output = MediaPlayer.Audio.Outputs.All.First(o => o.Devices.Any(d => d.DeviceIdentifier == currentDevice.DeviceIdentifier));
            MediaPlayer.Manager.SetAudioOutputDevice(_vlcInstance, output.Name, currentDevice.DeviceIdentifier);
            menuItem.IsChecked = true;
            if (_previousSelectedDeviceItem != null)
            {
                _previousSelectedDeviceItem.IsChecked = false;
            }

            _previousSelectedDeviceItem = menuItem;
        }

        private void OnSubTrackChanged(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var track = (string)menuItem.Header;
            var currentTrack = _state.SubTracks.First(s => s.Name == track);
            _state.CurrentSubTrack = currentTrack.Name;
            MediaPlayer.SubTitles.Current = currentTrack;
            menuItem.IsChecked = true;
            if (_previousSelectedSubItem != null)
            {
                _previousSelectedSubItem.IsChecked = false;
            }

            _previousSelectedSubItem = menuItem;
        }

        private void OpenAboutWindow(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow();
            window.Show();
        }
    }
}