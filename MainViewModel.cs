using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

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
            CastCommand = new RelayCommand<FrameworkElement>(StartCasting);
            OpenFileCommand = new RelayCommand<EventArgs>(OpenFile);
            OpenFileStreamCommand = new RelayCommand<EventArgs>(OpenFileStream);
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

        public ICommand OpenFileCommand { get; }

        public ICommand OpenFileStreamCommand { get; }

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

        public string KeyStoreFilename { get; set; } = "VLC_MediaElement_KeyStore";

        private MenuFlyout RendererFlyout
        {
            get
            {
                var flyout = new MenuFlyout();

                if (_rendererItems.Count == 0)
                {
                    flyout.Items.Add(new MenuFlyoutItem
                    {
                        IsEnabled = false,
                        Text = ("NoCastFound")
                    });
                }
                else
                {
                    foreach (var ri in _rendererItems)
                    {
                        var menuFlyoutItem = new MenuFlyoutItem
                        {
                            Text = ri.Name
                        };

                        menuFlyoutItem.Click += (object sender, RoutedEventArgs e) =>
                        {
                            var item = sender as MenuFlyoutItem;
                            MediaPlayer.Stop();
                            MediaPlayer.SetRenderer(_rendererItems.Where(rendererItem => rendererItem.Name == item.Text).First());
                            MediaPlayer.Play();
                        };

                        flyout.Items.Add(menuFlyoutItem);
                    }

                    var disconnectFlyoutItem = new MenuFlyoutItem
                    {
                        Text = ("DisconnectCast")
                    };

                    disconnectFlyoutItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        MediaPlayer.SetRenderer(null);
                    };

                    flyout.Items.Add(disconnectFlyoutItem);
                }

                return flyout;
            }
        }



        private async void Initialize(InitializedEventArgs eventArgs)
        {
            List<string> options = new List<string>(eventArgs.SwapChainOptions);
            options.Add("--verbose=3");
            options.Add("--aout=winstore");
            options.Add("--avcodec-fast");
            options.Add("--subsdec-encoding");


            options.Add($"--keystore-file={Path.Combine(ApplicationData.Current.LocalFolder.Path, KeyStoreFilename)}");
            LibVLC = new LibVLC(options.ToArray());
            LibVLC.Log += LibVLC_Log;

            MediaPlayer = new MediaPlayer(LibVLC);

            //MediaPlayer.Play(new Media(LibVLC, "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
            //    FromType.FromLocation));

            // create new media
            //LibVLCSharp.Shared.Media
            var media = new Media(LibVLC,
                //@"C:\Temp\lesson5.mp4",
                "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
                FromType.FromLocation);

            MediaPlayer.Media = media;

            MediaPlayer.Play();
        }

        private void LibVLC_Log(object sender, LogEventArgs e)
        {
            Debug.WriteLine($"LibVLC -> {e.Module}: {e.Message}");
            //Debug.WriteLine(e.Message);
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

        private const string FILE_TOKEN = "{1BBC4B94-BE33-4D79-A0CB-E5C6CDB9D107}";

        private async void OpenFile(EventArgs obj)
        {
            var fileOpenPicker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };
            fileOpenPicker.FileTypeFilter.Add("*");
            var file = await fileOpenPicker.PickSingleFileAsync();

            
            if (file != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(FILE_TOKEN, file);
                var media = new Media(LibVLC, $"winrt://{FILE_TOKEN}", FromType.FromLocation);

                MediaPlayer.Stop();
                MediaPlayer.Media = media;
                MediaPlayer.Play();
            }
            

        }

        private async void OpenFileStream(EventArgs obj)
        {
            var fileOpenPicker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };
            fileOpenPicker.FileTypeFilter.Add("*");
            var file = await fileOpenPicker.PickSingleFileAsync();


            if (file != null)
            {
                var stream = await file.OpenReadAsync();
                var streamForRead = stream.AsStreamForRead();
                Debug.WriteLine($"streamForRead.CanSeek: {streamForRead.CanSeek}");
                var media = new Media(LibVLC, new StreamMediaInput(streamForRead));


                MediaPlayer.Stop();
                MediaPlayer.Media = media;
                MediaPlayer.Play();
                //MediaPlayer = new MediaPlayer(media);
                //MediaPlayer.Play();
            }
        }


        private void StartCasting(FrameworkElement sender)
        {
            // abort casting if no renderer items were found
            //if (!_rendererItems.Any())
            //{
            //    Debug.WriteLine("No renderer items found. Abort casting...");
            //}
            //else
            //{
            //    MediaPlayer.Stop();
            //    // set the previously discovered renderer item (chromecast) on the mediaplayer
            //    // if you set it to null, it will start to render normally (i.e. locally) again
            //    var r = _rendererItems.FirstOrDefault(item => item.Name.Contains("Office"));
            //    MediaPlayer.SetRenderer(r);

            //    MediaPlayer.Play();
            //}
            RendererFlyout.ShowAt(sender);
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
