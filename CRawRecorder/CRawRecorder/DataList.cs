using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRawRecorder
{
    public class DataList : IDisposable
    {
        private List<long> colorDataList;
        private int dataCounter;
        private int colorDataListSize;
        private int colorDataPairSize;
        StreamWriter writer;

        public DataList(string fileDestionation)
        {
            dataCounter = 0;
            colorDataListSize = 0;
            colorDataPairSize = sizeof(long);
            colorDataList = new List<long>();
            writer = new StreamWriter(fileDestionation, true);
        }

        public int TotalFrame
        {
            get { return dataCounter; }
        }

        public int GetDataSequence(long timeStamp)
        {
            if (timeStamp == 0)
                return 0;

            colorDataList.Add(timeStamp);

            // 12 is the size of int + long
            colorDataListSize += colorDataPairSize;

            if (colorDataListSize > SystemDefinitions.BUFFER_DATA_LIST_SIZE)
                FlushDataList();

            return ++dataCounter;
        }

        private void FlushDataList()
        {
            for(int i = 0; i < colorDataList.Count; i++)
                writer.WriteLine(colorDataList[i]);

            colorDataList.Clear();
        }

        public void Dispose()
        {
            FlushDataList();
            writer.Close();
        }
    }
}
