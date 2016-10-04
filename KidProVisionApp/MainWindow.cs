// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace KidProVision
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Stack _undoStack;
        public PhotoList Photos;
        public PrintList PickUpList;

        public MainWindow()
        {
            _undoStack = new Stack();
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentPhoto);

            Photos = (PhotoList)(Application.Current.Resources["Photos"] as ObjectDataProvider)?.Data;
            Photos.Path = "..\\..\\Photos";
            PickUpList = (PrintList)(Application.Current.Resources["PickUpList"] as ObjectDataProvider)?.Data;
        }

        private void PhotoListSelection(object sender, RoutedEventArgs e)
        {
            var path = ((sender as ListBox)?.SelectedItem.ToString());
            BitmapSource img = BitmapFrame.Create(new Uri(path));
            CurrentPhoto.Source = img;

         }
      

        private void AddChildToPickUp(object sender, RoutedEventArgs e)
        {
            PrintBase item;
            item = new Print(CurrentPhoto.Source as BitmapSource);
            
                PickUpList.Add(item);
                PickUpListBox.ScrollIntoView(item);
                PickUpListBox.SelectedItem = item;
                if (false == CheckOutChildButton.IsEnabled)
                CheckOutChildButton.IsEnabled = true;
                if (false == RemoveButton.IsEnabled)
                    RemoveButton.IsEnabled = true;
            }

        private void RemoveShoppingCartItem(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveChild(object sender, RoutedEventArgs e)
        {
            if (null != PickUpListBox.SelectedItem)
            {
                var item = PickUpListBox.SelectedItem as PrintBase;
                PickUpList.Remove(item);
                PickUpListBox.SelectedIndex = PickUpList.Count - 1;
            }
            if (0 == PickUpList.Count)
            {
                RemoveButton.IsEnabled = false;
                CheckOutChildButton.IsEnabled = false;
            }
        }

        private void CheckOutChild(object sender, RoutedEventArgs e)
        {
            if (PickUpList.Count > 0)
            {
                var newForm = new FaceVerify(); //create  new window.
                this.Visibility = Visibility.Hidden;
                newForm.Show(); //show  new form.
                this.Close(); //close the current window.

            }
        }


    }


}