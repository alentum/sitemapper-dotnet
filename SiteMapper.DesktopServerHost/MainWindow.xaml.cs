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
using SiteMapper.ServerHost;
using SiteMapper.ServerDAL;

namespace SiteMapper.DesktopServerHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MappingServerHost _host;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusLabel.Content = "Starting...";

            _host = new MappingServerHost(new FileSiteRepository());

            if (!_host.Open("http://localhost:9000/"))
            {
                MessageBox.Show("Cannot start the server", "Server", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusLabel.Content = "Cannot start the server";
                return;
            }

            StatusLabel.Content = "Server is started. Close this window to stop the server.";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _host.Close();
        }

    }
}
