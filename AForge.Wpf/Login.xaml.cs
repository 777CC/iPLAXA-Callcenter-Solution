using Common;
using Common.Models;
using CommonUI;
using CommonUI.ViewModels;
using MaterialDesignThemes.Wpf;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace AForge.Wpf
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        const int defaultPort = 5000;
        const string path = "/api/";
        HubConnection connection;
        public Login()
        {
            InitializeComponent();
        }
        private Uri GetServerUri()
        {
            //UriBuilder address = new UriBuilder(IPAddress.Text);
            UriBuilder address = new UriBuilder(IPAddress.Text);
            //UriBuilder address = new UriBuilder("https://bacom.dyndns.org:5001");
            //address.Path = path;
            return address.Uri;
        }
        private void OnLoginClickAsync()
        {
            var loginData = new LoginData();
            //loginData.Username = UserName.Text;
            //loginData.Password = Password.Password;

            loginData.Username = UserName.Text;
            loginData.Password = Password.Password;
            Auth.Login(GetServerUri(), loginData, LoginSuccessCallback, LoginFailure);
        }

        void LoginSuccessCallback(Account account)
        {
            AppUser.CurrentAccount = new AccountViewModel(account, null);

           // MessageBox.Show("Login Success");
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

            await Dispatcher.InvokeAsync(async () =>
            {
                //fdsfds

                Close();
            });
        }

        private async void OnLoginClickAsync(object sender, RoutedEventArgs e)
        {
            // OnLoginClickAsync();
            UriBuilder address = new UriBuilder(IPAddress.Text);
            address.Path = path;
            RestService.CreateInstance(address.Uri);


            var loginData = new LoginData();
            loginData.Username = UserName.Text;
            loginData.Password = Password.Password;
            var auth_response = await RestService.Instance.Login(loginData);

            if (auth_response != null)
            {
                if (auth_response.StatusCode == HttpStatusCode.OK)
                {
                    //var cookie = RestService.Instance.GetCookie();
                    Console.WriteLine(RestService.Instance.AuthCookie);

                    var user_account = await auth_response.Content.ReadAsAsync<Account>();
                    AppUser.CurrentAccount = new AccountViewModel(user_account, null);

                    try
                    {
                        //List<Subsystem> subsystemList = await RestService.Instance.GetAsync<List<Subsystem>>("subsystem");
                        //if (subsystemList != null)
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                MainWindow main = new MainWindow();
                                main.ConnectAlarm(new Uri(IPAddress.Text), user_account.Id);
                                main.Show();
                                Close();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    MessageBox.Show("Password not correct.");
                }
            }
        }
        private void Sample2_DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine("SAMPLE 2: Closing dialog with parameter: " + (eventArgs.Parameter ?? ""));
        }

        private async Task OnOpenMainApp(List<Subsystem> subsystems)
        {

            //Task.Run(OnLoginClickAsync);
        }
        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}