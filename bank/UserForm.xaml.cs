using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

//
// for Graphics Bitmap ...
//https://stackoverflow.com/questions/54159041/bitmap-could-not-be-found
//
//using System.Drawing.Common;
using System.Drawing;
//using System.Drawing.Imaging;
using System.Windows.Interop;

//
// for file IO
//
using System.IO;
using System.Runtime.InteropServices;
//using Microsoft.ReportingServices.QueryDesigners.Interop;

//
// for HashTable
//
using System.Collections;

//
// for C# thread ... wait signal
//
using System.Threading;

//
// for QRcode / Barcode
//https://ithelp.ithome.com.tw/articles/10217146
//
using ZXing;                  // for BarcodeWriter
using ZXing.QrCode;           // for QRCode Engine
using ZXing.Common;           // for bitmap convertion

namespace bank
{
    /// <summary>
    /// ...
    /// </summary>
    /// 
    
    public partial class UserForm : Window
    {
        //
        // for mouse position (desktop coordinate)
        //
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator System.Windows.Point(POINT point)
            {
                return new System.Windows.Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static System.Windows.Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);

            return lpPoint;
        }

        //
        // member var
        //
        // who (accounts) are using
        private static HashSet<uint> usage = new HashSet<uint>();// currently using accounts !!! static initialize from here
        // bank
        //private string name;
        private Bank bank;
        // user
        private uint id;
        // timer
        private System.Windows.Threading.DispatcherTimer myTimer;
        // transfer
        private CountdownEvent key;
        private uint Input;
        private uint Amount;
        // image
        private Bitmap bp;
        private Graphics gh;
        private BitmapImage sc;
        // QRcode
        private ZXing.BarcodeWriter writer;
        private Result result;
        // login switch
        Grid g;

        //
        // constructor
        //

        // Static constructor is called at most one time, before any
        // instance constructor is invoked or member is accessed.
        static UserForm()
        {
            //...
        }

        // non-static constructor
        public UserForm(Bank bk)
        {
            InitializeComponent();

            // initializing var
            //name = s;
            this.Title = bk.get_name;
            bank = bk;
            key = new CountdownEvent(1);

            // showing  Login page
            swithchPage(Function_Page, Login_Page);
        }

        //
        // memeber function
        //

        private void swithchPage(Grid preGrid, Grid nextGrid)
        {
            // switching page by shifting among big canvas
            preGrid.Visibility = Visibility.Collapsed;
            g = nextGrid;
            Canvas.SetLeft(g, 0);
            Canvas.SetTop(g, 0);
            nextGrid.Visibility = Visibility.Visible;
            g.BringIntoView();
        }

