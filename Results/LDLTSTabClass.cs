using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Results
{
    class LDLTSTabClass
    {
        

    }

	public class LDLTSDataFile
	{
		public bool isSelected = false;
		private static string _SplitByOffsetValue;
		
		public static string SplitByOffsetValue { get {  return _SplitByOffsetValue; } set {
				_SplitByOffsetValue = value;
			} }
		public static List<int> VisibleHeadersParameter { get; set; }
		public static List<int> VisibleHeadersKey { get; set; }

		public string AddedHeaders { get; set; }

		public string FileName { get; set; }
        public string FileNameShort { get; set; }
        public decimal Temperature { get; set; }

        public List<List<string>> Properties { get; set; }

        public List<decimal> CapacitanceTransient { get; set; }
        public decimal MinCapacitanceTransient { get; set; }
        public decimal maxCapacitanceTransient { get; set; }
		public int NumberOfCroppedPoints { get; set; }

        public long SampleRate { get; set; }   // [Hz]
                                               // public long NumberOfSamples { get; set; }

        static public string[] ContinFileTypeExpansion  = { ".s05", ".s08", ".s10", ".s15", ".s20" };
        static public string[] FtikregFileTypeExpansion = { ".s25", ".s28", ".s30", ".s35", ".s40" };
        static public string[] FlogFileTypeExpansion    = { ".s45", ".s48", ".s50", ".s55", ".s60" };

        public class LDLTSSpectrumFile
        { 
            public decimal FrequencyMin { get; set; } 
            public decimal FrequencyMax { get; set; } 
            public string NumericalMethod { get; set; }

            public List<decimal> EmmisionRate { get; set; }
            public List<decimal> SpectarY { get; set; }
            public List<decimal> SpectarYError { get; set; }
            public List<decimal> SpectarXYNorm { get; set; }

            public decimal MaxSpectarY { get; set; }
            public decimal MaxSpectarXYNorm { get; set; }

            public class Peak
            {
                //  ;| em rate  | amplitude |broadening| err ampli | err em rate|
                public decimal EmRate { get; set; }
                public decimal Amplitude { get; set; }
                public decimal Broadening { get; set; }
                public decimal EmRateError { get; set; }
                public decimal AmplitudeError { get; set; }
				public double AmplitudeCorrected { get; set; }


				public string DefectName { get; set; }

                public System.Windows.Media.Brush Color { get; set; }
                public System.Windows.Media.Brush ColorName { get; set; }

                static public List<string> Defects {get; set;}
                static public ObservableCollection<DefectResult> DefectsResults { get; set; }


            }
            public List<Peak> Peaks { get; set; }
        }

        public List<LDLTSSpectrumFile> LDLTSSpectrumFiles { get; set; }
        public List<string> NumericalMethods { get; set; }
        public int SelectedNumericalMethodIndex { get; set; }
    }

    public class DefectResult
    {
        public string DefectName { get; set; }

        public decimal Energy { get; set; } // [eV]
        public decimal EnergyDeviation { get; set; }

        public double CrossSection { get; set; }
        public double CrossSectionDeviation { get; set; }
        public decimal B { get; set; }
        public decimal BDeviation { get; set; }

        public List<decimal> Emissions { get; set; }
        public List<decimal> EmissionsDevioations { get; set; }

        public List<decimal> Amplitudes           { get; set; }
        public List<decimal> AmplitudesDeviations { get; set; }
		public List<double> AmplitudesCorrected  { get; set; }

		public List<decimal> Broadenings { get; set; }

        public List<decimal> Temperatures { get; set; }

        public List<LDLTSDataFile> SourceFiles { get; set; }


    }
    

}
