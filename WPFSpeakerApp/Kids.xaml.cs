using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFSpeakerApp
{
    /// <summary>
    /// Interaction logic for Kids.xaml
    /// </summary>
    public partial class Kids : Window
    {
        public Kids()
        {
            InitializeComponent();
            DataContext = this;
        }
        public class MyImage
        {
            private ImageSource _image;
            private string _name;

            public MyImage(ImageSource image, string name)
            {
                _image = image;
                _name = name;
            }

            public override string ToString()
            {
                return _name;
            }

            public ImageSource Image { get { return _image; } }
            public string Name { get { return _name; } }
        }

        public List<MyImage> AllImages
        {
            get
            {
                string path = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                var outPutDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var files = Directory.GetFiles(System.IO.Path.Combine(root, "../../Images"), "*.*");
                List<MyImage> result = new List<MyImage>();
                foreach (string filename in files)
                {
                    try
                    {
                        result.Add(
                            new MyImage(
                            new BitmapImage(
                            new Uri(filename)),
                            System.IO.Path.GetFileNameWithoutExtension(filename)));


                    }
                    catch { }
                }
                return result;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new FaceVerify(); //create your new form.
            this.Visibility = Visibility.Hidden;
            newForm.Show(); //show the new form.
            this.Close(); //only if you want to close the current form.

        }
    }
}
