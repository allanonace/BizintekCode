﻿using Xamarin.Forms;
using Xml;
using System.Threading.Tasks;
using System;

using Acr.UserDialogs;

namespace MTUComm
{
    public class PageLinker
    {
        private const string BTN_TXT = "Ok";
    
        private static PageLinker instance;
        private static Page currentPage;
        public static Page mainPage;
        public static IDisposable popup;
    
        public static Page CurrentPage
        {
            get { return currentPage;  }
            set { currentPage = value; }
        }
    
        private PageLinker () {}
        
        private static PageLinker GetInstance ()
        {
            if ( instance == null )
                instance = new PageLinker ();
                
            return instance;
        }

        private async void _ShowAlert (
            string title,
            string message,
            string btnText,
            bool   kill = false )
        {
            if ( currentPage != null )
            {
                Device.BeginInvokeOnMainThread ( async () =>
                {
                    // NOTE: Xamarin DisplayAlert dialog cannot be closed/disposed from code
                    //await currentPage.DisplayAlert ( title, message, btnText );
                    
                    popup = UserDialogs.Instance.Alert ( message, title, btnText );
                    
                    if ( kill )
                    {
                        // Wait four seconds and kill the popup
                        await Task.Delay ( TimeSpan.FromSeconds ( 4 ) );
                        popup.Dispose ();
                        
                        // Close the app
                        System.Diagnostics.Process.GetCurrentProcess ().Kill ();
                    }
                });
            }
        }

        public static void ShowAlert (
            string title,
            Error  error,
            bool   kill    = false,
            string btnText = BTN_TXT )
        {
            if ( error.Id > -1 )
                GetInstance ()._ShowAlert (
                    title, "Error " + error.Id + ": " + error.Message, btnText, kill );
            else
                GetInstance ()._ShowAlert (
                    title, error.Message, btnText, kill );
        }
    }
}
