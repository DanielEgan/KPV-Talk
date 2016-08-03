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
    public partial class FaceVerify : Window 
    {

        private ObservableCollection<Face> _foundFaceCollection = new ObservableCollection<Face>();
        public string FullResult;




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


        public ObservableCollection<Face> FoundFaceCollection
        {
            get
            {
                return _foundFaceCollection;
            }
        }

        private async void Verification_Click(object sender, RoutedEventArgs e)
        {
            //Create image from Video
            string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var imagePath= System.IO.Path.Combine(root, "../../Pics");
            var pickedImagePath = imagePath + "picture.bmp";
            webCameraControl.GetCurrentImage().Save(pickedImagePath);
           

            // User already picked one image      
            var imageInfo = UIHelper.GetImageInfoForRendering(pickedImagePath);

            // Clear last time detection results
            FoundFaceCollection.Clear();

            //Write to screen
            Log(String.Format("Request: Detecting in {0}", pickedImagePath));

            // Create a filestream to read faces in images
            using (var fileStream = File.OpenRead(pickedImagePath))
            {
                try
                {
                    //subscription key
                    string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";

                    //initialize service
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);

                    //detect faces ( could be more than one ) 
                    var faces = await faceServiceClient.DetectAsync(fileStream);

                    // If it does not find any faces
                    // right now program crashes if no faces
                    if (faces == null)
                    {
                        Log("No Faces were found in picture" );
                        return;
                    }

                    //Write to screen
                    Log(String.Format("Response: Success. Detected {0} face(s) in {1}", faces.Length, pickedImagePath));

                    // calc rectange for face
                    foreach (var face in UIHelper.CalculateFaceRectangleForRendering(faces, MaxImageSize, imageInfo))
                    {
                        // Add faces to face collection
                        FoundFaceCollection.Add(face);
                    }
                }
                catch (FaceAPIException ex)
                {
                    //Write to screen
                    Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));

                    return;
                }
            }

            // This on is already stored on the service
            var faceId1 = "bc0440c9-4d5e-435e-a345-d3a7c36cafce";
            var faceId2 = FoundFaceCollection[0].FaceId;


            Log(String.Format("Request: Verifying face {0} and {1}", faceId1, faceId2));


            // Call verify passing in faceIDs
            try
            {
                //subscription API key
                string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";

                var faceServiceClient = new FaceServiceClient(subscriptionKey);
                var res = await faceServiceClient.VerifyAsync(Guid.Parse(faceId1), Guid.Parse(faceId2));

                // Verification result contains IsIdentical (true or false) and Confidence (in range 0.0 ~ 1.0),
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
            var newForm = new MainWindow(); //create  new window.
            this.Visibility = Visibility.Hidden;
            newForm.Show(); //show  new form.
            this.Close(); //close the current window.
        }
    }
}
