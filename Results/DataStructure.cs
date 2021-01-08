using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Results.CVDataFileClass;

namespace Results
{
    class DataStructure
    {
    }

    public class CVFilesClass
    {
        public List<string> CVFileNames { get; set; }
        public List<string> CVFileNamesShort { get; set; }
        public List<CVDataFileClass> FilesData { get; set; }
        public List<int> OpenDialogBoxFilterIndex { get; set; }

    }

    public class CVDataFileClass
    {
        public System.Collections.ObjectModel.ObservableCollection<OneData> data = new System.Collections.ObjectModel.ObservableCollection<OneData>();
        public decimal VoltageStep { get; set; }
        public class OneData
        {
            public decimal XData { get; set; }
            public decimal YData { get; set; }
            public decimal Y2Data { get; set; }
        }

    }

    public class MaterialClass
    {
        public string MaterialName { get; set; }
        public decimal RelativePermitivity { get; set; }
        public decimal Area { get; set; }
    }

    public class NWDataFileClass
    {
        public System.Collections.ObjectModel.ObservableCollection<NWOnePointClass> data = new System.Collections.ObjectModel.ObservableCollection<NWOnePointClass>();
        public System.Collections.ObjectModel.ObservableCollection<CVDataFileClass.OneData> CVResultFileData = new System.Collections.ObjectModel.ObservableCollection<OneData>();

        public class NWOnePointClass
        {
            public decimal W { get; set; }
            public decimal N { get; set; }
        }
    }
}
