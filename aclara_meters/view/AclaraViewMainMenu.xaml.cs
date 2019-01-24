﻿// Copyright M. Griffie <nexus@nexussays.com>
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acr.UserDialogs;
using aclara_meters.Helpers;
using aclara_meters.Models;
using Xamarin.Forms;
using System.Threading;
using nexus.protocols.ble.scan;
using System.Collections.ObjectModel;
using Plugin.Settings;
using System.Linq;
using MTUComm;

namespace aclara_meters.view
{
    public partial class AclaraViewMainMenu
    {
        private const bool DEBUG_MODE_ON = true;

        private bool autoConnect;
        private bool conectarDevice;

        private string page_to_controller;

        public DeviceItem last_item;

        private List<PageItem> MenuList { get; set; }
        private IUserDialogs dialogsSaved;
        private ObservableCollection<DeviceItem> employees;

        private int peripheralConnected = ble_library.BlePort.NO_CONNECTED;
        private Boolean peripheralManualDisconnection = false;
        private Thread printer;

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        public AclaraViewMainMenu()
        {
            InitializeComponent();
        }

        public AclaraViewMainMenu(IUserDialogs dialogs)
        {
            InitializeComponent();
            Settings.IsConnectedBLE = false;
            NavigationPage.SetHasNavigationBar(this, false); //Turn off the Navigation bar
            TappedListeners();
            LoadPreUIGFX();

            if (Device.Idiom == TargetIdiom.Tablet)
            {
                LoadTabletUI();
            }
            else
            {
                LoadPhoneUI();
            }

            dialogsSaved = dialogs;
            LoadPostUIGFX();

            //Change username textview to Prefs. String
            if (FormsApp.credentialsService.UserName != null)
            {
                userName.Text = FormsApp.credentialsService.UserName; //"Kartik";
                CrossSettings.Current.AddOrUpdateValue("session_username", FormsApp.credentialsService.UserName);
            }

            LoadSideMenuElements();

            if (Device.Idiom == TargetIdiom.Phone)
            {
                background_scan_page.Opacity = 0;
                background_scan_page.FadeTo(1, 250);
            }

            if (Device.RuntimePlatform == Device.Android)
            {
                backmenu.Scale = 1.42;

            }


            #region New Scripting method is called

            Device.BeginInvokeOnMainThread(() =>
            {
                PrintToConsole("Se va a empezar el flujo");

                PrintToConsole("Se va a lanzar una Tarea. Task.Factory.StartNew(Init_Scripting_Method)");

                Task.Factory.StartNew(Interface_background_scan_page);

            });

            #endregion





            //BluetoothPeripheralDisconnect ( null, null );
        }

        /*--------------------------------------------------*/
        /*          Device List Interface Contenview
        /---------------------------------------------------*/

        private bool GetAutoConnectStatus()
        {
            return autoConnect;
        }

        private void Interface_background_scan_page()
        {

            PrintToConsole("Va a lanzar un delay. Task.Delay(100)");

            PrintToConsole("Va a lanzar, en el hilo UI, la acción: Interface_background_scan_page");

            printer = new Thread(new ThreadStart(InvokeMethod));

            PrintToConsole("Va a lanzar un Hilo-Thread. new Thread(new ThreadStart(InvokeMethod)) printer.Start() - Interface_background_scan_page");

            printer.Start();

            employees = new ObservableCollection<DeviceItem>();

            DeviceList.RefreshCommand = new Command(async () =>
            {

                PrintToConsole("está ejecutando el RefreshCommand - Interface_background_scan_page");

                PrintToConsole("comprobar si autoConnect es falso - Interface_ContentView_DeviceLis");


                if (!GetAutoConnectStatus())
                {

                    PrintToConsole("ha entrado en la condicion - Interface_background_scan_page");

                    PrintToConsole("va a Activar la barra de progreso circular - Interface_background_scan_page");

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        #region New Circular Progress bar Animations    

                        DeviceList.IsRefreshing = false;
                        backdark_bg.IsVisible = true;
                        indicator.IsVisible = true;
                        background_scan_page.IsEnabled = false;

                        #endregion

                    });
                    PrintToConsole("Mostrar barra de progreso - Interface_background_scan_page");

                    // Hace un resume si se ha hecho un suspend (al pasar a config o logout)
                    // Problema: solo se hace si se refresca DeviceList
                    // TO-DO: eliminar el hilo o eliminar el suspend

                    PrintToConsole("comprobar si el hilo -printer- esta suspendido - Interface_background_scan_page");

                    if (printer.ThreadState == System.Threading.ThreadState.Suspended)
                    {
                        try
                        {
                            PrintToConsole("hilo -printer- suspendido, arranca -printer- printer.Resume(); -Interface_background_scan_page");

                            printer.Resume();
                        }
                        catch (Exception e11)
                        {
                            Console.WriteLine(e11.StackTrace);
                        }
                    }

                    //DeviceList.IsRefreshing = true;

                    employees = new ObservableCollection<DeviceItem>();

                    PrintToConsole("comienza el Escaneo de dispositivos - Interface_background_scan_page");

                    await FormsApp.ble_interface.Scan();

                    PrintToConsole("finaliza el Escaneo de dispositivos - Interface_background_scan_page");

                    PrintToConsole("comienza la detección de dispositivos almacenados para autoreconectarse - Interface_background_scan_page");

                    await ChangeListViewData();

                    PrintToConsole("finaliza la detección de dispositivos almacenados para autoreconectarse - Interface_background_scan_page");

                    //DeviceList.IsRefreshing = false;

                    if (employees.Count != 0)
                    {

                        DeviceList.ItemsSource = employees;
                    }

                }


            });

            PrintToConsole("en 3 segundos comienza un bucle cada 3 segundos (BUCLE REFRESH LIST) - Interface_background_scan_page");

            #region Execute the Refresh List method every 3 seconds if no elements are on list

            var minutes = TimeSpan.FromSeconds(3);

            Device.StartTimer(minutes, () => {

                PrintToConsole("Dentro del bucle (BUCLE REFRESH LIST) - Interface_background_scan_page");

                // call your method to check for notifications here

                if (employees.Count < 1)
                {
                    PrintToConsole("se va lanzar un Refresh Command (BUCLE REFRESH LIST) - Interface_background_scan_page");

                    DeviceList.RefreshCommand.Execute(true);
                }

                if (employees.Count > 0)
                {
                    DeviceList.ItemsSource = employees;
                }
                PrintToConsole("un ciclo del bucle (BUCLE REFRESH LIST) - Interface_background_scan_page");


                if (conectarDevice)
                {


                    PrintToConsole("autoConnect se pone a false - InvokeMethod");
                    autoConnect = false;
                    conectarDevice = false;

                    #region Autoconnect to stored device 

                    PrintToConsole("Se va a crear una Tarea al de 0.5 segundos (Task.Factory.StartNew(NewOpenConnectionWithDevice);) - InvokeMethod");
                    Task.Factory.StartNew(NewOpenConnectionWithDevice);

                    #endregion


                }





                // Returning true means you want to repeat this timer
                return true;
            });

            #endregion

