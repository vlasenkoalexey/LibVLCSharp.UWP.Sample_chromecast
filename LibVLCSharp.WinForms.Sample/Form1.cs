using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace LibVLCSharp.WinForms.Sample
{
    public partial class Form1 : Form
    {
        public LibVLC _libVLC;
        public MediaPlayer _mp;
        readonly HashSet<RendererItem> _rendererItems = new HashSet<RendererItem>();

        public Form1()
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            _libVLC = new LibVLC();
            _mp = new MediaPlayer(_libVLC);
            _libVLC.Log += LibVLC_Log;
            videoView1.MediaPlayer = _mp;
            Load += Form1_Load;
            _libVLC.SetDialogHandlers(OnDisplayError, OnDisplayLogin, OnDisplayQuestion, OnDisplayProgress, OnUpdateProgress);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RendererDescription renderer;
            renderer = _libVLC.RendererList.First();

            // create a renderer discoverer
            RendererDiscoverer _rendererDiscoverer = new RendererDiscoverer(_libVLC, renderer.Name);

            // register callback when a new renderer is found
            _rendererDiscoverer.ItemAdded += _rendererDiscoverer_ItemAdded; ;

            // start discovery on the local network
            bool result = _rendererDiscoverer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Invoke((Action)(() =>
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    //openFileDialog.RestoreDirectory = true;
                    openFileDialog.ShowHelp = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Get the path of specified file
                        _mp.Play(new Media(_libVLC, new Uri(openFileDialog.FileName)));
                    }
                }
            }));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RendererDescription renderer;
            renderer = _libVLC.RendererList.First();

            // create a renderer discoverer
            RendererDiscoverer _rendererDiscoverer = new RendererDiscoverer(_libVLC, renderer.Name);

            // register callback when a new renderer is found
            _rendererDiscoverer.ItemAdded += _rendererDiscoverer_ItemAdded; ;

            // start discovery on the local network
            bool result = _rendererDiscoverer.Start();

        }

        private void _rendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            Debug.WriteLine($"New item discovered: {e.RendererItem.Name} of type {e.RendererItem.Type}");
            if (e.RendererItem.CanRenderVideo)
                Debug.WriteLine("Can render video");
            if (e.RendererItem.CanRenderAudio)
                Debug.WriteLine("Can render audio");

            this._rendererItems.Add(e.RendererItem);

            Invoke((Action)(() =>
            {
                this.comboBox1.Items.Add(e.RendererItem.Name);
            }));
        }

        private void LibVLC_Log(object sender, LogEventArgs e)
        {
            Debug.WriteLine($"LibVLC -> {e.Module}: {e.Message}");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            videoView1.MediaPlayer.Stop();
            videoView1.MediaPlayer.SetRenderer(_rendererItems.Where(rendererItem => rendererItem.Name == comboBox1.SelectedItem.ToString()).First());
            videoView1.MediaPlayer.Play();
        }

        private async Task OnDisplayError(string title, string text)
        {
            Invoke((Action)(() =>
            {
                var result = MessageBox.Show(title, text,
                                             MessageBoxButtons.OK,
                                             MessageBoxIcon.Error);
            }));
        }

        private async Task OnDisplayLogin(Dialog dialog, string title, string text, string defaultUsername, bool askStore, CancellationToken token)
        {
            Debugger.Launch();
        }

        private async Task OnDisplayQuestion(Dialog dialog, string title, string text, DialogQuestionType type, string cancelText, string firstActionText, string secondActionText, CancellationToken token)
        {
            Invoke((Action)(() =>
            {
                var result = MessageBox.Show(text, title,
                                             MessageBoxButtons.YesNoCancel,
                                             MessageBoxIcon.Error);
                if (result == DialogResult.Yes)
                {
                    dialog.PostAction(1);
                } 
                else if (result == DialogResult.No)
                {
                    dialog.PostAction(2);
                }
                else if (result == DialogResult.Cancel)
                {
                    dialog.Dismiss();
                }
            }));
        }

        private async Task OnDisplayProgress(Dialog dialog, string title, string text, bool indeterminate, float position, string cancelText, CancellationToken token)
        {
            Debugger.Launch();
        }

        private async Task OnUpdateProgress(Dialog dialog, float position, string text)
        {
            Debugger.Launch();
        }
    }
}
