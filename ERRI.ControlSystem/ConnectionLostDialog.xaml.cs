using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EERIL.ControlSystem
{
    /// <summary>
    /// Interaction logic for ConnectionLostDialog.xaml
    /// </summary>
    public partial class ConnectionLostDialog : Window
    {
        public bool TerminateDeployment { get; private set; }
        public ConnectionLostDialog()
        {
            InitializeComponent();
        }

        private void terminateDeploymentButton_Click(object sender, RoutedEventArgs e) {
            TerminateDeployment = true;
            Close();
        }
    }
}
