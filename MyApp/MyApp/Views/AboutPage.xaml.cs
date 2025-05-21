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
        protected override async void OnAppearing()
        {
           await LocalDbService.ExportDatabaseForDebugging();
        }
        
    }
}