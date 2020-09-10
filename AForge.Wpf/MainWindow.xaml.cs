using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Common;
using Common.Models;
using CommonUI;
using CommonUI.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

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

        BitmapImage webcamImage = null;
        int accountUser = 0;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            GetVideoDevices();
            this.Closing += MainWindow_Closing;

            //OnLoginClickAsync();
        }
        private Uri GetServerUri()
        {
            //UriBuilder address = new UriBuilder(IPAddress.Text);
            //UriBuilder address = new UriBuilder("https://localhost:5001");
            //UriBuilder address = new UriBuilder("https://192.168.1.105:5001");
            UriBuilder address = new UriBuilder("https://bacom.dyndns.org:5001");
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
            accountUser = account.Id;
            MessageBox.Show("Login Success");
            ConnectAlarm(GetServerUri(), account.Id).GetAwaiter();
        }
        void LoginFailure(string result)
        {
            MessageBox.Show(result, "Login Failure", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public async Task ConnectAlarm(Uri uri,int accountId)
        {
            accountUser = accountId;
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

            DesktopClient client = new DesktopClient(connection);
            var list = await client.GetEventTypeList();
            //EventLog log = new EventLog
            //{
            //    Type = list.Where(i => i.Name == "CaptureWebcam").FirstOrDefault()
            //};

            //SubsystemEvent subsystemEvent = new SubsystemEvent
            //{
            //    Name
            //}

            //Console.WriteLine(JsonConvert.SerializeObject(log));

            while (true)
            {
                await Task.Delay(5000);
                if (webcamImage != null)
                {
                    var img = new Media
                    {
                        Type = "png",
                        Data = ImageToByte(webcamImage)
                    };
                    var frameImage = await client.CreateMedia(img);
                    var testresult = await client.GetEventFromSubsystemAsync(1, 0, accountUser, "CaptureWebcam", frameImage.Id.ToString());
                    Console.WriteLine(testresult);
                }
            }
        }

        public string ImageToBase64(BitmapImage image)
        {
            //string path = "D:\\SampleImage.jpg";
            //using (Image image = Image.FromFile(path))
            //{
                    byte[] imageBytes = ImageToByte(image);
                    var base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
            //}
        }

        public Byte[] ImageToByte(BitmapImage imageSource)
        {
            //Stream stream = imageSource.StreamSource;
            //Byte[] buffer = null;
            //if (stream != null && stream.Length > 0)
            //{
            //    using (BinaryReader br = new BinaryReader(stream))
            //    {
            //        buffer = br.ReadBytes((Int32)stream.Length);
            //    }
            //}


            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource));
            Byte[] buffer = null;
            using (var mem = new MemoryStream())
            {
                encoder.Save(mem);
                buffer = mem.ToArray();
            }
            return buffer;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            StopCamera();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartCamera();
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = bitmap.ToBitmapImage();
                }
                bi.Freeze(); // avoid cross thread operations and prevents leaks
                Dispatcher.BeginInvoke(new ThreadStart(delegate { 
                    videoPlayer.Source = bi;
                    webcamImage = bi;
                }));
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
