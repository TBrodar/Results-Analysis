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
        public List<RunPythonCodeClass> PythonCodeRunList = new List<RunPythonCodeClass>();

        public bool alive = false;

        static public MainWindow ThisWindowInstance;

        public MainWindow()
        {

            InitializeComponent();
            
            CVPlotCollection = new SeriesCollection();

            CVFiles = new CVFilesClass()
            {
                CVFileNames = new List<string>(),
                CVFileNamesShort = new List<string>(),
                FilesData = new List<CVDataFileClass>()
            };

            DataFiles.ItemsSource = CVFiles.CVFileNamesShort;
            CVFileNamesComboBox.ItemsSource = CVFiles.CVFileNamesShort;
            VieWSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource")); 
            DataContext = this;

            LoadConfig();

            SettingsMaterialAreaTextBox.DataContext = Material;
            SettingsMaterialMaterialNameTextBox.DataContext = Material;
            SettingsMaterialRelativePermittivityTextBox.DataContext = Material;

            ThisWindowInstance = this;
            RunPythonCodeClass.WindowInstance = this;

            CVPlotOption.SelectedIndex = 0;

            Closing += MainWindowDialog_Closing;

            alive = true;
        }


        private void LoadConfig()
        {
            workingdirectory = Environment.CurrentDirectory;
            if (File.Exists(Path.Combine(workingdirectory, "config.txt")))
            {
                List<String> lines = new List<string>();
                using (StreamReader reader = new StreamReader(Path.Combine(workingdirectory, "config.txt")))
                {
                    while (reader.Peek() > -1)
                        lines.Add(reader.ReadLine());
                }

                List<string> pythonLines = loadf.getProperties(loadf.python.python, ref lines);
                PythonPath = loadf.getValue(loadf.python.python, ref pythonLines);
                PythonCodePath = loadf.getValue(loadf.python.pythoncodepath, ref pythonLines);
                PythonCallPath = loadf.getValue(loadf.python.pythoncallpath, ref pythonLines);

                List<string> materialLines = loadf.getProperties(loadf.material.material, ref lines);
                Decimal area, varepsilon;
                bool error = false;
                if (!decimal.TryParse(loadf.getValue(loadf.material.varepsilon, ref materialLines), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out varepsilon))
                {
                    Status.Text = "Info : Problem is encountered while loading material properties (relative permittivity). Default values are used.";
                    Material = new MaterialClass
                    {
                        MaterialName = "SiC",
                        RelativePermitivity = 9.66M,
                        Area = 1.0M
                    };
                    error = true;
                }
                if (!decimal.TryParse(loadf.getValue(loadf.material.area, ref materialLines), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out area))
                {
                    Status.Text = "Info : Problem is encountered while loading material properties (area). Default values are used.";
                    Material = new MaterialClass
                    {
                        MaterialName = "SiC",
                        RelativePermitivity = 9.66M,
                        Area = 1.0M
                    };
                    error = true;
                }
                if (error == false)
                    Material = new MaterialClass
                    {
                        MaterialName = loadf.getValue(loadf.material.materialName, ref materialLines),
                        RelativePermitivity = varepsilon,
                        Area = area
                    };

                List<string> CVNWLines = loadf.getProperties(loadf.cvnw.cvnw, ref lines);
                AParameter.Text = loadf.getValue(loadf.cvnw.alpha, ref CVNWLines);
                int value;
                if (!int.TryParse(loadf.getValue(loadf.cvnw.ScaleChoice, ref CVNWLines), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    Status.Text = "Info : Problem is encountered while loading C-V->N-W properties. " + loadf.cvnw.ScaleChoice;  
                } else { 
                    ScaleComboBox.SelectedIndex = value;
                }
                if (!int.TryParse(loadf.getValue(loadf.cvnw.WIndexChoice, ref CVNWLines), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    Status.Text = "Info : Problem is encountered while loading C-V->N-W properties. " + loadf.cvnw.WIndexChoice;
                }
                else
                {
                    SmoothWCheckBox.SelectedIndex = value;
                }
                if (!int.TryParse(loadf.getValue(loadf.cvnw.CIndexChoice, ref CVNWLines), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    Status.Text = "Info : Problem is encountered while loading C-V->N-W properties. " + loadf.cvnw.CIndexChoice;
                }
                else
                {
                    CNWOptionComboBox.SelectedIndex = value;
                }
                MaxIterationsTextBox.Text = loadf.getValue(loadf.cvnw.maxIterations, ref CVNWLines);
                ZeroParameterTextBox.Text = loadf.getValue(loadf.cvnw.zeroParameter, ref CVNWLines);
                SmoothWWindowPointsTextBox.Text = loadf.getValue(loadf.cvnw.SmoothWindowPoints, ref CVNWLines);
                PolynomOrder.Text = loadf.getValue(loadf.cvnw.PolynomOrder, ref CVNWLines);
            }
            else
            {

                PythonPath = @"C:\Program Files\Anaconda3\python.exe";
                if (!File.Exists(PythonPath))
                {
                    PythonPath = @"D:\Program Files\Anaconda3\python.exe";
                    if (!File.Exists(PythonPath))
                    {
                        PythonPath = @"E:\Program Files\Anaconda3\python.exe";
                        if (!File.Exists(PythonPath))
                        {
                            PythonPath = "";
                        }
                    }
                }
                PythonCodePath = Path.Combine(workingdirectory, "PythonCode.py");
                if (!File.Exists(PythonCodePath)) PythonCodePath = "";
                PythonCallPath = Path.Combine(workingdirectory, "PythonConsoleCode.py");
                if (!File.Exists(PythonCallPath)) PythonCallPath = "";

                AParameter.Text = string.Format(CultureInfo.InvariantCulture, "{0:0E-0}/{1:0E-0}", 1E-3, 1E+3);
                MaxIterationsTextBox.Text = 1000.ToString("D", CultureInfo.InvariantCulture);
                ZeroParameterTextBox.Text = 1E-11.ToString("E2", CultureInfo.InvariantCulture);
                SmoothWWindowPointsTextBox.Text = 10.ToString("D", CultureInfo.InvariantCulture);
                PolynomOrder.Text = 3.ToString("D", CultureInfo.InvariantCulture);

                Material = new MaterialClass
                {
                    MaterialName = "SiC",
                    RelativePermitivity = 9.66M,
                    Area = 1.0M
                };
            }
        }

        private void MainWindowDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Exit?", "Question", MessageBoxButton.YesNo, MessageBoxImage.None);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {

            LDLTSPage.LDLTSPageInstance.onExit();
            foreach (RunPythonCodeClass PythonCodeRun in PythonCodeRunList)
            {
                PythonCodeRun.KillPythonCodeRun();
            } 

            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(workingdirectory, "config.txt")))
                {
                    List<string> writelines = new List<string>();
                    List<string> data = new List<string>();

                    data = loadf.setValue(loadf.material.materialName, "4H-SiC", data);
                    data = loadf.setValue(loadf.material.varepsilon, Material.RelativePermitivity.ToString("N", CultureInfo.InvariantCulture), data);
                    data = loadf.setValue(loadf.material.area, Material.Area.ToString("N", CultureInfo.InvariantCulture), data);
                    writelines = loadf.setProperties(loadf.material.material, data, writelines);

                    data.Clear();
                    data = loadf.setValue(loadf.python.python, PythonPath, data);
                    data = loadf.setValue(loadf.python.pythoncodepath, PythonCodePath, data);
                    data = loadf.setValue(loadf.python.pythoncallpath, PythonCallPath, data);
                    writelines = loadf.setProperties(loadf.python.python, data, writelines);

                    data.Clear();
                    data = loadf.setValue(loadf.cvnw.alpha, AParameter.Text, data);
                    data = loadf.setValue(loadf.cvnw.maxIterations, MaxIterationsTextBox.Text, data);
                    data = loadf.setValue(loadf.cvnw.ScaleChoice, ScaleComboBox.SelectedIndex.ToString("N"), data);
                    data = loadf.setValue(loadf.cvnw.CIndexChoice, CNWOptionComboBox.SelectedIndex.ToString("N"), data);
                    data = loadf.setValue(loadf.cvnw.WIndexChoice, SmoothWCheckBox.SelectedIndex.ToString("N"), data);
                    data = loadf.setValue(loadf.cvnw.SmoothWindowPoints, SmoothWWindowPointsTextBox.Text, data);
                    data = loadf.setValue(loadf.cvnw.PolynomOrder, PolynomOrder.Text, data);
                    data = loadf.setValue(loadf.cvnw.zeroParameter, ZeroParameterTextBox.Text, data);
                    writelines = loadf.setProperties(loadf.cvnw.cvnw, data, writelines);
                    

                    foreach (string line in writelines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {

            }
        }


        // CVNW Tab functions :
        private void LoadCVFile(object sender, RoutedEventArgs ee)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string Name in openFileDialog.FileNames)
                { 
                    string[] lines;
                    try
                    {
                        lines = System.IO.File.ReadAllLines(Name);
                    }
                    catch (Exception e)
                    {
                        Status.Text = "Error : Load file. Name : " + Name;
                        return;
                    }
                    CVDataFileClass fileData = new CVDataFileClass();

                    string ErrorText = "";
                    int ErrorNumber = 0;
                    bool ErrorFlag = false;
                    foreach (string line in lines)
                    {
                        try
                        {
                            string[] a = line.Split(' ');
                            if (a.Length == 1)
                            {
                                a = line.Split('\t');
                            }
                            else if (a.Length > 2)
                            {
                                throw new Exception();
                            }
                            if (a.Length == 1)
                            {
                                a = line.Split(',');
                            }
                            else if (a.Length > 2)
                            {
                                throw new Exception();
                            }
                            decimal y = decimal.Parse(a[1], System.Globalization.NumberStyles.Any);
                            fileData.data.Add(new CVDataFileClass.OneData()
                            {
                                XData = decimal.Parse(a[0], System.Globalization.NumberStyles.Any),
                                YData = y,
                                Y2Data = 1.0M / (y * y)
                            });
                        }
                        catch (Exception e)
                        {
                            if (ErrorNumber < 5)
                            { 
                                ErrorText += "Error : CV file syntax. File name : " + Path.GetFileName(Name) + " Line : " + line + "\n";
                                ErrorFlag = true;
                            }
                            ErrorNumber += 1;
                            break;
                        }
                    }
                    if (ErrorFlag == false) {
                        CVFiles.CVFileNames.Add(Name);
                        CVFiles.CVFileNamesShort.Add(Path.GetFileName(Name));
                        CVFiles.FilesData.Add(fileData);
                        Status.Text = "Completed : Load file.";
                        DataFiles.Items.Refresh();
                        CVFileNamesComboBox.Items.Refresh();


                        //::Check if voltage step is ok 
                        decimal Voltage0 = Convert.ToDecimal(CVFiles.FilesData[CVFiles.CVFileNames.Count - 1].data[0].XData);
                        decimal Voltage1 = Convert.ToDecimal(CVFiles.FilesData[CVFiles.CVFileNames.Count - 1].data[1].XData);
                        decimal step = Voltage1 - Voltage0;
                        CVFiles.FilesData[CVFiles.CVFileNames.Count - 1].VoltageStep = step;
                        decimal VoltageStepDeviation = 0;
                        for (int i = 0; i < CVFiles.FilesData[CVFiles.CVFileNames.Count - 1].data.Count; i++)
                        {
                            if ( Math.Abs((Voltage0 + i*step) - CVFiles.FilesData[CVFiles.CVFileNames.Count - 1].data[i].XData) > VoltageStepDeviation)
                            {
                                VoltageStepDeviation = Math.Abs((Voltage0 + i * step) - CVFiles.FilesData[CVFiles.CVFileNames.Count - 1].data[i].XData);
                            }
                        }
                        if (VoltageStepDeviation > step / 100)
                        {
                            Status.Text = string.Format("Completed : Load file. Warning : Voltage step isn\'t constant {0:E3} +/- {0:E3} ", step, VoltageStepDeviation);
                        }

                    } else
                    {
                        if (ErrorNumber > 5)
                        { 
                            Status.Text = ErrorText + "Error : Number of other files with error :" +  (ErrorNumber-5).ToString() + " .\n";
                        } else
                        {
                            Status.Text = ErrorText;
                        }
                    }
                }
            } 
        } 

        // :: Run python code
        private void Get_NW_Click(object sender, RoutedEventArgs ee)
        {
            if (AreCVParametersOk() != true) return; 
            if (CVFileNamesComboBox.SelectedIndex == -1) return;
    
            string SmoothWBool;
            if (SmoothWCheckBox.SelectedIndex == 1)
            {
                SmoothWBool = "1"; 
            } else if (SmoothWCheckBox.SelectedIndex == 0)
            {
                SmoothWBool = "0";
            }
            else
            {
                SmoothWBool = "2";
            }

            List<string> alphas = new List<string>();
            if (AParameter.Text.Contains("/"))
            {
                decimal value1, value2;
                if (!decimal.TryParse(AParameter.Text.Split('/')[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value1))
                {
                    return;
                }
                if (!decimal.TryParse(AParameter.Text.Split('/')[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value2))
                {
                    return;
                }
                if (value1 > value2)
                {
                    decimal value = value1;
                    value1 = value2;
                    value2 = value;
                }
                for (int i=1; value1*i<= value2; i*=10)
                {
                    alphas.Add(string.Format("{0:E2}", value1*i));
                }
            } else
            {
                alphas.Add(AParameter.Text);
            }

            
            //public void RunCode(string CVDataFile,          string alpha,        string MaxIterations, string ZeroParameterTextBox,       string SmoothWBool, 
            //        string SmoothWWindowPoints, string PolynomOrder, string RelativePermitivity, string Area)
 
            for (int i = 0; i< alphas.Count;i++)
            {
                RunPythonCodeClass PythonCodeRun = new RunPythonCodeClass();
                PythonCodeRun.RunCode(CVFiles.CVFileNames[CVFileNamesComboBox.SelectedIndex],
                                      alphas[i],
                                      MaxIterationsTextBox.Text,
                                      ZeroParameterTextBox.Text,
                                      SmoothWBool,
                                      SmoothWWindowPointsTextBox.Text,
                                      PolynomOrder.Text,
                                      Material.RelativePermitivity.ToString(),
                                      Material.Area.ToString(),
                                      VoltageStepTextBox.Text,
                                      CNWOptionComboBox.SelectedIndex.ToString(),
                                      ScaleComboBox.SelectedIndex.ToString()
                                      );
                PythonCodeRunList.Add(PythonCodeRun);
            }
        }

        private void RemoveCVFile_Click(object sender, RoutedEventArgs ee)
        {
            if (DataFiles.SelectedItems.Equals(null)) return;
            if (DataFiles.SelectedItems.Count < 1) return;
            try
            {
                foreach (var item in DataFiles.SelectedItems)
                {
                    int i = DataFiles.Items.IndexOf(item);
                    if (i == -1) return;
                    CVFiles.CVFileNames.RemoveAt(i);
                    CVFiles.CVFileNamesShort.RemoveAt(i);
                    CVFiles.FilesData.RemoveAt(i);
                }
                DataFiles.Items.Refresh();
                CVFileNamesComboBox.Items.Refresh();
            }
            catch (Exception e)
            {

            }

        }

        private void DataFiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataFiles.SelectedItems.Equals(null) == false)
            {
                if (DataFiles.SelectedItems.Count < 1) return;
                try
                {
                    int i = DataFiles.Items.IndexOf(DataFiles.SelectedItems[0]);
                    if (i == -1) return;

                    VieWSource.Source = CVFiles.FilesData[i].data;
                    if (CVFileNamesComboBox.SelectedIndex != i) CVFileNamesComboBox.SelectedIndex = i;
                    CVPlot_PlotNWresult = false;
                    RedrawCVPlot();
                }
                catch (System.ArgumentOutOfRangeException es)
                {
                }

            }
        }

        private void RedrawCVPlot()
        {
            if (alive == false) return;

            CVPlotCollection.Clear();
            if (CVPlot_PlotNWresult == false)
            {
                foreach (var item in DataFiles.SelectedItems)
                {
                    int j = DataFiles.Items.IndexOf(item);
                    ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();
                    if (CVPlotOption.SelectedIndex == 0)
                    {
                        foreach (var dataPoint in CVFiles.FilesData[j].data)
                        {
                            s.Add(new ObservablePoint((double)dataPoint.XData, (double)dataPoint.YData));
                        }
                    }
                    else if (CVPlotOption.SelectedIndex == 1)
                    {
                        foreach (var dataPoint in CVFiles.FilesData[j].data)
                        {
                            s.Add(new ObservablePoint((double)dataPoint.XData, (double)dataPoint.Y2Data));
                        }
                    }
                    CVPlotCollection.Add(
                     new LineSeries
                     {
                         Title = CVFiles.CVFileNamesShort[j],
                         LineSmoothness = 0,
                         Fill = Brushes.Transparent,
                         Values = s
                     });
                }
            }
            else  //  if (CVPlot_PlotNWresult == true)
            {
                List<string> InputCVFile = new List<string>();
                double min = -1.0;
                double max = -1.0;

                foreach (var item in NWResults.SelectedItems)
                {
                    foreach (RunPythonCodeClass PythonRunCodeInstance in PythonCodeRunList)
                    { 
                        if (PythonRunCodeInstance.RunLabel.Equals(item))
                        {
                            if (!InputCVFile.Contains(PythonRunCodeInstance.CVDataFile))
                            {
                                InputCVFile.Add(PythonRunCodeInstance.CVDataFile);

                                try
                                {
                                    ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();
                                    if (CVPlotOption.SelectedIndex == 0)
                                    {
                                        foreach (var OneData in CVFiles.FilesData[CVFiles.CVFileNames.IndexOf(PythonRunCodeInstance.CVDataFile)].data)
                                        {
                                            s.Add(new ObservablePoint((double)OneData.XData, (double)OneData.YData));
                                        }
                                    }
                                    else if (CVPlotOption.SelectedIndex == 1)
                                    {
                                        foreach (var OneData in CVFiles.FilesData[CVFiles.CVFileNames.IndexOf(PythonRunCodeInstance.CVDataFile)].data)
                                        {
                                            s.Add(new ObservablePoint((double)OneData.XData, (double)OneData.Y2Data));
                                        }
                                    }
									CVPlotCollection.Add(
										new LineSeries
										{
											Title = "Input " + PythonRunCodeInstance.RunLabelName.Substring(0, PythonRunCodeInstance.RunLabelName.IndexOf(" alpha(")),
											LineSmoothness = 0,
											Fill = Brushes.Transparent,
											PointGeometry = null,
                                            Values = s
                                        });
                                }
                                catch (Exception) { } 
                            }

                            if (PythonRunCodeInstance.ResultRedy == true)
                            { 
                                ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();
                                if (CVPlotOption.SelectedIndex == 2)
                                {
                                    if (min == -1)
                                    {
                                        min = (double)PythonRunCodeInstance.NWDataFile.data[0].N;
                                        max = min;
                                    }
                                    foreach (var dataPoint in PythonRunCodeInstance.NWDataFile.data)
                                    {
                                        s.Add(new ObservablePoint((double)dataPoint.W, Math.Log10((double)dataPoint.N)));
                                        if ((double)dataPoint.N < min)
                                        {
                                            min = (double)dataPoint.N;
                                        }
                                        if ((double)dataPoint.N > max)
                                        {
                                            max = (double)dataPoint.N;
                                        }
                                    }
                                } else if (CVPlotOption.SelectedIndex == 1)
                                {
                                    foreach (var dataPoint in PythonRunCodeInstance.NWDataFile.CVResultFileData)
                                    {
                                        s.Add(new ObservablePoint((double)dataPoint.XData, (double)dataPoint.Y2Data));
                                    }
                                } else
                                {
                                    foreach (var dataPoint in PythonRunCodeInstance.NWDataFile.CVResultFileData)
                                    {
                                        s.Add(new ObservablePoint((double)dataPoint.XData, (double)dataPoint.YData));
                                    }
                                }
                                CVPlotCollection.Add(
                                 new LineSeries
                                 {
                                     Title = PythonRunCodeInstance.RunLabelName.Substring(0, PythonRunCodeInstance.RunLabelName.IndexOf(",Zero(")),
                                     LineSmoothness = 0,
                                     Fill = Brushes.Transparent,
									 PointGeometry = null,
									 Values = s
                                 });
                                
                            }
                            break;
                        }
                    }
                }
                if (CVPlotOption.SelectedIndex == 2 && min != -1)
                { 
                    CVPlotGraf.AxisY[0].MinValue = Math.Log10(min) - 1;
                    CVPlotGraf.AxisY[0].MaxValue = Math.Log10(max) + 1;
                }
                InputCVFile.Clear(); 
            }
        }

        private void DataGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // First check so that we´ve only got one clicked cell
            if (FileContent.SelectedCells.Count != 1)
                return;

            // Then fetch the column header
            string selectedColumnHeader = (string)FileContent.SelectedCells[0].Column.Header;

            if (selectedColumnHeader.Equals("Capacitance (pF)"))
            {
                CVPlot_PlotNWresult = false;
                CVPlotOption.SelectedIndex = 0;  
            //    RedrawCVPlot();
            } else if (selectedColumnHeader.Equals("C^-2 (pF^-2)"))
            {
                CVPlot_PlotNWresult = false;
                CVPlotOption.SelectedIndex = 1;
            //    RedrawCVPlot(); CVPlotOption selectionChanged redraws CV Plot
            }
        }

        public delegate void SetStatusTextDelegate(string text);

        public void SetStatusText(string text)
        {
            Status.Text = text;
        }

        private void NextCVFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (CVFileNamesComboBox.SelectedItem != null )
            {
                if (CVFileNamesComboBox.SelectedIndex < CVFileNamesComboBox.Items.Count - 1)
                {
                    CVFileNamesComboBox.SelectedItem = CVFileNamesComboBox.Items[CVFileNamesComboBox.SelectedIndex + 1];
                } else
                {
                    CVFileNamesComboBox.SelectedItem = CVFileNamesComboBox.Items[0];
                }
            }
        }

        private void CVFileNamesComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
            if (DataFiles.Items.IndexOf(DataFiles.SelectedItems[0]) != CVFileNamesComboBox.SelectedIndex)
                DataFiles.SelectedIndex = CVFileNamesComboBox.SelectedIndex;
            } catch (System.ArgumentOutOfRangeException es)
            { }
            VoltageStepTextBox.Text = CVFiles.FilesData[CVFileNamesComboBox.SelectedIndex].VoltageStep.ToString();
        }

        private void NWResultsRemove_Click(object sender, RoutedEventArgs e)
        {
            if (NWResults.SelectedItems.Equals(null)) return;
            if (NWResults.SelectedItems.Count < 1) return;
            while (NWResults.SelectedItems.Count >0)
            {
                foreach (RunPythonCodeClass PythonInstance in PythonCodeRunList)
                {
                    if (PythonInstance.RunLabel.Equals(NWResults.SelectedItems[0]))
                    {
                        PythonInstance.KillPythonCodeRun();
                        break;
                    }
                }
                NWResults.Items.Remove(NWResults.SelectedItems[0]);
            }

            NWResults.Items.Refresh();
            
        }

        private void SmoothWCheckBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SmoothWCheckBox.SelectedIndex == 1)
            {
                SmoothWWindowPointsTextBox.IsEnabled = true;
                PolynomOrder.IsEnabled = true;
            } else
            {
                try
                {
                    SmoothWWindowPointsTextBox.IsEnabled = false;
                    PolynomOrder.IsEnabled = false;
                }
                catch { }
            }
        }


        private void NWResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (NWResults.SelectedItems.Equals(null)) return;
            if (NWResults.SelectedItems.Count < 1) return;
            CVPlot_PlotNWresult = true;
            RedrawCVPlot();
        }

        private bool AreCVParametersOk()
        {
            decimal value1;
            //::alpha check
            if (AParameter.Text.Split('/').Length > 2)
            {
                AParameter.Background = Brushes.PaleVioletRed;
                int index = AParameter.Text.IndexOf(AParameter.Text.Split('-')[3]);
                AParameter.Select(index, AParameter.Text.Length - index);
                return false;
            } else if (AParameter.Text.Split('/').Length == 2)
            { 
                if (!decimal.TryParse(AParameter.Text.Split('/')[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value1))
                {
                    AParameter.Background = Brushes.PaleVioletRed; 
                    AParameter.Select(0, AParameter.Text.Split('/')[0].Length);
                    return false;
                }
                if (!decimal.TryParse(AParameter.Text.Split('/')[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value1))
                {
                    AParameter.Background = Brushes.PaleVioletRed;
                    AParameter.Select(AParameter.Text.IndexOf(AParameter.Text.Split('/')[1]), AParameter.Text.Split('/')[1].Length);
                    return false;
                }
                AParameter.Background = null;
            } else 
            { 
                if (!decimal.TryParse(AParameter.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value1))
                {
                    AParameter.Background = Brushes.PaleVioletRed;
                    AParameter.SelectAll();
                    return false;
                }
                AParameter.Background = null;
            }
            // :: Voltage step check

            if (!decimal.TryParse(VoltageStepTextBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value1))
            {
                VoltageStepTextBox.Background = Brushes.PaleVioletRed;
                VoltageStepTextBox.SelectAll();
                return false;
            } else { 
                VoltageStepTextBox.Background = null;
                CVFiles.FilesData[CVFileNamesComboBox.SelectedIndex].VoltageStep = value1;
            }

            // :: Max iterations check
            int IntValue;
            if (!int.TryParse(MaxIterationsTextBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out IntValue))
            {
                MaxIterationsTextBox.Background = Brushes.PaleVioletRed;
                MaxIterationsTextBox.SelectAll();
                return false;
            } else
            {
                MaxIterationsTextBox.Background = null;
            }

            // :: Zero parameter check

            if (!decimal.TryParse(ZeroParameterTextBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value1))
            {
                ZeroParameterTextBox.Background = Brushes.PaleVioletRed;
                ZeroParameterTextBox.SelectAll();
                return false;
            }
            else
            {
                ZeroParameterTextBox.Background = null;
            }

            if (SmoothWCheckBox.SelectedIndex != 1) return true;

            // :: window points and polynom order
            if (!int.TryParse(SmoothWWindowPointsTextBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out IntValue))
            {
                SmoothWWindowPointsTextBox.Background = Brushes.PaleVioletRed;
                SmoothWWindowPointsTextBox.SelectAll();
                return false;
            }
            else
            {
                SmoothWWindowPointsTextBox.Background = null;
            }
            if (!int.TryParse(PolynomOrder.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out IntValue))
            {
                PolynomOrder.Background = Brushes.PaleVioletRed;
                PolynomOrder.SelectAll();
                return false;
            }
            else
            {
                PolynomOrder.Background = null;
            }

            return true;
            
        }

        private void CVNWSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (NWResults.SelectedItems.Equals(null) || NWResults.SelectedItems.Count < 1)
            {
                Status.Text = "Select N(W) data to be saved.";
                return;
            }
          
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*",
                FileName = CVFileNamesComboBox.SelectedItem.ToString()
            };
             
            if (saveFileDialog.ShowDialog() == true)
            {
                string directory =  Path.GetDirectoryName(saveFileDialog.FileName);
                string name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                if (name.IndexOf('.') > 0)
                { 
                    name = name.Substring(0, name.IndexOf('.'));
                }

                foreach (string RunLabelInstance in NWResults.SelectedItems)
                {
                    foreach(RunPythonCodeClass PythonCodeRunInstance in PythonCodeRunList)
                    { 

                        if (PythonCodeRunInstance.RunLabel.Equals(RunLabelInstance))
                        {
                            if (SaveInputCVFileCheckbox.IsChecked == true)
                            {
                                if (!File.Exists(Path.Combine(directory, name + "_InputCV.txt")))
                                {
                                    File.Copy(PythonCodeRunInstance.CVDataFile, Path.Combine(directory, name + "_InputCV.txt"), true);
                                }
                            }

                            if (SaveOutputNWFileCheckbox.IsChecked == true)
                                using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + "_Output_NW_a("+ PythonCodeRunInstance.writeLines[0] + ").txt")))
                                {
                                    writer.WriteLine(PythonCodeRunInstance.RunLabelName + writer.NewLine);
                                    writer.WriteLine("W(10^-6m) N(cm^-3)");
                                    foreach( var OneData in PythonCodeRunInstance.NWDataFile.data)
                                    {
                                        writer.WriteLine(OneData.W + "    " + OneData.N);
                                    }
                                } 
                            if (SaveOutputCVFileCheckbox.IsChecked == true)
                                using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + "_Output_CV_a(" + PythonCodeRunInstance.writeLines[0] + ").txt")))
                                {
                                    writer.WriteLine(PythonCodeRunInstance.RunLabelName + writer.NewLine);
                                    writer.WriteLine("Voltage(V) Capacitance(pF)");
                                    foreach (var OneData in PythonCodeRunInstance.NWDataFile.CVResultFileData)
                                    {
                                        writer.WriteLine(OneData.XData + "    " + OneData.YData);
                                    }
                                }
                            break;
                        }
                    }
                }
                Status.Text = "Completed : Save N(W) data.";
            }

        }

        bool CVPlot_IsLogAxis = false;
        private void CVPlotOption_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CVPlotOption.SelectedIndex == 2)
            {
                if (CVPlot_IsLogAxis == false)
                {
                    CVPlotGraf.AxisY.Add(new LogarithmicAxis()
                    {
                        Base = 10,
                        LabelFormatter = value => string.Format(CultureInfo.InvariantCulture, "{0:E2}", Math.Pow(10, value))
                    });
                    CVPlotGraf.AxisY.RemoveAt(0);
                    CVPlot_IsLogAxis = true;
                }
                CVPlotGraf.AxisY[0].Title = "N (cm^-3)";
                CVPlotGraf.AxisX[0].Title = "W (um)";
                 
            } else if (CVPlotOption.SelectedIndex == 1)
            {
                if (CVPlot_IsLogAxis == true)
                {
                    CVPlotGraf.AxisY.Add(new Axis());
                    CVPlotGraf.AxisY.RemoveAt(0);
                    CVPlot_IsLogAxis = false;
                }

                CVPlotGraf.AxisY[0].Title = "1/(C^2) (pF^-2)";
                CVPlotGraf.AxisX[0].Title = "Voltage (V)";
            }
            else
            {
                if (CVPlot_IsLogAxis == true)
                {
                    CVPlotGraf.AxisY.Add(new Axis());
                    CVPlotGraf.AxisY.RemoveAt(0);
                    CVPlot_IsLogAxis = false;
                }
                

                CVPlotGraf.AxisY[0].Title = "Capacitance (pF)";
                CVPlotGraf.AxisX[0].Title = "Voltage (V)";
            }
            RedrawCVPlot();

        }

        private void SettingsPythonPythonCodeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Python code (*.py)|*.py|All files (*.*)|*.*";

            if (openFileDialog1.ShowDialog() == true)
            {
                PythonCodePath = openFileDialog1.FileName;
                SettingsPythonPythonCodeTextBox.Text = openFileDialog1.FileName;
            }

        }

        private void SettingsPythonPythonConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Python console (*.exe)|*.exe|All files (*.*)|*.*"; 

            if (openFileDialog1.ShowDialog() == true)
            {
                PythonPath = openFileDialog1.FileName;
                SettingsPythonPythonConsoleTextBox.Text = openFileDialog1.FileName;
            }
        }

        public MainWindow getInstance()
        {
            return this;
        }

        private void SettingsPythonPythonConsoleCodeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Python code (*.py)|*.py|All files (*.*)|*.*";

            if (openFileDialog1.ShowDialog() == true)
            {
                PythonCallPath = openFileDialog1.FileName;
                SettingsPythonPythonConsoleCodeTextBox.Text = openFileDialog1.FileName;
            }
        }
    }


    }