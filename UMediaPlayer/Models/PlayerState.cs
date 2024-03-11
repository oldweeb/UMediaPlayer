using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;

namespace UMediaPlayer.Models
{
    public class PlayerState : INotifyPropertyChanged
    {
        private bool _isOpened = false;
        private bool _isFolderOpened = false;
        private bool _isMinimalHUD = false;
        private bool _isPaused = false;
        private bool _fitWindow = false;
        private TimeSpan _length = TimeSpan.Zero;
        private TimeSpan _current = TimeSpan.Zero;
        private TimeSpan _jumpTo = TimeSpan.Zero;
        private List<AudioOutputDevice> _outputDevices;
        private List<TrackDescription> _audioTracks;
        private List<TrackDescription> _subTracks;
        private string _currentDevice;
        private string _currentAudioTrack;
        private string _currentSubTrack;
        private string _currentVideo;
        private string _windowTitle = "UMedia Player";
        private bool _autoPlayNext = true;
        private int _selectedVideoIndex = 0;
        public List<string> VideoFiles { get; set; } = new List<string>();
        private ObservableCollection<VideoItem> _videoItems = new ObservableCollection<VideoItem>();

        public bool HasSubTracks => _subTracks?.Any() == true;

        public int SelectedVideoIndex
        {
            get => _selectedVideoIndex;
            set
            {
                _selectedVideoIndex = value;
                MainWindow.ChangeSelectedVideo.Execute(null);
                OnPropertyChanged(nameof(SelectedVideoIndex));
            }
        }

        public ObservableCollection<VideoItem> VideoItems
        {
            get => _videoItems;
            set
            {
                _videoItems = value;
                OnPropertyChanged(nameof(VideoItems));
            }
        }

        public bool AutoPlayNext
        {
            get => _autoPlayNext;
            set
            {
                _autoPlayNext = value;
                OnPropertyChanged(nameof(AutoPlayNext));
            }
        }

        public string CurrentVideo
        {
            get => _currentVideo;
            set
            {
                _currentVideo = value;
                OnPropertyChanged(nameof(CurrentVideo));
                WindowTitle = CurrentVideo + " - UMedia Player";
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public TimeSpan JumpTo
        {
            get => _jumpTo;
            set
            {
                _jumpTo = value;
                OnPropertyChanged(nameof(JumpTo));
            }
        }

        public string CurrentDevice
        {
            get => _currentDevice;
            set
            {
                _currentDevice = value;
                OnPropertyChanged(nameof(CurrentDevice));
            }
        }

        public string CurrentAudioTrack
        {
            get => _currentAudioTrack;
            set
            {
                _currentAudioTrack = value;
                OnPropertyChanged(nameof(CurrentAudioTrack));
            }
        }

        public string CurrentSubTrack
        {
            get => _currentSubTrack;
            set
            {
                _currentSubTrack = value;
                OnPropertyChanged(nameof(CurrentSubTrack));
            }
        }

        public List<AudioOutputDevice> OutputDevices
        {
            get => _outputDevices;
            set
            {
                _outputDevices = value;
                OnPropertyChanged(nameof(OutputDevices));
            }
        }

        public List<TrackDescription> AudioTracks
        {
            get => _audioTracks;
            set
            {
                _audioTracks = value;
                OnPropertyChanged(nameof(AudioTracks));
            }
        }

        public List<TrackDescription> SubTracks
        {
            get => _subTracks;
            set
            {
                _subTracks = value;
                OnPropertyChanged(nameof(SubTracks));
                OnPropertyChanged(nameof(HasSubTracks));
            }
        }

        public bool IsOpened
        {
            get => _isOpened;
            set
            {
                _isOpened = value;
                OnPropertyChanged(nameof(IsOpened));
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged(nameof(IsPaused));
            }
        }

        public bool IsFolderOpened
        {
            get => _isFolderOpened;
            set
            {
                _isFolderOpened = value;
                OnPropertyChanged(nameof(IsFolderOpened));
            }
        }

        public bool IsMinimalHUD
        {
            get => _isMinimalHUD;
            set
            {
                _isMinimalHUD = value;
                OnPropertyChanged(nameof(IsMinimalHUD));
            }
        }

        public bool FitWindow
        {
            get => _fitWindow;
            set
            {
                _fitWindow = value;
                OnPropertyChanged(nameof(FitWindow));
            }
        }

        public TimeSpan Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged(nameof(Length));
            }
        }

        public TimeSpan Current
        {
            get => _current;
            set
            {
                if (_current.Hours == value.Hours && _current.Minutes == value.Minutes && _current.Seconds == value.Seconds)
                {
                    _current = value;
                }
                else
                {
                    _current = value;
                    OnPropertyChanged(nameof(Current));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
