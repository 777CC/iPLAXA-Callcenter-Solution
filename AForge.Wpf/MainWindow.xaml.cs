using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
using AForge.Video;
using AForge.Video.DirectShow;
using Common;
using Common.Models;
using CommonUI;
using CommonUI.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;

namespace AForge.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region Public properties

        public ObservableCollection<FilterInfo> VideoDevices { get; set; }

        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; this.OnPropertyChanged("CurrentDevice"); }
        }
        private FilterInfo _currentDevice;

        #endregion

        HubConnection connection;
        #region Private fields

        private IVideoSource _videoSource;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            GetVideoDevices();
            this.Closing += MainWindow_Closing;

            OnLoginClickAsync();
        }
        private Uri GetServerUri()
        {
            //UriBuilder address = new UriBuilder(IPAddress.Text);
            UriBuilder address = new UriBuilder("https://localhost:5001");
            //address.Path = path;
            return address.Uri;
        }
        private void OnLoginClickAsync()
        {
            var loginData = new LoginData();
            //loginData.Username = UserName.Text;
            //loginData.Password = Password.Password;

            loginData.Username = "admin";
            loginData.Password = "password";
            Auth.Login(GetServerUri(), loginData, LoginSuccessCallback, LoginFailure);
        }

        void LoginSuccessCallback(Account account)
        {
            AppUser.CurrentAccount = new AccountViewModel(account, null);

            MessageBox.Show("Login Success");
            ConnectAlarm(GetServerUri()).GetAwaiter();
        }
        void LoginFailure(string result)
        {
            MessageBox.Show(result, "Login Failure", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public async Task ConnectAlarm(Uri uri)
        {

            //Console.WriteLine(uri);
            //string p = uri.AbsoluteUri.Replace(uri.LocalPath, string.Empty);
            string test = uri.AbsoluteUri + "OperatorHub";
            Console.WriteLine(test);
            connection = new HubConnectionBuilder()
                .WithUrl(test, options =>
                {
                    options.Cookies = RestService.Instance.Cookies;
                })
                .Build();
            
            await connection.StartAsync();
            Console.WriteLine(connection.State);


        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartCamera();
        }

        private void video_NewFrame(object sender, Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = bitmap.ToBitmapImage();
                }
                bi.Freeze(); // avoid cross thread operations and prevents leaks
                Dispatcher.BeginInvoke(new ThreadStart(delegate { videoPlayer.Source = bi; }));
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCamera();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void GetVideoDevices()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                VideoDevices.Add(filterInfo);
            }
            if (VideoDevices.Any())
            {
                CurrentDevice = VideoDevices[0];
            }
            else
            {
                MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCamera()
        {
            if (CurrentDevice != null)
            {
                _videoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
                _videoSource.NewFrame += video_NewFrame;
                _videoSource.Start();
            }
        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= new NewFrameEventHandler(video_NewFrame);
            }
        }

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        } 

        #endregion
    }
}
