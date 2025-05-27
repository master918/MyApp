using MyApp.Items;
using MyApp.Services;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyApp.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        //protected override void OnAppearing()
        //{
        //    var s = LocalDbService.Database.QueryAsync<User>("select * from User").Result;
        //}
    }
}