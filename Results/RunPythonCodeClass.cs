using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static Results.CVDataFileClass;

namespace Results
{
    public class RunPythonCodeClass
    {
        public static MainWindow WindowInstance { get; set; }
        public NWDataFileClass NWDataFile { get; set; }
        public string RunLabel { get; set; }
        public bool ResultRedy = false;

        private bool alive = true;

        public string RunLabelName;

        public string CVDataFile { get; set; }
        public string [] writeLines { get; set; } 
        
        //public byte PythonRunId;
        //public decimal alpha;
         
        Thread readThread;

        public void RunCode(string CVDataFile,          string alpha,        string MaxIterations,       string ZeroParameterTextBox, string SmoothWBool,
                            string SmoothWWindowPoints, string PolynomOrder, string RelativePermitivity, string Area, string voltageStep, string DeltaCMMSEOption, string selectedScale)
        {
            this.CVDataFile = CVDataFile; //  0           1            2                      3            4                 5                     6           7
            this.writeLines = new string[] { alpha, MaxIterations, ZeroParameterTextBox, SmoothWBool, SmoothWWindowPoints, PolynomOrder, RelativePermitivity, Area, voltageStep, DeltaCMMSEOption, selectedScale };
         
            
            readThread = new Thread(new ThreadStart(StartCode));
            readThread.Start();
        }

        private void StartCode()
        {
            NWDataFile = new NWDataFileClass();

            using (Process process = new Process())
            {
                //                               0     1                  2                   3             4                      5               6              7      8           9
                // writeLines = new string[] { alpha, MaxIterations, ZeroParameterTextBox, SmoothWBool, SmoothWWindowPoints, PolynomOrder, RelativePermitivity, Area, voltageStep};
                string name = Path.GetFileNameWithoutExtension(CVDataFile);
                if (name.IndexOf('.') > 0)
                {
                    name = name.Substring(0, name.IndexOf('.'));
                }

                RunLabelName = name + " alpha(" + writeLines[0] + "),Zero(" + writeLines[2] + "),Area("+writeLines[7]+"),\nVoltage step("+ writeLines[8] + "), ";
                if (writeLines[3] == "0")
                {
                    RunLabelName += "W(C(N))";
                } else if (writeLines[3] == "1")
                 { 
                     RunLabelName += "W(Savitzky-Golay filter(C))(windowpoints=" + writeLines[4] + ", PolynomOrder" + writeLines[5] + ")";
                 } else if (writeLines[3] == "2")
                {
                    RunLabelName += "W(C)";
                } 

                if (writeLines[9] == "0")
                {
                    RunLabelName += ", C(N(First) = C(First)";
                } else if (writeLines[9] == "1")
                {
                    RunLabelName += ", C(N(Last)) = C(Last)";
                }
                else
                {
                    RunLabelName += ", MMSE 1/C(V)^2-1C(N(V)^2";
                }

                if (writeLines[10] == "1")
                {
                    RunLabelName += ", large scale";
                }
                else
                {
                    RunLabelName += ", small scale";
                }

                WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsAddItem),
                    RunLabelName + "\n (Starting)"
                    );

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = MainWindow.PythonPath;
                startInfo.Arguments = WindowInstance.PythonCodePath + " "+ "\"" + CVDataFile.Replace('\\', '/') + "\""; //"\"" + CVFiles.CVFileNames[CVFileNamesComboBox.SelectedIndex].Replace('\\', '/') + "\""

                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                startInfo.RedirectStandardOutput = true;
                startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                startInfo.RedirectStandardInput = true;

                startInfo.RedirectStandardError = true;
                startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                //process.EnableRaisingEvents = true;

                process.StartInfo = startInfo;
                try { 
                process.Start();
                }  catch (System.Exception e)
                {
                    WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                                           RunLabelName + "\n " + "Error : Set correct Python path or Python code path."
                                           );
                    alive = false;
                    return;
                }

