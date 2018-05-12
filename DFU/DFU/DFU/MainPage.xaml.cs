using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DFU
{
	public partial class MainPage : ContentPage
	{
        private string HEX;
        private bool HexFileReady= false,ReadPages = false,ReadPagesEnd=false;
        private int NumberOfPageToWrite;

        int PageCounter = 0;
        int flashPageSize = 128;
        int flashSize = 32 * 1024;
        int epromSize = 1024;
        int pageSize = 1024;
        int PageBytes = 0;
        int TaskDelay = 200;
        int shift19 = 0;
        List<byte> bleReadPage = new List<byte>() { };
        byte[] combined = new byte[200];

        string[] bootProgress = new string[2];
        private bool LoadAdressOk;

        byte[,] bytePages = new byte[60, 128];

        byte[,] ReadPageBytes = new byte[60, 128];

        public int[] bleWriteAdresses = new int[100];

        public byte[,,] bleBytes = new byte[100, 7, 20];

        public byte[,] bleLastBytes = new byte[100, 8];

        public byte[] Bytess = new byte[20];

        public byte[] Bytess2 = { (byte)(32 * 1024 & 0xff), 0x20 };

        public byte[] Bytess22 = { 0x00, 0x20 };

        public byte[] InSync = { 0x30, 0x20 };

        public byte[] EnableProgrammingMode = { 0x50, 0x20 };

        public byte[] GetParameter = { 0x41, 0x81, 0x20 };

        public byte[] LeaveProgrammingMode = { 0x51, 0x20 };

        public byte[] initWrite = { 0x64, 0x00, 0x80, 0x46 };

        public byte[] STK_READ_PAGE = { 0x74, 0x00, 0x80, 0x46, 0x20 };

        static string SET_BAUD = "AT+BAUD1";
        static string SET_BAUD4 = "AT+BAUD4";
        static string RST_PIN_LOW = "AT+PIO20";
        static string RST_PIN_HIGH = "AT+PIO21";
        static string RST_RESET = "AT+RESET";
        static System.Text.Encoding encoding = System.Text.Encoding.UTF8; //or some other, but prefer some UTF is Unicode is used
        byte[] RST_PIN_LOW_REQUEST = encoding.GetBytes(RST_PIN_LOW);
        byte[] RST_PIN_HIGH_REQUEST = encoding.GetBytes(RST_PIN_HIGH);
        byte[] SET_BAUD_REQUEST = encoding.GetBytes(SET_BAUD);
        byte[] SET_BAUD4_REQUEST = encoding.GetBytes(SET_BAUD4);
        byte[] SET_RESET = encoding.GetBytes(RST_RESET);



        IBluetoothLE ble;
        IAdapter adapter;
        IDevice device;
        IService service;
        ICharacteristic characteristic;
        private bool isConnected;
        private bool ResponseOk;

        public MainPage(string Hexfile)
		{
			InitializeComponent();
            HEX = Hexfile;
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
            adapter.DeviceConnected += Adapter_DeviceConnected;
            adapter.DeviceConnectionLost += Adapter_DeviceConnectionLost;
            adapter.DeviceDisconnected += Adapter_DeviceDisconnected;
            adapter.StartScanningForDevicesAsync();
            progressSend("0", "Ready to Run !");

            Task.Run(async () =>
            {

                while (true)
                {

                    if (adapter.IsScanning)
                    {
                        Device.BeginInvokeOnMainThread(new Action(() =>
                        {
                            BtImage.Source = "bt_state.png";   
                        }));
                        for (int i = 0; i < 5; i++)
                        {
                            if (!adapter.IsScanning)
                                break;
                            MyBut.Opacity = 0.4;
                            await MyBut.ScaleTo(2, 600, Easing.SpringIn);
                            await Task.Delay(200);
                            while (MyBut.Opacity != 0)
                            {
                                MyBut.Opacity -= 0.05;
                                await Task.Delay(20);

                            }
                            await MyBut.ScaleTo(1, 300, Easing.SpringIn);

                        }
                    }
                }
            });
        }

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

        private async void Adapter_DeviceConnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            try
            {
                Device.BeginInvokeOnMainThread(new Action(() =>
                {

                    BtImage.Source = "bt_on.png";


                }));
                service = await
                    device.GetServiceAsync(Guid.Parse                    // HM-10 servis UUID si seçldi
                    ("0000FFE0-0000-1000-8000-00805F9B34FB"));
                characteristic = await
                    service.GetCharacteristicAsync(Guid.Parse            // HM-10 RX/TX karakteristigi seçldi
                    ("0000FFE1-0000-1000-8000-00805F9B34FB"));


                await characteristic.StartUpdatesAsync();

                characteristic.ValueUpdated += async (o, args) =>              // Arduino dan gelen datalar içn bildirim servisi çğrılacak
                {

                    var bytes = args.Characteristic.Value;

                    if (ReadPages)
                    {
                        if(VerifyRead)
                        {
                            Array.Copy(bytes, 0, combined, shift19, bytes.Length);
                            shift19 += bytes.Length;
                            if (shift19 == 130)
                            {
                                for (int i = 0; i < 128; i++)
                                {
                                    ReadPageBytes[VerifyReadPageNumber, i] = combined[i + 1];

                                }
                                PageCounter++;
                                ReadPagesEnd = true;
                            }
                        }
                        else
                        {
                            Array.Copy(bytes, 0, combined, shift19, bytes.Length);
                            shift19 += bytes.Length;
                            if (shift19 == 130)
                            {
                                for (int i = 0; i < 128; i++)
                                {
                                    ReadPageBytes[PageCounter, i] = combined[i + 1];

                                }
                                PageCounter++;
                                ReadPagesEnd = true;
                            }
                        }
                       
                        //bleReadPage.Insert(jjs,(byte)(bytes.Length));
                        //jjs++;

                        //if (bytes[0] == 0x14)
                        //{
                        //    if (shift19 == 0)
                        //    {
                        //        Array.Copy(bytes, 1, combined, shift19, 19);
                        //        shift19 += 19;
                        //    }
                        //}
                        //if (shift19 > 118  && bytes.Length==10)
                        //{
                        //    Array.Copy(bytes, 0, combined, shift19, 8);
                        //    ReadPagesEnd = true;
                        //    ReadPages = false;
                        //}
                        //if (shift19 > 0 && shift19 < 119 && bytes.Length==20)
                        //{
                        //    Array.Copy(bytes, 0, combined, shift19, 20);
                        //    shift19 += 20;
                        //}

                    }
                    if (bytes[0] == 0x14 && !ReadPages)
                    {
                        if (ReadPages)
                        {

                        }
                        else
                        {
                            if (bytes.Length == 2)
                            {
                                if (bytes[1] == 0x10)
                                {
                                    ResponseOk = true;
                                }
                            }
                        }
                    }
                };            
            }
            catch { }
        }

        private void Adapter_DeviceDisconnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Device.BeginInvokeOnMainThread(new Action(() =>
            {

                BtImage.Source = "bt_off.png";


            }));
            adapter.StartScanningForDevicesAsync();
        }

        private void Adapter_DeviceConnectionLost(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceErrorEventArgs e)
        {
            Device.BeginInvokeOnMainThread(new Action(() =>
            {

                BtImage.Source = "bt_off.png";


            }));
            adapter.StartScanningForDevicesAsync();

        }
        int Salak = 5;
        public async void HexFileInitialize()
        {    
            try
            {
                CihazBilgileri2();

                HexFileReady = false;        

                string mergedHexFile=null;

                string hexFile = HEX;

                string[] Lines = hexFile.Split('\n');


                for (int n = 0; n < Lines.Length ; n++)
                {
                    if (Lines[n].Length > 9)
                    {
                        if (n == 413)
                            Salak = 5;
                        if (Lines[n].Substring(1, 2) == "00")
                            Lines[n] = "X";
                        else
                            Lines[n] = Lines[n].Substring(9, Lines[n].Length - 12);

                    }
                }


                for (int i = 0; i < Lines.Length; i++)
                {
                    if (Lines[i].Length >= 2 && !Lines[i].Contains(" "))
                        mergedHexFile += Lines[i];              
                }

                var Tv = mergedHexFile.Length % 128;
                var dd = 128 - Tv;
                for (int u = 0; u < dd; u++)
                    mergedHexFile += "0";

                NumberOfPageToWrite =
                Convert.ToInt32(Math.Ceiling
                ((double)(Lines.Length * 16) / 128));

                //HEX değişkenler decimale çvrildi.

                byte[] ss = ConvertHexStringToByteArray(mergedHexFile);

                //boş sayfalar üetildi.

                string[] pages = new string[NumberOfPageToWrite];

                //yazilcak toplam byte sayısı belirlendi.

                int sizeToWrite = Lines.Length * 16;

                //Atmega328 program page size.

                int pageSize = 128;
                int asd = ss.Length;


                //her bir boş sayfaya sırayla 128 byte yazıldı.

                for (int t = 0; t < pages.Length; t++)
                {
                    for (int x = 0 + (pageSize * t); x < pageSize + (pageSize * t); x++)
                    {
                        if (x <= ss.Length - 1)
                        {
                            pages[t] += ss[x];
                            bytePages[t, x - (pageSize * t)] = ss[x];
                        }
                        else
                        {
                            pages[t] += 0;
                            bytePages[t, x - (pageSize * t)] = 0;

                        }
                    }
                }


                //yazma işlemine başlamak içn hazırız.

                int vIndex = 0;
                for (int g = 0; g < NumberOfPageToWrite; g++)
                {
                    vIndex = 0;
                    for (int d = 0; d < 7; d++)
                    {
                        for (int j = 0; j < 20; j++)
                        {
                            if (vIndex + j < 128)
                                bleBytes[g, d, j] = bytePages[g, vIndex + j];

                        }
                        vIndex += 20;
                        if (d == 6)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                bleLastBytes[g, k] = bleBytes[g, d, k];
                            }
                        }
                    }
                }
                PageBytes = 0;
                for (int q = 0; q < NumberOfPageToWrite; q++)
                {
                    bleWriteAdresses[q] = PageBytes;
                    PageBytes += 128;
                }

                for (int offset = 0; offset <= sizeToWrite; offset += pageSize)
                {


                }
                HexFileReady = true;
            }
            catch
            {
                await DisplayAlert("Uyarı", "HEX dosya formatı bozuk veya uygun değil", "Anladım");
            }

        }

        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] HexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return HexAsBytes;
        }

        public void CihazBilgileri()
        {
            Bytess[0] = 0x42;
            Bytess[1] = 0x86;
            Bytess[2] = 0;
            Bytess[3] = 0;
            Bytess[4] = 1;
            Bytess[5] = 1;
            Bytess[6] = 1;
            Bytess[7] = 1;
            Bytess[8] = 3;
            Bytess[9] = 0xff;
            Bytess[10] = 0xff;
            Bytess[11] = 0xff;
            Bytess[12] = 0xff;
            Bytess[13] = (byte)((flashPageSize >> 8) & 0x00ff);
            Bytess[14] = (byte)(flashPageSize & 0x00ff);
            Bytess[15] = (byte)((epromSize >> 8) & 0x00ff);
            Bytess[16] = (byte)(epromSize & 0x00ff);
            Bytess[17] = (byte)((flashSize >> 24) & 0xff);
            Bytess[18] = (byte)((flashSize >> 16) & 0xff);
            Bytess[19] = (byte)((flashSize >> 8) & 0xff);
        }
        public void CihazBilgileri2()
        {
            Bytess[0] = 0x42;
            Bytess[1] = 0x86;
            Bytess[2] = 0;
            Bytess[3] = 0;
            Bytess[4] = 1;
            Bytess[5] = 1;
            Bytess[6] = 1;
            Bytess[7] = 1;
            Bytess[8] = 0x03;
            Bytess[9] = 0xFF;
            Bytess[10] = 0xFF;
            Bytess[11] = 0xFF;
            Bytess[12] = 0xFF;
            Bytess[13] = 0x00;
            Bytess[14] = 0x80;
            Bytess[15] = 0x02;
            Bytess[16] = 0x00;
            Bytess[17] = 0x00;
            Bytess[18] = 0x00;
            Bytess[19] = 0x40;
        }

        public async void progressSend(string kk, string jj) //ilki yüzde iki yazi
        {
            bootProgress[0] = kk;
            bootProgress[1] = jj;
            MessagingCenter.Send<DFU.App, string[]>(Application.Current as DFU.App, "BootProgress", bootProgress);

            
            bootInfo.Text = jj;
            bootYuzdesi.Text = "%" + kk;
            double yuzde = Convert.ToDouble(kk);
            await bootPB.ProgressTo(yuzde / 100, 100, Easing.SpringIn);
        }

        private void StartBoot(object sender, EventArgs e)
        {
            //await SendValue(100, 0, 0, 0, 0);
            //BootConnect();
            STARTBOOT();  
        }
        private async void SendVal(object sender, EventArgs e)
        {
            await SendValue(100, 0, 0, 0, 0);
            //await characteristic.WriteAsync(SET_RESET);


        }
        private async void BootConnect()
        {
             await characteristic.WriteAsync(SET_BAUD_REQUEST);
            await Task.Delay(1000);
            await characteristic.WriteAsync(SET_RESET);
            await Task.Delay(2000);
            await adapter.DisconnectDeviceAsync(device);
            


            //await characteristic.WriteAsync(RST_PIN_HIGH_REQUEST);
            //await Task.Delay(150);
            //await characteristic.WriteAsync(RST_PIN_LOW_REQUEST);
            //await Task.Delay(150);
            //await characteristic.WriteAsync(RST_PIN_HIGH_REQUEST);
            //await Task.Delay(550);
        }
        async void STARTBOOT()
        {
            HexFileInitialize();
            if (HexFileReady)
            {
                
                await characteristic.WriteAsync(RST_PIN_HIGH_REQUEST);
                await Task.Delay(150);
                await characteristic.WriteAsync(RST_PIN_LOW_REQUEST);
                await Task.Delay(150);
                await characteristic.WriteAsync(RST_PIN_HIGH_REQUEST);
                await Task.Delay(550);

                for (int i = 0; i < 5; i++)
                {
                    ResponseOk = false;
                    await characteristic.WriteAsync(InSync);
                    await Task.Delay(TaskDelay);
                    if (ResponseOk)
                    {
                        progressSend("1", "Handshaking");
                        ResponseOk = false;
                        break;
                    }
                    //await characteristic.WriteAsync(InSync);
                }



                for (int i = 0; i < 5; i++)
                {
                    ResponseOk = false;
                    await characteristic.WriteAsync(Bytess);
                    await characteristic.WriteAsync(Bytess22);
                    await Task.Delay(TaskDelay);
                    if (ResponseOk)
                    {
                        progressSend("4", "Device Signature Checking");
                        ResponseOk = false;
                        break;
                    }
                    //await characteristic.WriteAsync(Bytess);
                    //await characteristic.WriteAsync(Bytess2);
                }

                for (int i = 0; i < 5; i++)
                {
                    ResponseOk = false;
                    await characteristic.WriteAsync(EnableProgrammingMode);
                    await Task.Delay(TaskDelay);
                    if (ResponseOk)
                    {
                        progressSend("5", "Enable Programming Mode");
                        ResponseOk = false;
                        break;
                    }
                    //await characteristic.WriteAsync(InSync);
                }

                BurnBoot();

                //ReadFlashPages();
            }
            async void BurnBoot()
            {
                byte[] sendBytes = new byte[20];
                byte[] sendLastBytes = new byte[9];

                progressSend("10", "Preparing Program Bytes");


                for (int m = 0; m < NumberOfPageToWrite; m++)
                {
                    int addr = bleWriteAdresses[m];
                    addr = addr >> 1;
                    byte Loww = (byte)(addr & 0xff);
                    byte High = (byte)((addr >> 8) & 0xff);
                    byte[] byyte = new byte[] { 0x55, Loww, High, 0x20 };
                    LoadAdressOk = false;
                    for (int i = 0; i < 5; i++)
                    {
                        ResponseOk = false;

                        await characteristic.WriteAsync(byyte);
                        await Task.Delay(TaskDelay);
                        if (ResponseOk)
                        {
                            LoadAdressOk = true;
                            ResponseOk = false;
                            break;
                        }
                    }
                    if (LoadAdressOk)
                    {
                        for (int v = 0; v < 5; v++)
                        {
                            ResponseOk = false;
                            for (int i = 0; i < 8; i++)
                            {
                                sendLastBytes[i] = bleLastBytes[m, i];
                            }
                            sendLastBytes[8] = 0x20;
                            await characteristic.WriteAsync(initWrite);
                            for (int n = 0; n < 6; n++)
                            {
                                for (int im = 0; im < 20; im++)
                                {
                                    sendBytes[im] = bleBytes[m, n, im];
                                }
                                await characteristic.WriteAsync(sendBytes);

                            }
                            await characteristic.WriteAsync(sendLastBytes);

                            await Task.Delay(TaskDelay);
                            if (ResponseOk)
                            {
                                progressSend((10 + m).ToString(), "Writing Program Bytes");
                                ResponseOk = false;
                                break;
                            }
                        }
                    }

                }
                ReadFlashPages();

            }

            async void ReadFlashPages()
            {
                progressSend("60", "Reading Program Bytes");

                PageCounter = 0;
                for (int m = 0; m < NumberOfPageToWrite; m++)
                {
                    int addr = bleWriteAdresses[m];
                    addr = addr >> 1;
                    byte Loww = (byte)(addr & 0xff);
                    byte High = (byte)((addr >> 8) & 0xff);
                    byte[] byyte = new byte[] { 0x55, Loww, High, 0x20 };
                    LoadAdressOk = false;
                    for (int i = 0; i < 5; i++)
                    {
                        ResponseOk = false;

                        await characteristic.WriteAsync(byyte);
                        await Task.Delay(TaskDelay);
                        if (ResponseOk)
                        {
                            LoadAdressOk = true;
                            ResponseOk = false;
                            break;
                        }
                    }
                    if (LoadAdressOk)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            ResponseOk = false;
                            ReadPages = true;
                            ReadPagesEnd = false;
                            shift19 = 0;
                            await characteristic.WriteAsync(STK_READ_PAGE);
                            await Task.Delay(TaskDelay);
                            if (ReadPagesEnd)
                            {
                                int dsw2 = 60 + m;
                                if (dsw2 <= 98)
                                {
                                    string hhs = dsw2.ToString();
                                    progressSend(hhs, "Reading Program Bytes");
                                }
                                else
                                    progressSend("98", "Verifying Bytes");


                                ReadPages = false;
                                ResponseOk = false;
                                break;
                            }
                        }

                    }


                }

                string VerifyInfo = "";
                for (int w = 0; w < NumberOfPageToWrite; w++)
                {
                    for (int f = 0; f < 128; f++)
                    {
                        if (ReadPageBytes[w, f] != bytePages[w, f])
                        {
                            VerifyInfo += w.ToString() + ".Sayfa " + f.ToString() + " Byte Hatalıdır" + Environment.NewLine;
                        }
                    }
                }

                VerifyInfo = "";
                for (int wS = 0; wS < NumberOfPageToWrite; wS++)
                {
                    for (int f = 0; f < 128; f++)
                    {
                        if (ReadPageBytes[wS, f] != bytePages[wS, f])
                        {
                            progressSend("98",wS.ToString()+".page Writing Again");

                            byte[] sendBytes = new byte[20];
                            byte[] sendLastBytes = new byte[9];

                            // Tekrar gönderim 



                            int addr = bleWriteAdresses[wS];
                            addr = addr >> 1;
                            byte Loww = (byte)(addr & 0xff);
                            byte High = (byte)((addr >> 8) & 0xff);
                            byte[] bytte = new byte[] { 0x55, Loww, High, 0x20 };
                            LoadAdressOk = false;
                            for (int i = 0; i < 5; i++)
                            {
                                ResponseOk = false;

                                await characteristic.WriteAsync(bytte);
                                await Task.Delay(TaskDelay);
                                if (ResponseOk)
                                {
                                    LoadAdressOk = true;
                                    ResponseOk = false;
                                    break;
                                }
                            }
                            if (LoadAdressOk)
                            {
                                for (int v = 0; v < 5; v++)
                                {
                                    ResponseOk = false;
                                    for (int i = 0; i < 8; i++)
                                    {
                                        sendLastBytes[i] = bleLastBytes[wS, i];
                                    }
                                    sendLastBytes[8] = 0x20;
                                    await characteristic.WriteAsync(initWrite);
                                    for (int n = 0; n < 6; n++)
                                    {
                                        for (int im = 0; im < 20; im++)
                                        {
                                            sendBytes[im] = bleBytes[wS, n, im];
                                        }
                                        await characteristic.WriteAsync(sendBytes);

                                    }
                                    await characteristic.WriteAsync(sendLastBytes);

                                    await Task.Delay(TaskDelay);
                                    if (ResponseOk)
                                    {
                                        progressSend("98",wS.ToString()+".page Wrote Again");
                                        ResponseOk = false;
                                        break;
                                    }
                                }
                            }
                            //Tekrar okuma yapılıyor
                            VerifyRead = true;
                            VerifyReadPageNumber = wS;
                            LoadAdressOk = false;
                            progressSend("98",wS.ToString()+ ".page Reading Again");

                            for (int i = 0; i < 5; i++)
                            {
                                ResponseOk = false;

                                await characteristic.WriteAsync(bytte);
                                await Task.Delay(TaskDelay);
                                if (ResponseOk)
                                {
                                    LoadAdressOk = true;
                                    ResponseOk = false;
                                    break;
                                }
                            }
                            if (LoadAdressOk)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    ResponseOk = false;
                                    ReadPages = true;
                                    ReadPagesEnd = false;
                                    shift19 = 0;
                                    await characteristic.WriteAsync(STK_READ_PAGE);
                                    await Task.Delay(TaskDelay);
                                    if (ReadPagesEnd)
                                    {
                                        VerifyRead = false;
                                        if (ReadPageBytes[wS, f] != bytePages[wS, f])
                                        {

                                        }
                                        progressSend("98", wS.ToString() +".page Read Again");
                                        ReadPages = false;
                                        ResponseOk = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                          
                for (int i = 0; i < 5; i++)
                {
                    ResponseOk = false;
                    await characteristic.WriteAsync(LeaveProgrammingMode);
                    await Task.Delay(TaskDelay);
                    if (ResponseOk)
                    {
                        progressSend("100", "Firmware succesfully upgraded! ");
                        bootInfo.TextColor = Color.LightGreen;
                        ResponseOk = false;
                        break;
                    }
                }

                //var d202 = VerifyInfo;
                //var dssd = ReadPageBytes[1, 1];
                ////await DisplayAlert("Uyarı", VerifyInfo, "Anladım");
                //await Task.Delay(2000);
                //await SendValue(100, 0, 0, 0, 0);
            }



        }

        bool VerifyRead = false;
        int VerifyReadPageNumber = 0;

        public static int[] paket = new int[10];
        
        int sendCount=0,BCC=0, FRAME_START = 133, FRAME_ESC = 233, FRAME_END = 33;

        public byte[] send = new byte[14];


        public async Task<int> SendValue(int comKind, int paket0, int paket1, int paket2, int paket3)
        {

            if (true)
            {
                try
                {
                    System.Array.Clear(paket, 0, paket.Length);


                    paket[0] = comKind;
                    paket[1] = paket0;
                    paket[2] = paket1;
                    paket[3] = paket2;
                    paket[4] = paket3;

                    BCC = 0;
                    sendCount = 0;
                    send[sendCount] = Convert.ToByte(FRAME_START);
                    sendCount++;

                    for (int k = 0; k < 10; k++)
                    {
                        if (paket[k] == FRAME_START || paket[k] == FRAME_END || paket[k] == FRAME_ESC)
                        {
                            send[sendCount] = Convert.ToByte(FRAME_ESC);
                            sendCount++;
                        }
                        BCC ^= paket[k];

                        send[sendCount] = Convert.ToByte(paket[k]);
                        sendCount++;
                    }

                    if (BCC == FRAME_START || BCC == FRAME_END || BCC == FRAME_ESC)
                    {
                        BCC ^= FRAME_ESC;

                        send[sendCount] = Convert.ToByte(FRAME_ESC);
                        sendCount++;
                    }

                    send[sendCount] = Convert.ToByte(BCC);
                    sendCount++;
                    send[sendCount] = Convert.ToByte(FRAME_END);
                    await characteristic.WriteAsync(send);
                    //sliderText.Text = send[0].ToString()+" "+send[1].ToString()+" " +
                    //send[2].ToString()+" " +send[3].ToString()+" " +
                    //send[4].ToString()+" " +send[5].ToString()+" " +
                    //send[6].ToString()+" " +send[7].ToString()+" " +
                    //send[8].ToString()+" " +send[9].ToString()+" " +
                    //send[10].ToString() + " " + send[11].ToString()+" "+
                    //send[12].ToString() + " " + send[13].ToString()
                    //;


                }
                catch
                {

                }
            }
            return 0;
        }
    }
}
