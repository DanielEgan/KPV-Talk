using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Verification;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KidProVision
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class VoiceEnroll : Window
    {
        private string _subscriptionKey;
        private Guid _speakerId = Guid.Empty;
        private int _remainingEnrollments;
        private WaveIn _waveIn;
        private WaveFileWriter _fileWriter;
        private Stream _stream;
        private SpeakerVerificationServiceClient _serviceClient;

        public static readonly string SPEAKER_FILENAME = "SpeakerId";
        public static readonly string SPEAKER_PHRASE_FILENAME = "SpeakerPhrase";
        public static readonly string SPEAKER_ENROLLMENTS = "Enrollments";

        /// <summary>
        /// Creates a new EnrollPage 
        /// </summary>
        /// <param name="subscriptionKey">The subscription key</param>
        public VoiceEnroll()
        {
            InitializeComponent();
            _subscriptionKey = "5e29fb937bcb42698c1d6a3d69789a0f";
            _serviceClient = new SpeakerVerificationServiceClient(_subscriptionKey);
            initializeRecorder();
            initializeSpeaker();
        }

        /// <summary>
        /// Initialize the speaker information
        /// </summary>
        private async void initializeSpeaker()
        {
            IsolatedStorageHelper _storageHelper = IsolatedStorageHelper.getInstance();
            string _savedSpeakerId = _storageHelper.readValue(SPEAKER_FILENAME);
            if (_savedSpeakerId != null && _savedSpeakerId != "Empty")
            {
                _speakerId = new Guid(_savedSpeakerId);
                
            }
            record.IsEnabled = false;
            if (_speakerId == Guid.Empty)
            {
                bool created = await createProfile();
                if (created)
                {
                    refreshPhrases();
                   
                }
            }
            else
            {
                setStatus("Using profile Id: " + _speakerId.ToString());
                refreshPhrases();
                string enrollmentsStatus = _storageHelper.readValue(SPEAKER_ENROLLMENTS);
                if ((enrollmentsStatus != null) && (enrollmentsStatus.Equals("Done")))
                {
                    resetBtn.IsEnabled = true;
                }
            }

        }

        /// <summary>
        /// Initialize NAudio recorder instance
        /// </summary>
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

        /// <summary>
        /// A listener called when the recording stops
        /// </summary>
        /// <param name="sender">Sender object responsible for event</param>
        /// <param name="e">A set of arguments sent to the listener</param>
        private void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _fileWriter.Dispose();
            _fileWriter = null;
            _stream.Seek(0, SeekOrigin.Begin);
            //Dispose recorder object
            _waveIn.Dispose();
            initializeRecorder();
            enrollSpeaker(_stream);
        }

        /// <summary>
        /// A method that's called whenever there's a chunk of audio is recorded
        /// </summary>
        /// <param name="sender">The sender object responsible for the event</param>
        /// <param name="e">The arguments of the event object</param>
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

        /// <summary>
        /// Enrolls the audio of the speaker
        /// </summary>
        /// <param name="audioStream">The audio stream</param>
        private async void enrollSpeaker(Stream audioStream)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                Enrollment response = await _serviceClient.EnrollAsync(audioStream, _speakerId);
                sw.Stop();
                _remainingEnrollments = response.RemainingEnrollments;
                setStatus("Enrollment Done, Elapsed Time: " + sw.Elapsed);
                verPhraseText.Text = response.Phrase;
                setStatus("Your phrase: " + response.Phrase);
                setUserPhrase(response.Phrase);
                remEnrollText.Text = response.RemainingEnrollments.ToString();
                if (response.RemainingEnrollments == 0)
                {
                    MessageBox.Show("You have now completed the minimum number of enrollments. You may perform verification or add more enrollments", "Speaker enrolled");
                    btnVerifyPage.IsEnabled = true;
                }
                resetBtn.IsEnabled = true;
                IsolatedStorageHelper _storageHelper = IsolatedStorageHelper.getInstance();
                _storageHelper.writeValue(SPEAKER_ENROLLMENTS, "Done");
            }
            catch (EnrollmentException exception)
            {
                setStatus("Cannot enroll speaker: " + exception.Message);
            }
            catch (Exception gexp)
            {
                setStatus("Error: " + gexp.Message);
            }
        }

        /// <summary>
        /// Helper method to set the status bar message
        /// </summary>
        /// <param name="status">Status bar message</param>
        private void setStatus(string status)
        {
            Log(status);
        }

        /// <summary>
        /// Click handler for the record button
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="e">Event arguments object</param>
        private void record_Click(object sender, RoutedEventArgs e)
        {
            record.IsEnabled = false;
            stopRecord.IsEnabled = true;
            _waveIn.StartRecording();
            setStatus("Recording...");
        }

        /// <summary>
        /// Click handler for the record button
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="e">Event arguments object</param>
        private void stopRecord_Click(object sender, RoutedEventArgs e)
        {
            record.IsEnabled = true;
            stopRecord.IsEnabled = false;
            _waveIn.StopRecording();
            setStatus("Enrolling...");
        }

        /// <summary>
        /// Persists the verification phrase of the speaker
        /// </summary>
        /// <param name="phrase">The user phrase</param>
        private void setUserPhrase(string phrase)
        {
            IsolatedStorageHelper _storageHelper = IsolatedStorageHelper.getInstance();
            _storageHelper.writeValue(SPEAKER_PHRASE_FILENAME, phrase);
        }

        /// <summary>
        /// Creates a speaker profile
        /// </summary>
        /// <returns>A boolean indicating the success/failure of the process</returns>
        private async Task<bool> createProfile()
        {
            setStatus("Creating Profile...");
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                CreateProfileResponse response = await _serviceClient.CreateProfileAsync("en-us");
                sw.Stop();
                setStatus("Profile Created, Elapsed Time: " + sw.Elapsed);
                IsolatedStorageHelper _storageHelper = IsolatedStorageHelper.getInstance();
                _storageHelper.writeValue(SPEAKER_FILENAME, response.ProfileId.ToString());
                _speakerId = response.ProfileId;
                return true;
            }
            catch (CreateProfileException exception)
            {
                setStatus("Cannot create profile: " + exception.Message);
                return false;
            }
            catch (Exception gexp)
            {
                setStatus("Error: " + gexp.Message);
                return false;
            }
        }

        /// <summary>
        /// Refresh the list of phrases
        /// </summary>
        private async void refreshPhrases()
        {
            setStatus("Retrieving available phrases...");
            record.IsEnabled = false;
            try
            {
                VerificationPhrase[] phrases = await _serviceClient.GetPhrasesAsync("en-us");
                foreach (VerificationPhrase phrase in phrases)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Content = phrase.Phrase;
                    phrasesList.Items.Add(item);
                }
                setStatus("Retrieving available phrases done");
            }
            catch (PhrasesException exp)
            {
                setStatus("Cannot retrieve phrases: " + exp.Message);
            }
            catch (Exception e)
            {
                setStatus("Error: " + e.Message);
            }
            record.IsEnabled = true;
        }

        /// <summary>
        /// Remove current speaker Id
        /// </summary>
        /// <param name="sender">Sender object responsible for event</param>
        /// <param name="e">A set of arguments sent to the listener</param>
        private async void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                setStatus("Resetting profile: " + _speakerId);
                await _serviceClient.ResetEnrollmentsAsync(_speakerId);
                setStatus("Profile reset");
                IsolatedStorageHelper _storageHelper = IsolatedStorageHelper.getInstance();
                _storageHelper.writeValue(SPEAKER_ENROLLMENTS, "Empty");
                _storageHelper.writeValue(SPEAKER_FILENAME, "Empty");
                _storageHelper.writeValue(SPEAKER_PHRASE_FILENAME, "Empty");
                resetBtn.IsEnabled = false;
                remEnrollText.Text = "";
                verPhraseText.Text = "";
            }
            catch (ResetEnrollmentsException exp)
            {
                setStatus("Cannot reset Profile: " + exp.Message);
            }
            catch (Exception gexp)
            {
                setStatus("Error: " + gexp.Message);
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

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

        private void return_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new VoiceVerify(); //create  new window.
            this.Visibility = Visibility.Hidden;
            newForm.Show(); //show  new form.
            this.Close(); //close the current window.
        }
    }


}