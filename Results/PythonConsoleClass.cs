using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Results
{
    class PythonConsoleClass
    {
        public bool alive = false;
        public bool busy = false;

        Process process;

        List<string> WriteBuffer = new List<string>();
        object writeLock = new object();
        List<string> ReadBuffer = new List<string>();
        object readLock = new object();
        Thread readThread;

        public string runOLS = "run OLS";

        public void startPythonConsole()
        {
            (new Thread(new ThreadStart(RunWrite))).Start();
        }

        void RunRead()
        {
            using (StreamReader reader = process.StandardOutput)
            {
               string pomstr;
               while (alive == true)
                {
                    if (reader.Peek() > -1)
                    { 
                        try
                        {
                            pomstr = reader.ReadLine();
                        }
                        catch (System.NullReferenceException e)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        if (pomstr == null)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        if (pomstr == "")
                        {
                            continue;
                        }
                        if (pomstr.Contains("Error"))
                        {
                            MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                                       pomstr
                                       );
                            alive = false;
                            break;
                        }
                        if (pomstr.Contains("Exited"))
                        {
                            alive = false;
                            break;
                        }
                        if (pomstr.Contains("C:"))
                        {
                            MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                                       pomstr
                                       );
                            continue;
                        }

                        lock (readLock)
                        {
                            ReadBuffer.Add(pomstr);
                        }
                    } else
                    {
                        Thread.Sleep(100);
                    }
                }

            }
        }
            
        void RunWrite() { 
         
            using (process = new Process())
            { 

                if (MainWindow.PythonPath == "" || MainWindow.PythonCallPath == "" || !File.Exists(MainWindow.PythonPath) || !File.Exists(MainWindow.PythonCallPath))
                {
                    MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                                          "Error : Set correct Python path."
                                          );
                    alive = false;
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = MainWindow.PythonPath;
                if (MainWindow.PythonCallPath.Contains("\""))
                { 
                    startInfo.Arguments = MainWindow.PythonCallPath;
                } else
                {
                    startInfo.Arguments = "\"" + MainWindow.PythonCallPath +"\"";
                }


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
                    MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                                           "Info : Python console started."
                                           );
                }  catch (System.Exception e)
                {
                    
                     MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                                           "Error : Python console didn't start."
                                           );
                    alive = false;
                    return;
                }
                alive = true;
                readThread = new Thread(new ThreadStart(RunRead));
                using (StreamWriter writer = process.StandardInput)
                {
                    readThread.Start();
                    while (alive == true)
                    {

                        // Write to console
                        lock (writeLock) { 
                            foreach(string str in WriteBuffer)
                            {
                                writer.WriteLine(str);
                            }
                            WriteBuffer.Clear();
                        }
                         
                        if (process.HasExited)
                        {
                            MainWindow.ThisWindowInstance.Status.Dispatcher.BeginInvoke(new MainWindow.SetStatusTextDelegate(MainWindow.ThisWindowInstance.SetStatusText),
                                           "Info: Python console exited."
                                           );
                            alive = false;
                        } else
                        {
                            Thread.Sleep(100);
                        }

                    } 
                }
                process.WaitForExit(100);
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        public void clearBuffers()
        {
            lock (writeLock)
            {
                lock (readLock)
                {
                    ReadBuffer.Clear();
                    WriteBuffer.Clear();
                }
            }
        }
         
        public List<string> ReadLines(int NumberOfLines)
        { 
            while (ReadBuffer.Count < NumberOfLines && alive == true)
            {
                Thread.Sleep(50);
            }
            if (alive == false  && ReadBuffer.Count < NumberOfLines) return null;

            List<string> r;
            lock (readLock)
            {
               
               r = ReadBuffer.GetRange(0, NumberOfLines);
               ReadBuffer.RemoveRange(0, NumberOfLines);
            }
            return r;
        }

        public void WriteLines(List<string> LinesToWrite)
        {
            lock (writeLock)
            { 
                WriteBuffer.AddRange(LinesToWrite);
            } 
        }
        
        public void KillPythonConsole()
        {
            alive = false;
        }

    }
}
