using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRawRecorder
{
    enum EDataObjectType { ColorData, DepthData, InfraredData, BodiesData };

    public static class SystemDefinitions
    {
        public static int COLOR_STREAM_WIDTH { get { return 1920; } }
        public static int COLOR_STREAM_CROPED_WIDTH { get { return 1304; } }
        // (1920-1304) / 2
        public static int COLOR_STREAM_CROPED_LEFT_STP { get { return 308 + 150; } }
        public static int COLOR_STREAM_HEIGH { get { return 1080; } }
        public static int DEPTH_STREAM_WIDTH { get { return 512; } }
        public static int DEPTH_STREAM_HEIGH { get { return 424; } }
        public static int INFRARED_STREAM_WIDTH { get { return 512; } }
        public static int INFRARED_STREAM_HEIGH { get { return 424; } }

        public static int DEPTH_DISTANCE_MIN_M { get { return 50; }}
        public static int DEPTH_DISTANCE_MAX_M { get { return 450; }}

        public static string DEST_FOLDER { get; set; }
        public static string DEST_COLOR_FOLDER { get { return DEST_FOLDER + "\\ColorData"; } }
        public static string DEST_DEPTH_FOLDER { get { return DEST_FOLDER + "\\DepthData"; } }
        public static string DEST_INFRARED_FOLDER { get { return DEST_FOLDER + "\\InfraredData"; } }
        public static string DEST_JOINTS_FOLDER { get { return DEST_FOLDER + "\\BodiesData"; } }
        public static string COLOR_OBJECT_EXT { get { return "cbyte"; } }
        public static string DEPTH_OBJECT_EXT { get { return "dbyte"; } }
        public static string INFRARED_OBJECT_EXT { get { return "ibyte"; } }
        public static string JOINTS_OBJECT_EXT { get { return "bcsv"; } }

        public static string DEFINITION_META_DEST { get { return DEST_FOLDER + string.Format("\\Meta_{0}.meta", Process.GetCurrentProcess().Id); } }

        /*
        public static string DEFINITION_COLOR_DEST { get { return DEST_FOLDER + string.Format("\\ColorData_{0}.cdef", Process.GetCurrentProcess().Id); } }
        public static string DEFINITION_DEPTH_DEST { get { return DEST_FOLDER + string.Format("\\DepthData_{0}.cdef", Process.GetCurrentProcess().Id); } }
        public static string DEFINITION_INFRARED_DEST { get { return DEST_FOLDER + string.Format("\\InfraredData_{0}.cdef", Process.GetCurrentProcess().Id); } }
        public static string DEFINITION_JOINTS_DEST { get { return DEST_FOLDER + string.Format("\\JointsData_{0}.cdef", Process.GetCurrentProcess().Id); } }
        */
        public static int BUFFER_DATA_LIST_SIZE { get { return 10240; } }    // Unit is in Byte
    }
}
