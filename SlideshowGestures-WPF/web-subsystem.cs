using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Threading;


using Microsoft.Samples.Kinect.Slideshow;

using System.Diagnostics;

/**
 * GG: Micro web Server
 * Test with the apache
 * ab -n 1200 -c 100  http://172.16.220.133:2323/test.txt
 * ab -n 1200 -c 200  http://172.16.220.133:2323/test.txt
 */
namespace MicroWebServer
{
    /// <summary>
    /// 
    /// </summary>
    public class WebServer
    {
        /// <summary>
        /// 
        /// </summary>
        public const int MaxThreads = 2;
        /// <summary>
        ///
        /// </summary>
        public const int MinSecondsSpeedLimit = 1;


        private Int32 slowDownFactor = 0;

        private Dictionary<string, string> extension2Mime = new Dictionary<string, string>();
        HttpListener listener;
        string baseFolder;
        Boolean pleaseRun = false;
        MainWindow mainWindow;
        public WebServer(List<string> urlmaps, string baseFolder, MainWindow mainWindowI)
        {
            this.mainWindow = mainWindowI;
            extension2Mime.Add("txt", "text/plain");
            extension2Mime.Add("html", "text/html");
            extension2Mime.Add("htm", "text/html");
            extension2Mime.Add("sys", "text/html");
            //System.Threading.ThreadPool.SetMaxThreads(50, 1000);
            System.Threading.ThreadPool.SetMaxThreads(((int)MaxThreads*(3/2)), 1000);
            System.Threading.ThreadPool.SetMinThreads(MaxThreads, MaxThreads);
            listener = new HttpListener();
            foreach (var u in urlmaps)
            {
                listener.Prefixes.Add(u);
                Console.WriteLine("Added:"+u);
            }
            this.baseFolder = baseFolder;
        }

        public void Start()
        {
            pleaseRun = true;
            listener.Start();
            while (pleaseRun)
            {
                try
                {
                    HttpListenerContext request = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, request);
                }
                catch (HttpListenerException e)
                {
                    printError(e);
                    
                }
                catch (InvalidOperationException e)
                {
                    printError(e);
                    
                }
            }
            listener.Stop(); 
        }
        public void Stop() { pleaseRun = false;  }
        public void ProcessRequest(object listnerContext)
        {
            try
            {
                var startTime=DateTime.Now.Millisecond;
                var context = (HttpListenerContext)listnerContext;
                var filename = Path.GetFileName(context.Request.RawUrl);
                var path = Path.Combine(baseFolder, filename);
                byte[] msg;
                if (!File.Exists(path))
                {
                    // TODO: Adding special exit url (smart)
                    // TO avoid it simply create a exit.sys which shadows it
                    if (context.Request.RawUrl.Equals("/data"))
                    {
                        //Debug.WriteLine("Exit Requested...");
                        // Read data and push on msg

                        msg = Encoding.UTF8.GetBytes(
                            mainWindow.getPositionStringData()+"\r\n"
                            );
                        //msg = Encoding.UTF8.GetBytes("Exit Requested");
                        //pleaseRun = false;
                        //path = "[Exit Command]";
                    }
                    else
                    {
                        // Not Found
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        msg = Encoding.UTF8.GetBytes("Page not found");
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    msg = File.ReadAllBytes(path);
                    context.Response.ContentType = findContentType(context.Request.RawUrl);
                }


                context.Response.Headers["Server"] = "Gioorgi.com";
                context.Response.Headers["X-Gioorgi-XZY"] = "BOF";
                context.Response.Headers["X-Gioorgi-Server"] = "MicroWebServer V 1.0";
                context.Response.ContentLength64 = msg.Length;
                string logmsg = Thread.CurrentThread.Name + " " + context.Response.StatusCode +
                    " "+context.Request.RawUrl+ "->" + path;
                logmsg += " " + context.Request.RemoteEndPoint.Address + "/" + context.Request.RemoteEndPoint.Port;

                
                var endTime = DateTime.Now.Millisecond;
                var elabTimeMs = endTime - startTime;
                Double deltaWait= (MinSecondsSpeedLimit*1000)-elabTimeMs;
                deltaWait *= 0.90; // Correct it a bit

                if (deltaWait > 1)
                {
                    // 1Kb = 1ms less. To be fair with very big chunk
                    var computedWait = (int)(deltaWait - (msg.Length / 1024));
                    if (computedWait < 0)
                    {
                        computedWait = (int) deltaWait;
                    }
                    
                    /*
                    Console.WriteLine(logmsg + " Done into:" + elabTimeMs + " (" +
                        ((elabTimeMs / (MinSecondsSpeedLimit * 1000))) * 100 + "%" +
                        ") ");
                     */

                    //Thread.Sleep(computedWait);
                }


                using (var s = context.Response.OutputStream)
                {
                    s.Write(msg, 0, msg.Length);
                }






            }
            catch (Exception ex)
            {
                printError(ex);
            }
        }

        
        private string findContentType(string uri)
        {
            string r;
            var elements=uri.Split('.');
            string extension=elements.Last();
            if (extension2Mime.ContainsKey(extension))
            {
                r= extension2Mime[extension];
            }
            else
            {
                r= "text/html";
            }
            //Console.WriteLine("Map "+uri+" as "+extension+" to "+r);
            return r;
        }

        private void printError(Exception ex)
        {
            Console.Error.WriteLine("");
            Console.Error.WriteLine("ERROR:" + ex.ToString());
            //Console.ReadLine();
        }

        public Boolean isRunning()
        {
            return pleaseRun;
        }
    }
    /*
    class Program
    {
        static void Main(string[] args)
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
            //names.Add("http://172.16.220.133:2323/");
            var server = new WebServer(names, ".");
            new System.Threading.Thread(server.Start).Start();
            Console.WriteLine("MicroWeb Server is Running. Press ^C to stop");
            //while (server.isRunning())
            //{
            //}            
            // Console.ReadLine();
            //server.Stop();
        }
    }*/
}
