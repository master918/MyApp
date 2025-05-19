using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;

namespace MyApp.Services
{
    public static class NetworkService
    {
        public static bool IsConnectedToInternet()
        {
            var current = Connectivity.NetworkAccess;
            return current == NetworkAccess.Internet;
        }
    }
}