            if (employees.Count != 0)
            {
                DeviceList.ItemsSource = employees;
            }
        }




        public void FirstRefreshSearchPucs()
        {
            DeviceList.RefreshCommand.Execute(true);
        }

        private void LoadSideMenuElements()
        {
            // Creating our pages for menu navigation
            // Here you can define title for item, 
            // icon on the left side, and page that you want to open after selection

            MenuList = new List<PageItem>();

            // Adding menu items to MenuList

            MenuList.Add(new PageItem() { Title = "Read MTU", Icon = "readmtu_icon.png", TargetType = "ReadMTU" });

            if (FormsApp.config.global.ShowTurnOff)
                MenuList.Add(new PageItem() { Title = "Turn Off MTU", Icon = "turnoff_icon.png", TargetType = "turnOff" });

            if (FormsApp.config.global.ShowAddMTU)
                MenuList.Add(new PageItem() { Title = "Add MTU", Icon = "addMTU.png", TargetType = "AddMTU" });

            if (FormsApp.config.global.ShowReplaceMTU)
                MenuList.Add(new PageItem() { Title = "Replace MTU", Icon = "replaceMTU2.png", TargetType = "replaceMTU" });

            if (FormsApp.config.global.ShowReplaceMeter)
                MenuList.Add(new PageItem() { Title = "Replace Meter", Icon = "replaceMeter.png", TargetType = "replaceMeter" });

            if (FormsApp.config.global.ShowAddMTUMeter)
                MenuList.Add(new PageItem() { Title = "Add MTU / Add Meter", Icon = "addMTUaddmeter.png", TargetType = "AddMTUAddMeter" });

            if (FormsApp.config.global.ShowAddMTUReplaceMeter)
                MenuList.Add(new PageItem() { Title = "Add MTU / Rep. Meter", Icon = "addMTUrepmeter.png", TargetType = "AddMTUReplaceMeter" });

            if (FormsApp.config.global.ShowReplaceMTUMeter)
                MenuList.Add(new PageItem() { Title = "Rep.MTU / Rep. Meter", Icon = "repMTUrepmeter.png", TargetType = "ReplaceMTUReplaceMeter" });

            if (FormsApp.config.global.ShowInstallConfirmation)
                MenuList.Add(new PageItem() { Title = "Install Confirmation", Icon = "installConfirm.png", TargetType = "InstallConfirm" });



            // ListView needs to be at least  elements for UI Purposes, even empty ones
            while (MenuList.Count < 9)
                MenuList.Add(new PageItem() { Title = "", Icon = "", TargetType = "" });

            // Setting our list to be ItemSource for ListView in MainPage.xaml
            navigationDrawerList.ItemsSource = MenuList;

        }


        void OnSwiped(object sender, SwipedEventArgs e)
        {
            if (Device.Idiom == TargetIdiom.Tablet)
                return;

            switch (e.Direction)
            {
                case SwipeDirection.Left:


                    fondo.Opacity = 1;
                    ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                    shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    Task.Delay(200).ContinueWith(t =>
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        ContentNav.Opacity = 0;
                        shadoweffect.IsVisible = false;
                        ContentNav.IsVisible = false;
                        background_scan_page.IsEnabled = true;
                        background_scan_page_detail.IsEnabled = true;
                    }));

                    break;
                case SwipeDirection.Right:
                    fondo.Opacity = 0;
                    ContentNav.IsVisible = true;
                    shadoweffect.IsVisible = true;
                    background_scan_page.Opacity = 0.5;
                    background_scan_page_detail.Opacity = 0.5;
                    ContentNav.Opacity = 1;
                    ContentNav.TranslateTo(0, 0, 175, Easing.SinIn);
                    shadoweffect.TranslateTo(0, 0, 175, Easing.SinIn);
                    background_scan_page.IsEnabled = false;
                    background_scan_page_detail.IsEnabled = false;
                    break;

            }
        }






        private void LoadPreUIGFX()
        {
            shadoweffect.IsVisible = false;
            background_scan_page_detail.IsVisible = true;
            background_scan_page_detail.IsVisible = false;
        }

        private void LoadPostUIGFX()
        {
            background_scan_page_detail.IsVisible = true;
            background_scan_page_detail.IsVisible = false;
            background_scan_page.IsVisible = true;
            navigationDrawerList.IsEnabled = true;
            navigationDrawerList.Opacity = 0.65;

            if (Device.Idiom == TargetIdiom.Tablet)
            {
                ContentNav.Opacity = 1;
                ContentNav.IsVisible = true;
            }
            else
            {
                ContentNav.Opacity = 0;
                ContentNav.IsVisible = false;
            }



            background_scan_page.Opacity = 1;
            background_scan_page_detail.Opacity = 1;
        }

        private void LoadPhoneUI()
        {
            background_scan_page.Margin = new Thickness(0, 0, 0, 0);
            background_scan_page_detail.Margin = new Thickness(0, 0, 0, 0);
            close_menu_icon.Opacity = 1;
            hamburger_icon.IsVisible = true;
            hamburger_icon_detail.IsVisible = true;
            aclara_detail_logo.IsVisible = true;
            aclara_logo.IsVisible = true;
            tablet_user_view.TranslationY = 0;
            tablet_user_view.Scale = 1;
            aclara_logo.IsVisible = true;
            logo_tablet_aclara.Opacity = 0;
            aclara_detail_logo.IsVisible = true;
            tablet_user_view.TranslationY = -22;
            tablet_user_view.Scale = 1.2;
            ContentNav.TranslationX = -310;
            shadoweffect.TranslationX = -310;
            ContentNav.IsVisible = true;
            shadoweffect.IsVisible = true;
            ContentNav.IsVisible = false;
            shadoweffect.IsVisible = false;
        }

        private void LoadTabletUI()
        {
            ContentNav.IsVisible = true;
            background_scan_page.Opacity = 1;
            background_scan_page_detail.Opacity = 1;
            close_menu_icon.Opacity = 0;
            hamburger_icon.IsVisible = false;
            hamburger_icon_detail.IsVisible = false;
            background_scan_page.Margin = new Thickness(310, 0, 0, 0);
            background_scan_page_detail.Margin = new Thickness(310, 0, 0, 0);
            aclara_logo.IsVisible = true;
            logo_tablet_aclara.Opacity = 0;
            aclara_detail_logo.IsVisible = true;
            tablet_user_view.TranslationY = -22;
            tablet_user_view.Scale = 1.2;
            shadoweffect.IsVisible = true;
            aclara_logo.Scale = 1.2;
            aclara_detail_logo.Scale = 1.2;
            aclara_detail_logo.TranslationX = 42;
            aclara_logo.TranslationX = 42;

            shadoweffect.Source = "shadow_effect_tablet";

        }

        private void TappedListeners()
        {
            turnoffmtu_ok.Tapped += TurnOffMTUOkTapped;
            turnoffmtu_no.Tapped += TurnOffMTUNoTapped;
            turnoffmtu_ok_close.Tapped += TurnOffMTUCloseTapped;
            replacemeter_ok.Tapped += ReplaceMtuOkTapped;
            replacemeter_cancel.Tapped += ReplaceMtuCancelTapped;
            meter_ok.Tapped += MeterOkTapped;
            meter_cancel.Tapped += MeterCancelTapped;

            dialog_AddMTUAddMeter_ok.Tapped += dialog_AddMTUAddMeter_okTapped;
            dialog_AddMTUAddMeter_cancel.Tapped += dialog_AddMTUAddMeter_cancelTapped;

            dialog_AddMTUReplaceMeter_ok.Tapped += dialog_AddMTUReplaceMeter_okTapped;
            dialog_AddMTUReplaceMeter_cancel.Tapped += dialog_AddMTUReplaceMeter_cancelTapped;

            dialog_ReplaceMTUReplaceMeter_ok.Tapped += dialog_ReplaceMTUReplaceMeter_okTapped;
            dialog_ReplaceMTUReplaceMeter_cancel.Tapped += dialog_ReplaceMTUReplaceMeter_cancelTapped;


            dialog_AddMTU_ok.Tapped += dialog_AddMTU_okTapped;
            dialog_AddMTU_cancel.Tapped += dialog_AddMTU_cancelTapped;



            disconnectDevice.Tapped += BluetoothPeripheralDisconnect;
            back_button.Tapped += SideMenuOpen;
            back_button_menu.Tapped += SideMenuClose;
            logout_button.Tapped += LogoutTapped;
            back_button_detail.Tapped += SideMenuOpen;
            settings_button.Tapped += OpenSettingsTapped;

            logoff_no.Tapped += LogOffNoTapped;
            logoff_ok.Tapped += LogOffOkTapped;
        }


        /***
         * 
         *  //Dynamic battery detection when connected
         * 
         * 
            try
            {
                battery = FormsApp.ble_interface.GetBatteryLevel();

                if(battery != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        
                        if(battery[0] < 101 && battery[0] > 1 )
                        {
                            batteryLevel.Text = battery[0].ToString() + " %";

                            if (battery[0] > 75)
                            {

                                imageBattery.Source = "battery_toolbar_high";
                                battery_level.Source = "battery_toolbar_high_white";
                                battery_level_detail.Source = "battery_toolbar_high_white";
                            }

                            if (battery[0] > 45 && battery[0] < 75)
                            {

                                imageBattery.Source = "battery_toolbar_mid";
                                battery_level.Source = "battery_toolbar_mid_white";
                                battery_level_detail.Source = "battery_toolbar_mid_white";
                            }

                            if (battery[0] > 15 && battery[0] < 45)
                            {

                                imageBattery.Source = "battery_toolbar_low";
                                battery_level.Source = "battery_toolbar_low_white";
                                battery_level_detail.Source = "battery_toolbar_low_white";
                            }

                            if (battery[0] < 15)
                            {

                                imageBattery.Source = "battery_toolbar_empty";
                                battery_level.Source = "battery_toolbar_empty_white";
                                battery_level_detail.Source = "battery_toolbar_empty_white";
                            }

                        }
                    });
                }
            }catch (Exception e5){
                
            }
         *
         ***/


        private void InvokeMethod()
        {
            PrintToConsole("dentro del metodo - InvokeMethod");

            int timeout_connecting = 0;

            PrintToConsole("se va a ejecutar un bucle (WHILE TRUE) - InvokeMethod");

            while (true)
            {
                PrintToConsole("dentro del bucle (WHILE TRUE) - InvokeMethod");

                PrintToConsole("buscamos el estado de la conexion - InvokeMethod");

                int status = FormsApp.ble_interface.GetConnectionStatus();

                PrintToConsole("se obtiene el estado de la conexion - InvokeMethod");

                if (status != peripheralConnected)
                {
                    PrintToConsole("buscamos el estado de la conexion - InvokeMethod");

                    PrintToConsole("¿ES NO_CONNECTED? - InvokeMethod");

                    if (peripheralConnected == ble_library.BlePort.NO_CONNECTED)
                    {
                        peripheralConnected = status;
                        timeout_connecting = 0;
                    }
                    else if (peripheralConnected == ble_library.BlePort.CONNECTING)
                    {
                        PrintToConsole("Nop, es CONNECTING - InvokeMethod");

                        if (status == ble_library.BlePort.NO_CONNECTED)
                        {
                            PrintToConsole("Se va a ejecutar algo en la UI - InvokeMethod");

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                PrintToConsole("Se va a detectar el estado de la conexion - InvokeMethod");

                                switch (FormsApp.ble_interface.GetConnectionError())
                                {
                                    case ble_library.BlePort.NO_ERROR:
                                        PrintToConsole("Estado conexion: NO_ERROR - InvokeMethod");
                                        break;
                                    case ble_library.BlePort.CONECTION_ERRROR:
                                        PrintToConsole("Estado conexion: CONECTION_ERRROR - InvokeMethod");

                                        Device.BeginInvokeOnMainThread(() =>
                                        {
                                            #region New Circular Progress bar Animations    

                                            DeviceList.IsRefreshing = false;
                                            backdark_bg.IsVisible = false;
                                            indicator.IsVisible = false;
                                            background_scan_page.IsEnabled = true;

                                            #endregion
                                        });

                                        PrintToConsole("Desactivar barra de progreso - InvokeMethod");

                                        Application.Current.MainPage.DisplayAlert("Alert", "Connection error. Please, retry", "Ok");
                                        break;
                                    case ble_library.BlePort.DYNAMIC_KEY_ERROR:
                                        PrintToConsole("Estado conexion: DYNAMIC_KEY_ERROR - InvokeMethod");

                                        Device.BeginInvokeOnMainThread(() =>
                                        {
                                            #region New Circular Progress bar Animations    

                                            DeviceList.IsRefreshing = false;
                                            backdark_bg.IsVisible = false;
                                            indicator.IsVisible = false;
                                            background_scan_page.IsEnabled = true;

                                            #endregion
                                        });

                                        PrintToConsole("Desactivar barra de progreso - InvokeMethod");
                                        Application.Current.MainPage.DisplayAlert("Alert", "Please, press the button to change PAIRING mode", "Ok");
                                        break;
                                    case ble_library.BlePort.NO_DYNAMIC_KEY_ERROR:
                                        PrintToConsole("Estado conexion: NO_DYNAMIC_KEY_ERROR - InvokeMethod");

                                        Device.BeginInvokeOnMainThread(() =>
                                        {
                                            #region New Circular Progress bar Animations    

                                            DeviceList.IsRefreshing = false;
                                            backdark_bg.IsVisible = false;
                                            indicator.IsVisible = false;
                                            background_scan_page.IsEnabled = true;

                                            #endregion

                                        });
                                        PrintToConsole("Desactivar barra de progreso - InvokeMethod");
                                        Application.Current.MainPage.DisplayAlert("Alert", "Please, press the button to change PAIRING mode", "Ok");
                                        break;
                                }
                                DeviceList.IsEnabled = true;
                                fondo.Opacity = 1;
                                background_scan_page.Opacity = 1;
                                background_scan_page.IsEnabled = true;

                            });
                            peripheralConnected = status;
                            FormsApp.peripheral = null;
                        }
                        else // status == ble_library.BlePort.CONNECTED
                        {
                            PrintToConsole("Estas Conectado - InvokeMethod");

                            DeviceList.IsEnabled = true;
                           
                            peripheralConnected = status;
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                fondo.Opacity = 1;
                                background_scan_page.Opacity = 1;
                                background_scan_page.IsEnabled = true;

                                IsConnectedUIChange(true);
                            });
                        }
                    }
                    else if (peripheralConnected == ble_library.BlePort.CONNECTED)
                    {
                        PrintToConsole("Nop, es CONNECTED - InvokeMethod");

                        DeviceList.IsEnabled = true;
                       
                        peripheralConnected = status;
                        FormsApp.peripheral = null;
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            fondo.Opacity = 1;
                            background_scan_page.Opacity = 1;
                            background_scan_page.IsEnabled = true;

                            IsConnectedUIChange(false);
                        });
                    }
                }

                PrintToConsole("¿Está en CONNECTING? - InvokeMethod");
                if (peripheralConnected == ble_library.BlePort.CONNECTING)
                {
                    PrintToConsole("Si, es CONNECTING - InvokeMethod");
                    timeout_connecting++;
                    if (timeout_connecting >= 2 * 10) // 10 seconds
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            PrintToConsole("Un Timeout que te llevas - InvokeMethod");
                            Application.Current.MainPage.DisplayAlert("Timeout", "Connection Timeout", "Ok");
                            DeviceList.IsEnabled = true;
                            fondo.Opacity = 1;
                            background_scan_page.Opacity = 1;
                            background_scan_page.IsEnabled = true;

                            autoConnect = false;

                            Device.BeginInvokeOnMainThread(() =>
                            {

                                #region Disable Circular Progress bar Animations when done

                                backdark_bg.IsVisible = false;
                                indicator.IsVisible = false;
                                background_scan_page.IsEnabled = true;

                                #endregion

                            });

                            try
                            {
                                printer.Suspend();
                            }
                            catch (Exception e5)
                            {
                                Console.WriteLine(e5.StackTrace);
                            }


                        });
                        peripheralConnected = ble_library.BlePort.NO_CONNECTED;
                        timeout_connecting = 0;

                        PrintToConsole("Cerrar Conexion - InvokeMethod");

                        FormsApp.ble_interface.Close();
                    }
                }
                else
                {
                    PrintToConsole("Nop, no es CONNECTING - InvokeMethod");
                }

                PrintToConsole("Esperamos 300 ms - InvokeMethod");
                Thread.Sleep(300); // 0.5 Second

                PrintToConsole("¿Se va a realizar reconexion? - InvokeMethod");

            }

        }

        private void IsConnectedUIChange(bool v)
        {
            if (v)
            {
                try
                {
                    // TODO: la siguente linea siempre da error xq peripheral es null
                    deviceID.Text = FormsApp.peripheral.Advertisement.DeviceName;
                    macAddress.Text = DecodeId(FormsApp.peripheral.Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Take(4).ToArray());

                    //imageBattery.Source = "battery_toolbar_high";
                    // imageRssi.Source = "rssi_toolbar_high";
                    // batteryLevel.Text = "100%";
                    // rssiLevel.Text = peripheral.Rssi.ToString() + " dBm";

                    byte[] battery_ui = FormsApp.peripheral.Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Skip(4).Take(1).ToArray();

                    if (battery_ui[0] < 101 && battery_ui[0] > 1)
                    {
                        batteryLevel.Text = battery_ui[0].ToString() + " %";

                        if (battery_ui[0] >= 75)
                        {
                            imageBattery.Source = "battery_toolbar_high";
                            battery_level.Source = "battery_toolbar_high_white";
                            battery_level_detail.Source = "battery_toolbar_high_white";
                        }
                        else if (battery_ui[0] >= 45 && battery_ui[0] < 75)
                        {
                            imageBattery.Source = "battery_toolbar_mid";
                            battery_level.Source = "battery_toolbar_mid_white";
                            battery_level_detail.Source = "battery_toolbar_mid_white";
                        }
                        else if (battery_ui[0] >= 15 && battery_ui[0] < 45)
                        {
                            imageBattery.Source = "battery_toolbar_low";
                            battery_level.Source = "battery_toolbar_low_white";
                            battery_level_detail.Source = "battery_toolbar_low_white";
                        }
                        else // battery_ui[0] < 15
                        {
                            imageBattery.Source = "battery_toolbar_empty";
                            battery_level.Source = "battery_toolbar_empty_white";
                            battery_level_detail.Source = "battery_toolbar_empty_white";
                        }
                    }

                    /*** RSSI ICONS UPDATE ***/
                    if (FormsApp.peripheral.Rssi <= -90)
                    {
                        imageRssi.Source = "rssi_toolbar_empty";
                        rssi_level.Source = "rssi_toolbar_empty_white";
                        rssi_level_detail.Source = "rssi_toolbar_empty_white";
                    }
                    else if (FormsApp.peripheral.Rssi <= -80 && FormsApp.peripheral.Rssi > -90)
                    {
                        imageRssi.Source = "rssi_toolbar_low";
                        rssi_level.Source = "rssi_toolbar_low_white";
                        rssi_level_detail.Source = "rssi_toolbar_low_white";
                    }
                    else if (FormsApp.peripheral.Rssi <= -60 && FormsApp.peripheral.Rssi > -80)
                    {
                        imageRssi.Source = "rssi_toolbar_mid";
                        rssi_level.Source = "rssi_toolbar_mid_white";
                        rssi_level_detail.Source = "rssi_toolbar_mid_white";
                    }
                    else // (peripheral.Rssi > -60) 
                    {
                        imageRssi.Source = "rssi_toolbar_high";
                        rssi_level.Source = "rssi_toolbar_high_white";
                        rssi_level_detail.Source = "rssi_toolbar_high_white";
                    }

                    //Save Battery & Rssi info for the next windows
                    CrossSettings.Current.AddOrUpdateValue("battery_icon_topbar", battery_level.Source.ToString().Substring(6));
                    CrossSettings.Current.AddOrUpdateValue("rssi_icon_topbar", rssi_level.Source.ToString().Substring(6));

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }

                background_scan_page_detail.IsVisible = true;
                block_ble_disconnect.Opacity = 0;
                block_ble_disconnect.FadeTo(1, 250);
                block_ble.Opacity = 0;
                block_ble.FadeTo(1, 250);
                background_scan_page.IsVisible = false;
                navigationDrawerList.IsEnabled = true;
                navigationDrawerList.Opacity = 1;

                #region Disable Circular Progress bar Animations when done

                backdark_bg.IsVisible = false;
                indicator.IsVisible = false;

                #endregion



            }
            else
            {
                background_scan_page_detail.IsVisible = false;
                navigationDrawerList.Opacity = 0.65;
                navigationDrawerList.IsEnabled = true;
                background_scan_page.IsVisible = true;
                DeviceList.RefreshCommand.Execute(true);


            }
        }

        private string DecodeId(byte[] id)
        {
            string s;
            try
            {
                s = System.Text.Encoding.ASCII.GetString(id.Take(2).ToArray());
                byte[] byte_aux = new byte[4];
                byte_aux[0] = id[3];
                byte_aux[1] = id[2];
                byte_aux[2] = 0;
                byte_aux[3] = 0;
                s += BitConverter.ToInt32(byte_aux, 0);
            }
            catch (Exception e)
            {
                s = BitConverter.ToString(id);
            }
            return s;
        }

        private async Task ChangeListViewData()
        {
            await Task.Factory.StartNew(() =>
            {
                // wait until scan finish
                while (FormsApp.ble_interface.IsScanning())
                {
                    try
                    {
                        List<IBlePeripheral> blePeripherals;
                        blePeripherals = FormsApp.ble_interface.GetBlePeripheralList();

                        // YOU CAN RETURN THE PASS BY GETTING THE STRING AND CONVERTING IT TO BYTE ARRAY TO AUTO-PAIR
                        byte[] bytes = System.Convert.FromBase64String(CrossSettings.Current.GetValueOrDefault("session_peripheral_DeviceId", string.Empty));

                        byte[] byte_now = new byte[] { };

                        int sizeList = blePeripherals.Count;

                        for (int i = 0; i < sizeList; i++)
                        {
                            try
                            {
                                if (blePeripherals[i] != null)
                                {
                                    byte_now = blePeripherals[i].Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Take(4).ToArray();

                                    bool enc = false;
                                    int sizeListTemp = employees.Count;

                                    for (int j = 0; j < sizeListTemp; j++)
                                    {
                                        if (employees[j].Peripheral.Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Take(4).ToArray()
                                            .SequenceEqual(blePeripherals[i].Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Take(4).ToArray()))
                                        {
                                            enc = true;
                                        }
                                    }

                                    string icono_bateria;

                                    byte[] bateria;

                                    if (!enc)
                                    {
                                        bateria = blePeripherals[i].Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Skip(4).Take(1).ToArray();

                                        icono_bateria = "battery_toolbar_high";

                                        if (bateria[0] >= 75)
                                        {
                                            icono_bateria = "battery_toolbar_high";
                                        }
                                        else if (bateria[0] >= 45 && bateria[0] < 75)
                                        {
                                            icono_bateria = "battery_toolbar_mid";
                                        }
                                        else if (bateria[0] >= 15 && bateria[0] < 45)
                                        {
                                            icono_bateria = "battery_toolbar_low";
                                        }
                                        else // bateria[0] < 15
                                        {
                                            icono_bateria = "battery_toolbar_empty";
                                        }

                                        string rssiIcono = "rssi_toolbar_high";

                                        /*** RSSI ICONS UPDATE ***/

                                        if (blePeripherals[i].Rssi <= -90)
                                        {
                                            rssiIcono = "rssi_toolbar_empty";
                                        }
                                        else if (blePeripherals[i].Rssi <= -80 && blePeripherals[i].Rssi > -90)
                                        {
                                            rssiIcono = "rssi_toolbar_low";
                                        }
                                        else if (blePeripherals[i].Rssi <= -60 && blePeripherals[i].Rssi > -80)
                                        {
                                            rssiIcono = "rssi_toolbar_mid";
                                        }
                                        else // (blePeripherals[i].Rssi > -60) 
                                        {
                                            rssiIcono = "rssi_toolbar_high";
                                        }

                                        DeviceItem device = new DeviceItem
                                        {
                                            deviceMacAddress = DecodeId(byte_now),
                                            deviceName = blePeripherals[i].Advertisement.DeviceName,
                                            deviceBattery = bateria[0].ToString() + "%",
                                            deviceRssi = blePeripherals[i].Rssi.ToString() + " dBm",
                                            deviceBatteryIcon = icono_bateria,
                                            deviceRssiIcon = rssiIcono,
                                            Peripheral = blePeripherals[i]
                                        };

                                        employees.Add(device);

                                        //VERIFY IF PREVIOUSLY BOUNDED DEVICES WITH THE RIGHT USERNAME
                                        if (CrossSettings.Current.GetValueOrDefault("session_dynamicpass", string.Empty) != string.Empty &&
                                            FormsApp.credentialsService.UserName.Equals(CrossSettings.Current.GetValueOrDefault("session_username", string.Empty)) &&
                                            bytes.Take(4).ToArray().SequenceEqual(byte_now) &&
                                            blePeripherals[i].Advertisement.DeviceName.Equals(CrossSettings.Current.GetValueOrDefault("session_peripheral", string.Empty)) &&
                                            !peripheralManualDisconnection &&
                                            FormsApp.peripheral == null)
                                        {
                                            if (!FormsApp.ble_interface.IsOpen())
                                            {
                                                try
                                                {
                                                    FormsApp.peripheral = blePeripherals[i];
                                                    peripheralConnected = ble_library.BlePort.NO_CONNECTED;
                                                    peripheralManualDisconnection = false;


                                                    #region Autoconnect to stored device 

                                                    conectarDevice = true;

                                                    autoConnect = true;

                                                    #endregion



                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine(e.StackTrace);
                                                }

                                            }
                                            else
                                            {

                                                if (autoConnect)
                                                {

                                                    Device.BeginInvokeOnMainThread(() =>
                                                    {
                                                        #region Disable Circular Progress bar Animations when done

                                                        backdark_bg.IsVisible = false;
                                                        indicator.IsVisible = false;
                                                        background_scan_page.IsEnabled = true;

                                                        #endregion
                                                    });

                                                }

                                            }


                                        }

                                        else
                                        {

                                            // if (autoConnect)
                                            //  {

                                            Device.BeginInvokeOnMainThread(() =>
                                            {
                                                #region Disable Circular Progress bar Animations when done

                                                DeviceList.IsRefreshing = false;
                                                backdark_bg.IsVisible = false;
                                                indicator.IsVisible = false;
                                                background_scan_page.IsEnabled = true;

                                                #endregion

                                            });

                                            //  }

                                        }
                                    }
                                }
                            }
                            catch (Exception er)
                            {
                                Console.WriteLine(er.StackTrace); //2018-09-21 13:08:25.918 aclara_meters.iOS[505:190980] System.NullReferenceException: Object reference not set to an instance of an object
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
        }


        #region We want to connect to the device if there is not scanning running

        private void NewOpenConnectionWithDevice()
        {

            PrintToConsole("Se va a entrar en un bucle mientras esté Escaneando bluetooth - NewOpenConnectionWithDevice");

            while (FormsApp.ble_interface.IsScanning())
            {
                PrintToConsole("A esperar 100 ms mientras escanea... - NewOpenConnectionWithDevice");
                Thread.Sleep(100);
            }

            PrintToConsole("Se va a ejecutar algo en el UI - NewOpenConnectionWithDevice");

            Device.BeginInvokeOnMainThread(() =>
            {
                var seconds = TimeSpan.FromSeconds(1); // Don't execute it asap!

                Device.StartTimer(seconds, () =>
                {
                    PrintToConsole("Cada 1 segundo, se ejectua lo siguinete en el UI - NewOpenConnectionWithDevice");
                    Device.BeginInvokeOnMainThread(() =>
                    {

                        PrintToConsole("¿Esta la conexion abierta ? - NewOpenConnectionWithDevice");


                        if (!FormsApp.ble_interface.IsOpen())
                        {
                            PrintToConsole("¿Esta escaneando perifericos ? - NewOpenConnectionWithDevice");
                            while (FormsApp.ble_interface.IsScanning())
                            {
                                PrintToConsole("A esperar 100 ms en bucle - NewOpenConnectionWithDevice");
                                Thread.Sleep(100);
                            }

                            // call your method to check for notifications here
                            FormsApp.ble_interface.Open(FormsApp.peripheral, true);
                        }
                        else
                        {
                            PrintToConsole("NOPE, no lo esta - NewOpenConnectionWithDevice");
                        }
                    });

                    return false;
                });
            });
        }

        #endregion


        protected override void OnAppearing()
        {
            //DeviceList.RefreshCommand.Execute ( true );
        }

        private void LogOffOkTapped(object sender, EventArgs e)
        {
            dialog_logoff.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
            printer.Suspend();
            Settings.IsLoggedIn = false;
            FormsApp.credentialsService.DeleteCredentials();
            FormsApp.ble_interface.Close();
            background_scan_page.IsEnabled = true;
            background_scan_page_detail.IsEnabled = true;
            Navigation.PopAsync();

        }

        private void LogOffNoTapped(object sender, EventArgs e)
        {
            dialog_logoff.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        private void ReplaceMtuCancelTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        private void ReplaceMtuOkTapped(object sender, EventArgs e)
        {
            dialog_replacemeter_one.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;


            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "replaceMTU";
                Task.Factory.StartNew(BasicReadThread);
            });

        }

        private void TurnOffMTUCloseTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        private void TurnOffMTUNoTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        private void TurnOffMTUOkTapped(object sender, EventArgs e)
        {
            dialog_turnoff_one.IsVisible = false;
            dialog_turnoff_two.IsVisible = true;

            Task.Factory.StartNew(TurnOffMethod);
        }

        private void TurnOffMethod()
        {

            MTUComm.Action turnOffAction = new MTUComm.Action(
                config: FormsApp.config,
                serial: FormsApp.ble_interface,
                type: MTUComm.Action.ActionType.TurnOffMtu,
                user: FormsApp.credentialsService.UserName);

            turnOffAction.OnFinish += ((s, args) =>
            {
                ActionResult actionResult = args.Result;

                Task.Delay(2000).ContinueWith(t =>
                   Device.BeginInvokeOnMainThread(() =>
                   {
                       this.dialog_turnoff_text.Text = "MTU turned off Successfully";

                       dialog_turnoff_two.IsVisible = false;
                       dialog_turnoff_three.IsVisible = true;
                   }));
            });

            turnOffAction.OnError += ((s, args) =>
            {
                Task.Delay(2000).ContinueWith(t =>
                   Device.BeginInvokeOnMainThread(() =>
                   {
                       this.dialog_turnoff_text.Text = "MTU turned off Unsuccessfully";

                       dialog_turnoff_two.IsVisible = false;
                       dialog_turnoff_three.IsVisible = true;
                   }));
            });

            turnOffAction.Run();
        }

        void MeterCancelTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            dialog_meter_replace_one.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        void MeterOkTapped(object sender, EventArgs e)
        {
            dialog_meter_replace_one.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "replaceMeter";
                Task.Factory.StartNew(BasicReadThread);
            });


        }

        void dialog_AddMTUAddMeter_cancelTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            dialog_AddMTUAddMeter.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        void dialog_AddMTUAddMeter_okTapped(object sender, EventArgs e)
        {
            dialog_AddMTUAddMeter.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "AddMTUAddMeter";
                Task.Factory.StartNew(BasicReadThread);
            });


        }

        void dialog_AddMTUReplaceMeter_cancelTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            dialog_AddMTUReplaceMeter.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        void dialog_AddMTUReplaceMeter_okTapped(object sender, EventArgs e)
        {
            dialog_AddMTUReplaceMeter.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "AddMTUReplaceMeter";
                Task.Factory.StartNew(BasicReadThread);
            });

        }

        void dialog_ReplaceMTUReplaceMeter_cancelTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            dialog_ReplaceMTUReplaceMeter.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        void dialog_ReplaceMTUReplaceMeter_okTapped(object sender, EventArgs e)
        {
            dialog_ReplaceMTUReplaceMeter.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "ReplaceMTUReplaceMeter";
                Task.Factory.StartNew(BasicReadThread);
            });


        }

        void dialog_AddMTU_cancelTapped(object sender, EventArgs e)
        {
            dialog_open_bg.IsVisible = false;
            dialog_AddMTU.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;
        }

        void dialog_AddMTU_okTapped(object sender, EventArgs e)
        {
            dialog_AddMTU.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                // TODO: cambiar usuario
                // TODO: BasicRead no loguea
                try
                {
                    page_to_controller = "AddMTU";
                    Task.Factory.StartNew(BasicReadThread);
                }
                catch (Exception addmtu)
                {
                    Console.WriteLine(addmtu.StackTrace);
                }

            });

            //Bug fix Android UI Animation

        }

        void BasicReadThread()
        {
            MTUComm.Action basicRead = new MTUComm.Action(
               config: FormsApp.config,
               serial: FormsApp.ble_interface,
               type: MTUComm.Action.ActionType.BasicRead,
               user: FormsApp.credentialsService.UserName);

            basicRead.OnFinish += ((s, args) =>
            { });

            basicRead.Run();

            basicRead.OnFinish += ((s, e) =>
            {
                Task.Delay(200).ContinueWith(t =>
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage.Navigation.PushAsync(new AclaraViewAddMTU(dialogsSaved, page_to_controller), false);
                    })
                );
            });


        }


        private void BluetoothPeripheralDisconnect(object sender, EventArgs e)
        {
            FormsApp.ble_interface.Close();

            peripheralManualDisconnection = true;

            CrossSettings.Current.AddOrUpdateValue("session_dynamicpass", string.Empty);

        }

        private void LogoutTapped(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                dialog_turnoff_one.IsVisible = false;
                dialog_open_bg.IsVisible = true;
                dialog_meter_replace_one.IsVisible = false;
                dialog_turnoff_two.IsVisible = false;
                dialog_turnoff_three.IsVisible = false;
                dialog_replacemeter_one.IsVisible = false;
                dialog_logoff.IsVisible = true;
                dialog_open_bg.IsVisible = true;
                turnoff_mtu_background.IsVisible = true;
            });

        }

        public void externalReconnect(Boolean reassociate)
        {
            FormsApp.ble_interface.Open(FormsApp.peripheral, reassociate);
        }

        // Event for Menu Item selection, here we are going to handle navigation based
        // on user selection in menu ListView
        private void OnMenuItemSelectedListDevices(object sender, ItemTappedEventArgs e)
        {
            var item = (DeviceItem)e.Item;
            //fondo.Opacity = 0;
            //background_scan_page.Opacity = 0.5;
            background_scan_page.IsEnabled = false;

            #region New Circular Progress bar Animations    

            DeviceList.IsRefreshing = false;
            backdark_bg.IsVisible = true;
            indicator.IsVisible = true;

            #endregion

            bool reassociate = false;

            if (CrossSettings.Current.GetValueOrDefault("session_dynamicpass", string.Empty) != string.Empty &&
                FormsApp.credentialsService.UserName.Equals(CrossSettings.Current.GetValueOrDefault("session_username", string.Empty)) &&
                System.Convert.ToBase64String(item.Peripheral.Advertisement.ManufacturerSpecificData.ElementAt(0).Data.Take(4).ToArray()).Equals(CrossSettings.Current.GetValueOrDefault("session_peripheral_DeviceId", string.Empty)) &&
                item.Peripheral.Advertisement.DeviceName.Equals(CrossSettings.Current.GetValueOrDefault("session_peripheral", string.Empty)))
            {
                reassociate = true;
            }

            last_item = item;

            try
            {

                FormsApp.peripheral = item.Peripheral;

                externalReconnect(reassociate);

                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        deviceID.Text = item.deviceName;
                        macAddress.Text = item.deviceMacAddress;
                        imageBattery.Source = item.deviceBatteryIcon;
                        imageRssi.Source = item.deviceRssiIcon;
                        batteryLevel.Text = item.deviceBattery;
                        rssiLevel.Text = item.deviceRssi;
                    }
                    catch (Exception e4)
                    {
                        Console.WriteLine(e4.StackTrace);
                    }
                });

            }
            catch (Exception e22)
            {
                Console.WriteLine(e22.StackTrace);
            }

        }

        // Event for Menu Item selection, here we are going to handle navigation based
        // on user selection in menu ListView
        private void OnMenuItemSelected(object sender, ItemTappedEventArgs e)
        {
            if (!FormsApp.ble_interface.IsOpen())
            {
                // don't do anything if we just de-selected the row.
                if (e.Item == null) return;
                // Deselect the item.
                if (sender is ListView lv) lv.SelectedItem = null;
            }

            if (FormsApp.ble_interface.IsOpen())
            {
                navigationDrawerList.SelectedItem = null;
                try
                {
                    var item = (PageItem)e.Item;
                    String page = item.TargetType;

                    ((ListView)sender).SelectedItem = null;

                    switch (page)
                    {
                        case "ReadMTU":
                            NavigationController("ReadMTU");
                            //OnCaseReadMTU();
                            break;

                        case "AddMTU":
                            NavigationController("AddMTU");
                            //OnCaseAddMTU();
                            break;

                        case "turnOff":
                            NavigationController("turnOff");
                            //OnCaseTurnOff();
                            break;

                        case "InstallConfirm":
                            NavigationController("InstallConfirm");
                            //OnCaseInstallConfirm();
                            break;

                        case "replaceMTU":
                            NavigationController("replaceMTU");
                            //OnCaseReplaceMTU();
                            break;

                        case "replaceMeter":
                            //Application.Current.MainPage.DisplayAlert("Alert", "Feature not available", "Ok");
                            NavigationController("replaceMeter");
                            //OnCaseReplaceMeter();
                            break;

                        case "AddMTUAddMeter":
                            //Application.Current.MainPage.DisplayAlert("Alert", "Feature not available", "Ok");
                            NavigationController("AddMTUAddMeter");
                            //OnCaseAddMTUAddMeter();
                            break;

                        case "AddMTUReplaceMeter":
                            //Application.Current.MainPage.DisplayAlert("Alert", "Feature not available", "Ok");
                            NavigationController("AddMTUReplaceMeter");
                            //OnCaseAddMTUReplaceMeter();
                            break;

                        case "ReplaceMTUReplaceMeter":
                            //Application.Current.MainPage.DisplayAlert("Alert", "Feature not available", "Ok");
                            NavigationController("ReplaceMTUReplaceMeter");
                            //OnCaseReplaceMTUReplaceMeter();
                            break;
                    }
                }
                catch (Exception w1)
                {
                    Console.WriteLine(w1.StackTrace);
                }
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("Alert", "Connect to a device and retry", "Ok");
            }
        }

        private void NavigationController(string page)
        {
            switch (page)
            {
                case "ReadMTU":

                    #region Read Mtu Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            navigationDrawerList.SelectedItem = null;

                            Application.Current.MainPage.Navigation.PushAsync(new AclaraViewReadMTU(dialogsSaved), false);

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }
                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; // if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        })
                    );

                    #endregion

                    break;

                case "AddMTU":

                    #region Add Mtu Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;
                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_turnoff_one.IsVisible = false;
                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;
                            dialog_replacemeter_one.IsVisible = false;
                            dialog_meter_replace_one.IsVisible = false;

                            dialog_AddMTUAddMeter.IsVisible = false;
                            dialog_AddMTUReplaceMeter.IsVisible = false;
                            dialog_ReplaceMTUReplaceMeter.IsVisible = false;

                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_AddMTU.IsVisible = true;
                            else
                                CallLoadViewAddMtu();

                            #endregion

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }

                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone;
                        })
                    );

                    #endregion

                    break;

                case "turnOff":

                    #region Turn Off Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_meter_replace_one.IsVisible = false;

                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_turnoff_one.IsVisible = true;
                            else
                                CallLoadViewTurnOff();

                            #endregion

                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;
                            dialog_replacemeter_one.IsVisible = false;

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }

                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone;
                        })
                    );

                    #endregion

                    break;

                case "InstallConfirm":

                    #region Install Confirm Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            navigationDrawerList.SelectedItem = null;

                            Application.Current.MainPage.Navigation.PushAsync(new AclaraViewInstallConfirmation(dialogsSaved), false);

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }
                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone;
                        })
                    );

                    #endregion

                    break;

                case "replaceMTU":

                    #region Replace Mtu Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_meter_replace_one.IsVisible = false;
                            dialog_turnoff_one.IsVisible = false;
                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;

                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_replacemeter_one.IsVisible = true;
                            else
                                CallLoadViewReplaceMtu();

                            #endregion

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }

                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; //if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        })
                    );

                    #endregion

                    break;

                case "replaceMeter":

                    #region Replace Meter Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }
                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_turnoff_one.IsVisible = false;
                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;
                            dialog_replacemeter_one.IsVisible = false;


                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_meter_replace_one.IsVisible = true;
                            else
                                CallLoadViewReplaceMeter();

                            #endregion

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }
                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; // if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        })
                    );

                    #endregion

                    break;

                case "AddMTUAddMeter":

                    #region Add Mtu | Add Meter Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_turnoff_one.IsVisible = false;
                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;
                            dialog_replacemeter_one.IsVisible = false;
                            dialog_meter_replace_one.IsVisible = false;

                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_AddMTUAddMeter.IsVisible = true;
                            else
                                CallLoadViewAddMTUAddMeter();

                            #endregion

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }
                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; // if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        })
                    );

                    #endregion

                    break;

                case "AddMTUReplaceMeter":

                    #region Add Mtu | Replace Meter Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_turnoff_one.IsVisible = false;
                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;
                            dialog_replacemeter_one.IsVisible = false;
                            dialog_meter_replace_one.IsVisible = false;
                            dialog_AddMTUAddMeter.IsVisible = false;

                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_AddMTUReplaceMeter.IsVisible = true;
                            else
                                CallLoadViewAddMTUReplaceMeter();

                            #endregion

                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }
                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; // if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        })
                    );

                    #endregion

                    break;

                case "ReplaceMTUReplaceMeter":

                    #region Replace Mtu | Replace Meter Controller

                    background_scan_page.Opacity = 1;
                    background_scan_page_detail.Opacity = 1;

                    background_scan_page.IsEnabled = true;
                    background_scan_page_detail.IsEnabled = true;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                        shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
                    }

                    Task.Delay(200).ContinueWith(t =>

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            dialog_open_bg.IsVisible = true;
                            turnoff_mtu_background.IsVisible = true;
                            dialog_turnoff_one.IsVisible = false;
                            dialog_turnoff_two.IsVisible = false;
                            dialog_turnoff_three.IsVisible = false;
                            dialog_replacemeter_one.IsVisible = false;
                            dialog_meter_replace_one.IsVisible = false;
                            dialog_AddMTUAddMeter.IsVisible = false;
                            dialog_AddMTUReplaceMeter.IsVisible = false;

                            #region Check ActionVerify

                            if (FormsApp.config.global.ActionVerify)
                                dialog_ReplaceMTUReplaceMeter.IsVisible = true;
                            else
                                CallLoadViewReplaceMTUReplaceMeter();

                            #endregion


                            background_scan_page.Opacity = 1;
                            background_scan_page_detail.Opacity = 1;

                            if (Device.Idiom == TargetIdiom.Tablet)
                            {
                                ContentNav.Opacity = 1;
                                ContentNav.IsVisible = true;
                            }
                            else
                            {
                                ContentNav.Opacity = 0;
                                ContentNav.IsVisible = false;
                            }
                            shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; // if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        })
                    );

                    #endregion

                    break;

            }
        }

        private void CallLoadViewReplaceMTUReplaceMeter()
        {
            dialog_ReplaceMTUReplaceMeter.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "ReplaceMTUReplaceMeter";
                Task.Factory.StartNew(BasicReadThread);
            });

        }

        private void CallLoadViewAddMTUReplaceMeter()
        {
            dialog_AddMTUReplaceMeter.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "AddMTUReplaceMeter";
                Task.Factory.StartNew(BasicReadThread);
            });
        }

        private void CallLoadViewAddMTUAddMeter()
        {

            dialog_AddMTUAddMeter.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "AddMTUAddMeter";
                Task.Factory.StartNew(BasicReadThread);
            });

        }

        private void CallLoadViewReplaceMeter()
        {
            dialog_meter_replace_one.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "replaceMeter";
                Task.Factory.StartNew(BasicReadThread);
            });
        }

        private void CallLoadViewReplaceMtu()
        {
            dialog_replacemeter_one.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;


            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "replaceMTU";
                Task.Factory.StartNew(BasicReadThread);
            });

        }

        private void CallLoadViewTurnOff()
        {
            dialog_turnoff_one.IsVisible = false;
            dialog_turnoff_two.IsVisible = true;

            Task.Factory.StartNew(TurnOffMethod);
        }

        private void CallLoadViewAddMtu()
        {
            dialog_AddMTU.IsVisible = false;
            dialog_open_bg.IsVisible = false;
            turnoff_mtu_background.IsVisible = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                page_to_controller = "AddMTU";
                Task.Factory.StartNew(BasicReadThread);
            });
        }



        private void OpenSettingsTapped(object sender, EventArgs e)
        {
            printer.Suspend();
            background_scan_page.Opacity = 1;
            background_scan_page_detail.Opacity = 1;
            background_scan_page.IsEnabled = true;
            background_scan_page_detail.IsEnabled = true;

            if (Device.Idiom == TargetIdiom.Phone)
            {
                ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
                shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
            }

            Task.Delay(200).ContinueWith(t =>
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (FormsApp.ble_interface.IsOpen())
                    {
                        Application.Current.MainPage.Navigation.PushAsync(new AclaraViewSettings(dialogsSaved), false);
                        if (Device.Idiom == TargetIdiom.Tablet)
                        {
                            ContentNav.Opacity = 1;
                            ContentNav.IsVisible = true;
                        }
                        else
                        {
                            ContentNav.Opacity = 0;
                            ContentNav.IsVisible = false;
                        }
                        background_scan_page.Opacity = 1;
                        background_scan_page_detail.Opacity = 1;

                        shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; //   if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false;
                        return;
                    }
                    else
                    {
                        Application.Current.MainPage.Navigation.PushAsync(new AclaraViewSettings(true), false);

                        if (Device.Idiom == TargetIdiom.Tablet)
                        {
                            ContentNav.Opacity = 1;
                            ContentNav.IsVisible = true;
                        }
                        else
                        {
                            ContentNav.Opacity = 0;
                            ContentNav.IsVisible = false;
                        }

                        background_scan_page.Opacity = 1;
                        background_scan_page_detail.Opacity = 1;

                        shadoweffect.IsVisible &= Device.Idiom != TargetIdiom.Phone; // if (Device.Idiom == TargetIdiom.Phone) shadoweffect.IsVisible = false; 
                    }
                }
                catch (Exception i2)
                {
                    Console.WriteLine(i2.StackTrace);
                }
            }));
        }

        private void SideMenuOpen(object sender, EventArgs e)
        {
            fondo.Opacity = 0;
            ContentNav.IsVisible = true;
            shadoweffect.IsVisible = true;
            background_scan_page.Opacity = 0.5;
            background_scan_page_detail.Opacity = 0.5;
            ContentNav.Opacity = 1;
            ContentNav.TranslateTo(0, 0, 175, Easing.SinIn);
            shadoweffect.TranslateTo(0, 0, 175, Easing.SinIn);
            background_scan_page.IsEnabled = false;
            background_scan_page_detail.IsEnabled = false;
        }

        private void SideMenuClose(object sender, EventArgs e)
        {
            fondo.Opacity = 1;
            ContentNav.TranslateTo(-310, 0, 175, Easing.SinOut);
            shadoweffect.TranslateTo(-310, 0, 175, Easing.SinOut);
            background_scan_page.Opacity = 1;
            background_scan_page_detail.Opacity = 1;

            Task.Delay(200).ContinueWith(t =>
            Device.BeginInvokeOnMainThread(() =>
            {
                ContentNav.Opacity = 0;
                shadoweffect.IsVisible = false;
                ContentNav.IsVisible = false;
                background_scan_page.IsEnabled = true;
                background_scan_page_detail.IsEnabled = true;
            }));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // todo: this is a hack - hopefully Xamarin adds the ability to name a Pushed Page.
            //MainMenu.IsSegmentShowing = false;
            bool value = FormsApp.ble_interface.IsOpen();
            value &= Navigation.NavigationStack.Count >= 3; //  if(Navigation.NavigationStack.Count < 3) Settings.IsLoggedIn = false;
        }


        public void PrintToConsole(string printConsole)
        {

            if (DEBUG_MODE_ON)
                Console.WriteLine("DEBUG_ACL: " + printConsole);
        }


    }
}
