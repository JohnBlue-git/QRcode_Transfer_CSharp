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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace bank
{
    /// <summary>
    /// ...
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        // memeber var
        //
        private Central ct;
        private Bank[] bk;
        private MenuItem[] BK;
        //private UserForm uf;

        //
        // constructor
        //
        public MainWindow()
        {
            InitializeComponent();

            // initilazing var
            ct = new Central();
            bk = new Bank[38];
            bk[0] = new Bank(ct, "JPMorgan", 0);
            bk[1] = new Bank(ct, "Bank of America", 1);
            bk[2] = new Bank(ct, "HSBC", 2);
            bk[3] = new Bank(ct, "Mitsubishi", 3);
            bk[4] = new Bank(ct, "Citigroup", 4);
            bk[5] = new Bank(ct, "ING", 5);
            bk[6] = new Bank(ct, "UBS", 6);
            bk[7] = new Bank(ct, "DBS", 7);
            BK = new MenuItem[] { Bank_0, Bank_1, Bank_2, Bank_3, Bank_4, Bank_5, Bank_6, Bank_7 };
            //uf = new UserForm(bk[0]);

            //
            // random background account generator ...
            //
            Random rnd = new Random();
            Transfer_Info info = new Transfer_Info();
            for (uint i = 0; i < 33; i++)
            {
                //info.serial_A = 3 + i * 1000;
                info.serial_A = (uint)rnd.Next() % 8 + ((uint)rnd.Next() % 1000) * 1000;
                Console.WriteLine("create ... {0}", bk[0].remote_request("create", info));
            }
            //
            // end
            //

            // binding sources to grids
            Accounts_data.ItemsSource = ct.source_account;
            Transfer_data.ItemsSource = ct.source_history;

            // initializing timer
            System.Windows.Threading.DispatcherTimer myTimer = new System.Windows.Threading.DispatcherTimer();
            myTimer.Tick += new EventHandler(grid_update);
            myTimer.Interval = new TimeSpan(0, 0, 3);
            myTimer.Start();
        }

        // timer function
        private void grid_update(object sender, EventArgs e)
        {
            Accounts_data.Items.Refresh();
            Transfer_data.Items.Refresh();

            // what is the function of this? 
            //((MainWindow)System.Windows.Application.Current.MainWindow).UpdateLayout();
        }

        // Menu function
        private void Click_MenuItem(object sender, RoutedEventArgs e)
        {
            UserForm fm;
            //MenuItem item = sender as MenuItem;
            for (uint i = 0; i < 8; i++)
            {
                if (BK[i].Equals(sender))
                {
                    fm = new UserForm(bk[i]);
                    fm.Show();
                    break;
                }
                if (i == 8)
                {
                    //https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/how-to-open-message-box?view=netdesktop-6.0
                    MessageBox.Show("Bank not existed", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        // Final()
        // trgger after Close()
        //https://stackoverflow.com/questions/59445631/what-is-the-differences-between-2-events-closing-and-closed-in-wpf-apps
        private void Final(object sender, EventArgs e)
        {
            // close all forms
            Environment.Exit(1);
        }
    }
}
