# Device-Firmware-Update-Over-the-Air

 xamarin pcl based over the air device firmware update service for Atmega series
 
 this repository allows atmega328p chip update firmware by IOS and Android. 
 
 using HM-10 bluetooth low energy module (ble with the same characteristics and service as hm-10 can be used) 
# SETUP

## HM-10 BLE Module configuration

Serial converter used for Bluetooth module configuration

Send AT commands to HM-10 from any serial monitor program

* AT+BAUD4 //115200 baud rate
* AT+MODE2 //to use the io pin

![b21](https://user-images.githubusercontent.com/18028933/39959839-7f2c1c96-5620-11e8-871d-9b9d4ff0f47b.png)




## Copy and paste the hex file you created into the text file;

file path:

for IOS ..DFU\DFU\DFU.iOS\Assets_

for Android ..DFU\DFU\DFU.Android\Assets

![b32](https://user-images.githubusercontent.com/18028933/39961121-1ec424da-5638-11e8-9b58-26767f99f988.png)

## Create the Circuit Shown Below

![h_1](https://user-images.githubusercontent.com/18028933/39955746-1b2469e4-55dd-11e8-8578-43fef2bacea5.png)

## Your Bluetooth Module Mac Adress define here;

'''

       private async void Adapter_DeviceDiscovered(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (e.Device.NativeDevice.ToString() == "D0:5F:B8:1A:34:F0")                            
            {
                await adapter.StopScanningForDevicesAsync();        
                device = e.Device as IDevice;
                                                  
                try
                {
                    await adapter.ConnectToDeviceAsync(device);      
                }
                catch
                {
                    isConnected = false;
                }
            }
        }
        
'''
