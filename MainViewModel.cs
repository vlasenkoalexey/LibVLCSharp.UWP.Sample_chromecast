using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibVLCSharp.UWP.Sample
{
    /// <summary>
    /// Main view model
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        readonly HashSet<RendererItem> _rendererItems = new HashSet<RendererItem>();

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initialized a new instance of <see cref="MainViewModel"/> class
        /// </summary>
        public MainViewModel()
        {
            InitializedCommand = new RelayCommand<InitializedEventArgs>(Initialize);
            DiscoverCommand = new RelayCommand<EventArgs>(DiscoverChromecasts);
            CastCommand = new RelayCommand<EventArgs>(StartCasting);
            Core.Initialize();
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MainViewModel()
        {
            Dispose();
        }

        /// <summary>
        /// Gets the command for the initialization
        /// </summary>
        public ICommand InitializedCommand { get; }

        public ICommand DiscoverCommand { get; }

        public ICommand CastCommand { get; }

        private LibVLC LibVLC { get; set; }

        private MediaPlayer _mediaPlayer;
        /// <summary>
        /// Gets the media player
        /// </summary>
        public MediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            private set => Set(nameof(MediaPlayer), ref _mediaPlayer, value);
        }

        private void Set<T>(string propertyName, ref T field, T value)
        {
            if (field == null && value != null || field != null && !field.Equals(value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private async void Initialize(InitializedEventArgs eventArgs)
        {
            List<string> options = new List<string>(eventArgs.SwapChainOptions);
            options.Add("--verbose=3");
            LibVLC = new LibVLC(options.ToArray());
            LibVLC.Log += LibVLC_Log;

            MediaPlayer = new MediaPlayer(LibVLC);

            //MediaPlayer.Play(new Media(LibVLC, "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
            //    FromType.FromLocation));
        }

        private void LibVLC_Log(object sender, LogEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        void DiscoverChromecasts(EventArgs args)
        {
            RendererDescription renderer;
            LibVLC.RendererList.ToList().ForEach(item =>
            {
                Debug.WriteLine(item.LongName);
            });

            renderer = LibVLC.RendererList.First();

            // create a renderer discoverer
            RendererDiscoverer _rendererDiscoverer = new RendererDiscoverer(LibVLC, renderer.Name);

            // register callback when a new renderer is found
            _rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;

            // start discovery on the local network
            bool result =  _rendererDiscoverer.Start();

        }

        private void StartCasting(EventArgs eventArgs)
        {
            // create new media
            //LibVLCSharp.Shared.Media
            var media = new Media(LibVLC,
                "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
                FromType.FromLocation);

            // abort casting if no renderer items were found
            if (!_rendererItems.Any())
            {
                Debug.WriteLine("No renderer items found. Abort casting...");
            }
            else
            {
                // set the previously discovered renderer item (chromecast) on the mediaplayer
                // if you set it to null, it will start to render normally (i.e. locally) again
                var r = _rendererItems.FirstOrDefault(item => item.Name.Contains("Play room"));
                MediaPlayer.SetRenderer(r);
            }

            MediaPlayer.Stop();

            MediaPlayer.Media = media;

            // start the playback
            var result = MediaPlayer.Play();
        }


        /// <summary>
        /// Raised when a renderer has been discovered or has been removed
        /// </summary>
        void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            Debug.WriteLine($"New item discovered: {e.RendererItem.Name} of type {e.RendererItem.Type}");
            if (e.RendererItem.CanRenderVideo)
                Debug.WriteLine("Can render video");
            if (e.RendererItem.CanRenderAudio)
                Debug.WriteLine("Can render audio");

            // add newly found renderer item to local collection
            _rendererItems.Add(e.RendererItem);
        }


        /// <summary>
        /// Cleaning
        /// </summary>
        public void Dispose()
        {
            var mediaPlayer = MediaPlayer;
            MediaPlayer = null;
            mediaPlayer?.Dispose();
            LibVLC?.Dispose();
            LibVLC = null;
        }
    }
}
