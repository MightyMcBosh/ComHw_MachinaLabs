using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Enumeration;
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

namespace Comm_HW_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            openButton.Click += OpenButton_Click;
            ProcessButton.Click += ProcessButton_Click;
            Controller.Init();
            Controller.OnProcessStepChange += Controller_OnProcessStepChange;
        }



        //Update the status string when the process step changes
        private void Controller_OnProcessStepChange(ProcessStep newProcessStep)
        {
            switch (newProcessStep)
            {
                case ProcessStep.Ready:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Ready";
                        statusLabel.InvalidateVisual();
                    });

                    break;
                case ProcessStep.ReadingFile:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Reading File From Disk";
                        statusLabel.InvalidateVisual();
                    });
                    break;
                case ProcessStep.SendingFile:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Sending File To Server";
                        statusLabel.InvalidateVisual();

                        File1.Text = System.Text.Encoding.UTF8.GetString(Controller.FileData);
                        File1.InvalidateVisual();
                    });
                    break;
                case ProcessStep.Waiting:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Waiting for Server Response";
                        statusLabel.InvalidateVisual();
                    });
                    break;
                case ProcessStep.ReceivingFile:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Receiving Response From Server";
                        statusLabel.InvalidateVisual();
                    });
                    break;
                case ProcessStep.ComparingFiles:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Comparing Files to Ensure Completeness";
                        statusLabel.InvalidateVisual();
                    });
                    break;
                case ProcessStep.Complete:
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        statusLabel.Content = "Task Complete";
                        statusLabel.InvalidateVisual();

                        File2.Text = System.Text.Encoding.UTF8.GetString(Controller.ReturnedData);
                        File2.InvalidateVisual();
                    });
                    break;
            }

        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (e.RoutedEvent.Name == "Click")
            {
                var openDialog = new OpenFileDialog();
                if (openDialog.ShowDialog() == true)
                {
                    fileAddressBox.Text = openDialog.FileName;
                    if (string.IsNullOrEmpty(fileAddressBox.Text))
                    {

                    }
                }
            }
        }
        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (e.RoutedEvent.Name == "Click")
            {
                string tmp = fileAddressBox.Text;
                if (!string.IsNullOrEmpty(tmp))
                {
                    Controller.StartTask(tmp);
                }
            }
        }

        ~MainWindow()
        {
            Controller.AbortTask();
        }

        
       
    }
}
