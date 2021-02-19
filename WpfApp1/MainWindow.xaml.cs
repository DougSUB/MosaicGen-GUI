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
using Microsoft.Win32;
using System.IO;
using MosaicGen.Classes;
using System.Drawing;
using System.ComponentModel;

namespace MosaicGen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        string baseImage = "";
        string mosaicDirectory = "";
        public BackgroundWorker bw = new BackgroundWorker();
        float opacity;
        private bool dragStarted = false;

        
        private void browse_Click(object sender, RoutedEventArgs e)
		{
            // Create OpenFileDialog
            OpenFileDialog openFileDlg = new OpenFileDialog();

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                directoryTextBox.Text = openFileDlg.FileName;
                mosaicDirectory = openFileDlg.FileName.Substring(0, openFileDlg.FileName.Length - openFileDlg.SafeFileName.Length);
                baseImage = openFileDlg.SafeFileName;
            }
        }

		private void runButton_Click(object sender, RoutedEventArgs e)
		{
			opacity = (float)opacitySlider.Value;
			bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
			bw.RunWorkerAsync();
		}
        
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
			{
				while (!bw.CancellationPending)
				{
                    MosaicEngine engine = new MosaicEngine(mosaicDirectory, baseImage);
                    engine.CreateMosaicWithBaseImageOverlay(opacity, bw);
                }

            }
			catch (Exception)
			{
                MessageBox.Show("Please enter a valid path.");
                bw.CancelAsync();
			}
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
            dictionaryProgress.Value = e.ProgressPercentage;
            progressPercent.Text = $"{ e.UserState}\n{e.ProgressPercentage}%";
		}
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dictionaryProgress.Value = 0;
            progressPercent.Text = "Complete";

			mosaic.Source = ImageLoader("mosaic.png");
			finalMosaic.Source = ImageLoader("final mosaic.png");
			baseIMG.Source = ImageLoader("baseimage.png");

			opacitySlider.IsEnabled = true;
			opacitySlider.Visibility = Visibility.Visible;
			transparentText.Visibility = Visibility.Visible;
			opaqueText.Visibility = Visibility.Visible;
			opacityValue.Visibility = Visibility.Visible;
			opacitySlider.Value = 0.50f;
		}

        private BitmapImage ImageLoader(string fileName)
		{
            BitmapImage imageToDisplay = new BitmapImage();
            imageToDisplay.BeginInit();
            imageToDisplay.CacheOption = BitmapCacheOption.OnLoad;
            imageToDisplay.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            imageToDisplay.UriSource = new Uri(Directory.GetCurrentDirectory() + "\\" + fileName);
            imageToDisplay.EndInit();
            return imageToDisplay;
		}
        
		private void opacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!dragStarted)
			{
                opacity = (float)opacitySlider.Value;
                MosaicEngine engine = new MosaicEngine(mosaicDirectory, baseImage);
                engine.ChangeOpacityOfMosaicFromFiles(opacity);
                finalMosaic.Source = ImageLoader("final mosaic.png");
            }
		}
		private void opacitySlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
            opacity = (float)opacitySlider.Value;
            MosaicEngine engine = new MosaicEngine(mosaicDirectory, baseImage);
            engine.ChangeOpacityOfMosaicFromFiles(opacity);
            finalMosaic.Source = ImageLoader("final mosaic.png");
            dragStarted = false;
        }
		private void opacitySlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
            dragStarted = true;
		}
	}
}
