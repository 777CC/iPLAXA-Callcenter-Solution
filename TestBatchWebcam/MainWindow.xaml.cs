using Common;
using Common.Models;
using CommonUI;
using CommonUI.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace TestBatchWebcam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HubConnection connection;

        BitmapImage webcamImage = null;
        int accountUser = 0;
        DesktopClient client;
        List<BitmapImage> imageList = new List<BitmapImage>();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

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

        public async Task ConnectAlarm(Uri uri, int accountId)
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

            client = new DesktopClient(connection);
            //var list = await client.GetEventTypeList();


            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/01.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/02.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/03.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/04.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/05.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/06.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/07.png")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/08.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/09.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/10.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/11.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/12.jpg")));
            imageList.Add(new BitmapImage(new Uri("pack://application:,,/;Component/Images/13.jpg")));
            int idIndex = 3;
            Random random = new Random();
            while (true)
            {
                await Task.Delay(1000);
                AddMedia(idIndex, imageList[random.Next(imageList.Count)]);
                idIndex++;
                if(idIndex == 22)
                {
                    idIndex = 3;
                }
            }
        }

        async void AddMedia(int accountUser,BitmapImage webcamImage)
        {
            var img = new Media
            {
                Type = "png",
                Data = ImageToByte(webcamImage)
            };
            var frameImage = await client.CreateMedia(img);
            var testresult = await client.GetEventFromSubsystemAsync(1, 0, accountUser, "CaptureWebcam", frameImage.Id.ToString());
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
    }
}
