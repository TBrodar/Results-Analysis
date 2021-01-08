using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Results
{
    /// <summary>
    /// Interaction logic for DLTSPage.xaml
    /// </summary> 
    public partial class DLTSPage : Page
    {
        public List<DLTSDataFile> DLTSDataFiles { get; set; }
        LoadForm lform = new LoadForm();

        public SeriesCollection DLTSPlotCollection { get; set; }
        public List<HeaderForSaveDataClass> HeadersForSaveDataList { get; set; }
        public List<HeaderLinesClass> HeaderLinesList { get; set; } 


        private bool repaint = true; 
        public bool alive = false;
        public DLTSPage()
        {
            DLTSPlotCollection = new SeriesCollection();
            InitializeComponent();
            DLTSPlotGraf.DisableAnimations = true;

            DLTSDataFiles = new List<DLTSDataFile>();
            DLTSDataFilesListBox.ItemsSource = DLTSDataFiles;

            DLTSDataFile.Peak.DeepLevelNames = new List<string>();
            DLTSDataFile.Peak.DeepLevels = new List<List<DLTSDataFile.Peak>>(); 
            HeadersForSaveDataList = new List<HeaderForSaveDataClass>();
            HeaderLinesList = new List<HeaderLinesClass>();

            DLTSDataFile.Peak.selectedPeak = new List<DLTSDataFile.Peak>();

            var a = new DLTSDataFile.Peak();
            DLTSDataFile.Peak.selectedPeak.Add(a);

            PeaksListBox_Pick.ItemsSource = DLTSDataFile.Peak.selectedPeak;
            PeaksListBox_Pick.Items.Refresh();

            DLTSDeepLevel.ItemsSource = DLTSDataFile.Peak.DeepLevelNames;
            DLTSDeepLevel.Items.Refresh();

            DataContext = this;

            // Load Deault DLTS headers ::
            HeaderLinesList = new List<HeaderLinesClass>();
            HeaderLinesClass.HeadersNames = new List<string>();
            if (File.Exists(Path.Combine(MainWindow.ThisWindowInstance.workingdirectory, "DLTSHeadersConfig.txt")))
            {
                List<String> lines = new List<string>();
                using (StreamReader reader = new StreamReader(Path.Combine(MainWindow.ThisWindowInstance.workingdirectory, "DLTSHeadersConfig.txt")))
                {
                    while (reader.Peek() > -1)
                        lines.Add(reader.ReadLine());
                }
                List<string> HeaderLines = lform.getProperties("HeaderLines", ref lines);

                for (int i = 0; i < HeaderLines.Count; i += 3)
                {
                    try
                    {
                        string Namea = "";
                        string[] keysa, parametersa;
                        if (i % 3 == 0 && HeaderLines[i].Contains("name") && HeaderLines[i].Contains('='))
                        {
                            Namea = HeaderLines[i].Split('=')[1];
                        }
                        else { continue; }
                        if (HeaderLines[i + 1].Contains("parameters") && HeaderLines[i + 1].Contains('='))
                        {
                            parametersa = HeaderLines[i + 1].Split('=')[1].Split(',');
                        }
                        else { continue; }
                        if (HeaderLines[i + 2].Contains("keys") && HeaderLines[i + 2].Contains('='))
                        {
                            keysa = HeaderLines[i + 2].Split('=')[1].Split(',');
                        }
                        else { continue; }
                        HeaderLinesList.Add(new HeaderLinesClass() { keys = keysa, parameters = parametersa });
                        HeaderLinesClass.HeadersNames.Add(Namea);
                    }
                    catch { }
                }


            }
            DLTSDataFile.VisibleHeadersParameter = new List<int>();
            DLTSDataFile.VisibleHeadersKey = new List<int>();

            UpdateVisibleDLTSFilesHeaders();

            if (HeaderLinesList.Count > 0) SavedHeaderTemplatesComboBox.SelectedIndex = 0;
            SavedHeaderTemplatesComboBox.Items.Clear();
            foreach (string s in HeaderLinesClass.HeadersNames) SavedHeaderTemplatesComboBox.Items.Add(s);
            alive = true;
        }
        public void UpdateVisibleDLTSFilesHeaders()
        {
            RemoveHeader.Items.Clear();
            for (int i = 0; i < DLTSDataFile.VisibleHeadersParameter.Count; i++)
            {
                MenuItem m = new MenuItem();
                m.Header = LoadForm.DLTFile.AllParameters[DLTSDataFile.VisibleHeadersParameter[i]] + " - " + LoadForm.DLTFile.AllKeys[DLTSDataFile.VisibleHeadersParameter[i]][DLTSDataFile.VisibleHeadersKey[i]];
                m.Name = "R" + i.ToString();
                m.Click += RemoveHeader_Click;
                RemoveHeader.Items.Add(m);
            }

            AddHeader.Items.Clear();
            for (int i = 0; i < LoadForm.DLTFile.AllParameters.Count; i++)
            {
                MenuItem m = new MenuItem();
                m.Name = "N" + i.ToString();
                m.Header = LoadForm.DLTFile.AllParameters[i];


                for (int j = 0; j < LoadForm.DLTFile.AllKeys[i].Count; j++)
                {
                    if (i == 0 && j < 8) continue;
                    MenuItem s = new MenuItem();
                    s.Header = LoadForm.DLTFile.AllKeys[i][j];
                    s.Click += AddHeader_Click;
                    bool ok = true;
                    for (int z = 0; z < DLTSDataFile.VisibleHeadersParameter.Count; z++)
                    {
                        if (DLTSDataFile.VisibleHeadersParameter[z] == i && DLTSDataFile.VisibleHeadersKey[z] == j)
                        {
                            ok = false;
                        }
                    }
                    if (ok)
                    {
                        m.Items.Add(s);
                    }
                }
                AddHeader.Items.Add(m);
            }
            foreach (DLTSDataFile file in DLTSDataFiles)
            {
                file.AddedHeaders = "";
                for (int i = 0; i < DLTSDataFile.VisibleHeadersKey.Count; i++)
                {
                    file.AddedHeaders += LoadForm.DLTFile.AllKeys[DLTSDataFile.VisibleHeadersParameter[i]][DLTSDataFile.VisibleHeadersKey[i]]
                         + "(" + file.Properties[DLTSDataFile.VisibleHeadersParameter[i]][DLTSDataFile.VisibleHeadersKey[i]] + ") ";
                }
            }

            DLTSDataFilesListBox.Items.Refresh();
        }

        private void DLTSPlotOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            redrawDLTSPlot();
        }

        private void AddPeakButton_Click(object sender, RoutedEventArgs e)
        {
            int index = DLTSDeepLevel.SelectedIndex;
            if (index < 0) return;
            if (DLTSDataFile.Peak.selectedPeak[0].Amplitude == 0M && DLTSDataFile.Peak.selectedPeak[0].EmRate == 0M && DLTSDataFile.Peak.selectedPeak[0].Temperature == 0M) return;

            DLTSDataFile.Peak.DeepLevels[index].Add(DLTSDataFile.Peak.selectedPeak[0]);
            DLTSDataFile.Peak.selectedPeak[0] = new DLTSDataFile.Peak();
            PeaksListBox_Pick.Items.Refresh();

            PeaksListBox.Items.Refresh();
        }

        private bool clicked = false;
        private void ChartMouseMove(object sender, MouseEventArgs e)
        {
            var point = DLTSPlotGraf.ConvertToChartValues(e.GetPosition(DLTSPlotGraf));

            if (clicked)
            {
                MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                "Mouse position :  " + string.Format("{0:0.0}", point.X) + ", " + string.Format("{0:0.000}", point.Y)
                );
                 
            }
        }
        Point MouseDownPosition, MouseUpPosition;
        private void DLTSPlotGraf_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = DLTSPlotGraf.ConvertToChartValues(e.GetPosition(DLTSPlotGraf));
            MouseDownPosition = point; 

            clicked = true;
        }

        private bool isLeft = false;
        private Point _Position;
        private void DLTSPlotGraf_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point point;
            if (isLeft == false)
            {
                point = DLTSPlotGraf.ConvertToChartValues(e.GetPosition(DLTSPlotGraf));
            }
            else
            {
                point = _Position;
                isLeft = false;
            }
            clicked = false;
            MouseUpPosition = point;

            if (MouseUpPosition.X < MouseDownPosition.X)
            {
                var p = MouseUpPosition;
                MouseUpPosition = MouseDownPosition;
                MouseDownPosition = p;
            }

            bool setUp = false;
            decimal tempMax = -1;
            decimal AmplitudeMax = -1;
            decimal EmissionMax = -1;
            DLTSDataFile sourceFileMax = null;

            foreach (var f in DLTSDataFiles)
            {
                if (DLTSPlotOption.SelectedItem == null) continue;
                if (DLTSDataFilesListBox.SelectedItems.Contains(f) == false)
                {
                    continue;
                }
                if (DLTSPlotOption.SelectedIndex == 0)
                { }
                else if ((string)((ComboBoxItem)DLTSPlotOption.SelectedItem).Content == f.FileNameShort) { }
                else { continue; }

                for (int i = 0; i < f.DLTSSpectrum.Count; i++)
                {
                    if ((double)f.DLTSSpectrumTemperatures[i] > MouseDownPosition.X && (double)f.DLTSSpectrumTemperatures[i] < MouseUpPosition.X)
                    {
                        if (setUp == false)
                        {
                            tempMax = f.DLTSSpectrumTemperatures[i];
                            AmplitudeMax = f.DLTSSpectrum[i];
                            EmissionMax = f.Emission;
                            sourceFileMax = f;
                            setUp = true;
                        }
                        if (f.DLTSSpectrum[i] > AmplitudeMax)
                        {
                            tempMax = f.DLTSSpectrumTemperatures[i];
                            AmplitudeMax = f.DLTSSpectrum[i];
                            EmissionMax = f.Emission;
                            sourceFileMax = f;
                        }
                    }
                }
            }
            if (setUp)
            {
                DLTSDataFile.Peak.selectedPeak[0].Amplitude = AmplitudeMax;
                DLTSDataFile.Peak.selectedPeak[0].EmRate = EmissionMax;
                DLTSDataFile.Peak.selectedPeak[0].Temperature = tempMax;
                DLTSDataFile.Peak.selectedPeak[0].sourceFile = sourceFileMax;
                PeaksListBox_Pick.Items.Refresh();
                try
                {
                    var s = new AxisSection()
                    {
                        Value = (double)tempMax,
                        Stroke = Brushes.YellowGreen,
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection(new[] { 10d })
                    };
                    if (DLTSPlotGraf.AxisX[0].Sections == null) DLTSPlotGraf.AxisX[0].Sections = new SectionsCollection();
                    DLTSPlotGraf.AxisX[0].Sections.Clear();
                    DLTSPlotGraf.AxisX[0].Sections.Add(s);
                    DLTSPlotGraf.Update();
                }
                catch (System.NullReferenceException nullex)
                {
                    MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                    "Error : LiveChart NullReferenceException."
                    );
                }
            }
        }

        private void DLTSPlotGraf_MouseLeave(object sender, MouseEventArgs e)
        {
            if (clicked)
            {
                _Position = e.GetPosition(DLTSPlotGraf);
                isLeft = true;
                DLTSPlotGraf_MouseUp(sender, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left));
            }
        }

        private void RemovePeakButton_Click(object sender, RoutedEventArgs e)
        {
            int index = DLTSDeepLevel.SelectedIndex;
            if (index == -1) return;

            foreach (DLTSDataFile.Peak s in PeaksListBox.SelectedItems)
            {
                DLTSDataFile.Peak.DeepLevels[index].Remove(s);
            }
            PeaksListBox.Items.Refresh();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "DLTS data (*.dlt)|*.dlt|Results data (*.DLTSresults)|*.DLTSresults|Text data with FileName header (*.txt)|*.txt|All(*.*)|*";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string Name in openFileDialog.FileNames)
                {
                    if (Path.GetExtension(Name).Contains("DLTSresults"))
                    {
                        string text = "";
                        using (StreamReader reader = new StreamReader(Name))
                        {
                            text = reader.ReadToEnd();
                        }
                        List<DLTSDataFile> filesLoad = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DLTSDataFile>>(text);
                        foreach (DLTSDataFile f in filesLoad)
                        {
                            if (f.AddedHeaders == null)
                            {
                                f.AddedHeaders = "";
                            }
                        }

                        DLTSDataFiles.AddRange(filesLoad); 

                        DLTSDataFilesListBox.Items.Refresh();
                        foreach (DLTSDataFile f in filesLoad)
                        {
                            if (f.isSelected == true)
                            {
                                if (DLTSDataFilesListBox.Items.IndexOf(f) > -1)
                                {
                                    DLTSDataFilesListBox.SelectedItems.Add(f);
                                }
                            }
                        }

                        continue;
                    }
                    else if (Path.GetExtension(Name).Contains("txt"))
                    {
                        List<string> lines2 = new List<string>();
                        try
                        {
                            using (StreamReader reader = new StreamReader(Name))
                            {
                                while (reader.Peek() > -1)
                                    lines2.Add(reader.ReadLine());
                                if (lines2[0].Contains("FileName"))
                                {
                                    OpenFileDialog openFileDialog2 = new OpenFileDialog();
                                    openFileDialog2.Title = "Select file in folder with measurement files";
                                    if (openFileDialog.ShowDialog() == true)
                                    {
                                        string[] l = lines2[0].Split(' ');
                                        int headerindex = -1;
                                        for (int i = 0; i < l.Length; i++)
                                        {
                                            if (l[i].Contains("FileName") == true)
                                            {
                                                headerindex = i;
                                                break;
                                            }
                                        }
                                        if (headerindex == -1) continue;

                                        string Directory = Path.GetDirectoryName(openFileDialog.FileName);
                                        List<string> a = new List<string>();
                                        for (int lineInt = 1; lineInt < lines2.Count; lineInt++)
                                        {
                                            l = lines2[lineInt].Split(' ');
                                            bool isLoaded = false;
                                            foreach (DLTSDataFile file in DLTSDataFiles)
                                            {
                                                if (file.FileName == Path.Combine(Directory, l[headerindex] + ".dft"))
                                                {
                                                    isLoaded = true;
                                                    break;
                                                }
                                            }
                                            if (isLoaded) continue; 
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                        continue;
                    }
                    LoadDLTSFileFromPath(Name);
                }
                UpdateVisibleDLTSFilesHeaders();
                DLTSDataFilesListBox.Items.Refresh();
            }
        }

        private int LoadDLTSFileFromPath(string Name)
        {
            List<string> lines = new List<string>();
            try
            {
                using (StreamReader reader = new StreamReader(Name))
                {
                    while (reader.Peek() > -1)
                        lines.Add(reader.ReadLine());
                }
            }
            catch (Exception ee)
            {
                MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                "Error : Load file. Name : " + Name
                );
                return 0;
            }

            string shortName = Path.GetFileNameWithoutExtension(Name);
            int number = -1;

            foreach (var f in DLTSDataFiles)
            {
                if (f.FileNameShort == shortName && number == -1)
                {
                    number = 0;
                }
                if (f.FileNameShort.Contains("(") && f.FileNameShort.Contains(")"))
                {
                    string poms = f.FileNameShort.Substring(0, f.FileNameShort.LastIndexOf('('));
                    if (poms == shortName)
                    {
                        int pomInt = -1;
                        string numstr = f.FileNameShort.Split('(')[f.FileNameShort.Split('(').Length - 1];
                        if (numstr[numstr.Length - 1].Equals(')'))
                        {
                            if (int.TryParse(numstr.Substring(0, numstr.Length - 1), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out pomInt))
                            {
                                if (pomInt > number)
                                {
                                    number = pomInt;
                                }
                            }
                        }
                    }
                }
            }
            if (number != -1)
            {
                shortName = Path.GetFileNameWithoutExtension(Name) + "(" + (number + 1).ToString("N0") + ")";
            }



            List<string> ParametersLines = lform.getProperties(lform.dltfile.parameters, ref lines);
            string EmissionString = lform.getValue(lform.dltfile.parameterskeys.ratewindow, ref ParametersLines);
            decimal Emission;
            if (!decimal.TryParse(EmissionString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Emission))
            {
                MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                "Error : Emission is not number. " + EmissionString + "\n" + Name
                );
                return 0;
            } 

            List<List<string>> propLines = lform.fillWithValuesDFTFile(ref lines);
            propLines[0][4] = shortName;
            propLines[0][5] = Name; 

            DLTSDataFile DataFileInstance = new DLTSDataFile()
            {
                FileName = Name,
                FileNameShort = shortName,
                Emission = Emission,
                AddedHeaders = "",
                Properties = propLines

            }; 

            List<string> DataLines = lform.getProperties(lform.dltfile.data, ref lines);
            List<decimal> DLTSSpectrum = new List<decimal>();
            List<decimal> DLTSSpectrumTemperatures = new List<decimal>();
            foreach (string line in DataLines)
            {
                string[] splitline = line.Split(',');
                decimal t, dc;
                bool ok = true;
                if (!decimal.TryParse(splitline[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out t))
                {
                    MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                    "Error : temperature is not number. " + splitline[1] + "\n" + Name
                    );
                    ok = false;
                }
                if (!decimal.TryParse(splitline[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out dc))
                {
                    MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                    "Error : DLTS signal is not number. " + splitline[2] + "\n" + Name
                    );
                    ok = false;
                }

                if (ok)
                {
                    DLTSSpectrum.Add(dc);
                    DLTSSpectrumTemperatures.Add(t);
                }
            }

            DataFileInstance.DLTSSpectrum = DLTSSpectrum;
            DataFileInstance.DLTSSpectrumTemperatures = DLTSSpectrumTemperatures;

            DLTSDataFiles.Add(DataFileInstance);
            return 1;
        }
         
        private void DLTSDataFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.ThisWindowInstance.alive == false) return;
            if (alive == false) return;
            ComboBoxItem item = null;
            if (DLTSPlotOption.SelectedIndex > -1)
            {
                item = (ComboBoxItem)DLTSPlotOption.SelectedItem;
            }
            List<ComboBoxItem> a = new List<ComboBoxItem>();
            a.Add((ComboBoxItem)DLTSPlotOption.Items[0]);
            foreach (DLTSDataFile f in DLTSDataFilesListBox.Items)
            {
                if (DLTSDataFilesListBox.SelectedItems.Contains(f) == false)
                {
                    continue;
                }
                var s = new ComboBoxItem();
                s.Content = f.FileNameShort;
                a.Add(s);
            }
            DLTSPlotOption.Items.Clear();
            foreach (var c in a)
                DLTSPlotOption.Items.Add(c);
            int index = -1;
            foreach (ComboBoxItem i in DLTSPlotOption.Items)
            {
                if (i.Content == item.Content)
                {
                    index = DLTSPlotOption.Items.IndexOf(i);
                    break;
                }
            }
            if (index > -1)
            {
                DLTSPlotOption.SelectedIndex = index;
            }
            else
            {
                DLTSPlotOption.SelectedIndex = 0;
            }
            redrawDLTSPlot();
        } 

        private void SaveDLTSDeepLevelButton_Click(object sender, RoutedEventArgs e)
        {
            int index = -1;
            foreach (var s in DLTSDataFile.Peak.DeepLevelNames)
            {
                if (s == DLTSDeepLevel.Text)
                { index = DLTSDataFile.Peak.DeepLevelNames.IndexOf(s); }
            }
            if (index == -1)
            {
                DLTSDataFile.Peak.DeepLevelNames.Add(DLTSDeepLevel.Text);
                DLTSDataFile.Peak.DeepLevels.Add(new List<DLTSDataFile.Peak>());
            }
            DLTSDeepLevel.Items.Refresh();
            if (DLTSDeepLevel.SelectedIndex == -1)
            {
                DLTSDeepLevel.SelectedIndex = DLTSDeepLevel.Items.Count - 1;
            }
        }
         
        private void DLTSDeepLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DLTSDeepLevel.SelectedIndex < 0) return;
            int index = DLTSDeepLevel.SelectedIndex;

            PeaksListBox.ItemsSource = DLTSDataFile.Peak.DeepLevels[index];
            PeaksListBox.Items.Refresh();

        }

        private void RefreshGraph_Click(object sender, RoutedEventArgs e)
        {
            redrawDLTSPlot();
        }

        private void DataToolTipToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ThisWindowInstance.alive == false) return;
            if (alive == false) return;
            if (DataToolTipToggleButton.IsChecked == true)
            {
                DLTSPlotGraf.Hoverable = true;
                DLTSPlotGraf.DataTooltip = new DefaultTooltip();
            }
            else
            {
                DLTSPlotGraf.Hoverable = false;
                DLTSPlotGraf.DataTooltip = null;
            }
        } 
        private void SelectAllDLTSFiles_Click(object sender, RoutedEventArgs e)
        {
            repaint = false;
            DLTSDataFilesListBox.SelectAll();
            repaint = true;
            redrawDLTSPlot();
        }

        private void DeselectAllDLTSFiles_Click(object sender, RoutedEventArgs e)
        {
            repaint = false;
            DLTSDataFilesListBox.SelectedIndex = -1;
            repaint = true;
            redrawDLTSPlot();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (DLTSDataFilesListBox.SelectedIndex < 0) return;
            if (DLTSDataFilesListBox.SelectedItem == null) return;

            int count = DLTSDataFilesListBox.SelectedItems.Count;
            repaint = false;
            for (int i = 0; i < count; i++)
            {
                DLTSDataFiles.Remove((DLTSDataFile)DLTSDataFilesListBox.SelectedItems[i]);
            }
            repaint = true;
            DLTSDataFilesListBox.Items.Refresh();
            redrawDLTSPlot();

        }

        private void redrawDLTSPlot()
        {
            if (repaint == false) return;
            if (MainWindow.ThisWindowInstance.alive == false) return;
            if (alive == false) return;
            decimal value = 0M;

            DLTSPlotCollection.Clear();
            DLTSPlotGraf.VisualElements.Clear();


            foreach (DLTSDataFile item in DLTSDataFilesListBox.Items)
            {

                if (DLTSDataFilesListBox.SelectedItems.Contains(item) == false)
                {
                    continue;
                }
                if (DLTSPlotOption.Items.Count == 0)
                {
                    continue;
                }
                if (DLTSPlotOption.SelectedIndex == 0) // Plot All
                { }
                else if ((string)((ComboBoxItem)DLTSPlotOption.SelectedItem).Content == item.FileNameShort)
                { }
                else
                { continue; }

                ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();
                int index = DLTSDataFilesListBox.Items.IndexOf(item);
                if (index < 0) continue;

                int count = DLTSDataFiles[index].DLTSSpectrumTemperatures.Count;

                decimal offset = 0M; 
                decimal StartTemperature, EndTemperature;
                if (!decimal.TryParse(AxisXStartComboBox.Text, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out StartTemperature))
                {
                    StartTemperature = -1;
                }
                if (!decimal.TryParse(AxisXEndComboBox.Text, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out EndTemperature))
                {
                    EndTemperature = -1;
                }

                for (int i = 0; i < DLTSDataFiles[index].DLTSSpectrumTemperatures.Count; i += 1)
                {
                    if ((DLTSDataFiles[index].DLTSSpectrumTemperatures[i] >= StartTemperature) && (EndTemperature == -1 || DLTSDataFiles[index].DLTSSpectrumTemperatures[i] <= EndTemperature))
                        s.Add(new ObservablePoint((double)DLTSDataFiles[index].DLTSSpectrumTemperatures[i], (double)(DLTSDataFiles[index].DLTSSpectrum[i] - offset)));
                }

                string title;
                if (EmissionsLabelsToggleButton.IsChecked == true)
                {
                    title = string.Format("{0:0.0} 1/s", DLTSDataFiles[index].Emission, CultureInfo.InvariantCulture);
                }
                else
                {
                    title = DLTSDataFiles[index].FileNameShort;
                }
                DLTSPlotCollection.Add(
                     new LineSeries
                     {
                         Title = title,
                         LineSmoothness = 0,
                         PointGeometry = null,
                         Fill = System.Windows.Media.Brushes.Transparent,
                         Values = s
                     });

            }

            if (DLTSDataFile.Peak.selectedPeak[0].EmRate != 0M && DLTSDataFile.Peak.selectedPeak[0].Temperature != 0M && DLTSDataFile.Peak.selectedPeak[0].Amplitude != 0M)
            {
                try
                {
                    var s = new AxisSection()
                    {
                        Value = (double)DLTSDataFile.Peak.selectedPeak[0].Temperature,
                        Stroke = Brushes.YellowGreen,
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection(new[] { 10d })
                    };

                    DLTSPlotGraf.AxisX[0].Sections.Clear();
                    DLTSPlotGraf.AxisX[0].Sections.Add(s);
                }
                catch (System.NullReferenceException nullex)
                {
                    MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                    "Error : LiveChart NullReferenceException."
                    );
                }
            }

        }


        private void EmissionsLabelsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmissionsLabelsToggleButton.IsChecked == true)
            {
                EmissionsLabelsToggleButton.Content = "Show emissions";
            }
            else
            {
                EmissionsLabelsToggleButton.Content = "Show names";
            }
            redrawDLTSPlot();

        }

        private void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "Image png(*.png)|*.png|Text (.*txt)|*.txt|All(*.*)|*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string directory = Path.GetDirectoryName(saveFileDialog.FileName);
                string name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                RenderTargetBitmap renderTargetBitmap =
                                    new RenderTargetBitmap((int)Math.Ceiling(ForGraphSave.ActualWidth), (int)Math.Ceiling(ForGraphSave.ActualHeight) + 50, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(ForGraphSave);
                PngBitmapEncoder pngImage = new PngBitmapEncoder();
                pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                using (Stream fileStream = File.Create(Path.Combine(directory, name + ".png")))
                {
                    pngImage.Save(fileStream);
                }
                using (StreamWriter fileStream = new StreamWriter(Path.Combine(directory, name + ".txt")))
                {
                    decimal previousOffset = 0M;
                    decimal value = 0;

                    List<decimal> offsets = new List<decimal>(); 
                    string line = "";
                    string line0 = "";
                    int numberofrows = 0;
                    foreach (DLTSDataFile DLTSFile in DLTSDataFilesListBox.Items)
                    {
                        if (DLTSDataFilesListBox.SelectedItems.Contains(DLTSFile) == false)
                        {
                            continue;
                        }
                        if (DLTSPlotOption.SelectedIndex == 0)
                        { }
                        else if ((string)((ComboBoxItem)DLTSPlotOption.SelectedItem).Content == DLTSFile.FileNameShort) { }
                        else { continue; }

                        line0 += ",    Temperature(K),    DLTSSignal(pF) ";
                        line += ",    " + DLTSFile.FileNameShort + ",  " + DLTSFile.FileName; 

                        if (DLTSFile.DLTSSpectrum.Count > numberofrows)
                        {
                            numberofrows = DLTSFile.DLTSSpectrum.Count;
                        }
                    }
                    line = line.Remove(0, 1);
                    line0 = line0.Remove(0, 1);
                    fileStream.WriteLine(line);
                    fileStream.WriteLine(line0);
                    line0 = null;
                    line = null;
                    foreach (DLTSDataFile DLTSFile in DLTSDataFilesListBox.Items)
                    {
                        if (DLTSDataFilesListBox.SelectedItems.Contains(DLTSFile) == false)
                        {
                            continue;
                        }
                        if (DLTSPlotOption.SelectedIndex == 0)
                        { }
                        else if ((string)((ComboBoxItem)DLTSPlotOption.SelectedItem).Content == DLTSFile.FileNameShort) { }
                        else { continue; }

                        offsets.Add(0M);
                        previousOffset = previousOffset + 0M;
                    }
                    string line3 = "";
                    for (int j = 0; j < numberofrows; j++)
                    {
                        foreach (DLTSDataFile DLTSFile in DLTSDataFilesListBox.Items)
                        {
                            if (DLTSDataFilesListBox.SelectedItems.Contains(DLTSFile) == false)
                            {
                                continue;
                            }
                            if (DLTSPlotOption.SelectedIndex == 0)
                            { }
                            else if ((string)((ComboBoxItem)DLTSPlotOption.SelectedItem).Content == DLTSFile.FileNameShort) { }
                            else { continue; }

                            if (j < DLTSFile.DLTSSpectrum.Count)
                            {
                                line3 += ",    " + DLTSFile.DLTSSpectrumTemperatures[j].ToString()
                                    + ",    " + DLTSFile.DLTSSpectrum[j].ToString();
                            }
                            else
                            {
                                line3 += "    ,    ,   ";
                            }
                        }
                        line3 = line3.Remove(0, 1);
                        fileStream.WriteLine(line3);
                        line3 = "";
                    }

                }
            }



        }
        private void AddHeaderButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> a = new List<string>();
            a.AddRange(LoadForm.DLTFile.AllKeys[0]);
            List<string> b = new List<string>();
            b.AddRange(LoadForm.DLTFile.AllParameters);
            if (DefectSaveHeadersListBox.SelectedIndex < 0)
            {
                HeadersForSaveDataList.Add(new HeaderForSaveDataClass()
                {
                    index = HeadersForSaveDataList.Count + 1,
                    properties = b,
                    selectedKeysIndex = 0,
                    selectedPropertiesIndex = 0,
                    keys = a
                });
            }
            else
            {
                HeadersForSaveDataList.Insert(DefectSaveHeadersListBox.SelectedIndex, new HeaderForSaveDataClass()
                {
                    index = DefectSaveHeadersListBox.SelectedIndex + 1,
                    properties = b,
                    selectedKeysIndex = 0,
                    selectedPropertiesIndex = 0,
                    keys = a
                });
                for (int i = DefectSaveHeadersListBox.SelectedIndex + 1; i < HeadersForSaveDataList.Count; i++)
                {
                    HeadersForSaveDataList[i].index = i + 1;
                }
            }
            DefectSaveHeadersListBox.Items.Refresh();
        }

        private void RemoveHeaderButton_Click(object sender, RoutedEventArgs e)
        {
            if (DefectSaveHeadersListBox.SelectedIndex < 0) return;
            if (DefectSaveHeadersListBox.SelectedItems == null) return;

            int count = DefectSaveHeadersListBox.SelectedItems.Count;

            for (int i = 0; i < count; i++)
            {
                HeadersForSaveDataList.Remove((HeaderForSaveDataClass)DefectSaveHeadersListBox.SelectedItems[i]);
            }
            DefectSaveHeadersListBox.Items.Refresh();
        }

        private void SavedHeaderTemplatesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SavedHeaderTemplatesComboBox.SelectedIndex < 0) return;
                HeadersForSaveDataList.Clear();
                for (int i = 0; i < HeaderLinesList[SavedHeaderTemplatesComboBox.SelectedIndex].parameters.Length; i++)
                {
                    int parameterIndex = -1;
                    foreach (string param in LoadForm.DLTFile.AllParameters)
                    {
                        if (HeaderLinesList[SavedHeaderTemplatesComboBox.SelectedIndex].parameters[i].Contains(param))
                        {
                            parameterIndex = LoadForm.DLTFile.AllParameters.IndexOf(param);
                            break;
                        }
                    } 
                    int keyIndex = -1;
                    foreach (string keyValue in LoadForm.DLTFile.AllKeys[parameterIndex])
                    {
                        if (HeaderLinesList[SavedHeaderTemplatesComboBox.SelectedIndex].keys[i].Contains(keyValue))
                        {
                            keyIndex = LoadForm.DLTFile.AllKeys[parameterIndex].IndexOf(keyValue);
                            break;
                        }
                    } 
                    List<string> a = new List<string>();
                    a.AddRange(LoadForm.DLTFile.AllKeys[parameterIndex]);
                    List<string> b = new List<string>();
                    b.AddRange(LoadForm.DLTFile.AllParameters);
                    HeadersForSaveDataList.Add(new HeaderForSaveDataClass()
                    {
                        index = HeadersForSaveDataList.Count + 1,
                        properties = b,
                        selectedKeysIndex = keyIndex,
                        selectedPropertiesIndex = parameterIndex,
                        keys = a
                    });
                }
                DefectSaveHeadersListBox.Items.Refresh();
            }
            catch { }
        }

        private void RemoveHeaderTemplateListButton_Click(object sender, RoutedEventArgs e)
        {
            if (SavedHeaderTemplatesComboBox.SelectedIndex < 0) return;

            HeaderLinesList.Remove(HeaderLinesList[SavedHeaderTemplatesComboBox.SelectedIndex]);
            HeaderLinesClass.HeadersNames.RemoveAt(SavedHeaderTemplatesComboBox.SelectedIndex);
            SavedHeaderTemplatesComboBox.Items.RemoveAt(SavedHeaderTemplatesComboBox.SelectedIndex);
            SaveHeadersList();

        }

        private void SaveHeadersList()
        {
            if (HeaderLinesList.Count < 0) return;
            using (StreamWriter writer = new StreamWriter(Path.Combine(MainWindow.ThisWindowInstance.workingdirectory, "DLTSHeadersConfig.txt")))
            {
                writer.WriteLine("[HeaderLines]");
                foreach (var header in HeaderLinesList)
                {
                    string param = header.parameters[0];
                    string keyss = header.keys[0];
                    writer.WriteLine("name=" + HeaderLinesClass.HeadersNames[HeaderLinesList.IndexOf(header)]);
                    for (int i = 1; i < header.parameters.Length; i++)
                    {
                        param += "," + header.parameters[i];
                        keyss += "," + header.keys[i];
                    }
                    writer.WriteLine("parameters=" + param);
                    writer.WriteLine("keys=" + keyss);
                }

            }
        }

        private void RemoveHeader_Click(object sender, RoutedEventArgs e)
        { 
            int i = int.Parse(((MenuItem)e.Source).Name.Substring(1));
            DLTSDataFile.VisibleHeadersKey.RemoveAt(i);
            DLTSDataFile.VisibleHeadersParameter.RemoveAt(i);
            UpdateVisibleDLTSFilesHeaders();
        }

        private void AddHeader_Click(object sender, RoutedEventArgs e)
        { 
            MenuItem m = (MenuItem)e.Source;
            MenuItem mp = (MenuItem)m.Parent;
            string key = (string)m.Header;
            string param = (string)mp.Header;
            foreach (string p in LoadForm.DLTFile.AllParameters)
            {
                if (p == param)
                {
                    int i = LoadForm.DLTFile.AllParameters.IndexOf(p);
                    foreach (string s in LoadForm.DLTFile.AllKeys[i])
                    {
                        if (s == key)
                        {
                            int j = LoadForm.DLTFile.AllKeys[i].IndexOf(s);
                            DLTSDataFile.VisibleHeadersParameter.Add(i);
                            DLTSDataFile.VisibleHeadersKey.Add(j);
                            UpdateVisibleDLTSFilesHeaders();
                            return;
                        }
                    }
                }
            }
        }

        private void AcceptHeaderChanges_Click(object sender, RoutedEventArgs e)
        {
            foreach (DLTSDataFile file in DLTSDataFilesListBox.SelectedItems)
            {
                string[] s = file.AddedHeaders.Split(')');
                if (s.Length <= 0) break;

                foreach (string s2 in s)
                {
                    string[] s3 = s2.Split('(');
                    if (s3.Length != 2) break;
                    s3[0] = s3[0].Replace(" ", "");
                    for (int i = 0; i < LoadForm.DLTFile.AllParameters.Count; i++)
                    {
                        if (LoadForm.DLTFile.AllKeys[i].Contains(s3[0]))
                        {
                            int index = LoadForm.DLTFile.AllKeys[i].IndexOf(s3[0]);
                            file.Properties[i][index] = s3[1];
                        }
                    }
                }
            }

        }

        private void SortDLTSFiles_Click(object sender, RoutedEventArgs e)
        {
            DLTSFilePopupButton.ContextMenu.IsOpen = false; 

            for (int i = 0; i < DLTSDataFiles.Count; i++)
                for (int j = i + 1; j < DLTSDataFiles.Count; j++)
                {
                    if (DLTSDataFiles[i].Emission > DLTSDataFiles[j].Emission)
                    {
                        DLTSDataFile pom = DLTSDataFiles[i];
                        DLTSDataFiles[i] = DLTSDataFiles[j];
                        DLTSDataFiles[j] = pom;
                    }

                }
            DLTSDataFilesListBox.Items.Refresh();

        }
        private void SaveHeaderListButton_Click(object sender, RoutedEventArgs e)
        {
            List<String> lines = new List<string>();

            if (HeadersForSaveDataList.Count < 1) return;

            if (SavedHeaderTemplatesComboBox.Text == "")
            {
                SavedHeaderTemplatesComboBox.Text = "Default";
            }

            int index = -1;
            if (HeaderLinesClass.HeadersNames.Contains(SavedHeaderTemplatesComboBox.Text))
            {
                index = HeaderLinesClass.HeadersNames.IndexOf(SavedHeaderTemplatesComboBox.Text);
            }
            else
            {
                HeaderLinesClass.HeadersNames.Add(SavedHeaderTemplatesComboBox.Text);
                HeaderLinesList.Add(new HeaderLinesClass());
                index = HeaderLinesClass.HeadersNames.Count - 1;
                SavedHeaderTemplatesComboBox.Items.Add(SavedHeaderTemplatesComboBox.Text);
            }
            List<string> keysList = new List<string>();
            List<string> paramList = new List<string>();
            try
            {

                for (int i = 0; i < HeadersForSaveDataList.Count; i++)
                {
                    paramList.Add(HeadersForSaveDataList[i].properties[HeadersForSaveDataList[i].selectedPropertiesIndex]);
                    keysList.Add(HeadersForSaveDataList[i].keys[HeadersForSaveDataList[i].selectedKeysIndex]);
                }
            }
            catch
            {
                MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText), "Info: error while saving headers template list");
            }
            HeaderLinesList[index].parameters = paramList.ToArray();
            HeaderLinesList[index].keys = keysList.ToArray();

            SaveHeadersList();
        }

        private void SaveDefectProperies_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*"
            };
            foreach (DLTSDataFile f in DLTSDataFilesListBox.Items)
            {
                if (DLTSDataFilesListBox.SelectedItems.Contains(f))
                {
                    f.isSelected = true;
                }
                else
                {
                    f.isSelected = false;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                string directory = Path.GetDirectoryName(saveFileDialog.FileName);
                string name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + ".DLTSresults")))
                {  
                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(DLTSDataFiles);
                    writer.Write(data);
                }

                for (int index = 0; index < DLTSDataFile.Peak.DeepLevels.Count; index++)
                {


                    using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + "_" + DLTSDataFile.Peak.DeepLevelNames[index] + ".txt")))
                    {
                        string pom = "";
                        foreach (HeaderForSaveDataClass s in HeadersForSaveDataList)
                        {
                            pom = pom + "," + (LoadForm.DLTFile.AllKeys[s.selectedPropertiesIndex])[s.selectedKeysIndex].Replace(' ', '_') + " ";
                        }
                        pom = pom.Remove(0, 1);
                        writer.WriteLine(pom);


                        int count = DLTSDataFile.Peak.DeepLevels[index].Count;
                        for (int i = 0; i < count; i++)
                        {
                            string line = "";
                            foreach (HeaderForSaveDataClass s in HeadersForSaveDataList)
                            {
                                if (s.selectedPropertiesIndex < 0) continue;
                                if (s.selectedPropertiesIndex > 0)
                                {
                                    string a;
                                    if (false)//(s.selectedPropertiesIndex == 1 && s.selectedKeysIndex == 7)
                                    {
                                        a = (DLTSDataFile.Peak.DeepLevels[index][i].sourceFile.Properties[s.selectedPropertiesIndex])[s.selectedKeysIndex].Replace(' ', '_');
                                    }
                                    else
                                    {
                                        a = (DLTSDataFile.Peak.DeepLevels[index][i].sourceFile.Properties[s.selectedPropertiesIndex])[s.selectedKeysIndex];
                                    }
                                    line = line + string.Format(",{0," + ((LoadForm.DLTFile.AllKeys[s.selectedPropertiesIndex])[s.selectedKeysIndex].Length).ToString("0.#", CultureInfo.InvariantCulture) + "} ", a);
                                }
                                else
                                {
                                    int len = (LoadForm.DLTFile.AllKeys[s.selectedPropertiesIndex])[s.selectedKeysIndex].Length;
                                    if (s.selectedKeysIndex == 0)
                                    {

                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].Temperature.ToString("N", CultureInfo.InvariantCulture));
                                    }
                                    else if (s.selectedKeysIndex == 1)
                                    {
                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].EmRate.ToString("E", CultureInfo.InvariantCulture));
                                    }
                                    else if (s.selectedKeysIndex == 2)
                                    {
                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].Amplitude.ToString("E", CultureInfo.InvariantCulture));
                                    }
                                    else if (s.selectedKeysIndex == 3)
                                    {
                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].AmplitudeCorrected.ToString("E", CultureInfo.InvariantCulture));
                                    }
                                    else if (s.selectedKeysIndex == 4)
                                    {
                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].sourceFile.FileNameShort);
                                    }
                                    else if (s.selectedKeysIndex == 5)
                                    {
                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].sourceFile.FileName);
                                    }
                                    else if (s.selectedKeysIndex == 6)
                                    {
                                        line = line + string.Format(",{0," + (len).ToString("0.#") + "} ", DLTSDataFile.Peak.DeepLevels[index][i].DefectName);
                                    }
                                }
                            }
                            line = line.Remove(0, 1);
                            writer.WriteLine(line);
                        }
                    }
                }
            }
        }

        private void Properties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            int index = DefectSaveHeadersListBox.Items.IndexOf((sender as ComboBox).DataContext);

            if (index < 0) return;
            if ((sender as ComboBox).Name == "keys") return;

            HeadersForSaveDataList[index].selectedPropertiesIndex = (sender as ComboBox).SelectedIndex;
            HeadersForSaveDataList[index].keys.Clear();
            HeadersForSaveDataList[index].keys.AddRange(LoadForm.DLTFile.AllKeys[HeadersForSaveDataList[index].selectedPropertiesIndex]);
            HeadersForSaveDataList[index].selectedKeysIndex = 0;
            DefectSaveHeadersListBox.Items.Refresh();
        }

        private void DLTSFilePopupButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)DLTSFilePopupButton.Content != "Hide more")
            {
                try
                {
                    DLTSFilePopupButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    DLTSFilePopupButton.ContextMenu.PlacementTarget = DLTSFilePopupButton;
                    DLTSFilePopupButton.ContextMenu.IsOpen = true;
                }
                catch { }
                DLTSFilePopupButton.Content = "Hide more";
            }
            else
            {
                DLTSFilePopupButton.ContextMenu.IsOpen = false;
                DLTSFilePopupButton.Content = "Show more";
            }

        }

        private void DLTSFileContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            DLTSFilePopupButton_Click(new object(), new RoutedEventArgs());
        }


    }
     
    public class HeaderLinesClass
    {
        public static List<string> HeadersNames { get; set; }
        public string[] parameters { get; set; }
        public string[] keys { get; set; }
    }

}
