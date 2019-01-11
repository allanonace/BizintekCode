﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;

namespace aclara_meters.view
{
    public partial class ErrorInitView : ContentPage
    {
        public ErrorInitView(string error = "")
        {
            InitializeComponent();


            #region Permissions

            Task.Run(async () =>
            {
                await Task.Delay(1000); Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var statusLocation = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                        var statusStorage = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);

                        if (statusLocation != PermissionStatus.Granted)
                        {
                            await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                        }

                        if (statusStorage != PermissionStatus.Granted)
                        {
                            await CrossPermissions.Current.RequestPermissionsAsync(Permission.Storage);
                        }

                    }
                    catch (Exception ex)
                    {

                    }

                });
            });

            #endregion


            //Turn off the Navigation bar
            NavigationPage.SetHasNavigationBar(this, false);

            Task.Run(async () =>
            {
                await Task.Delay(1000); Device.BeginInvokeOnMainThread(() =>
                {
                    if(!error.Equals(""))
                    {
                        CheckError(error);
                    }else{
                        CheckError();
                    }
                   
                });
            });
        }


        private async void CheckError(string error = "")
        {
            string respstr = "Internet Access is needed at first run, please enable data connection";

            if(!error.Equals(""))
            {
                respstr = error;
            }

            if (respstr.Equals("scripting"))
            {
                return;
            }
            var response = await Application.Current.MainPage.DisplayAlert("Error", respstr, "Ok", "Cancel");

            if (response == false || response == true)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();

            }

        }

    }
}
