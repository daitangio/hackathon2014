//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
// 	 
//	 Copyright 2013 Microsoft Corporation 
// 	 
//	Licensed under the Apache License, Version 2.0 (the "License"); 
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at
// 	 
//		 http://www.apache.org/licenses/LICENSE-2.0 
// 	 
//	Unless required by applicable law or agreed to in writing, software 
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//	See the License for the specific language governing permissions and 
//	limitations under the License. 
// 	 
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Slideshow
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.SwipeGestureRecognizer;

    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using MicroWebServer;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The recognizer being used.
        /// </summary>
        private readonly Recognizer activeRecognizer;

        /// <summary>
        /// The paths of the picture files.
        /// </summary>
        private readonly string[] picturePaths = CreatePicturePaths();

        /// <summary>
        /// Array of arrays of contiguous line segements that represent a skeleton.
        /// </summary>
        private static readonly JointType[][] SkeletonSegmentRuns = new JointType[][]
        {
            new JointType[] 
            { 
                JointType.Head, JointType.ShoulderCenter, JointType.HipCenter 
            },
            new JointType[] 
            { 
                JointType.HandLeft, JointType.WristLeft, JointType.ElbowLeft, JointType.ShoulderLeft,
                JointType.ShoulderCenter,
                JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight
            },
            new JointType[]
            {
                JointType.FootLeft, JointType.AnkleLeft, JointType.KneeLeft, JointType.HipLeft,
                JointType.HipCenter,
                JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight
            }
        };

        /// <summary>
        /// The sensor we're currently tracking.
        /// </summary>
        private KinectSensor nui;

        /// <summary>
        /// There is currently no connected sensor.
        /// </summary>
        private bool isDisconnectedField = true;

        /// <summary>
        /// Any message associated with a failure to connect.
        /// </summary>
        private string disconnectedReasonField;

        /// <summary>
        /// Array to receive skeletons from sensor, resize when needed.
        /// </summary>
        private Skeleton[] skeletons = new Skeleton[0];

        /// <summary>
        /// Time until skeleton ceases to be highlighted.
        /// </summary>
        private DateTime highlightTime = DateTime.MinValue;

        /// <summary>
        /// The ID of the skeleton to highlight.
        /// </summary>
        private int highlightId = -1;

        /// <summary>
        /// The ID if the skeleton to be tracked.
        /// </summary>
        private int nearestId = -1;

        /// <summary>
        /// The index of the current image.
        /// </summary>
        private int indexField = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            
            this.PreviousPicture = this.LoadPicture(this.Index - 1);
            this.Picture = this.LoadPicture(this.Index);
            this.NextPicture = this.LoadPicture(this.Index + 1);

            InitializeComponent();

            // Create the gesture recognizer.
            this.activeRecognizer = this.CreateRecognizer();

            // Wire-up window loaded event.
            Loaded += this.OnMainWindowLoaded;

            initWebServer(this);

        }


        private MicroWebServer.WebServer initWebServer(MainWindow mainWindow)
        {
            List<string> names = new List<string>();
            names.Add("http://" + System.Net.Dns.GetHostName() + ":2323/");
            //var ipadresses=System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
            //foreach (var ip in ipadresses)
            //{
            //    //Console.WriteLine(ip.ToString());
            //    names.Add("http://" + ip.ToString() + ":2323/");
            //}
            names.Add("http://localhost:2323/");
            names.Add("http://127.0.0.1:2323/");
            //names.Add("http://172.16.220.133:2323/");
            var server = new MicroWebServer.WebServer(names, @"C:\giorgi\ht\htdocs", mainWindow);
            new System.Threading.Thread(server.Start).Start();
            Console.WriteLine("MicroWeb Server is Running. Press ^C to stop");
            return server;
        }

        /// <summary>
        /// Event implementing INotifyPropertyChanged interface.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a value indicating whether no Kinect is currently connected.
        /// </summary>
        public bool IsDisconnected
        {
            get
            {
                return this.isDisconnectedField;
            }

            private set
            {
                if (this.isDisconnectedField != value)
                {
                    this.isDisconnectedField = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("IsDisconnected"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets any message associated with a failure to connect.
        /// </summary>
        public string DisconnectedReason
        {
            get
            {
                return this.disconnectedReasonField;
            }

            private set
            {
                if (this.disconnectedReasonField != value)
                {
                    this.disconnectedReasonField = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("DisconnectedReason"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the index number of the image to be shown.
        /// </summary>
        public int Index
        {
            get
            {
                return this.indexField;
            }

            set
            {
                if (this.indexField != value)
                {
                    this.indexField = value;

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Index"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the previous image displayed.
        /// </summary>
        public BitmapImage PreviousPicture { get; private set; }

        /// <summary>
        /// Gets the current image to be displayed.
        /// </summary>
        public BitmapImage Picture { get; private set; }

        /// <summary>
        /// Gets teh next image displayed.
        /// </summary>
        public BitmapImage NextPicture { get; private set; }

        /// <summary>
        /// Get list of files to display as pictures.
        /// </summary>
        /// <returns>Paths to pictures.</returns>
        private static string[] CreatePicturePaths()
        {
            var list = new List<string>();


            Debug.WriteLine(Environment.CurrentDirectory);
            list.AddRange(Directory.GetFiles(Environment.CurrentDirectory+"\\..\\..\\3d"
/*
@"C:\giorgi\ht\kinectforwindows\v1.x\ToolkitSamples1.6.0\C#\SlideshowGestures-WPF\3d"*/, "*.png", SearchOption.AllDirectories));
            /*
            var commonPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);
            list.AddRange(Directory.GetFiles(commonPicturesPath, "*.jpg", SearchOption.AllDirectories));
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(commonPicturesPath, "*.png", SearchOption.AllDirectories));
            }

            if (list.Count == 0)
            {
                var myPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                list.AddRange(Directory.GetFiles(myPicturesPath, "*.jpg", SearchOption.AllDirectories));
                if (list.Count == 0)
                {
                    list.AddRange(Directory.GetFiles(commonPicturesPath, "*.png", SearchOption.AllDirectories));
                }
            }
            */
            return list.ToArray();
        }

        /// <summary>
        /// Load the picture with the given index.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>Corresponding image.</returns>
        private BitmapImage LoadPicture(int index)
        {
            BitmapImage value;

            if (this.picturePaths.Length != 0)
            {
                var actualIndex = index % this.picturePaths.Length;
                if (actualIndex < 0)
                {
                    actualIndex += this.picturePaths.Length;
                }

                Debug.Assert(0 <= actualIndex, "Index used will be non-negative");
                Debug.Assert(actualIndex < this.picturePaths.Length, "Index is within bounds of path array");

                try
                {
                    value = new BitmapImage(new Uri(this.picturePaths[actualIndex]));
                }
                catch (NotSupportedException)
                {
                    value = null;
                }
            }
            else
            {
                value = null;
            }

            return value;
        }



        /// <summary>
        /// Create a wired-up recognizer for running the slideshow.
        /// </summary>
        /// <returns>The wired-up recognizer.</returns>
        private Recognizer CreateRecognizer()
        {
            // Instantiate a recognizer.
            var recognizer = new Recognizer();

            // Wire-up swipe right to manually advance picture.
            // GG KERNEL
            recognizer.SwipeRightDetected += (s, e) =>
            {
                if (e.Skeleton.TrackingId == nearestId)
                {
                    Index++;

                    // Setup corresponding picture if pictures are available.
                    this.PreviousPicture = this.Picture;
                    this.Picture = this.NextPicture;
                    this.NextPicture = LoadPicture(Index + 1);

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                    }

                    var storyboard = Resources["LeftAnimate"] as Storyboard;
                    if (storyboard != null)
                    {
                        storyboard.Begin();
                    }

                    HighlightSkeleton(e.Skeleton);
                }
            };

            // Wire-up swipe left to manually reverse picture.
            recognizer.SwipeLeftDetected += (s, e) =>
            {
                if (e.Skeleton.TrackingId == nearestId)
                {
                    Index--;

                    // Setup corresponding picture if pictures are available.
                    this.NextPicture = this.Picture;
                    this.Picture = this.PreviousPicture;
                    this.PreviousPicture = LoadPicture(Index - 1);

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                    }

                    var storyboard = Resources["RightAnimate"] as Storyboard;
                    if (storyboard != null)
                    {
                        storyboard.Begin();
                    }

                    HighlightSkeleton(e.Skeleton);
                }
            };

            return recognizer;
        }

        /// <summary>
        /// Handle insertion of Kinect sensor.
        /// </summary>
        private void InitializeNui()
        {
            this.UninitializeNui();

            var index = 0;
            while (this.nui == null && index < KinectSensor.KinectSensors.Count)
            {
                try
                {
                    this.nui = KinectSensor.KinectSensors[index];

                    this.nui.Start();

                    this.IsDisconnected = false;
                    this.DisconnectedReason = null;
                }
                catch (IOException ex)
                {
                    this.nui = null;

                    this.DisconnectedReason = ex.Message;
                }
                catch (InvalidOperationException ex)
                {
                    this.nui = null;

                    this.DisconnectedReason = ex.Message;
                }

                index++;
            }

            if (this.nui != null)
            {
                this.nui.SkeletonStream.Enable();

                this.nui.SkeletonFrameReady += this.OnSkeletonFrameReady;
            }
        }

        /// <summary>
        /// Handle removal of Kinect sensor.
        /// </summary>
        private void UninitializeNui()
        {
            if (this.nui != null)
            {
                this.nui.SkeletonFrameReady -= this.OnSkeletonFrameReady;

                this.nui.Stop();

                this.nui = null;
            }

            this.IsDisconnected = true;
            this.DisconnectedReason = null;
        }

        /// <summary>
        /// Window loaded actions to initialize Kinect handling.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Start the Kinect system, this will cause StatusChanged events to be queued.
            this.InitializeNui();

            // Handle StatusChange events to pick the first sensor that connects.
            KinectSensor.KinectSensors.StatusChanged += (s, ee) =>
            {
                switch (ee.Status)
                {
                    case KinectStatus.Connected:
                        if (nui == null)
                        {
                            Debug.WriteLine("New Kinect connected");

                            InitializeNui();
                        }
                        else
                        {
                            Debug.WriteLine("Existing Kinect signalled connection");
                        }

                        break;
                    default:
                        if (ee.Sensor == nui)
                        {
                            Debug.WriteLine("Existing Kinect disconnected");

                            UninitializeNui();
                        }
                        else
                        {
                            Debug.WriteLine("Other Kinect event occurred");
                        }

                        break;
                }
            };
        }

        /// <summary>
        /// Handler for skeleton ready handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            // Get the frame.
            using (var frame = e.OpenSkeletonFrame())
            {
                // Ensure we have a frame.
                if (frame != null)
                {
                    // Resize the skeletons array if a new size (normally only on first call).
                    if (this.skeletons.Length != frame.SkeletonArrayLength)
                    {
                        this.skeletons = new Skeleton[frame.SkeletonArrayLength];
                    }

                    // Get the skeletons.
                    frame.CopySkeletonDataTo(this.skeletons);

                    // Assume no nearest skeleton and that the nearest skeleton is a long way away.
                    var newNearestId = -1;
                    var nearestDistance2 = double.MaxValue;

                    // Look through the skeletons.
                    foreach (var skeleton in this.skeletons)
                    {
                        // Only consider tracked skeletons.
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // Find the distance squared.
                            var distance2 = (skeleton.Position.X * skeleton.Position.X) +
                                (skeleton.Position.Y * skeleton.Position.Y) +
                                (skeleton.Position.Z * skeleton.Position.Z);

                            // Is the new distance squared closer than the nearest so far?
                            if (distance2 < nearestDistance2)
                            {
                                // Use the new values.
                                newNearestId = skeleton.TrackingId;
                                nearestDistance2 = distance2;
                            }
                        }
                    }

                    if (this.nearestId != newNearestId)
                    {
                        this.nearestId = newNearestId;
                    }

                    // Pass skeletons to recognizer.
                    this.activeRecognizer.Recognize(sender, frame, this.skeletons);

                    this.DrawStickMen(this.skeletons);
                }
            }
        }

        /// <summary>
        /// Select a skeleton to be highlighted.
        /// </summary>
        /// <param name="skeleton">The skeleton</param>
        private void HighlightSkeleton(Skeleton skeleton)
        {
            // Set the highlight time to be a short time from now.
            this.highlightTime = DateTime.UtcNow + TimeSpan.FromSeconds(0.5);

            // Record the ID of the skeleton.
            this.highlightId = skeleton.TrackingId;
        }

        /// <summary>
        /// Draw stick men for all the tracked skeletons.
        /// </summary>
        /// <param name="skeletons">The skeletons to draw.</param>
        private void DrawStickMen(Skeleton[] skeletons)
        {
            // Remove any previous skeletons.
            StickMen.Children.Clear();
            guysHere = 0;

            foreach (var skeleton in skeletons)
            {
                // Only draw tracked skeletons.
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // Draw a background for the next pass.
                    this.DrawStickMan(skeleton, Brushes.WhiteSmoke, 7);
                }
            }

            foreach (var skeleton in skeletons)
            {
                // Only draw tracked skeletons.
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // Pick a brush, Red for a skeleton that has recently gestures, black for the nearest, gray otherwise.
                    var brush = DateTime.UtcNow < this.highlightTime && skeleton.TrackingId == this.highlightId ? Brushes.Red :
                        skeleton.TrackingId == this.nearestId ? Brushes.Black : Brushes.Gray;

                    if (brush == Brushes.Black || brush == Brushes.Gray)
                    {
                        guysHere++;
                    }


                    // Draw the individual skeleton.
                    this.DrawStickMan(skeleton, brush, 3);
                }
            }
            //guysHere = skeletons.Length;


        }

        private int PrintItPersistence = 0;
        const int MinimalHandDistance = 10; //10;
        const int Minwait2accept = 9 ; // 7 is too tiny
        private string status = "??";
        const string PrintNowCmd = "Print NOW!!";
        

        public Point polsoDestro = new Point(0, 0), spallaDestra = new Point(0, 0), 
                     spallaSinistra = new Point(0, 0), polsoSinistro = new Point(0, 0);
        Brush skelCurrentColor = Brushes.Black;
        int guysHere = 0;
        /// <summary>
        /// Draw an individual skeleton.
        /// </summary>
        /// <param name="skeleton">The skeleton to draw.</param>
        /// <param name="brush">The brush to use.</param>
        /// <param name="thickness">This thickness of the stroke.</param>
        private void DrawStickMan(Skeleton skeleton, Brush brush, int thickness)
        {           
            Debug.Assert(skeleton.TrackingState == SkeletonTrackingState.Tracked, "The skeleton is being tracked.");

            skelCurrentColor = brush;
            Boolean isFrontGuy = skelCurrentColor == Brushes.Black;

            var rightHand = new Point(0, 0);
            var leftHand = new Point(0, 0);


            foreach (var run in SkeletonSegmentRuns)
            {
                var next = this.GetJointPoint(skeleton, run[0]);
                for (var i = 0; i < run.Length; i++)
                {
                    var prev = next;
                    next = this.GetJointPoint(skeleton, run[i]);

                    var lineTh=thickness;
                    var jt = run[i];

                    if (isFrontGuy)
                    {
                        if (jt == JointType.HandRight)
                        {
                            rightHand = next;
                            polsoDestro = rightHand;
                        }

                        if (jt == JointType.HandLeft)
                        {
                            leftHand = next;
                            polsoSinistro = leftHand;
                        }


                        if (jt == JointType.ShoulderRight)
                        {
                            spallaDestra = next;
                        }
                        if (jt == JointType.ShoulderLeft)
                        {
                            spallaSinistra = next;
                        }

                    }


                    if (status == PrintNowCmd)
                    {
                        lineTh *= 3;
                        brush = Brushes.Cyan;
                        
                    }


                    //if ( (jt == JointType.HandRight) || ( jt== JointType.HandLeft)  )
                    //{
                    //    lineTh *= 4;                        
                    //}
                    //else  if (jt == JointType.Head)
                    //{
                    //    lineTh *= 2;
                    //}

                    var line = new Line
                    {
                        Stroke = brush,
                        StrokeThickness = lineTh,
                        X1 = prev.X,
                        Y1 = prev.Y,
                        X2 = next.X,
                        Y2 = next.Y,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round
                    };

                    StickMen.Children.Add(line);
                }
            }

            
            var dist=Math.Sqrt( Math.Pow( (rightHand.X - leftHand.X) ,  2) + 
                                Math.Pow( (leftHand.Y - rightHand.Y) ,  2)   );
            





            // Status stopper?

            if (dist <= MinimalHandDistance)
            {
                status = "Print?? DIST:"+dist+ " Cold down:"+PrintItPersistence;
                if (PrintItPersistence >= Minwait2accept)
                {
                    status = PrintNowCmd;
                }
                PrintItPersistence++;
            }
            else
            {
                status = "Dist" + dist;
                PrintItPersistence = 0;
            }

            //Debug.WriteLine(status+" Right//Left" + rightHand.X + " " + rightHand.Y + " // " + leftHand.X + " " + leftHand.Y);

            if (guysHere > 0)
            {
                Debug.WriteLine(status + " // " + getPositionStringData());
            }
            
            if (status == PrintNowCmd)
            {
                
                var storyboard = Resources["LeftAnimate"] as Storyboard;
                string pic;
                try
                {
                    pic = (this.picturePaths[this.indexField]);
                }
                catch (IndexOutOfRangeException ie)
                {
                    pic = (this.picturePaths[0]);
                }
                Debug.WriteLine("Firing http client message for pic:"+pic);
                //fireCmd("olaf");


                Debug.WriteLine("Fired!");
                System.Threading.Thread.Sleep(2500);
                PrintItPersistence = -100;
            }
        }

        public Boolean fireCmd(String gcode)
        {
             HttpWebRequest request = (HttpWebRequest)
                                     WebRequest.Create("http://printer3d-mi3.valueteam.com:9080/api/files/local/" + gcode + ".gcode");
            request.Method = "POST";
            request.ContentType = "application/json";

            string json = "{ \"command\": \"select\", "+
                          "  \"print\": true }";
                  
            byte[] postBytes = Encoding.UTF8.GetBytes(json);
            request.ContentLength = postBytes.Length;
    
            request.Headers["X-Api-Key"] = "F70FA681895D476E835BBEB23F28F803";

            StreamWriter writer = new StreamWriter(request.GetRequestStream());
            writer.Write(json);
            writer.Close();

            return true;
        }

        /// <summary>
        /// Convert skeleton joint to a point on the StickMen canvas.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="jointType">The joint to project.</param>
        /// <returns>The projected point.</returns>
        private Point GetJointPoint(Skeleton skeleton, JointType jointType)
        {
            var joint = skeleton.Joints[jointType];

            // Points are centered on the StickMen canvas and scaled according to its height allowing
            // approximately +/- 1.5m from center line.
            var point = new Point
            {
                X = (StickMen.Width / 2) + (StickMen.Height * joint.Position.X / 3),
                Y = (StickMen.Width / 2) - (StickMen.Height * joint.Position.Y / 3)
            };

            return point;
        }

        internal string getPositionStringData()
        {
            // First value is how much people are here?
            String v;
            v = "" + guysHere + ";";

            //if (this.skelCurrentColor == Brushes.Red)
            //{
            //    v = "1;";
            //}
            //else if (this.skelCurrentColor == Brushes.Cyan)
            //{
            //    v = "2;"; 
            //}
            //else
            //{
            //    v = "0;";
            //}
            
            // Number with "."
            v= (v + this.polsoDestro.X + ";" + this.polsoDestro.Y + ";" +
                             this.spallaDestra.X + ";" + this.spallaDestra.Y + ";" +
                             this.polsoSinistro.X + ";" + this.polsoSinistro.Y + ";" +
                             this.spallaSinistra.X + ";" + this.spallaSinistra.Y).Replace(',','.');
            return (DateTime.Now.ToString("yyyy-MM-dd H:mm:ss")+";"+v );

        }
    }
}
