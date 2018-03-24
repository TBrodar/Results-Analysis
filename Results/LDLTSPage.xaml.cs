using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf; 
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq; 
using System.Windows;
using System.Windows.Controls; 
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Results
{
	/// <summary>
	/// Interaction logic for LDLTSPage.xaml
	/// </summary>
	public partial class LDLTSPage : Page
	{

		static public LDLTSPage LDLTSPageInstance { get; set; }
		LoadForm lform = new LoadForm();
		public bool alive = false;
		bool repaint = true;

		public List<HeaderForSaveDataClass> HeadersForSaveDataList { get; set; }

		public SeriesCollection LDLTSPlotCollection { get; set; }
		public List<LDLTSDataFile> LDLTSDataFiles { get; set; }

		public double AxisXStart { get; set; }
		public double AxisXEnd { get; set; }
		public int AxisXPoints { get; set; }

		PythonConsoleClass console = new PythonConsoleClass();

		public class HeaderLinesClass
		{
			public static List<string> HeadersNames { get; set; }
			public string[] parameters { get; set; }
			public string[] keys { get; set; }
		}

		public List<HeaderLinesClass> HeaderLinesList { get; set; }

		public LDLTSPage()
		{

			LDLTSPlotCollection = new SeriesCollection();
			LDLTSDataFile.LDLTSSpectrumFile.Peak.Defects = new List<string>();
			LDLTSPageInstance = this;

			HeadersForSaveDataList = new List<HeaderForSaveDataClass>();

			InitializeComponent();
			LDLTSPlotGraf.DisableAnimations = true;

			LDLTSDataFiles = new List<LDLTSDataFile>();

			DefectsResultsListbox.ItemsSource = LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults;
			DefectsResultsListbox.DataContext = LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults;

			DataContext = this;

			AxisXStart = 0;
			AxisXEnd = -1;
			AxisXPoints = 50;
			alive = true;
			LDLTSDataFile.SplitByOffsetValue = "0";
			SplitByOffsetTextBox.Text = LDLTSDataFile.SplitByOffsetValue;
			
			LoadForm loadf = new LoadForm();

			LDLTSPlotGraf.AxisY[0].LabelFormatter = value => (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.13) ? (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.013) ? value.ToString("N6") : value.ToString("N4") : value.ToString("N2");
			HeaderLinesList = new List<HeaderLinesClass>();
			HeaderLinesClass.HeadersNames = new List<string>();
			if (File.Exists(Path.Combine(MainWindow.ThisWindowInstance.workingdirectory, "HeadersConfig.txt")))
			{
				List<String> lines = new List<string>();
				using (StreamReader reader = new StreamReader(Path.Combine(MainWindow.ThisWindowInstance.workingdirectory, "HeadersConfig.txt")))
				{
					while (reader.Peek() > -1)
						lines.Add(reader.ReadLine());
				}
				List<string> HeaderLines = loadf.getProperties("HeaderLines", ref lines);

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
			if (HeaderLinesList.Count > 0) SavedHeaderTemplatesComboBox.SelectedIndex = 0;
			SavedHeaderTemplatesComboBox.Items.Clear();
			foreach (string s in HeaderLinesClass.HeadersNames) SavedHeaderTemplatesComboBox.Items.Add(s);
		}

		public void LoadLDLTSFile_Click(object sender, RoutedEventArgs e)
		{
			
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Multiselect = true;
			openFileDialog.Filter = "LDLTS data (*.iso)|*.iso|Results data (*.LDLTSresults)|*.LDLTSresults|All(*.*)|*";

			if (openFileDialog.ShowDialog() == true)
			{
				foreach (string Name in openFileDialog.FileNames)
				{
					if (Path.GetExtension(Name).Contains("LDLTSresults"))
					{
						string text = "";
						using (StreamReader reader = new StreamReader(Name))
						{
							text = reader.ReadToEnd();
						}
						List<LDLTSDataFile> filesLoad = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LDLTSDataFile>>(text);
						LDLTSDataFiles.AddRange(filesLoad);
						foreach(LDLTSDataFile f in filesLoad)
						{
							if (f.isSelected == true)
							{
								if (LDLTSDataFilesListBox.Items.IndexOf(f) > -1)
								{
									LDLTSDataFilesListBox.SelectedItems.Add(f);
								}
							}
						}
						break;
					}
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
						continue;
					}

					string shortName = Path.GetFileNameWithoutExtension(Name);
					int number = -1;

					foreach (var f in LDLTSDataFiles)
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


					List<decimal> data = new List<decimal>();
					decimal value;
					foreach (string OneData in lform.getProperties(lform.ldltsisofile.data, ref lines))
					{
						if (!decimal.TryParse(OneData, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
						{
							MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
							"Error : Capacitance transient data is not number. " + OneData + "\n" + Name
							);
							return;
						}
						data.Add(value);
					}

					List<string> ParametersLines = lform.getProperties(lform.ldltsisofile.parameters, ref lines);
					string temperaturestring = lform.getValue(lform.ldltsisofile.parameterskeys.temperature, ref ParametersLines);
					decimal temperature;
					if (!decimal.TryParse(temperaturestring, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out temperature))
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : temperature is not number. " + temperaturestring + "\n" + Name
						);
						continue;
					}

					List<string> GeneratorLines = lform.getProperties(lform.ldltsisofile.acquisition, ref lines);
					string sampleRate = lform.getValue(lform.ldltsisofile.acquisitionkeys.samplingRate, ref GeneratorLines);
					long sampleRateNum;
					if (!long.TryParse(sampleRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out sampleRateNum))
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : Sample rate is not number. " + sampleRate + "\n" + Name
						);
						continue;
					}

					LDLTSDataFile DataFileInstance = new LDLTSDataFile()
					{
						FileName = Name,
						FileNameShort = shortName,
						CapacitanceTransient = data,
						Temperature = temperature,
						SampleRate = sampleRateNum,

						Properties = lform.fillWithValues(ref lines),

						maxCapacitanceTransient = data.Max(),
						MinCapacitanceTransient = data.Min(),

						LDLTSSpectrumFiles = new List<LDLTSDataFile.LDLTSSpectrumFile>(),
						NumericalMethods = new List<string>(),
						SelectedNumericalMethodIndex = 0
					};


					//add LDLTS spectrum files to last LDLTS data file

					foreach (string ext in LDLTSDataFile.ContinFileTypeExpansion)
					{
						addLDLTSSpectrum(Name.Replace(".iso", ext), "Contin (" + ext + ")", ref DataFileInstance);
					}
					foreach (string ext in LDLTSDataFile.FtikregFileTypeExpansion)
					{
						addLDLTSSpectrum(Name.Replace(".iso", ext), "Ftikreg (" + ext + ")", ref DataFileInstance);
					}
					foreach (string ext in LDLTSDataFile.FlogFileTypeExpansion)
					{
						addLDLTSSpectrum(Name.Replace(".iso", ext), "Flog (" + ext + ")", ref DataFileInstance);
					}

					LDLTSDataFiles.Add(DataFileInstance);

				}

				LDLTSDataFilesListBox.Items.Refresh();
			}

		}

		public void addLDLTSSpectrum(string FilePath, string Type, ref LDLTSDataFile LDLTSDataFileInstance)
		{
			List<string> spectrumlines = new List<string>();
			if (!File.Exists(FilePath)) return;

			using (StreamReader reader = new StreamReader(FilePath))
			{
				while (reader.Peek() > -1)
					spectrumlines.Add(reader.ReadLine());
			}

			List<string> ParametersLinesSpectrum = new List<string>();
			ParametersLinesSpectrum = lform.getProperties(lform.ldltsspectrumfile.Parameters, ref spectrumlines);

			decimal freqmin, freqmax;
			string freqminstr = lform.getValue(lform.ldltsspectrumfile.parameterskeys.FrequencyMin, ref ParametersLinesSpectrum);
			if (!decimal.TryParse(freqminstr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out freqmin))
			{
				MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
				"Error : Frequency min is not number. " + "\n" + FilePath
				);
				return;
			}
			string freqmaxstr = lform.getValue(lform.ldltsspectrumfile.parameterskeys.FrequencyMax, ref ParametersLinesSpectrum);
			if (!decimal.TryParse(freqmaxstr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out freqmax))
			{
				MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
				"Error : Frequency max is not number. " + "\n" + FilePath
				);
				return;
			}

			List<string> DataLines = new List<string>();
			List<decimal> EmmisionRate = new List<decimal>();
			List<decimal> SpectarY = new List<decimal>();
			List<decimal> SpectarYError = new List<decimal>();
			List<decimal> SpectarXYNorm = new List<decimal>();
			DataLines = lform.getProperties(lform.ldltsspectrumfile.Spectrum, ref spectrumlines);
			try
			{
				foreach (var dataline in DataLines)
				{
					//   ; Columns mean:
					//   ;| X | Y | err Y | norm * X * Y |

					string Repdataline = dataline.Replace("  ", "|");
					Repdataline = Repdataline.Replace(", ", "|");

					string[] Sdata = Repdataline.Split('|');
					if (Sdata.Length != 4) continue;
					EmmisionRate.Add(decimal.Parse(Sdata[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture));
					SpectarY.Add(decimal.Parse(Sdata[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture));
					SpectarYError.Add(decimal.Parse(Sdata[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture));
					SpectarXYNorm.Add(decimal.Parse(Sdata[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture));
				}
			}
			catch (Exception ex)
			{
				MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
					"Error : While reading LDLTS spectrum file. \n" + FilePath
					);
				return;
			}


			List<string> PeakLinesSpectrum = new List<string>();
			List<LDLTSDataFile.LDLTSSpectrumFile.Peak> SPeaks = new List<LDLTSDataFile.LDLTSSpectrumFile.Peak>();

			PeakLinesSpectrum = lform.getProperties(lform.ldltsspectrumfile.Peaks, ref spectrumlines);
			try
			{
				foreach (string PeakData in PeakLinesSpectrum)
				{

					LDLTSDataFile.LDLTSSpectrumFile.Peak Peak = new LDLTSDataFile.LDLTSSpectrumFile.Peak();
					// ;| em rate  | amplitude |broadening| err ampli | err em rate|
					string RepPeakData = PeakData.Replace("  ", "|");
					RepPeakData = RepPeakData.Replace(", ", "|").Replace(",", ".");

					string[] Speak = RepPeakData.Split('|');

					Peak.Color = System.Windows.Media.Brushes.Transparent;
					if (Speak.Length != 5)
					{
						if (Speak.Length == 6)
						{
							Peak.Color = System.Windows.Media.Brushes.Yellow;
						}
						else continue;
					}
					Peak.EmRate = decimal.Parse(Speak[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
					Peak.Amplitude = decimal.Parse(Speak[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
					Peak.Broadening = decimal.Parse(Speak[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
					Peak.AmplitudeError = decimal.Parse(Speak[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
					Peak.EmRateError = decimal.Parse(Speak[4], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
					Peak.ColorName = System.Windows.Media.Brushes.Black;

					SPeaks.Add(Peak);
				}
			}
			catch (Exception ex)
			{
				MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
					"Error : While reading LDLTS spectrum peaks. \n" + FilePath
					);
				return;
			}
			if (EmmisionRate.Count < 1) { return; }

			LDLTSDataFileInstance.LDLTSSpectrumFiles.Add(new LDLTSDataFile.LDLTSSpectrumFile()
			{
				EmmisionRate = EmmisionRate,
				SpectarY = SpectarY,
				SpectarYError = SpectarYError,
				SpectarXYNorm = SpectarXYNorm,

				FrequencyMin = freqmin,
				FrequencyMax = freqmax,

				MaxSpectarY = SpectarY.Max(),
				MaxSpectarXYNorm = SpectarXYNorm.Max(),

				Peaks = SPeaks,

				NumericalMethod = Type

			});
			LDLTSDataFileInstance.NumericalMethods.Add(Type);

		}

		public void redrawLDLTSPlot()
		{
			if (repaint == false) return;
			if (alive == false) return;

			LDLTSPlotCollection.Clear();
			LDLTSPlotGraf.VisualElements.Clear();
			decimal offsetPrev = 0M;
			if (LDLTSPlotOption.SelectedIndex == 0) // Plot C(t)
			{

				foreach (LDLTSDataFile item in LDLTSDataFilesListBox.Items)
				{
					if (LDLTSDataFilesListBox.SelectedItems.Contains(item) == false)
					{
						continue;
					}
					ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();
					int index = LDLTSDataFilesListBox.Items.IndexOf(item);
					if (index < 0) continue;

					int count = LDLTSDataFiles[index].CapacitanceTransient.Count;

					int step;
					if (AxisXPoints > 500)
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Info : Time needed to plot graph depends on number of points."
						);
					}

					decimal offset;
					if (SplitOptionComboBox.SelectedIndex == 0)
					{
						if (LDLTSDataFilesListBox.SelectedItems.IndexOf(item) == 0)
						{
							offset = item.maxCapacitanceTransient;
							offsetPrev = item.maxCapacitanceTransient - item.MinCapacitanceTransient;

						}
						else
						{
							offset = item.maxCapacitanceTransient + offsetPrev;
							offsetPrev = offsetPrev + (item.maxCapacitanceTransient - item.MinCapacitanceTransient);
						}
					}
					else if (SplitOptionComboBox.SelectedIndex == 1)
					{
						offset = item.MinCapacitanceTransient;
					}
					else
					{
						offset = 0M;
					}

					if (LDLTSScaleTypeComboBox.SelectedIndex == 1)
					{
						if (AxisXEnd < 0 || AxisXEnd > LDLTSDataFiles[index].CapacitanceTransient.Count)
						{
							AxisXEnd = LDLTSDataFiles[index].CapacitanceTransient.Count;
						}


						if (AxisXEnd - AxisXStart > AxisXPoints)
						{
							step = (int)Math.Round(AxisXEnd - AxisXStart) / AxisXPoints;
						}
						else step = 1;

						for (int i = (int)Math.Round(AxisXStart); i < AxisXEnd; i += step)
						{
							s.Add(new ObservablePoint((double)i, (double)(LDLTSDataFiles[index].CapacitanceTransient[i] - offset)));
						}
					}
					else
					{
						int end;
						if (AxisXEnd < 0 || (int)Math.Round(AxisXEnd * LDLTSDataFiles[index].SampleRate) > LDLTSDataFiles[index].CapacitanceTransient.Count)
						{
							end = LDLTSDataFiles[index].CapacitanceTransient.Count;
						}
						else
						{
							end = (int)Math.Round(AxisXEnd * LDLTSDataFiles[index].SampleRate);
						}

						if (end - (int)Math.Round(AxisXStart * LDLTSDataFiles[index].SampleRate) > (double)AxisXPoints)
						{
							step = (end - (int)Math.Round(AxisXStart * LDLTSDataFiles[index].SampleRate)) / AxisXPoints;
						}
						else step = 1;

						for (int i = (int)Math.Round(AxisXStart * LDLTSDataFiles[index].SampleRate); i < end; i += step)
						{
							s.Add(new ObservablePoint(((double)i) / LDLTSDataFiles[index].SampleRate, (double)(LDLTSDataFiles[index].CapacitanceTransient[i] - offset)));
						}
					}

					LDLTSPlotCollection.Add(
						 new LineSeries
						 {
							 Title = LDLTSDataFiles[index].FileNameShort,
							 LineSmoothness = 0,
							 Fill = System.Windows.Media.Brushes.Transparent,
							 Values = s
						 });

				}
			}
			else if (LDLTSPlotOption.SelectedIndex == 2 || LDLTSPlotOption.SelectedIndex == 1)
			{
				foreach (LDLTSDataFile item in LDLTSDataFilesListBox.Items)  // Plot LDLTS spectra
				{
					if (LDLTSDataFilesListBox.SelectedItems.Contains(item) == false)
					{
						continue;
					}
					if (item.LDLTSSpectrumFiles.Count == 0)
					{
						continue;
					}
					decimal offset;

					if (item == null) continue;
					if (item.SelectedNumericalMethodIndex < 0) continue;

					int NumericalMethodIndex = item.SelectedNumericalMethodIndex;

					ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();
					int SpectrumPointsCount = item.LDLTSSpectrumFiles[NumericalMethodIndex].EmmisionRate.Count;

					if (SplitOptionComboBox.SelectedIndex == 0)
					{
						if (LDLTSPlotOption.SelectedIndex == 1)
						{
							offset = item.LDLTSSpectrumFiles[NumericalMethodIndex].MaxSpectarY + offsetPrev;
						}
						else
						{
							offset = item.LDLTSSpectrumFiles[NumericalMethodIndex].MaxSpectarXYNorm + offsetPrev;
						}
						offsetPrev = offset;
					} else if (SplitOptionComboBox.SelectedIndex == 1 || SplitOptionComboBox.SelectedIndex == 2)
					{
						offset = 0M;
					} else
					{
						decimal value;
						if(!decimal.TryParse(SplitByOffsetTextBox.Text,out value)){
							SplitByOffsetTextBox.Background = Brushes.Red;
							value = 0;
						} else
						{
							SplitByOffsetTextBox.Background = null;
							
						}
						offset = value + offsetPrev;
						offsetPrev = offset;
					}

					if (LDLTSPlotOption.SelectedIndex == 1)
					{
						for (int i = 0; i < SpectrumPointsCount; i++)
						{
							s.Add(new ObservablePoint(Math.Log10((double)item.LDLTSSpectrumFiles[NumericalMethodIndex].EmmisionRate[i]), (double)(item.LDLTSSpectrumFiles[NumericalMethodIndex].SpectarY[i] - offset)));
						}
					}
					else
					{
						for (int i = 0; i < SpectrumPointsCount; i++)
						{
							s.Add(new ObservablePoint(Math.Log10((double)item.LDLTSSpectrumFiles[NumericalMethodIndex].EmmisionRate[i]), (double)(item.LDLTSSpectrumFiles[NumericalMethodIndex].SpectarXYNorm[i] - offset)));
						}
					}
					try
					{
						LDLTSPlotCollection.Add(
						 new LineSeries
						 {
							 Title = item.FileNameShort + "\n(" + item.NumericalMethods[NumericalMethodIndex] + ", " + SpectrumPointsCount.ToString("N", CultureInfo.InvariantCulture) + ")",
							 LineSmoothness = 0,
							 Fill = Brushes.Transparent,
							 PointGeometry = null,
							 Values = s
						 });
					}
					catch (System.NullReferenceException nullex1)
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : LiveChart NullReferenceException. (LDLTSPlotCollection.add func)"
						);
					}

					foreach (LDLTSDataFile.LDLTSSpectrumFile.Peak peak in item.LDLTSSpectrumFiles[item.SelectedNumericalMethodIndex].Peaks)
					{
						if (peak.DefectName == null || peak.DefectName == "") continue;
						try
						{
							LDLTSPlotGraf.VisualElements.Add(new VisualElement
							{
								X = Math.Log10((double)peak.EmRate),
								Y = (double)(-offset),
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Top,
								UIElement = new TextBlock //notice this property must be a wpf control
								{
									Text = peak.DefectName,
									FontWeight = FontWeights.Bold,
									FontSize = 16,
									Opacity = 0.6
								}
							});
						}
						catch (System.NullReferenceException nullex)
						{
							MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
							"Error : LiveChart NullReferenceException. (VisualElements.add func)"
							);
						}
					}
				}
			}
			else if (LDLTSPlotOption.SelectedIndex == 3)
			{
				FillDefectList();
				if (LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults == null) return;
				if (LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults.Count < 1) return;

				foreach (DefectResult item in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)  // Plot LDLTS spectra
				{
					if (item == null) continue;
					if (item.Temperatures.Count < 1) continue;
					int count = item.Temperatures.Count;

					ChartValues<ObservablePoint> s = new ChartValues<ObservablePoint>();

					for (int i = 0; i < count; i++)
					{
						s.Add(new ObservablePoint(((double)(1.0M / (item.Temperatures[i] * 8.61733E-5M))), Math.Log((double)(item.Emissions[i] / (item.Temperatures[i] * item.Temperatures[i])))));
					}
					ScatterSeries ScatterSeriesInstance = new ScatterSeries
					{
						Title = item.DefectName,
						Values = s
					};

					LDLTSPlotCollection.Add(ScatterSeriesInstance);

					if (item.Energy != 0)
					{
						ChartValues<ObservablePoint> s2 = new ChartValues<ObservablePoint>();
						s2.Add(new ObservablePoint((double)(1.0M / (item.Temperatures[0] * 8.61733E-5M)), (double)(item.B - item.Energy * (1.0M / (item.Temperatures[0] * 8.61733E-5M)))));
						s2.Add(new ObservablePoint((double)(1.0M / (item.Temperatures[count - 1] * 8.61733E-5M)), (double)(item.B - item.Energy * (1.0M / (item.Temperatures[count - 1] * 8.61733E-5M)))));

						// "E = {0:0.##E+00} ± " "{}{0:0.##E+00} eV" "σ = {0:0.##E+00} cm^2"

						LDLTSPlotCollection.Add(
							 new LineSeries
							 {
								 Title = string.Format("{0} E = {1:0.##E+00} ± {2:0.##E+00} \nσ = {0:0.##E+00} cm^2", item.DefectName, item.Energy, item.EnergyDeviation, item.CrossSection),
								 Values = s2,
								 PointGeometry = null,
								 Fill = Brushes.Transparent
							 }
							 );
					}

				}
			}

		}

		private void SelectAll_Click(object sender, RoutedEventArgs e)
		{
			repaint = false;
			LDLTSDataFilesListBox.SelectAll();
			repaint = true;
			redrawLDLTSPlot();
		}

		private void Deselectall_Click(object sender, RoutedEventArgs e)
		{
			repaint = false;
			LDLTSDataFilesListBox.SelectedIndex = -1;
			repaint = true;
			redrawLDLTSPlot();
		}

		private void LDLTSPlotOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (alive == false) return;

			if (LDLTSPlotOption.SelectedIndex == 0)
			{
				LDLTSPlotGraf.AxisX.Add(new Axis());
				LDLTSPlotGraf.AxisX.RemoveAt(0);

				LDLTSPlotGraf.AxisY[0].Title = "Capacitance (pF)";
				LDLTSPlotGraf.AxisY[0].LabelFormatter = value => (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.13) ? (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.013) ? value.ToString("N6") : value.ToString("N4") : value.ToString("N2");
				CtOption.Visibility = Visibility.Visible;
				if (LDLTSScaleTypeComboBox.SelectedIndex == 0)
				{
					LDLTSPlotGraf.AxisX[0].Title = "Time (s)";
				}
				else
				{
					LDLTSPlotGraf.AxisX[0].Title = "Sample";
				}
			}
			else if (LDLTSPlotOption.SelectedIndex == 1)
			{
				LDLTSPlotGraf.AxisX.Add(new LogarithmicAxis()
				{
					Base = 10,
					LabelFormatter = value => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:E2}", Math.Pow(10, value))
				});
				LDLTSPlotGraf.AxisX.RemoveAt(0);

				LDLTSPlotGraf.AxisX[0].Title = "Emmision (1/s)";
				LDLTSPlotGraf.AxisY[0].Title = "Y (pF s)";
				LDLTSPlotGraf.AxisY[0].LabelFormatter = value => (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.13) ? (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.013) ? value.ToString("N6") : value.ToString("N4") : value.ToString("N2");
				CtOption.Visibility = Visibility.Collapsed;
			}
			else if (LDLTSPlotOption.SelectedIndex == 2)
			{
				LDLTSPlotGraf.AxisX.Add(new LogarithmicAxis()
				{
					Base = 10,
					LabelFormatter = value => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:E2}", Math.Pow(10, value))
				});
				LDLTSPlotGraf.AxisX.RemoveAt(0);

				LDLTSPlotGraf.AxisX[0].Title = "Emmision (1/s)";
				LDLTSPlotGraf.AxisY[0].Title = "Norm XY";
				LDLTSPlotGraf.AxisY[0].LabelFormatter = value => (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.13) ? (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.013) ? value.ToString("N6") : value.ToString("N4") : value.ToString("N2");
				CtOption.Visibility = Visibility.Collapsed;
			}
			else if (LDLTSPlotOption.SelectedIndex == 3)
			{
				LDLTSPlotGraf.AxisX.Add(new Axis());
				LDLTSPlotGraf.AxisX.RemoveAt(0);

				LDLTSPlotGraf.AxisY[0].Title = "ln(e/T^2 *(1 sK^2))";
				LDLTSPlotGraf.AxisY[0].LabelFormatter = value => (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.13) ? (LDLTSPlotGraf.AxisY[0].ActualMaxValue - LDLTSPlotGraf.AxisY[0].ActualMinValue < 0.013) ? value.ToString("N6") : value.ToString("N4") : value.ToString("N2");
				CtOption.Visibility = Visibility.Collapsed;
				LDLTSPlotGraf.AxisX[0].Title = "1/(kB T) [eV^-1]";
			}
			redrawLDLTSPlot();
		}

		private void LDLTSDataFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (alive == false) return;

			LDLTSFileComboBox.Items.Clear();
			if (LDLTSDataFilesListBox.SelectedIndex < 0) return;
			
			foreach (LDLTSDataFile s in LDLTSDataFilesListBox.Items)
			{
				if (LDLTSDataFilesListBox.SelectedItems.Contains(s) == false)
				{
					continue;
				}
				LDLTSFileComboBox.Items.Add(s.FileNameShort);
			}

			LDLTSFileComboBox.SelectedIndex = 0;
			redrawLDLTSPlot();

		}

		private void AxisXEndComboBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (AxisXEndComboBox.Text.Equals("end"))
			{
				AxisXEnd = -1;
				AxisXEndComboBox.BorderThickness = new Thickness(1);
				return;
			}
			double end;
			if (double.TryParse(AxisXEndComboBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out end))
			{
				AxisXEnd = end;
				AxisXEndComboBox.BorderThickness = new Thickness(1);
				return;
			}
			AxisXEndComboBox.BorderThickness = new Thickness(3);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				redrawLDLTSPlot();
			}
			catch (System.Windows.Media.Animation.AnimationException exc)
			{
			}
		}

		private void NextLDLTSFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (checkPeakLabels() == false)
			{
				return;
			}

			if (LDLTSFileComboBox.SelectedIndex < 0)
			{
				LDLTSFileComboBox.SelectedIndex = 0;
			}
			else
			{
				if (LDLTSFileComboBox.SelectedIndex == LDLTSFileComboBox.Items.Count - 1)
				{
					LDLTSFileComboBox.SelectedIndex = 0;
				}
				else
				{
					LDLTSFileComboBox.SelectedIndex = LDLTSFileComboBox.SelectedIndex + 1;
				}
			}
		}

		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			if (LDLTSDataFilesListBox.SelectedIndex < 0) return;
			if (LDLTSDataFilesListBox.SelectedItem == null) return;

			int count = LDLTSDataFilesListBox.SelectedItems.Count;
			repaint = false;
			for (int i = 0; i < count; i++)
			{
				LDLTSDataFiles.Remove((LDLTSDataFile)LDLTSDataFilesListBox.SelectedItems[i]);
			}
			repaint = true;
			LDLTSDataFilesListBox.Items.Refresh();
			redrawLDLTSPlot();

		}

		private bool checkPeakLabels()
		{
			List<string> pomoc = new List<string>();
			bool ok = true;
			foreach (LDLTSDataFile.LDLTSSpectrumFile.Peak peak in PeaksListBox.Items)
			{
				if (peak.DefectName == null) continue;
				if (peak.DefectName == "") continue;

				if (pomoc.Contains(peak.DefectName) == false)
				{
					pomoc.Add(peak.DefectName);
					peak.ColorName = System.Windows.Media.Brushes.Black;
				}
				else
				{
					peak.ColorName = System.Windows.Media.Brushes.Red;
					ok = false;
				}
			}
			if (ok == false)
			{
				LDLTSFileComboBox.Foreground = System.Windows.Media.Brushes.Blue;
				PeaksListBox.Items.Refresh();
				return false;
			}
			else
			{
				LDLTSFileComboBox.Foreground = System.Windows.Media.Brushes.Black;
			}
			return true;
		}

		private void LDLTSFileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.RemovedItems.Count > 0)
			{
				if (checkPeakLabels() == false)
				{
					return;
				}
			}



			foreach (LDLTSDataFile s in LDLTSDataFilesListBox.SelectedItems)
			{
				if (s.FileNameShort.Equals(LDLTSFileComboBox.SelectedItem))
				{
					PeaksListBox.Items.Clear();
					if (s.LDLTSSpectrumFiles.Count > 0)
						foreach (var item in s.LDLTSSpectrumFiles[s.SelectedNumericalMethodIndex].Peaks)
						{
							PeaksListBox.Items.Add(item);
						}
					return;
				}
			}
		}

		private void DefectNamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

			if (sender == null)
			{
				return;
			}

			int index = PeaksListBox.Items.IndexOf(((ComboBox)sender).DataContext);
			if (index < 0)
			{
				return;
			}
			if (((ComboBox)sender).Text == null) return;
			if (((ComboBox)sender).Text == "") return;

			((LDLTSDataFile.LDLTSSpectrumFile.Peak)PeaksListBox.Items[index]).DefectName = ((ComboBox)sender).Text;

		}

		private void AddDefect_Click(object sender, RoutedEventArgs e)
		{
			int index = PeaksListBox.Items.IndexOf(((Button)sender).DataContext);

			if (((LDLTSDataFile.LDLTSSpectrumFile.Peak)PeaksListBox.Items[index]).DefectName == null) return;
			if (((LDLTSDataFile.LDLTSSpectrumFile.Peak)PeaksListBox.Items[index]).DefectName == "") return;

			foreach (string pom in LDLTSDataFile.LDLTSSpectrumFile.Peak.Defects)
			{
				if (pom.Equals(((LDLTSDataFile.LDLTSSpectrumFile.Peak)PeaksListBox.Items[index]).DefectName))
				{
					return;
				}
			}
			LDLTSDataFile.LDLTSSpectrumFile.Peak.Defects.Add(((LDLTSDataFile.LDLTSSpectrumFile.Peak)PeaksListBox.Items[index]).DefectName);
			PeaksListBox.Items.Refresh();

		}

		private void GetResult_Click(object sender, RoutedEventArgs e)
		{
			if (console.alive == true)
			{
				if (console.busy == true)
				{
					MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Info: Python console is alive and busy. "
						);
					return;
				}
			}
			else
			{
				console.startPythonConsole();
				MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Info: starting Python console. "
						);
			}

			List<string> spom;
			FillDefectList();
			// calculate Results
			System.Threading.Tasks.Task Zadatak = new System.Threading.Tasks.Task(() =>
			{
				foreach (DefectResult res in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)
				{
					int count = res.Temperatures.Count;

					if (count < 3)
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						  "Info: OLS - Less than 3 points for defect (" + res.DefectName + ")"
						  );
						continue;
					}

					double[] X = new double[count];
					double[] Y = new double[count];

					for (int i = 0; i < count; i++)
					{
						X[i] = (double)((1.0M / 8.61733E-5M) / res.Temperatures[i]);
						Y[i] = (Math.Log((double)(res.Emissions[i] / (res.Temperatures[i] * res.Temperatures[i]))));
					}

					spom = new List<string>();
					spom.Add(console.runOLS);


					string s = X[0].ToString("E10", CultureInfo.InvariantCulture);
					for (int i = 1; i < count; i++)
					{
						s = s + "|" + X[i].ToString("E10", CultureInfo.InvariantCulture);
					}
					spom.Add(s);
					s = Y[0].ToString("E10", CultureInfo.InvariantCulture);
					for (int i = 1; i < count; i++)
					{
						s = s + "|" + Y[i].ToString("E10", CultureInfo.InvariantCulture);
					}
					spom.Add(s);
					console.WriteLines(spom);
					spom = console.ReadLines(2);


					if (spom == null) return;

					string[] stringList = spom[0].Split('|');
					decimal value;


					if (!decimal.TryParse(stringList[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : Energy is not number. " + stringList[1]
						);
						return;
					}
					res.Energy = -value;

					if (!decimal.TryParse(stringList[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : cross section is not number. " + stringList[0]
						);
						return;
					}
					res.B = value;

					stringList = spom[1].Split('|');
					if (!decimal.TryParse(stringList[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : Energy standard deviation is not number. " + stringList[1]
						);
						return;
					}
					res.EnergyDeviation = value;
					if (!decimal.TryParse(stringList[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
						"Error : cross section standard deviation is not number. " + stringList[0]
						);
						return;
					}

					try
					{
						res.BDeviation = value;
						res.CrossSection = (Math.Exp((double)res.B) / 3.625E21);
						res.CrossSectionDeviation = res.CrossSection * (double)res.BDeviation;

					}
					catch (Exception exce)
					{
						MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
							"Error : Cross section is to large for double or decimal type. "
							);
					}

				}
				LDLTSPageInstance.DefectsResultsListbox.Dispatcher.BeginInvoke(new voidDelegate(UpdateDefectsResultsListBox));
			});
			Zadatak.Start();
		}

		public void onExit()
		{
			console.WriteLines(new List<string>() { "Exit" });
			console.KillPythonConsole();
		}

		private void FillDefectList()
		{
			ObservableCollection<DefectResult> defres = new ObservableCollection<DefectResult>();

			foreach (LDLTSDataFile item in LDLTSDataFilesListBox.Items)
			{
				if (LDLTSDataFilesListBox.SelectedItems.Contains(item) == false)
				{
					continue;
				}
				if (item.LDLTSSpectrumFiles.Count == 0)
				{
					continue;
				}
				//Fill Defects results by points and then calculate E, sigma
				foreach (LDLTSDataFile.LDLTSSpectrumFile.Peak peak in item.LDLTSSpectrumFiles[item.SelectedNumericalMethodIndex].Peaks)
				{

					if (peak.DefectName == null) continue;
					if (peak.DefectName == "") continue;

					// Find if defect allready exist in defect results 
					int index = -1;
					foreach (DefectResult res in defres)
					{

						if (res.DefectName.Equals(peak.DefectName))
						{
							index = defres.IndexOf(res);
							break;
						}
					}

					// add item to DefectsResults if defect allready doesn't exist in it
					if (index == -1)
					{
						defres.Add(new DefectResult()
						{
							DefectName = peak.DefectName,
							Temperatures = new List<decimal>(),
							Emissions = new List<decimal>(),
							EmissionsDevioations = new List<decimal>(),
							Amplitudes = new List<decimal>(),
							AmplitudesDeviations = new List<decimal>(),
							Broadenings = new List<decimal>(),
							SourceFiles = new List<LDLTSDataFile>()

						});
						index = defres.Count - 1;
					}

					// add point
					defres[index].Temperatures.Add(item.Temperature);
					defres[index].Emissions.Add(peak.EmRate);
					defres[index].EmissionsDevioations.Add(peak.EmRateError);
					defres[index].Amplitudes.Add(peak.Amplitude);
					defres[index].AmplitudesDeviations.Add(peak.AmplitudeError);
					defres[index].Broadenings.Add(peak.Broadening);
					defres[index].SourceFiles.Add(item);
				}
			}

			foreach (DefectResult res in defres)
			{
				int count = res.Temperatures.Count;

				// sort points by temperature
				for (int i = 0; i < count; i++)
				{
					for (int j = i + 1; j < count; j++)
					{
						if (res.Temperatures[i] > res.Temperatures[j])
						{
							decimal pom = res.Temperatures[i];
							res.Temperatures[i] = res.Temperatures[j];
							res.Temperatures[j] = pom;

							pom = res.Emissions[i];
							res.Emissions[i] = res.Emissions[j];
							res.Emissions[j] = pom;

							pom = res.EmissionsDevioations[i];
							res.EmissionsDevioations[i] = res.EmissionsDevioations[j];
							res.EmissionsDevioations[j] = pom;

							pom = res.Amplitudes[i];
							res.Amplitudes[i] = res.Amplitudes[j];
							res.Amplitudes[j] = pom;

							pom = res.AmplitudesDeviations[i];
							res.AmplitudesDeviations[i] = res.AmplitudesDeviations[j];
							res.AmplitudesDeviations[j] = pom;

							pom = res.Broadenings[i];
							res.Broadenings[i] = res.Broadenings[j];
							res.Broadenings[j] = pom;

							LDLTSDataFile item = res.SourceFiles[i];
							res.SourceFiles[i] = res.SourceFiles[j];
							res.SourceFiles[j] = item;
						}
					}
				}
			}

			if (LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults != null)
				foreach (DefectResult res in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)
				{
					foreach (DefectResult dres in defres)
					{
						if (dres.DefectName == res.DefectName)
						{
							dres.Energy = res.Energy;
							dres.EnergyDeviation = res.EnergyDeviation;
							dres.CrossSection = res.CrossSection;
							dres.CrossSectionDeviation = res.CrossSectionDeviation;
							dres.B = res.B;
							dres.BDeviation = res.BDeviation;
							break;
						}
					}
				}
			LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults = defres;
		}

		private delegate void voidDelegate();

		private void UpdateDefectsResultsListBox()
		{
			DefectsResultsListbox.Items.Clear();
			foreach (DefectResult res in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)
			{
				DefectsResultsListbox.Items.Add(res);
			}
			DefectsResultsListbox.Items.Refresh();
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (DefectsResultsListbox.SelectedItems.Equals(null) || DefectsResultsListbox.SelectedItems.Count < 1)
			{
				MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
					"Info : Select wich results should be saved. "
					);
				return;
			}
			

			SaveFileDialog saveFileDialog = new SaveFileDialog()
			{
				Filter = "Text Files(*.txt)|*.txt|All(*.*)|*"
			};

			if (saveFileDialog.ShowDialog() == true)
			{
				string directory = Path.GetDirectoryName(saveFileDialog.FileName);
				string name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

				using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + "_DefectsResults.txt")))
				{
					foreach (DefectResult def in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)
					{
						writer.WriteLine("Name: " + def.DefectName);
						writer.WriteLine(string.Format("Energy (eV): {0:N} ± {1:N}", def.Energy, def.EnergyDeviation));
						writer.WriteLine(string.Format("B (eV/K^2): {0:0.##E+00} ± {1:0.##E+00}", def.B, def.BDeviation));
						writer.WriteLine(string.Format("Cross section (cm^2): {0:0.##E+00}", def.CrossSection));
						writer.WriteLine();
					}
				}

				//foreach (DefectResult def in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)
				//{
				//    using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + "_" + def.DefectName + ".txt")))
				//    {
				//        writer.WriteLine(string.Format("{0,15} {1,15} {2,16} {3,23} {4,24} {5,15}",
				//                         "Temperature(K)",
				//                         "Emission(eV)",
				//                         "StandardDeviation(eV)",
				//                         "△CAmplitude(pF)",
				//                         "△CStandandDeviation(pF)",
				//                         "Broadening"));
				//        for (int i = 0; i < def.Emissions.Count; i++)
				//        {
				//            writer.WriteLine(string.Format("{0,15} {1,15} {2,16} {3,23} {4,24} {5,15}",
				//                         def.Temperatures[i].ToString("N", CultureInfo.InvariantCulture).PadLeft(15, ' '),
				//                         def.Emissions[i].ToString("E", CultureInfo.InvariantCulture).PadLeft(15, ' '),
				//                         def.EmissionsDevioations[i].ToString("E", CultureInfo.InvariantCulture).PadLeft(16, ' '),
				//                         def.Amplitudes[i].ToString("E", CultureInfo.InvariantCulture).PadLeft(23, ' '),
				//                         def.AmplitudesDeviations[i].ToString("E", CultureInfo.InvariantCulture).PadLeft(24, ' '),
				//                         def.Broadenings[i].ToString("E", CultureInfo.InvariantCulture).PadLeft(15, ' ')
				//                         ));
				//        }
				//    }
				//} 
			}
		}

		private void AddHeaderButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> a = new List<string>();
			a.AddRange(LoadForm.LDLTSIsoFile.AllKeys[0]);
			List<string> b = new List<string>();
			b.AddRange(LoadForm.LDLTSIsoFile.AllParameters);
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
			} else
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

		private void Properties_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

			int index = DefectSaveHeadersListBox.Items.IndexOf((sender as ComboBox).DataContext);

			if (index < 0) return;
			if ((sender as ComboBox).Name == "keys") return;

			HeadersForSaveDataList[index].selectedPropertiesIndex = (sender as ComboBox).SelectedIndex;
			HeadersForSaveDataList[index].keys.Clear();
			HeadersForSaveDataList[index].keys.AddRange(LoadForm.LDLTSIsoFile.AllKeys[HeadersForSaveDataList[index].selectedPropertiesIndex]);
			HeadersForSaveDataList[index].selectedKeysIndex = 0;
			DefectSaveHeadersListBox.Items.Refresh();
		}

		private void SaveDefectProperies_Click(object sender, RoutedEventArgs e)
		{
			FillDefectList();

			SaveFileDialog saveFileDialog = new SaveFileDialog()
			{
				Filter = "Text Files(*.txt)|*.txt|All(*.*)|*"
			};
			foreach (LDLTSDataFile f in LDLTSDataFilesListBox.Items)
			{
				if (LDLTSDataFilesListBox.SelectedItems.Contains(f))
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

				using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + ".LDLTSresults")))
				{
					string data = Newtonsoft.Json.JsonConvert.SerializeObject(LDLTSDataFiles);
					writer.Write(data);
				}

					foreach (DefectResult def in LDLTSDataFile.LDLTSSpectrumFile.Peak.DefectsResults)
				{
					using (StreamWriter writer = new StreamWriter(Path.Combine(directory, name + "_" + def.DefectName + ".txt")))
					{
						string pom = "";
						foreach (HeaderForSaveDataClass s in HeadersForSaveDataList)
						{
							pom = pom + (LoadForm.LDLTSIsoFile.AllKeys[s.selectedPropertiesIndex])[s.selectedKeysIndex].Replace(' ', '_') + " ";
						}
						writer.WriteLine(pom);


						int count = def.Emissions.Count;
						for (int i = 0; i < count; i++)
						{
							string line = "";
							foreach (HeaderForSaveDataClass s in HeadersForSaveDataList)
							{
								if (s.selectedPropertiesIndex < 0) continue;
								if (s.selectedPropertiesIndex > 0)
								{
									line = line + string.Format("{0," + ((LoadForm.LDLTSIsoFile.AllKeys[s.selectedPropertiesIndex])[s.selectedKeysIndex].Length).ToString("0.#", CultureInfo.InvariantCulture) + "} ", (def.SourceFiles[i].Properties[s.selectedPropertiesIndex])[s.selectedKeysIndex]);
								}
								else
								{
									int len = (LoadForm.LDLTSIsoFile.AllKeys[s.selectedPropertiesIndex])[s.selectedKeysIndex].Length;
									if (s.selectedKeysIndex == 0)
									{

										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.Temperatures[i].ToString("N", CultureInfo.InvariantCulture));
									}
									else if (s.selectedKeysIndex == 1)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.Emissions[i].ToString("E", CultureInfo.InvariantCulture));
									}
									else if (s.selectedKeysIndex == 2)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.EmissionsDevioations[i].ToString("E", CultureInfo.InvariantCulture));
									}
									else if (s.selectedKeysIndex == 3)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.Amplitudes[i].ToString("E", CultureInfo.InvariantCulture));
									}
									else if (s.selectedKeysIndex == 4)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.AmplitudesDeviations[i].ToString("E", CultureInfo.InvariantCulture));
									}
									else if (s.selectedKeysIndex == 5)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.Broadenings[i].ToString("N", CultureInfo.InvariantCulture));
									}
									else if (s.selectedKeysIndex == 6)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.SourceFiles[i].NumericalMethods[def.SourceFiles[i].SelectedNumericalMethodIndex].Replace(' ', '_'));
									}
									else if (s.selectedKeysIndex == 7)
									{
										line = line + string.Format("{0," + (len).ToString("0.#") + "} ", def.SourceFiles[i].FileNameShort);
									}
								}
							}

							writer.WriteLine(line);
						}
					}
				}
			}
		}

		private void SameNumericalMethodContexMenu_Click(object sender, RoutedEventArgs e)
		{
			int selectedNumericalMethodIndex = ((LDLTSDataFile)(sender as MenuItem).DataContext).SelectedNumericalMethodIndex;
			string selectedNumericalMethod = ((LDLTSDataFile)(sender as MenuItem).DataContext).NumericalMethods[selectedNumericalMethodIndex];

			foreach (LDLTSDataFile f in LDLTSDataFiles)
			{
				int i = f.NumericalMethods.Count - 1;
				for (; i > -1; i--)
				{
					if (f.NumericalMethods[i] == selectedNumericalMethod)
					{
						break;
					}
				}
				if (i > -1)
				{
					f.SelectedNumericalMethodIndex = i;
				}
			}
			LDLTSDataFilesListBox.Items.Refresh();
		}

		private void SortLDLTSFiles_Click(object sender, RoutedEventArgs e)
		{

			for (int i = 0; i < LDLTSDataFiles.Count; i++)
				for (int j = i + 1; j < LDLTSDataFiles.Count; j++)
				{
					if (LDLTSDataFiles[i].Temperature > LDLTSDataFiles[j].Temperature)
					{
						LDLTSDataFile pom = LDLTSDataFiles[i];
						LDLTSDataFiles[i] = LDLTSDataFiles[j];
						LDLTSDataFiles[j] = pom;
					}

				}
			LDLTSDataFilesListBox.Items.Refresh();

		}

		private void DataToolTipToggleButton_Click(object sender, RoutedEventArgs e)
		{
			if (alive == false) return;
			if (DataToolTipToggleButton.IsChecked == true)
			{
				LDLTSPlotGraf.Hoverable = true;
				LDLTSPlotGraf.DataTooltip = new DefaultTooltip();
			}
			else
			{
				LDLTSPlotGraf.Hoverable = false;
				LDLTSPlotGraf.DataTooltip = null;
			}
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
					if (LDLTSPlotOption.SelectedIndex != 1 && LDLTSPlotOption.SelectedIndex != 2)
					{
						return;
					}
					decimal value;
					if (!decimal.TryParse(SplitByOffsetTextBox.Text, out value))
					{
						SplitByOffsetTextBox.Background = Brushes.Red;
						value = 0;
						return;
					}
					else
					{
						SplitByOffsetTextBox.Background = null;
					}
					string line = "";
					string line0 = "";
					foreach (var LDLTSFile in LDLTSDataFiles)
					{
						line0 += "    emission(1/s)    SpectarXYNorm    SpectarXYNormWithOffset";
						line += "    " + LDLTSFile.FileNameShort.Replace(" ", "_") + "    Temperature=" + LDLTSFile.Temperature.ToString() + "K";
						if (LDLTSPlotOption.SelectedIndex == 2)
						{
							line += "    _";
						}
						else if (LDLTSPlotOption.SelectedIndex == 1)
						{
							line0 += "    emission(1/s)    SpectarY    SpectarYError + SpectarYErrorWithOffset";
							line += "     _    _";
						}
					}
					fileStream.WriteLine(line0);
					fileStream.WriteLine(line);
					int numberofrows = 0;
					foreach(var LDLTSFile in LDLTSDataFiles)
					{
						if (LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].EmmisionRate.Count > numberofrows)
						{
							numberofrows = LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].EmmisionRate.Count;
						}
					}
					string line3 = "";
					if (LDLTSPlotOption.SelectedIndex == 2)
					{
						for (int j = 0; j < numberofrows; j++)
							foreach (var LDLTSFile in LDLTSDataFiles)
							{
								if (j < LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].EmmisionRate.Count)
								{
									line3 += "    " + LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].EmmisionRate[j].ToString()
										+ "    " + LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].SpectarXYNorm[j].ToString()
										+ "    " + (LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].SpectarXYNorm[j] + previousOffset).ToString();
								} else
								{
									line3 += "    _    _    _";
								}
								fileStream.WriteLine(line3);

								previousOffset = previousOffset + value;
							}
					}
					else if (LDLTSPlotOption.SelectedIndex == 1)
					{
						for (int j = 0; j < numberofrows; j++)
							foreach (var LDLTSFile in LDLTSDataFiles)
							{
								if (j < LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].EmmisionRate.Count)
								{
									line3 += "    " + LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].EmmisionRate[j].ToString()
									+ "    " + LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].SpectarY[j].ToString()
									+ "    " + LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].SpectarYError[j].ToString()
									+ "    " + (LDLTSFile.LDLTSSpectrumFiles[LDLTSFile.SelectedNumericalMethodIndex].SpectarY[j] + previousOffset).ToString();
								}
								else
								{
									line3 += "    _    _    _    _";
								}
								fileStream.WriteLine(line3);

								previousOffset = previousOffset + value;
							}
					}
					
				}
			}
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
					foreach (string param in LoadForm.LDLTSIsoFile.AllParameters)
					{
						if (HeaderLinesList[SavedHeaderTemplatesComboBox.SelectedIndex].parameters[i].Contains(param))
						{
							parameterIndex = LoadForm.LDLTSIsoFile.AllParameters.IndexOf(param);
							break;
						}
					}
					//if (parameterIndex == -1) continue;

					int keyIndex = -1;
					foreach (string keyValue in LoadForm.LDLTSIsoFile.AllKeys[parameterIndex])
					{
						if (HeaderLinesList[SavedHeaderTemplatesComboBox.SelectedIndex].keys[i].Contains(keyValue))
						{
							keyIndex = LoadForm.LDLTSIsoFile.AllKeys[parameterIndex].IndexOf(keyValue);
							break;
						}
					}
					//if (keyIndex == -1) continue;
					List<string> a = new List<string>();
					a.AddRange(LoadForm.LDLTSIsoFile.AllKeys[parameterIndex]);
					List<string> b = new List<string>();
					b.AddRange(LoadForm.LDLTSIsoFile.AllParameters);
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

				for (int i = 1; i < HeadersForSaveDataList.Count; i++)
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

		private void SaveHeadersList()
		{
			if (HeaderLinesList.Count < 0) return;
			using (StreamWriter writer = new StreamWriter(Path.Combine(MainWindow.ThisWindowInstance.workingdirectory, "HeadersConfig.txt")))
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
	}
	public class HeaderForSaveDataClass
    {
        public int index { get; set; }
        public List<string> properties { get; set; }
        public int selectedPropertiesIndex { get; set; }
        public List<string> keys { get; set; }
        public int selectedKeysIndex { get; set; }
    }


}