                using (StreamWriter writer = process.StandardInput )
                using (StreamReader reader = process.StandardOutput)
                {
                    foreach (string line in writeLines)
                    {
                        writer.WriteLine(line);
                        writer.Flush();
                    }
                    string result = "";

                    while (alive)
                    {
                        Thread.Sleep(20);
                        try { 
                        result = reader.ReadLine();
                        } catch (System.NullReferenceException e)
                        {
                            Thread.Sleep(500);
                            continue;
                        }
                        if (result == null)
                        {
                            Thread.Sleep(500);
                            continue;
                        }

                        if (result.Contains("Error"))
                        { 
                            WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                               RunLabelName + "\n" + result
                               );
                            alive = false;
                            continue; 
                        } else if (result.Contains("info"))
                        {
                            try
                            {
                                WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                                   RunLabelName + "\n" + result.Split(':')[1]
                                   );
                                Thread.Sleep(2000);
                            }
                            catch (System.Exception e) { }
                        } else if (result.Contains("iteration"))
                        { 
                            WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                               RunLabelName + "\n " + result.Substring(0,result.IndexOf('|')) + "\n" + result.Substring(result.IndexOf('|') +1)
                               );
                        }
                        else if (result.Contains("Result"))
                        {
 
                            if (result.Substring(0, result.IndexOf(':')).Contains("NW"))
                            {
                                decimal XValue;
                                decimal YValue;

                                foreach (string dataPoint in result.Substring(result.IndexOf(':') + 1).Split(';'))
                                {

                                    if (!decimal.TryParse(dataPoint.Split('|')[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out XValue))
                                    {
                                        WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                                           RunLabelName + "\n " + "Error : NW result is not number. W=" + dataPoint.Split('|')[0]
                                           );
                                        alive = false;
                                        break;
                                    }
                                    if (!decimal.TryParse(dataPoint.Split('|')[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out YValue))
                                    {
                                        WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                                           RunLabelName + "\n " + "Error : NW result is not number. N=" + dataPoint.Split('|')[1]
                                           );
                                        alive = false;
                                        break;
                                    }
                                    NWDataFile.data.Add(new NWDataFileClass.NWOnePointClass()
                                    {
                                        W = XValue,
                                        N = YValue
                                    });

                                }
                            } else if (result.Substring(0, result.IndexOf(':')).Contains("CV"))
                            {
                                decimal XValue;
                                decimal YValue;
 
                                foreach (string dataPoint in result.Substring(result.IndexOf(':') + 1).Split(';'))
                                {
 
                                    if (!decimal.TryParse(dataPoint.Split('%')[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out XValue))
                                    {
                                        WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                                           RunLabelName + "\n " + "Error : CV result is not number. V=" + dataPoint.Split('%')[0]
                                           ); 
                                        alive = false;
                                        break;
                                    }
                                    if (!decimal.TryParse(dataPoint.Split('%')[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out YValue))
                                    {
                                        WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                                           RunLabelName + "\n " + "Error : CV result is not number. C=" + dataPoint.Split('%')[1]
                                           );
                                        alive = false;
                                        break;
                                    }

                                    NWDataFile.CVResultFileData.Add(new OneData()
                                    {
                                        XData = XValue,
                                        YData = YValue,
                                        Y2Data = 1.0M / (YValue * YValue)
                                    });
 
                                }
                            }
                        }
                        else if (result.Contains("MMSEOption"))
                        { 
                            RunLabelName += ", " +  result.Split(':')[1];
                        }
                        else if (result.Contains("Done"))
                        {
                            alive = false;
                            ResultRedy = true;
                            WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                               RunLabelName + "\n " + "(Complete)"
                               );
                        } else if (result.Contains("NotConverged"))
                        {
                            alive = false;
                            ResultRedy = true;
                            WindowInstance.NWResults.Items.Dispatcher.BeginInvoke(new NWResultsListBoxItemUpdateDelegate(NWResultsUpdateItem),
                               RunLabelName + "\n " + "(Result did not converge)"
                               );
                        }

                    }

                    process.WaitForExit(1000);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit();
                    } 
                }
            } 
        }

        public void KillPythonCodeRun()
        {
            alive = false;
        }

        public delegate void NWResultsListBoxItemUpdateDelegate(string text);


        public void NWResultsAddItem(string text)
        {
            WindowInstance.NWResults.Items.Add(text);
            RunLabel = text;
        }
 
        public void NWResultsUpdateItem(string text)
        { 
            try
            {
                WindowInstance.NWResults.Items[WindowInstance.NWResults.Items.IndexOf(RunLabel)] = text;
                RunLabel = text;
            }
            catch (System.Exception e) { }
        }
    }


}
