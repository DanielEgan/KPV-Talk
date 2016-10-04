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


namespace KidProVision
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FaceVerify : Window
    {


        public PhotoList Photos;
        public string FullResult;

        string cameraPic;
        string filePic;

        //TALK - 01 need subscription key
        //subscription key
        //we need the subscription key from 

        //You will need to create an environment variable with your subscription key
        //or just add it here if you are not putting this in source control
        //string subscriptionKey = <my subscription key>
        string subscriptionKey =  Environment.GetEnvironmentVariable("FaceSubscriptionKey");


        public FaceVerify()
        {

            InitializeComponent();
            InitializeComboBox();
            btnVoice.IsEnabled = false;

        }

        //TALK - 02 observable colletion
        //using this collection to hold the faceID's once recieved
        private ObservableCollection<Face> _foundFaceCollection = new ObservableCollection<Face>();
        public ObservableCollection<Face> FoundFaceCollection
        {
            get
            {
                return _foundFaceCollection;
            }
        }

        //loads up all cameras connected to the computer
        //I hide the combobox on the form and just select the first one.
        private void InitializeComboBox()
        {
            comboBox.ItemsSource = webCameraControl.GetVideoCaptureDevices();

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedItem = comboBox.Items[0];
            }
        }


        //TALK - 03 Starting Camera 
        private void StartCamera()
        {
            var cameraId = (WebCameraId)comboBox.SelectedItem;
            webCameraControl.StartCapture(cameraId);

        }


        //windows loaded starts the camera to show.
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            StartCamera();
        }



        //TALK - 04 - Verification
        // This is used to verify and send up to cognitive services 
        private async void Verification_Click(object sender, RoutedEventArgs e)
        {
            //settingn folder for pulling up image already saved (in assests folder)
            string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var imagePath = System.IO.Path.Combine(root, "../../Assets/");

            //cameraPic is the image that will be taken by the camera
            cameraPic = imagePath + "picture.bmp";

            //file Pic is the one already on file in assets folder
            filePic = imagePath + "daniel-egan.jpg";
            webCameraControl.GetCurrentImage().Save(cameraPic);

            //calling checkForFace on the pic taken
            await CheckForFace(cameraPic);
            Log(String.Format("Request: Detecting in {0}", cameraPic));

            //calling checkForFace on the photo on file
            await CheckForFace(filePic);
            Log(String.Format("Request: Detecting in {0}", filePic));


            //TALK - 06 pull faceIDs and call verify
            //Pull faceIDs from faces put into collection after calling CheckForFace
            var faceId1 = FoundFaceCollection[0].FaceId;
            var faceId2 = FoundFaceCollection[1].FaceId;
            await CompareFaces(faceId1, faceId2);


        }


        //TALK - 05 getting face attributes
        //sending in path of file
        private async Task CheckForFace(string pickedImagePath)
        {
            // Create a filestream to read faces in images
            using (var fileStream = File.OpenRead(pickedImagePath))
            {
                try
                {
                    var imageInfo = UIHelper.GetImageInfoForRendering(pickedImagePath);

                    //initialize face service client 
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);

                    //You need to tell it what attributes you would like to pull from the image
                    var requiredFaceAttributes = new FaceAttributeType[] {
                        FaceAttributeType.Age,
                        FaceAttributeType.Gender,
                        FaceAttributeType.Smile,
                        FaceAttributeType.FacialHair,
                        FaceAttributeType.HeadPose,
                        FaceAttributeType.Glasses
                    };

                    //detect faces ( could be more than one ) 
                    //send filestream
                    //return FaceID (needed for verification)
                    //optional return face landmarks
                    //optional - attribues (noted above)
                    var faces = await faceServiceClient.DetectAsync(fileStream, returnFaceId: true, returnFaceLandmarks: true, returnFaceAttributes: requiredFaceAttributes);

                    // If it does not find any faces
                    // right now program crashes if no faces :) 
                    if (faces == null)
                    {
                        Log("No Faces were found in picture");
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
        }


        //TALK - 07 CompareFaces
        //passing in two faceIDs
        private async Task CompareFaces(string faceId1, string faceId2)
        {
            Log(String.Format("Request: Verifying face {0} and {1}", faceId1, faceId2));

            // Call verify passing in faceIDs
            try
            {
                //New up FaceServiceClient with subscriptionkey
                var faceServiceClient = new FaceServiceClient(subscriptionKey);
                var res = await faceServiceClient.VerifyAsync(Guid.Parse(faceId1), Guid.Parse(faceId2));

                //check the result.IsIndentical
                if (res.IsIdentical)
                {
                    btnVoice.IsEnabled = true;
                }
                // Verification result contains IsIdentical (true or false) and Confidence (in range 0.0 ~ 1.0),
                FullResult = string.Format("Response: Success. Face {0} and {1} {2} to the same person", faceId1, faceId2, res.IsIdentical ? "belong" : "not belong");
                Log(FullResult);
                decimal confidence = Convert.ToDecimal(res.Confidence) * 100;

                Log("Confidence " + String.Format("{0:0.00}", confidence) + "%");


            }
            catch (FaceAPIException ex)
            {
                Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));

                return;
            }
        }

        private void btnVoice_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new VoiceVerify(); //create  new window.
            this.Visibility = Visibility.Hidden;
            newForm.Show(); //show  new form.
            this.Close(); //close the current window.
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

        public int MaxImageSize
        {
            get
            {
                return 300;
            }
        }
    }


}