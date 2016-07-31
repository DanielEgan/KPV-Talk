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
using System.Windows.Shapes;
using WebEye.Controls.Wpf;
using WebEye.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Controls;
using System.ComponentModel;

namespace WPFSpeakerApp
{
    /// <summary>
    /// Interaction logic for Face.xaml
    /// </summary>
    public partial class FaceVerify : Window , INotifyPropertyChanged 
    {

        private ObservableCollection<Face> _rightResultCollection = new ObservableCollection<Face>();
        private string _verifyResult;
        public string FullResult;

        public event PropertyChangedEventHandler PropertyChanged;

        public string VerifyResult
        {
            get
            {
                return _verifyResult;
            }

            set
            {
                _verifyResult = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("VerifyResult"));
                }
            }
        }

        public FaceVerify()
        {
            InitializeComponent();
            InitializeComboBox();
            btnVoice.IsEnabled = false;
            Loaded += Face_Loaded;
           
        }


        private void Face_Loaded(object sender, RoutedEventArgs e)
        {
            StartCamera();
            //LeftImageDisplay.Source = new BitmapImage(new Uri("WPFSpeakerApp;component/Assets/daniel-egan.jpg"));
        }

        private void InitializeComboBox()
        {
            comboBox.ItemsSource = webCameraControl.GetVideoCaptureDevices();

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedItem = comboBox.Items[0];
                
            }
        }
        private void StartCamera()
        {
            var cameraId = (WebCameraId)comboBox.SelectedItem;
            webCameraControl.StartCapture(cameraId);

        }

        private void LeftImagePicker_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Gets face detection results for image on the right
        /// </summary>
        public ObservableCollection<Face> RightResultCollection
        {
            get
            {
                return _rightResultCollection;
            }
        }

        private async void Verification_Click(object sender, RoutedEventArgs e)
        {

            VerifyResult = string.Empty;
            //Create image from Video
            webCameraControl.GetCurrentImage().Save("C:\\Users\\danie\\Documents\\Visual Studio 2015\\Projects\\WPFSpeakerApp\\WPFSpeakerApp\\Pics\\picture.bmp");

            // User already picked one image
            var pickedImagePath = "C:\\Users\\danie\\Documents\\Visual Studio 2015\\Projects\\WPFSpeakerApp\\WPFSpeakerApp\\Pics\\picture.bmp";
            var imageInfo = UIHelper.GetImageInfoForRendering(pickedImagePath);
            //RightImageDisplay.Source = new BitmapImage(new Uri(pickedImagePath));

            // Clear last time detection results
            RightResultCollection.Clear();

            //show this somewhere
            //MainWindow.Log("Request: Detecting in {0}", pickedImagePath);
            var sw = Stopwatch.StartNew();

            // Call detection REST API, detect faces inside the image
            using (var fileStream = File.OpenRead(pickedImagePath))
            {
                try
                {

                    string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";

                    var faceServiceClient = new FaceServiceClient(subscriptionKey);

                    var faces = await faceServiceClient.DetectAsync(fileStream);

                    // Handle REST API calling error
                    if (faces == null)
                    {
                        return;
                    }

                    Log(String.Format("Response: Success. Detected {0} face(s) in {1}", faces.Length, pickedImagePath));

                    // Convert detection results into UI binding object for rendering
                    foreach (var face in UIHelper.CalculateFaceRectangleForRendering(faces, MaxImageSize, imageInfo))
                    {
                        // Detected faces are hosted in result container, will be used in the verification later
                        RightResultCollection.Add(face);
                    }
                }
                catch (FaceAPIException ex)
                {
                    Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));

                    return;
                }
            }


            var faceId1 = "5d77c990-f771-4663-ace0-9f855a79a933";
            var faceId2 = RightResultCollection[0].FaceId;

            Log(String.Format("Request: Verifying face {0} and {1}", faceId1, faceId2));

            // Call verify REST API with two face id
            try
            {
                MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
                string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";

                var faceServiceClient = new FaceServiceClient(subscriptionKey);
                var res = await faceServiceClient.VerifyAsync(Guid.Parse(faceId1), Guid.Parse(faceId2));

                // Verification result contains IsIdentical (true or false) and Confidence (in range 0.0 ~ 1.0),
                // here we update verify result on UI by VerifyResult binding
                VerifyResult = string.Format("{0} ({1:0.0})", res.IsIdentical ? "Equals" : "Does not equal", res.Confidence);
                FullResult = string.Format("Response: Success. Face {0} and {1} {2} to the same person", faceId1, faceId2, res.IsIdentical ? "belong" : "not belong");
                Log(FullResult);
                decimal confidence = Convert.ToDecimal(res.Confidence) * 100;
                
                Log("Confidence " + String.Format("{0:0.00}", confidence) + "%");
                btnVoice.IsEnabled = true;
               
            }
            catch (FaceAPIException ex)
            {
                Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));

                return;
            }

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

        private void RightImagePicker_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public int MaxImageSize
        {
            get
            {
                return 300;
            }
        }

        private void btnVoice_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new MainWindow(); //create your new form.
            this.Visibility = Visibility.Hidden;
            newForm.Show(); //show the new form.
            this.Close(); //only if you want to close the current form.
        }
    }
}