        private void Login_Check(object sender, RoutedEventArgs e)
        {
            //System.Threading.Thread.Sleep(3000);

            // input checking
            //https://learn.microsoft.com/zh-tw/dotnet/api/system.convert.toint32?view=net-7.0#system-convert-toint32(system-string)
            //
            uint input;
            try
            {
                input = Convert.ToUInt32(login_id.Text);
            }
            catch (OverflowException)
            {
                MessageBox.Show("Range out of the UInt32", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (System.FormatException)
            {
                MessageBox.Show("Input is not unsigned int", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // checking id valid
            input = input * 1000 + bank.get_bank;
            if (bank.inquire_account(input))
            {
                if (usage.Contains(input))// static set: contain?
                {
                    MessageBox.Show("Sorry … you cannot login twice", "Notification", MessageBoxButton.OK);
                    return;
                }
            }
            else
            {
                // 換行 MultiLine
                if (MessageBox.Show("Account not existed!\nDo you want to create new one?", "Notification", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    Transfer_Info info = new Transfer_Info();
                    info.serial_A = input;
                    if (bank.remote_request("create", info) != "Success create")
                    {
                        MessageBox.Show("Sorry … something wrong", "Notification", MessageBoxButton.OK);
                        return;
                    }
                }
            }
            usage.Add(input);// adding to who is using

            // updating
            id = input / 1000;
            ac_name.Text = id.ToString();
            //ac_amount.Text = "0";

            // showing Function page
            swithchPage(Login_Page, Function_Page);

            // initialzing Function page
            Function_Page_Setting();
        }

        private void Function_Page_Setting()
        {
            // initilizing QRcode frame
            writer = new BarcodeWriter();// using (writer) { 不可
            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new QrCodeEncodingOptions
            {
                Height = 300,
                Width = 300,
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
                //CharacterSet = 語言
            };

            // initialzing timer
            myTimer = new System.Windows.Threading.DispatcherTimer();
            myTimer.Tick -= new EventHandler(scan_update);      // scanning screen update
            myTimer.Tick += new EventHandler(information_update);// information update
            //myTimer.Interval = new TimeSpan(0, 0, 1);
            myTimer.Interval = TimeSpan.FromMilliseconds(100);
            myTimer.Start();
        }

        private void Transfer_Page_Run()
        {
            // waiting for transfer signal
            key.Wait();

            // loading Transfer info
            Transfer_Info info = new Transfer_Info();
            info.serial_A = id * 1000 + bank.get_bank;
            info.serial_B = Input;
            info.amount = Amount;
            MessageBox.Show(bank.remote_request("transfer", info), "Notification", MessageBoxButton.OK);

            // showing Function page
            // !!! thread call control object of main thread !!!
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                // textbox
                amount_transfer.Text = null;

                // Function page
                swithchPage(Transfer_Page, Function_Page);
            });
        }

        private void Transfer_Click(object sender, RoutedEventArgs e)
        {
            // input checking
            //https://learn.microsoft.com/zh-tw/dotnet/api/system.convert.toint32?view=net-7.0#system-convert-toint32(system-string)
            //
            //uint Amount;
            try
            {
                Amount = Convert.ToUInt32(amount_transfer.Text);
            }
            catch (OverflowException)
            {
                MessageBox.Show("Range out of the UInt32", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (System.FormatException)
            {
                MessageBox.Show("Amount is not unsigned int", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            finally
            {
                //
            }

            // returing
            key.Signal();
        }

        private void show_frame()
        {
            // bp is null ...
            if (bp == null) return;

            // showing frame
            //
            // ... how to "using"
            // !!! have to be IDispose class
            //https://learn.microsoft.com/zh-tw/dotnet/standard/garbage-collection/using-objects
            using (MemoryStream memory = new MemoryStream())
            {
                bp.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                sc = new BitmapImage();
                sc.BeginInit();
                sc.StreamSource = memory;
                sc.CacheOption = BitmapCacheOption.OnLoad;
                sc.EndInit();
            }
            Frame.Source = sc;
        }

        private void Click_Scan(object sender, RoutedEventArgs e)
        {
            // += twice will trigger twice
            //https://stackoverflow.com/questions/5180695/c-sharp-event-handler-being-added-twice
            // begin scanning
            myTimer.Tick += new EventHandler(scan_update);
            //myTimer.Tick -= new EventHandler(timer_update);
        }

        private void Click_Recieve(object sender, RoutedEventArgs e)
        {
            // stop scanning
            myTimer.Tick -= new EventHandler(scan_update);

            // writing QRcode
            bp = writer.Write((id * 1000 + bank.get_bank).ToString());

            // showing QRcode
            show_frame();
        }
        
        // timer function for updating account information
        private void information_update(object sender, EventArgs e)
        {
            Account ac = bank.update_info(id * 1000 + bank.get_bank);
            if (ac == null) return;
            ac_amount.Text = ac.account_amount.ToString();
        }

        // timer function for QRcode scanning
        private void scan_update(object sender, EventArgs e)
        {
            // copying from screen
            bp = new Bitmap(600, 600);
            gh = Graphics.FromImage(bp);
            System.Windows.Point wpt;// = this.PointToScreen(Mouse.GetPosition(this));// NON
            wpt = GetCursorPosition();
            gh.CopyFromScreen(new System.Drawing.Point((int)wpt.X - 300, (int)wpt.Y - 300), new System.Drawing.Point(0, 0), bp.Size);

            // showing frame copied from screen
            show_frame();

            // QRcode checking and extrating
            //
            //LuminanceSource source = new BitmapLuminanceSource(bp);
            //BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
            //result = new MultiFormatReader().decode(bitmap);
            result = new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(new BitmapLuminanceSource(bp))));
            if (result != null)
            {
                // input checking
                //https://learn.microsoft.com/zh-tw/dotnet/api/system.convert.toint32?view=net-7.0#system-convert-toint32(system-string)
                uint input;
                try
                {
                    input = Convert.ToUInt32(result.Text);
                }
                catch (OverflowException)
                {
                    //MessageBox.Show("Range out of the UInt32", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                catch (System.FormatException)
                {
                    //MessageBox.Show("Input is not unsigned int", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                finally
                {
                    result = null;
                }
                // account checking
                if (bank.inquire_account(input))
                {
                    // stop scanning
                    myTimer.Tick -= new EventHandler(scan_update);

                    // showing Transfer page
                    swithchPage(Function_Page, Transfer_Page);
                    bank_transfer.Text = (input % 1000).ToString();
                    id_transfer.Text = (input / 1000).ToString();
                    //
                    Input = input;

                    // starting Transfer thread
                    Thread td = new Thread(new ThreadStart(Transfer_Page_Run));
                    td.Start();

                }
            }
        }

        private void Final(object sender, EventArgs e)
        {
            // deleting "who is using"
            usage.Remove(id * 1000 + bank.get_bank);
        }
    }
}

