using System.ComponentModel;
using Xamarin.Forms;
using MyApp.ViewModels;

namespace MyApp.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}