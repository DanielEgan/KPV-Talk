using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Utils;
using NAudio.Wave;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Verification;
using Microsoft.ProjectOxford.SpeakerRecognition;
using System.Diagnostics;

namespace WPFSpeakerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeakerVerificationServiceClient _serviceClient;
        private string _subscriptionKey;
        private Guid _speakerId = Guid.Empty;
        private WaveIn _waveIn;
        private WaveFileWriter _fileWriter;
        private Stream _stream;

        public MainWindow()
        {
            InitializeComponent();
            //we need subscription and speaker ID to send to service
            _subscriptionKey = "5e29fb937bcb42698c1d6a3d69789a0f";
            _speakerId = new Guid( "1470311b-5ead-4311-bce6-d4688959b911");

            //initialize _waveIn recorder
            initializeRecorder();

            _serviceClient = new SpeakerVerificationServiceClient(_subscriptionKey);

            //disable stop button
            stopRecordBtn.IsEnabled = false;
        }

        private void initializeRecorder()
        {
            _waveIn = new WaveIn();
            _waveIn.DeviceNumber = 0;
            int sampleRate = 16000; // 16 kHz
            int channels = 1; // mono
            _waveIn.WaveFormat = new WaveFormat(sampleRate, channels);
            _waveIn.DataAvailable += waveIn_DataAvailable;
            _waveIn.RecordingStopped += waveSource_RecordingStopped;
        }

        private void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _fileWriter.Dispose();
            _fileWriter = null;
            _stream.Seek(0, SeekOrigin.Begin);
            //Dispose recorder object
            _waveIn.Dispose();
            initializeRecorder();

            //when recording is done we send the stream to cognitive services
            verifySpeaker(_stream);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_fileWriter == null)
            {
                _stream = new IgnoreDisposeStream(new MemoryStream());
                _fileWriter = new WaveFileWriter(_stream, _waveIn.WaveFormat);
            }
            _fileWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private async void verifySpeaker(Stream audioStream)
        {
            try
            {
                statusResTxt.Text ="Verifying..";
                Stopwatch sw = Stopwatch.StartNew();
                Verification response = await _serviceClient.VerifyAsync(audioStream, _speakerId);
                sw.Stop();
                statusResTxt.Text = "Verification Done, Elapsed Time: " + sw.Elapsed;
                statusResTxt.Text = response.Result.ToString();
                confTxt.Text = response.Confidence.ToString();

                if (response.Result == Result.Accept)
                {
                    statusResTxt.Background = Brushes.Green;
                    statusResTxt.Foreground = Brushes.White;
                    tbVerified.Background = Brushes.Green;
                    tbVerified.Text = "Verified";
                }
                else
                {
                    statusResTxt.Background = Brushes.Red;
                    statusResTxt.Foreground = Brushes.White;
                    tbVerified.Visibility = Visibility.Visible;
                    tbVerified.Background = Brushes.Red;
                    tbVerified.Text = "Not Verified";
                }
            }
            catch (VerificationException exception)
            {
                statusResTxt.Text = "Cannot verify speaker: " + exception.Message;
            }
            catch (Exception e)
            {
                statusResTxt.Text = "Error: " + e;
            }
        }

        private void recordBtn_Click_1(object sender, RoutedEventArgs e)
        {
            recordBtn.IsEnabled = false;
            stopRecordBtn.IsEnabled = true;
            _waveIn.StartRecording();
            statusResTxt.Text = "Recording";
        }

        private void stopRecordBtn_Click_1(object sender, RoutedEventArgs e)
        {
            recordBtn.IsEnabled = true;
            stopRecordBtn.IsEnabled = false;
            _waveIn.StopRecording();
            statusResTxt.Text = "Stopped Recording";
        }
    }
}
