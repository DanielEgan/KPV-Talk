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

namespace KidProVision
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class VoiceVerify : Window
    {

        private SpeakerVerificationServiceClient _serviceClient;
        private string _subscriptionKey;
        private Guid _speakerId = Guid.Empty;
        private WaveIn _waveIn;
        private WaveFileWriter _fileWriter;
        private Stream _stream;

        public VoiceVerify()
        {
           //TALK - 15 Initialize and set service key
            InitializeComponent();
            //we need subscription and speaker ID to send to service
            //You will need to create an environment variable with your subscription key
            //or just add it here if you are not putting this in source control
            //_subscriptionKey = <my subscription key>
            _subscriptionKey = Environment.GetEnvironmentVariable("VoiceSubscriptionKey");

            //Look to see if speakerID already exists
            IsolatedStorageHelper _storageHelper = IsolatedStorageHelper.getInstance();
            string _savedSpeakerId = _storageHelper.readValue(VoiceEnroll.SPEAKER_FILENAME);
            if (_savedSpeakerId != null && _savedSpeakerId != "Empty")
            {
                _speakerId = new Guid(_savedSpeakerId);
            }

            //If it does not exist you need to create it.
            if (_speakerId == Guid.Empty)
            {
                Log("You need to create a profile and complete enrollments first before verification");
                recordBtn.IsEnabled = false;
                stopRecordBtn.IsEnabled = false;
                enrollBtn.IsEnabled = true;
            }
            else
            {
                initializeRecorder();
                _serviceClient = new SpeakerVerificationServiceClient(_subscriptionKey);
                string userPhrase = _storageHelper.readValue(VoiceEnroll.SPEAKER_PHRASE_FILENAME);
                userPhraseTxt.Text = userPhrase;
                stopRecordBtn.IsEnabled = false;
            }
        }

        private void initializeRecorder()
        {
            //TALK - 16 Initialize wav file
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
            //TALK - 17 Call verify speaker
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
            //TALK - 17 Verify Speaker
            try
            {
                statusResTxt.Text = "Verifying..";
                Stopwatch sw = Stopwatch.StartNew();
                //This compares it to the samples we aleady have on file for this user
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


        private void recordBtn_Click(object sender, RoutedEventArgs e)
        {
            recordBtn.IsEnabled = false;
            stopRecordBtn.IsEnabled = true;
            _waveIn.StartRecording();
            statusResTxt.Text = "Recording";
        }

        private void stopRecordBtn_Click(object sender, RoutedEventArgs e)
        {
            recordBtn.IsEnabled = true;
            stopRecordBtn.IsEnabled = false;
            _waveIn.StopRecording();
            statusResTxt.Text = "Stopped Recording";
        }

        public void Log(string logMessage)
        {
            if (String.IsNullOrEmpty(logMessage) || logMessage == "\n")
            {
                _logTextBox.Text += "\n";
            }
            else
            {
                string timeStr = DateTime.Now.ToString("HH:mm:ss.ffffff");
                string messaage = "[" + timeStr + "]: " + logMessage + "\n";
                _logTextBox.Text += messaage;
            }
            _logTextBox.ScrollToEnd();
        }

        public void ClearLog()
        {
            _logTextBox.Text = "";
        }

        private void enrollBtn_click(object sender, RoutedEventArgs e)
        {
            var newForm = new VoiceEnroll(); //create  new window.
            this.Visibility = Visibility.Hidden;
            newForm.Show(); //show  new form.
            this.Close(); //close the current window.
        }
    }


}