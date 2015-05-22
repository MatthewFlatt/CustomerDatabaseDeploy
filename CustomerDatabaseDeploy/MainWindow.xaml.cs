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
using System.Windows.Forms;

namespace CustomerDatabaseDeploy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    NugetPackage.Text = fileDialog.FileName;
                    break;
                default:
                    NugetPackage.Text = null;
                    break;
            }
        }

        private void BtnDeploy_Click(object sender, RoutedEventArgs e)
        {
            var dbDeployment = new DatabaseDeployment(this);
            dbDeployment.Deploy(ServerName.Text, DatabaseName.Text, NugetPackage.Text);
        }

        public void UpdateOutputText(string outputText)
        {
            Output.Text += outputText + Environment.NewLine;
        }

        private void ServerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableBtnDeploy();
        }

        private void DatabaseName_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableBtnDeploy();
        }

        private void NugetPackage_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableBtnDeploy();
        }

        private void EnableBtnDeploy()
        {
            if(!String.IsNullOrWhiteSpace(ServerName.Text) && !String.IsNullOrWhiteSpace(DatabaseName.Text) && !String.IsNullOrWhiteSpace(NugetPackage.Text))
            {
                BtnDeploy.IsEnabled = true;
            }
            else
            {
                BtnDeploy.IsEnabled = false;
            }
        }
    }
}
