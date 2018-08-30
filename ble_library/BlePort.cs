﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nexus.protocols.ble;
using nexus.protocols.ble.gatt;
using nexus.protocols.ble.scan;
using nexus.protocols.ble.scan.advertisement;
using Xamarin.Forms;
using System.Security.Cryptography;
using System.IO;

namespace ble_library
{
    /*
    ObserverReporter Class.
    Contains all methods that allow to know the connection status
    */
    public class ObserverReporter : IObserver<ConnectionState>
    {
        private IDisposable unsubscriber;
        private BlePort blePort;

        public ObserverReporter(BlePort port)
        {
            blePort = port;
        }

        public virtual void Subscribe(IObservable<ConnectionState> provider)
        {
            unsubscriber = provider.Subscribe(this);
        }

        public virtual void Unsubscribe()
        {
            unsubscriber.Dispose();
        }

        public virtual void OnCompleted()
        {
            Console.WriteLine("Status Report Completed");
        }

        public virtual void OnError(Exception error)
        {

        }

        public void OnNext(ConnectionState value)
        {
            Console.WriteLine("Status: " + value.ToString());

            if (value == ConnectionState.Disconnected)
            {
                //dialogs.Toast("Device disconnected");
                try
                {
                    Task.Factory.StartNew(blePort.DisconnectDevice).Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
    }

    public class BlePort
    {
        private Queue<byte> buffer_ble_data;
        private Queue<byte> buffer_aes;

        private IBluetoothLowEnergyAdapter adapter;
        private IBleGattServerConnection gattServer_connection;
        private IDisposable Listen_aes_conection_Handler;
        private IDisposable Listen_Characteristic_Notification_Handler;


        private Boolean isConnected;
        private List<IBlePeripheral> BlePeripheralList;

        private byte[] dynamicPass;
        private bool isCiphered = true;
        private bool busy;
        private int cipheredDataSentCounter;

        //private ArrayList ListAllServices;
        //private ArrayList ListAllCharacteristics;

        /// <summary>
        /// Initizalize Bluetooth LE Serial Port
        /// </summary>
        /// <param name="adapter_app">The Bluetooth Low Energy Adapter from the OS</param>
        public BlePort(IBluetoothLowEnergyAdapter adapter_app)
        {
            adapter = adapter_app;
            buffer_ble_data = new Queue<byte>();
            isConnected = false;
            busy = false;
            cipheredDataSentCounter = 1;
        }

        /// <summary>
        /// Returns the Connection status with the Bluetooth device
        /// </summary>
        /// <returns>The Bluetooth connection status.</returns>
        public Boolean GetConnectionStatus()
        {
            return isConnected;
        }
             
        /// <summary>
        /// Returns the byte array from buffer and drops the element out of the queue
        /// </summary>
        /// <returns>The byte array from the buffer that is dropped out the queue</returns>
        public byte GetBufferElement()
        {
            return buffer_ble_data.Dequeue();
        }

        /// <summary>
        /// Returns the number of bytes to read from the buffer
        /// </summary>
        /// <returns>The number of bytes to read from the buffer</returns>
        public int BytesToRead()
        {
            return buffer_ble_data.Count;
        }

        /// <summary>
        /// Clears the buffer queue
        /// </summary>
        public void ClearBuffer()
        {
            buffer_ble_data.Clear();
        }     


        /// <summary>
        /// Returns the Bluetooth LE Peripherals detected by the scan
        /// </summary>
        /// <returns>The Bluetooth LE periphals around the scanning device</returns>
        public List<IBlePeripheral> GetBlePeripherals()
        {
            return BlePeripheralList;
        }

        /// <summary>
        /// If bluetooth antenna is enabled on device, starts scanning devices. If not, turns it on, and proceeds to scan.
        /// </summary>
        public void StartScan(){
            
             Device.StartTimer(
             TimeSpan.FromSeconds(1),
             () =>
             {

                 Task.Factory.StartNew(BluetoothEnable);

                 if (adapter.CurrentState.IsEnabledOrEnabling())
                 {
                    Task.Factory.StartNew(ScanForBroadcasts);
                 }
                 return false;
             });

        }

        /// <summary>
        /// Enables bluetooth antenna on device.
        /// </summary>
        private async Task BluetoothEnable()
        {
            if (adapter.AdapterCanBeEnabled && adapter.CurrentState.IsDisabledOrDisabling())
            {
                await adapter.EnableAdapter();
            }
        }

        /// <summary>
        /// Listen to the characteristic notifications of a peripheral
        /// </summary>
        private void Listen_Characteristic_Notification()
        {
            try
            {         
                // Will also stop listening when gattServer
                // is disconnected, so if that is acceptable,
                // you don't need to store this disposable.

                Listen_Characteristic_Notification_Handler = gattServer_connection.NotifyCharacteristicValue(
                   new Guid("2cf42000-7992-4d24-b05d-1effd0381208"),
                   new Guid("00000003-0000-1000-8000-00805f9b34fb"),
                   UpdateBuffer                 
                );

            }
            catch (GattException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Stops listening to the characteristic notifications of a peripheral
        /// </summary>
        private  void Stop_Listen_Characteristic_Notification()
        {
            try{
                Listen_Characteristic_Notification_Handler.Dispose();
            }catch(Exception e){
                Console.WriteLine(e.StackTrace);  
            }
        }


        /// <summary>
        /// Writes a number of bytes via Bluetooth LE to the peripheral gatt connnection
        /// </summary>
        /// <param name="buffer">The byte array to write the input to.</param>
        /// <param name="offset">The offset in buffer at which to write the bytes.</param>
        /// <param name="count">The maximum number of bytes to read. Fewer bytes are read if count is greater than the number of bytes in the input buffer.</param>
        public async Task Write_Characteristic(byte[] buffer, int offset, int count)
        {
            try
            {               
                byte[] ret = new byte[count];
              
                for (int i = 0; i < count; i++){
                    ret[i] = buffer[i + offset];
                }

                if(isCiphered){
                    int header = 3; int bufferSize = 16;
                    byte[] fillzeros = ret.Skip(header).Take(count - header).ToArray();
                    int zerosPadding = bufferSize - buffer[2]; 

                    for (int i = 0; i < zerosPadding; i++ )
                    {
                        byte [] temp = fillzeros.Concat(new byte[] { 0x00 }).ToArray();
                        fillzeros = temp;
                    }

                    ret = new byte[] { 0x02, Convert.ToByte(cipheredDataSentCounter.ToString(), 16), buffer[2] }.ToArray().Concat(AES_Encrypt(fillzeros, dynamicPass)).Concat(new byte[] { 0x00 }).ToArray();
                                 
                }

   
                await gattServer_connection.WriteCharacteristicValue(
                    new Guid("2cf42000-7992-4d24-b05d-1effd0381208"),
                    new Guid("00000002-0000-1000-8000-00805f9b34fb"),
                    ret
                );
                cipheredDataSentCounter++;
            }
            catch (GattException ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        /// <summary>
        /// Updates buffer with the notification data received 
        /// </summary>
        private void UpdateBuffer(byte[] bytes )
        {
            byte[] tempArray = new byte[bytes[2]];

            if(isCiphered){
                Array.Copy(AES_Decrypt(bytes.Skip(3).Take(16).ToArray(), dynamicPass), 0, tempArray, 0, bytes[2]);
            }else{
                Array.Copy(bytes, 3, tempArray, 0, bytes[2]);
            }
           
            for (int i = 0; i < tempArray.Length; i++)
            {
                buffer_ble_data.Enqueue(tempArray[i]);
            }
         }
     
        /// <summary>
        /// Updates buffer with the notification data received 
        /// </summary>
        /// <param name="ble_device">The Bluetooth LE peripheral to connect.</param>
        public async Task ConnectoToDevice(IBlePeripheral ble_device){
            var connection = await adapter.ConnectToDevice(
                // The IBlePeripheral to connect to
                ble_device,
                // TimeSpan or CancellationToken to stop the
                // connection attempt.
                // If you omit this argument, it will use
                // BluetoothLowEnergyUtils.DefaultConnectionTimeout
                TimeSpan.FromSeconds(15),
                // Optional IProgress<ConnectionProgress>
                progress => {
                    Console.WriteLine(progress);
                    //dialogs.Toast("Progreso: " + progress.ToString());
                }
            );

            if (connection.IsSuccessful())
            {
                gattServer_connection = connection.GattServer;

                Console.WriteLine(gattServer_connection.State); // e.g. ConnectionState.Connected
                                                                // the server implements IObservable<ConnectionState> so you can subscribe to its state

                gattServer_connection.Subscribe(new ObserverReporter(this));                
               

                // TO-DO: comprobar que tiene servicios y caracteristicas de un PUK? consultar Maria.
                /*
                try
                {
                    ListAllServices = new ArrayList();
                    ListAllCharacteristics = new ArrayList();

                    foreach (var guid in await gattServer_connection.ListAllServices())
                    {
                        ListAllServices.Add(guid);
                        ListAllCharacteristics.Add("_______________________________");                  
                        ListAllCharacteristics.Add("Service: " + "\n\r" + guid + "\n\r");
                        ListAllCharacteristics.Add("________Caracteristics_________");                  
                        foreach (var DescriptionOrGuid in await gattServer_connection.ListServiceCharacteristics(guid))
                        {
                            ListAllCharacteristics.Add(DescriptionOrGuid);
                        }

                    }

                }catch(Exception j){
                    
                }
                */
                await AESConnectionVerifyAsync();
            }
            else
            {
                // Do something to inform user or otherwise handle unsuccessful connection.
                Console.WriteLine("Error connecting to device. result={0:g}", connection.ConnectionResult);
                // e.g., "Error connecting to device. result=ConnectionAttemptCancelled"
                isConnected = false;
            }

        }

        /// <summary>
        /// Updates AES buffer with the notification data received 
        /// </summary>
        private void UpdateAESBuffer(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                buffer_aes.Enqueue(bytes[i]);
            }
        }

        /// <summary>
        /// AES Verification to connect Bluetooth LE peripheral 
        /// </summary>
        private async Task AESConnectionVerifyAsync()
        {
            byte [] static_pass = { 0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x20, 0x74, 0x68, 0x65, 0x20, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6f, 0x72, 0x64, 0x20, 0x66, 0x6f, 0x72, 0x20, 0x41, 0x63, 0x6c, 0x61, 0x72, 0x61, 0x2e };

            buffer_aes = new Queue<byte>();
            try
            {
                // Will also stop listening when gattServer
                // is disconnected, so if that is acceptable,
                // you don't need to store this disposable.
                Listen_Characteristic_Notification();

                Listen_aes_conection_Handler = gattServer_connection.NotifyCharacteristicValue(
                   new Guid("ba792500-13d9-409b-8abb-48893a06dc7d"),
                   new Guid("00000041-0000-1000-8000-00805f9b34fb"),
                   UpdateAESBuffer
                );


                //Read Pass H data from Characteristic
                byte [] PassH_crypt = await gattServer_connection.ReadCharacteristicValue(
                    new Guid("ba792500-13d9-409b-8abb-48893a06dc7d"),
                    new Guid("00000040-0000-1000-8000-00805f9b34fb")
                );

       
                //Read Pass L data from Characteristic
                byte[] PassL_crypt = await gattServer_connection.ReadCharacteristicValue(
                    new Guid("ba792500-13d9-409b-8abb-48893a06dc7d"),
                    new Guid("00000042-0000-1000-8000-00805f9b34fb")
                );
               
                byte[] PassH_decrypt = AES_Decrypt(PassH_crypt, static_pass);
                byte[] PassL_decrypt = AES_Decrypt(PassL_crypt, static_pass);

                //Generate dynamic password
                dynamicPass = new byte[PassH_decrypt.Length + PassL_decrypt.Length];

                Array.Copy(PassH_decrypt, 0, dynamicPass, 0, PassH_decrypt.Length);
                Array.Copy(PassL_decrypt, 0, dynamicPass, PassH_decrypt.Length, PassL_decrypt.Length);

                byte [] say_hi =  {0x48, 0x69, 0x2c, 0x20, 0x49, 0x27, 0x6d, 0x20, 0x41, 0x63, 0x6c, 0x61, 0x72, 0x61, 0x00, 0x00};

                byte[] hi_msg = AES_Encrypt(say_hi, dynamicPass);

                await gattServer_connection.WriteCharacteristicValue(
                  new Guid("ba792500-13d9-409b-8abb-48893a06dc7d"),
                  new Guid("00000041-0000-1000-8000-00805f9b34fb"),
                  hi_msg
                );

                isConnected = true;


                bool isPairing = true;


                for (int i = 0; i < buffer_aes.Count; i++)
                {
                    isPairing &= buffer_aes.Take(buffer_aes.Count).ToArray()[i].Equals(0x11); // if (!buffer_aes.Take(buffer_aes.Count).ToArray()[i].Equals(0x11)) isCiphered = false;
                }

                if(isPairing)
                {
                    // TO-DO
                    string encoded = System.Convert.ToBase64String(dynamicPass);
                    // HERE GOES THE - SAVE DYNAMIC PASS TO PREFERENCES STORAGE

                    // YOU CAN RETURN THE PASS BY GETTING THE STRING AND CONVERTING IT TO BYTE ARRAY TO AUTO-PAIR
                    byte[] bytes = System.Convert.FromBase64String(encoded);
                }

            }
            catch (GattException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// AES Decryptation algorithm
        /// </summary>
        private byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            //byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Padding = PaddingMode.None;
                    AES.Key = passwordBytes; 
                    //var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    //AES.Key = key.GetBytes(AES.KeySize / 8);
                    //AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.ECB;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        /// <summary>
        /// AES Encryptation algorithm
        /// </summary>
        private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            //byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Padding = PaddingMode.None;
                    AES.Key = passwordBytes; 
                    //var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    //AES.Key = key.GetBytes(AES.KeySize / 8);
                    //AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.ECB;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        /// <summary>
        /// Disconnects from Bluetooth LE peripheral 
        /// </summary>
        public async Task DisconnectDevice()
        {
            if (isConnected)
            {
                Stop_Listen_Characteristic_Notification();
                await gattServer_connection.Disconnect();
                isConnected = false;
            }
        }

        /// <summary>
        /// Scans for Bluetooth LE peripheral broadcasts 
        /// </summary>
        private async Task ScanForBroadcasts()
        {
            if(!busy){
                
                BlePeripheralList = new List<IBlePeripheral> { };
                busy = true;
                await adapter.ScanForBroadcasts(
                // Optional scan filter to ensure that the
                // observer will only receive peripherals
                // that pass the filter. If you want to scan
                // for everything around, omit this argument.
                new ScanFilter()

                   .SetIgnoreRepeatBroadcasts(false),
                    // IObserver<IBlePeripheral> or Action<IBlePeripheral>
                    // will be triggered for each discovered peripheral
                    // that passes the above can filter (if provided).
                    (IBlePeripheral peripheral) =>
                    {
                    // read the advertising data...
                    var adv = peripheral.Advertisement;
                    Console.WriteLine(adv.DeviceName);

                    String serv = adv.Services
                                    .Select
                                     (x => {
                                         var name = adv.DeviceName;
                                         return name != null || name.Equals("")
                                                    ? x.ToString()
                                                    : x.ToString() + " (" + name + ")";
                                     }
                                     ).ToString();

                    serv = serv + ", ";

                    Console.WriteLine(serv);
                    Console.WriteLine(adv.ManufacturerSpecificData.FirstOrDefault().CompanyName());
                    Console.WriteLine(adv.ServiceData);

                    //Show dialog with name
                    if(adv.DeviceName!=null){
                        if ( adv.DeviceName.Contains("Aclara") || adv.DeviceName.Contains("Acl") )
                        {
                            if(BlePeripheralList.Any(p => p.DeviceId.Equals(peripheral.DeviceId)))
                            {
                                BlePeripheralList[BlePeripheralList.FindIndex(f => f.DeviceId.Equals(peripheral.DeviceId))] = peripheral;
                            }else{
                                BlePeripheralList.Add(peripheral);
                              
                            }
                        } 
                    }
                    //  connect to the device
                   },
                    // TimeSpan or CancellationToken to stop the scan
                    TimeSpan.FromSeconds(3)
                    // If you omit this argument, it will use
                    // BluetoothLowEnergyUtils.DefaultScanTimeout
                );  
            }
            busy = false;
            // scanning has been stopped when code reached this point
        }
    }
}