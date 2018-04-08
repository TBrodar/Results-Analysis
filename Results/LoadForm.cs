using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Results
{
    public class LoadForm
    {

        public class LDLTSIsoFile
        {
            static public List<string> AllParameters = new List<string>();
            static public List<List<string>> AllKeys = new List<List<string>>();

            public LDLTSIsoFile()
            {
                if (AllParameters.Count <= 0)
                {
                    AllParameters.Add("Defect");
                    AllParameters.Add(general);
                    AllParameters.Add(generator);
                    AllParameters.Add(acquisition);
                    AllParameters.Add(parameters);
                    AllParameters.Add(noise);
                }

                if (AllKeys.Count <= 0)
                {
                    List<string> pom = new List<string>();
                    pom.Add("Temperature(K)");
                    pom.Add("Emission(eV)");
                    pom.Add("EmissionStandardDeviation(eV)");
                    pom.Add("△CAmplitude(pF)");
                    pom.Add("△CStandandDeviation(pF)");
                    pom.Add("Broadening");
                    pom.Add("NumericalMethod");
					pom.Add("△CAmplitudePointsCutCorrected(pF)");
					pom.Add("FileName");
					pom.Add("PointsCut");

					AllKeys.Add(pom);
                    AllKeys.Add(generalkeys.GeneralKeysList);
                    AllKeys.Add(generatorkeys.GeneratorKeysList);
                    AllKeys.Add(acquisitionkeys.AcquisitionKeysList);
                    AllKeys.Add(parameterskeys.ParametersKeysList);
                    AllKeys.Add(noisekeys.NoiseKeysList);
                }

            }

			public string data = "data";

            public string general = "general";
            public class GeneralKeys
            {
                public List<string> GeneralKeysList = new List<string>();

                public GeneralKeys()
                {
                    GeneralKeysList.Add(software);
                    GeneralKeysList.Add(hardware);
                    GeneralKeysList.Add(serialnumber);
                    GeneralKeysList.Add(user);
                    GeneralKeysList.Add(type);
                    GeneralKeysList.Add(source);
                    GeneralKeysList.Add(date);
                    GeneralKeysList.Add(comment);
                    GeneralKeysList.Add(database);
                    GeneralKeysList.Add(dataname);
                }

                public string software = "software";
                public string hardware = "hardware";
                public string serialnumber = "serial number";
                public string user = "user";
                public string type = "type";
                public string source = "source";
                public string date = "date";
                public string comment = "comment";
                public string database = "data base";
                public string dataname = "data name";
            }
            public GeneralKeys generalkeys = new GeneralKeys();

            public string sample = "sample";
            public string generator = "generator";
            public class GeneratorKeys
            {
                public List<string> GeneratorKeysList = new List<string>();

                public GeneratorKeys()
                {
                    GeneratorKeysList.Add(bias);
                    GeneratorKeysList.Add(FirstPulseBias);
                    GeneratorKeysList.Add(SecondPulseBias);
                    GeneratorKeysList.Add(InjectionPulseBias);
                    GeneratorKeysList.Add(FirstPulseWidth);
                    GeneratorKeysList.Add(SecondPulseWidth);
                    GeneratorKeysList.Add(InjectionPulseWidth);
                    GeneratorKeysList.Add(Secondpulse);
                    GeneratorKeysList.Add(Secondpulseinterlacing);
                    GeneratorKeysList.Add(Injectionpulse);
                    GeneratorKeysList.Add(LikePulse1);
                }

                public string bias = "bias";
                public string FirstPulseBias = "1st Pulse Bias";
                public string SecondPulseBias = "2nd Pulse Bias";
                public string InjectionPulseBias = "Injection Pulse Bias";
                public string FirstPulseWidth = "1st Pulse Width";
                public string SecondPulseWidth = "2nd Pulse Width";
                public string InjectionPulseWidth = "Injection Pulse Width";
                public string Secondpulse = "2nd pulse";
                public string Secondpulseinterlacing = "2nd pulse interlacing";
                public string Injectionpulse = "Injection pulse";
                public string LikePulse1 = "Like Pulse1";
            }
            public GeneratorKeys generatorkeys = new GeneratorKeys();
            
            public string acquisition = "acquisition";
            public class AcquisitionKeys
            {
                public List<string> AcquisitionKeysList = new List<string>();
                public AcquisitionKeys()
                {
                    AcquisitionKeysList.Add(firstsample);
                    AcquisitionKeysList.Add(lastsample);
                    AcquisitionKeysList.Add(samplingRate);
                    AcquisitionKeysList.Add(NumberOfSamples);
                    AcquisitionKeysList.Add(Noscans);
                }

                public string firstsample = "first sample";
                public string lastsample = "last sample";
                public string samplingRate = "Sampling Rate";
                public string NumberOfSamples = "No samples";
                public string Noscans = "No scans";
            }
            public AcquisitionKeys acquisitionkeys = new AcquisitionKeys();

            public string parameters = "parameters";
            public class ParametersKeys
            {
                public List<string> ParametersKeysList = new List<string>();

                public ParametersKeys()
                {
                    ParametersKeysList.Add(temperature);
                    ParametersKeysList.Add(temperatureSet);
                    ParametersKeysList.Add(capacitancemeterrange);
                    ParametersKeysList.Add(gain);
                    ParametersKeysList.Add(BiasCapacitance);
                    ParametersKeysList.Add(CurrentTransient);
                    ParametersKeysList.Add(magneticfield);
                    ParametersKeysList.Add(pressure);
                    ParametersKeysList.Add(illumination);
                }

                public string temperature = "temperature";
                public string temperatureSet = "temperatureSet";
                public string capacitancemeterrange = "capacitance meter range";
                public string gain = "gain";
                public string BiasCapacitance = "Bias Capacitance";
                public string CurrentTransient = "CurrentTransient";
                public string magneticfield = "magnetic field";
                public string pressure = "pressure";
                public string illumination = "illumination";
            }
            public ParametersKeys parameterskeys = new ParametersKeys();
            public string noise = "noise";
            public class NoiseKeys
            {
                public List<string> NoiseKeysList = new List<string>();
                public NoiseKeys()
                {
                    NoiseKeysList.Add(level);
                }
                public string level = "level";
            }
            public NoiseKeys noisekeys = new NoiseKeys();
            
        }
        public LDLTSIsoFile ldltsisofile = new LDLTSIsoFile();

        public class LDLTSFile
        {
            public string general = "general";
            public class GeneralKeys
            {
                public string Method = "Method";
            }
            public GeneralKeys generalkeys = new GeneralKeys();

            public string Parameters = "Parameters";
            public class ParametersKeys
            {
                public string FrequencyMin = "Frequency Min";
                public string FrequencyMax = "Frequency Max";
                public string Peaks = "Peaks";
            }
            public ParametersKeys parameterskeys = new ParametersKeys();
            
            

            public string Spectrum = "Spectrum";
            public string Peaks = "Peaks";
            public string Summary = "Summary";
            public string BaseLine = "BaseLine";
        }
        public LDLTSFile ldltsspectrumfile = new LDLTSFile();

        public class Python
        {
            public string python = "Python";
            public string pythoncodepath = "Python code path";
            public string pythoncallpath = "Python call path";
         //   public string pythonconsolepath = "Python console path";
        }
        public Python python = new Python();

        public class CVNW
        {
            public string cvnw = "C-V->N-W";
            public string alpha = "Alpha parameter";
            public string zeroParameter = "Zero parameter";
            public string maxIterations = "Max iterations";
            public string SmoothWindowPoints = "Window points for Savitzky-Golay filter";
            public string PolynomOrder = "Polynom order for Savitzky-Golay filter";
            public string WIndexChoice = "Choice of W option index";
            public string CIndexChoice = "Choice of 1/C^2 offset option index";
            public string ScaleChoice = "Choice of Scale option index";
        }
        public CVNW cvnw = new CVNW();

        public class Material
        {
            public string material = "Material";
            public string materialName = "Material name";
            public string area = "Diode area";
            public string varepsilon = "Relative permittivity";
        }
        public Material material = new Material();



        public List<List<string>> fillWithValues(ref List<string> allData)
        {
            List<List<string>> n = new List<List<string>>();
            foreach (List<string> s in LDLTSIsoFile.AllKeys)
            {
                List<string> poms = new List<string>();
                poms.AddRange(s);
                n.Add(poms);
            }
            int paramCount = LDLTSIsoFile.AllParameters.Count;
            for (int i = 0; i < paramCount; i++)
            {
                if (i == 0) continue; // don't fill Defect property

                List<string> properties = getProperties(LDLTSIsoFile.AllParameters[i], ref allData);
                int keyCount = n[i].Count;
                for(int j = 0; j < keyCount; j++)
                {
                    n[i][j] = getValue(n[i][j],ref properties);
                }
            }

            return n;
        }

        public List<String> getProperties(string property, ref List<String> allData)
        {
            List<string> returnList = new List<string>();
            bool intresting = false;
            foreach (var line in allData)
            {
                if (line.Contains("["))
                {
                    if (intresting == true)
                    {
                        break;
                    }
                    if (line.Contains(property))
                    {
                        intresting = true;
                    }
                }
                if (intresting == true)
                {
                    if (line.Length < 1) continue;
                    if (line[0] == ';')  continue;   //Remove comment lines
                    returnList.Add(line);
                }
            }
            if (intresting == true)
                returnList.RemoveAt(0);
            return returnList;
        }
        public List<String> setProperties(string property, List<String> items, List<String> allData)
        {
            int start = 0;
            int count = 0;
            bool found = false;

            for (int i = 0; i < allData.Count; i++)
            {
                if (allData[i].Contains("["))
                {
                    if (allData[i].Contains(property))
                    {
                        found = true;
                        start = i;
                        break;
                    }
                }
            }
            if (found == true)
                for (int i = start + 1; i < allData.Count; i++)
                {
                    if (allData[i].Contains("["))
                    {
                        break;
                    }
                    count += 1;
                }
            if (found == false)
            {
                if (items.Count > 0)
                {
                    allData.Add("[" + property + "]");
                }
                foreach (var item in items)
                {
                    allData.Add(item);
                }
            }
            else
            {
                while (count > 0)
                {
                    count -= 1;
                    allData.RemoveAt(start);
                }
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    allData.Insert(start, items[i]);
                }
                if (items.Count > 0)
                {
                    allData.Insert(start, "[" + property + "]");
                }
            }
            return allData;
        }
        public string getValue(string item, ref List<string> propertyData)
        {
            foreach (string line in propertyData)
            {
                if (line.Contains(item))
                {
                    return line.Split('=')[1];
                }
            }
            return "";
        }
        public List<string> setValue(string item, string value, List<string> propertyData)
        {
            for (int i = 0; i < propertyData.Count; i++)
            {
                if (propertyData[i].Contains(item))
                {
                    propertyData[i] = item + "=" + value;
                    return propertyData;
                }
            }
            propertyData.Add(item + "=" + value);
            return propertyData;
        }

		/// <summary>
		/// Writes the given object instance to a Json file.
		/// <para>Object type must have a parameterless constructor.</para>
		/// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
		/// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [JsonIgnore] attribute.</para>
		/// </summary>
		/// <typeparam name="T">The type of object being written to the file.</typeparam>
		/// <param name="filePath">The file path to write the object instance to.</param>
		/// <param name="objectToWrite">The object instance to write to the file.</param>
		/// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>

		public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
		{
			TextWriter writer = null;
			try
			{
				var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
				writer = new StreamWriter(filePath, append);
				writer.Write(contentsToWriteToFile);
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}

		/// <summary>
		/// Reads an object instance from an Json file.
		/// <para>Object type must have a parameterless constructor.</para>
		/// </summary>
		/// <typeparam name="T">The type of object to read from the file.</typeparam>
		/// <param name="filePath">The file path to read the object instance from.</param>
		/// <returns>Returns a new instance of the object read from the Json file.</returns>
		public static T ReadFromJsonFile<T>(string filePath) where T : new()
		{
			TextReader reader = null;
			try
			{
				reader = new StreamReader(filePath);
				var fileContents = reader.ReadToEnd();
				return JsonConvert.DeserializeObject<T>(fileContents);
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
		}
	}
}
