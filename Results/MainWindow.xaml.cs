using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic; 
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media; 

namespace Results
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        CVFilesClass CVFiles;
        LoadForm loadf = new LoadForm();
         
        CollectionViewSource VieWSource;
        MaterialClass Material { get; set; }

        public SeriesCollection CVPlotCollection { get; set; }

        bool CVPlot_PlotNWresult = false;

        public string workingdirectory { get; set; }
        static public string PythonPath { get; set; }
        static public string PythonCallPath { get; set; }
        public string PythonCodePath { get; set; } 
        public bool alive = false;

        static public MainWindow ThisWindowInstance;

        public static string BufferFileName = "BufferFile.txt";
        public static bool BufferFileCreated = false;

        public MainWindow()
        {

            InitializeComponent();
             
            VieWSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource"));
            DataContext = this;
            workingdirectory = Environment.CurrentDirectory; 
            ThisWindowInstance = this; 

            alive = true;
        }

         
        private void MainWindowDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Exit?", "Question", MessageBoxButton.YesNo, MessageBoxImage.None);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
           
        public delegate void SetStatusTextDelegate(string text);

        public void SetStatusText(string text)
        {
            Status.Text = text;
        }
               
        public MainWindow getInstance()
        {
            return this;
        }
         
    }


    }