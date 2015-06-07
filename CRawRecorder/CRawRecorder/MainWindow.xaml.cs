using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CRawRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Application Attibutes
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
  
        IList<Body> _bodies;
        Timer _timer;
        DataList _dataList;
        bool _hasDestFolder;
        bool _isRecording;
        int _timerCount;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
                _reader.Dispose();

            if (_sensor != null && _sensor.IsOpen)
                _sensor.Close();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize timer event
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;

            // Initilize Sensor
            _sensor = KinectSensor.GetDefault();

            if(_sensor != null)
            {
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | 
                    FrameSourceTypes.Depth |
                    FrameSourceTypes.Infrared |
                    FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += _reader_MultiSourceFrameArrived;

                // Inform Log
                txtbLog.Text = "Hardware is connected.\r\nConnection is initialized.\r\n";
            }
            else
            {
                MessageBox.Show("No Sensor Detected!!!!");
            }

            UpdateDisplay();
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timerCount++;
            UpdateRecordingTime();
        }

        private void UpdateRecordingTime()
        {
            // Calculate Time Display
            int _s = _timerCount % 60;
            int _m = _timerCount / 60;
            int _h = _m / 60;

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                lbsRecordingTime.Text = String.Format("{0:00}:{1:00}:{2:00}", _h, _m, _s);
            });
        }


        void _reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame frames = e.FrameReference.AcquireFrame();

            long timeStamp = DateTime.Now.ToBinary();
            // Console.WriteLine("Data Entry at " + timeStamp);
            
            // Data sequence value preparation
            int dataSequence = 0;

            if(_isRecording)
                dataSequence = _dataList.GetDataSequence(timeStamp);
            
            // This bitmap share data for color and body frame.
            BitmapSource bSource = null;

            // Color Frame Source
            using(ColorFrame frame = frames.ColorFrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    PixelFormat format = PixelFormats.Bgr32;

                    byte[] pixels = new byte[frame.FrameDescription.Width * frame.FrameDescription.Height * ((format.BitsPerPixel + 7) / 8)];

                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToArray(pixels);
                    }
                    else
                    {
                        frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
                    }

                    int stride = frame.FrameDescription.Width * format.BitsPerPixel / 8;

                    // Set Color to the frame
                    bSource = BitmapSource.Create(SystemDefinitions.COLOR_STREAM_WIDTH,
                        SystemDefinitions.COLOR_STREAM_HEIGH, 96, 96, format, null, pixels, stride);

                    imgColor.Source = bSource;

                    #region SAVE COLOR DATA
                    if (_isRecording && chkColor.IsChecked == true)
                    {
                        string outputFile = SystemDefinitions.DEST_COLOR_FOLDER + "//" + EDataObjectType.ColorData + "_" + dataSequence + "." + SystemDefinitions.COLOR_OBJECT_EXT;
                        Stream stream = new FileStream(outputFile,
                            FileMode.Create,
                            FileAccess.Write, FileShare.None);
                        stream.Write(pixels, 0, pixels.Length);
                        stream.Close();
                    }
                    #endregion
                }
            }

            // Depth Frame Source
            using (var frame = frames.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    PixelFormat format = PixelFormats.Bgr32;

                    ushort minDepth = frame.DepthMinReliableDistance;
                    ushort maxDepth = frame.DepthMaxReliableDistance;

                    Console.WriteLine("Min Reliable " + minDepth + "\tMax Reliable " + maxDepth);

                    ushort[] pixelData = new ushort[width * height];
                    byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

                    frame.CopyFrameDataToArray(pixelData);

                    int colorIndex = 0;
                    for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
                    {
                        ushort depth = pixelData[depthIndex];

                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        pixels[colorIndex++] = intensity; // Blue
                        pixels[colorIndex++] = intensity; // Green
                        pixels[colorIndex++] = intensity; // Red

                        ++colorIndex;
                    }

                    int stride = width * format.BitsPerPixel / 8;

                    // Set image to the frame
                    imgDepth.Source = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

                    #region SAVE DEPTH DATA
                    if(_isRecording && chkDepth.IsChecked == true)
                    {
                        string outputFile = SystemDefinitions.DEST_DEPTH_FOLDER + "//" + EDataObjectType.DepthData + "_" + dataSequence + "." + SystemDefinitions.DEPTH_OBJECT_EXT;
                        byte[] pixelByte = new byte[width * height * sizeof(ushort)];

                        Stream stream = new FileStream(outputFile,
                            FileMode.Create,
                            FileAccess.Write, FileShare.None);

                        Buffer.BlockCopy(pixelData, 0, pixelByte, 0, pixelByte.Length);
                        
                        stream.Write(pixelByte, 0, pixelByte.Length);
                        stream.Close();
                    }
                    #endregion
                }
            }

            // Infrared Frame Source
            using (var frame = frames.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    PixelFormat format = PixelFormats.Bgr32;

                    ushort[] frameData = new ushort[width * height];
                    byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

                    frame.CopyFrameDataToArray(frameData);

                    int colorIndex = 0;
                    for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
                    {
                        ushort ir = frameData[infraredIndex];

                        byte intensity = (byte)(ir >> 7);

                        pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                        pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                        pixels[colorIndex++] = (byte)(intensity / 1); // Red

                        colorIndex++;
                    }

                    int stride = width * format.BitsPerPixel / 8;

                    imgInfrared.Source = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

                    #region SAVE INFRARED DATA
                    if (_isRecording && chkInfrared.IsChecked == true)
                    {
                        // create a tmp for holding value
                        string outputFile = SystemDefinitions.DEST_INFRARED_FOLDER + "//" + EDataObjectType.InfraredData + "_" + dataSequence + "." + SystemDefinitions.INFRARED_OBJECT_EXT;
                        byte[] pixelByte = new byte[width * height * sizeof(ushort)];
                        Stream stream = new FileStream(outputFile,
                            FileMode.Create,
                            FileAccess.Write, FileShare.None);

                        Buffer.BlockCopy(frameData, 0, pixelByte, 0, pixelByte.Length);

                        stream.Write(pixelByte, 0, pixelByte.Length);
                        stream.Close();
                    }
                    #endregion
                }
            }

            // Bodies Frame Source
            using (var frame = frames.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    CroppedBitmap chained = new CroppedBitmap(bSource, 
                                                new Int32Rect(SystemDefinitions.COLOR_STREAM_CROPED_LEFT_STP,
                                                0, 
                                                SystemDefinitions.COLOR_STREAM_CROPED_WIDTH,
                                                SystemDefinitions.COLOR_STREAM_HEIGH));
                    imgBodies.Source = chained;

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    ///// CREATE A DATA LIST
                    List<StringBuilder> dataBuilder = new List<StringBuilder>();
                    
                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            canvas.DrawSkeleton(body);

                            ///// CREATE ASTRING BUILDER TO HOLD BODIES INFO
                            StringBuilder dataString = new StringBuilder();

                            foreach (Joint joint in body.Joints.Values)
                            {
                                if (joint.TrackingState == TrackingState.NotTracked)
                                    continue;
                                
                                ///// APPEND DATA TO BE EXPORT TO StringBuilder
                                dataString.Append(joint.JointType + "," +
                                    joint.TrackingState + "," + 
                                    joint.Position.X + "," + 
                                    joint.Position.Y + "," +
                                    joint.Position.Z + "\r\n");
                            }

                            ///// ADD DATA BUILDER TO THE LIST
                            dataBuilder.Add(dataString);
                        }
                    }

                    #region SAVE BODIES DATA
                    if (_isRecording && chkBodies.IsChecked == true)
                    {
                        // create a tmp for holding value
                        string outputFile = SystemDefinitions.DEST_JOINTS_FOLDER + "//" + EDataObjectType.BodiesData + "_" + dataSequence + "." + SystemDefinitions.JOINTS_OBJECT_EXT;
                        StreamWriter writer = new StreamWriter(outputFile);
                        for (int i = 0; i < dataBuilder.Count; i++ )
                        {
                            writer.WriteLine(dataBuilder[i].ToString());

                            if (i < dataBuilder.Count - 1)
                                writer.WriteLine("|");
                        }
                        writer.Close();
                    }
                    #endregion
                }
            }
        }

        private void UpdateDisplay()
        {
            if (_hasDestFolder)
                btnRecord.IsEnabled = true;
            else
                btnRecord.IsEnabled = false;

            if (_isRecording)
            {
                btnRecord.Content = "Stop";
                chkColor.IsEnabled = false;
                chkInfrared.IsEnabled = false;
                chkDepth.IsEnabled = false;
                chkBodies.IsEnabled = false;
            }
            else
            {
                btnRecord.Content = "Record";
                chkColor.IsEnabled = true;
                chkInfrared.IsEnabled = true;
                chkDepth.IsEnabled = true;
                chkBodies.IsEnabled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog destFolder = new System.Windows.Forms.FolderBrowserDialog();
            if (destFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtbOutputFolder.Text = destFolder.SelectedPath;
                SystemDefinitions.DEST_FOLDER = destFolder.SelectedPath;

                _hasDestFolder = true;
                UpdateDisplay();
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (chkColor.IsChecked == false && chkInfrared.IsChecked == false && chkDepth.IsChecked == false && chkBodies.IsChecked == false)
            {
                MessageBox.Show("You do not select any recording source!");
                return;
            }

            if (!_isRecording)
            {
                // its not recording, so let record
                CreateDestFolder();

                // Start Recording
                _timer.Start();
                _isRecording = true;
                _dataList = new DataList(SystemDefinitions.DEFINITION_META_DEST);

                txtbLog.Text += "START RECORDING!!!\r\nStart Time " + DateTime.Now.ToBinary() + "\r\n";
                if(chkColor.IsChecked == true)
                    txtbLog.Text += "Color Data is recording.\r\n";
                if (chkInfrared.IsChecked == true)
                    txtbLog.Text += "Infrared Data is recording.\r\n";
                if (chkDepth.IsChecked == true)
                    txtbLog.Text += "Depth Data is recording.\r\n";
                if (chkBodies.IsChecked == true)
                    txtbLog.Text += "Bodies Data is recording.\r\n";
                
            }
            else
            {
                // Its recording, so let stop recording
                _timer.Stop();
                _isRecording = false;
                _dataList.Dispose();


                // Update clear the counter section.
                lbsRecentRecordingTime.Text = lbsRecordingTime.Text;
                _timerCount = 0;
                UpdateRecordingTime();

                txtbLog.Text += "STOP RECORDING!!!\r\nTotal Time: " + lbsRecentRecordingTime.Text + "\r\n";
            }

            UpdateDisplay();
        }

        private void CreateDestFolder()
        {
            // Check that the folder already exist or not?

            SystemDefinitions.DEST_FOLDER = txtbOutputFolder.Text + "\\cRaw";
            if (Directory.Exists(SystemDefinitions.DEST_FOLDER))
            {
                for (int i = 1; i < int.MaxValue; i++)
                {
                    SystemDefinitions.DEST_FOLDER = txtbOutputFolder.Text + "\\cRaw " + i;

                    if (!Directory.Exists(SystemDefinitions.DEST_FOLDER))
                        break;
                }
            }

            // Initiize Log
            txtbLog.Text = "Time: " + DateTime.Today.ToString() + "\r\n";

            // Create main folder and subfolder for each type of data
            Directory.CreateDirectory(SystemDefinitions.DEST_FOLDER);
            txtbLog.Text += SystemDefinitions.DEST_FOLDER + "Created.\r\n";

            Directory.CreateDirectory(SystemDefinitions.DEST_COLOR_FOLDER);
            txtbLog.Text += SystemDefinitions.DEST_COLOR_FOLDER + "Created.\r\n";

            Directory.CreateDirectory(SystemDefinitions.DEST_INFRARED_FOLDER);
            txtbLog.Text += SystemDefinitions.DEST_INFRARED_FOLDER + "Created.\r\n";

            Directory.CreateDirectory(SystemDefinitions.DEST_DEPTH_FOLDER);
            txtbLog.Text += SystemDefinitions.DEST_DEPTH_FOLDER + "Created.\r\n";

            Directory.CreateDirectory(SystemDefinitions.DEST_JOINTS_FOLDER);
            txtbLog.Text += SystemDefinitions.DEST_JOINTS_FOLDER + "Created.\r\n";

            txtbLog.Text += "START RECORDING!\r\n";
        }
    }
}
