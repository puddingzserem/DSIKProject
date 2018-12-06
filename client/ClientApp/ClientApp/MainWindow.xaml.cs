using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using ClientApp.Models;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace ClientApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string videoCache;
        List<string> logs = new List<string>();
        TcpClient tcpClient = new TcpClient();
        NetworkStream stream;


        public MainWindow()
        {
            InitializeComponent();
        }

        #region server logic
        private void Connect(IPAddress serverIP, Int32 serverPort)
        {
            LogEvent($"*** Requested connecting to a server ***\nIP: {serverIP.ToString()}\nPort: {serverPort.ToString()}");
            try
            {
                LogEvent("Connecting to server");
                tcpClient.Connect(serverIP, serverPort);

                // Get a client stream for reading and writing.
                LogEvent("Initialising stream");
                stream = tcpClient.GetStream();
                GetListOfEntities();
                DownloadEntity("Pies");
            }
            catch (ArgumentNullException e)
            {
                LogEvent($"ArgumentNullException: {e}");
                SetDefaultWindowState();
            }
            catch (SocketException e)
            {
                LogEvent($"SocketException: {e}");
                SetDefaultWindowState();
            }
        }
        private void SendMessage(string message)
        {
            LogEvent("*** Sending a message ***");

            // Translate the passed message into ASCII and store it as a Byte array.
            Byte[] data = System.Text.Encoding.ASCII.GetBytes($"{message}");

            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);

            LogEvent($"Sent: {message}");
        }
        private string GetResponse()
        {
            LogEvent("*** Waiting for response ***");

            // Buffer to store the response bytes.
            Byte[] data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            LogEvent($"Received: {responseData}");
            return responseData;
        }
        private bool Request(RequestActions requestActions, char action)
        {
            LogEvent($"*** Requested action {requestActions.ToString()} from server ***");

            //Send info about request
            SendMessage(action.ToString());

            // Receive the TcpServer.response.
            if (GetResponse().Contains("OK"))
            {
                LogEvent($"Initialising action {requestActions.ToString()}");
                return true;
            }
            else
            {
                LogEvent($"Server denied action {requestActions.ToString()}");
                return false;
            }
        }

        #endregion

        #region interaction logic
        private void GetListOfEntities()
        {
            SendMessage("l");
            string entities = GetResponse();
            //SendMessage("l");
            //List<string> listOfEntities = new List<string>();
            //while (!String.IsNullOrWhiteSpace(entities))
            //{
            //    var remainingString = entities.Substring(entities.LastIndexOf('\n') + 1);
            //    string entity = entities.Substring(0, entities.LastIndexOf('\n'));
            //    entities = remainingString;
            //    listOfEntities.Add(entity);
            //}
            //ItemsListing.Items.Clear();
            //foreach (string entity in listOfEntities)
            //{
            //    ItemsListing.Items.Add(entity);
            //}
        }
        private void DownloadEntity(string whichEntity)
        {
            if (Request(RequestActions.DownloadAnimal,'d'))
            {
                SendMessage(whichEntity);
                if (GetResponse().Contains("OK"))
                {
                    //int howManyFiles = Int32.Parse(GetResponse());
                    //Directory.CreateDirectory($"./_CacheDirectory/{whichEntity}");
                    SendMessage("Piesek");
                    int wielkosc = int.Parse(GetResponse());
                    //for(int i = 0; i < howManyFiles; i++)
                    //{
                    //    using (var output = File.Create("result.dat"))
                    //    {
                    //        LogEvent($"*** Receiving a file {i+1} of {howManyFiles} ***");

                    //        // read the file in chunks of 1KB
                    //        var receivedFileBuffer = new byte[1024];
                    //        int bytesRead;
                    //        while ((bytesRead = stream.Read(receivedFileBuffer, 0, receivedFileBuffer.Length)) > 0)
                    //        {
                    //            output.Write(receivedFileBuffer, 0, bytesRead);
                    //        }
                    //    }
                    //}
                }
            }
            SendMessage("Pie");
        }
        private void UploadEntity(Entity entity)
        {
            if (Request(RequestActions.UploadAnimal,'u'))
                SendMessage("bedzie upload");
        }
        private void DisconnectButtonClick(object sender, RoutedEventArgs e)
        {
            tcpClient.Close();
            SetDefaultWindowState();
            Environment.Exit(0);
        }
        private void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            bool go = true;
            Int32 serverPort = 0;
            IPAddress serverIP = IPAddress.Parse("0.0.0.0");

            LogEvent("Requested connecting");
            string ipAddress = IPAddressBox.Text;
            string port = PortBox.Text;

            LogEvent("Parsing IP address and port");
            try
            {
                serverPort = Int32.Parse(port);
                serverIP = IPAddress.Parse(ipAddress);
            }
            catch
            {
                LogEvent("Parameters invalid");
                SetDefaultWindowState();
                go = false;
            }
            if (go)
            {
                //window setting
                DisconnectButton.IsEnabled = true;
                IPAddressBox.IsEnabled = false;
                PortBox.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                SearchButton.IsEnabled = true;
                UploadFolderPath.IsEnabled = true;
                ServerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 108));

                CreateCache();

                Connect(serverIP, serverPort);
            }
        }
        private void LogEvent(string message)
        {
            logs.Add(message);
            LogBox.Items.Add(message);
        }
        #endregion

        #region window logic
        private void SetDefaultWindowState()
        {
            IPAddressBox.IsEnabled = true;
            PortBox.IsEnabled = true;
            ServerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(108,108,108));
            UploadFolderPath.IsEnabled = false;
            UploadButton.IsEnabled = false;
            SearchButton.IsEnabled = false;
            ConnectButton.IsEnabled = true;
            IPAddressBox.IsEnabled = true;
            PortBox.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
        }

        private void CreateCache()
        {
            LogEvent("Creating Cache");
            try
            {
                Directory.CreateDirectory("./_CacheDirectory");
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show($"Error: {e.Data.ToString()}");
            }
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    UploadFolderPath.Text = folderDialog.SelectedPath;
                    UploadButton.IsEnabled = true;
                }
            }
        }

        private void Preview(Entity entityObject)
        {
            if (!String.IsNullOrEmpty(entityObject.GetEntityName()))
                ItemName.Text = entityObject.GetEntityName();
            else
                ItemName.Text = null;
            if (!String.IsNullOrEmpty(entityObject.GetEntityDescription()))
                ItemDescription.Text = File.ReadAllText(entityObject.GetEntityDescription());
            else
                ItemDescription.Text = null;
            if (!String.IsNullOrEmpty(entityObject.GetEntityImage()))
            {
                var uriSource = new Uri(entityObject.GetEntityImage());
                ItemThumbnail.Source = new BitmapImage(uriSource);
            }
            else
                ItemThumbnail.Source = null;
            if (!String.IsNullOrEmpty(entityObject.GetEntityVideo()))
            {
                ItemVideoButton.IsEnabled = true;
                videoCache = entityObject.GetEntityVideo();
            }
            else
            {
                ItemVideoButton.IsEnabled = false;
                videoCache = null;
            }
        }

        private void UploadButtonClick(object sender, RoutedEventArgs e)
        {
            LogEvent("Uploading entity data");
            String pathToFiles = UploadFolderPath.Text;
            String name = pathToFiles.Substring(pathToFiles.LastIndexOfAny(new char[] { '\\', '/' }) + 1);
            Entity newObject = new Entity(name);
            List <string> files = Directory.GetFiles(pathToFiles).ToList();
            //description
            string description = files.Find(p => p.Contains(".txt"));
            if (!String.IsNullOrEmpty(description)) newObject.SetEntityDescription(description);
            //image
            string image = files.Find(p => p.Contains(".jpg"));
            if (!String.IsNullOrEmpty(image)) newObject.SetEntityImage(image);
            //video
            string video = files.Find(p => p.Contains(".mp4"));
            if (!String.IsNullOrEmpty(video)) newObject.SetEntityVideo(video);
            Preview(newObject);
            ColorBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 106, 2));
            UploadEntity(newObject);
        }

        private void ItemVideoButtonClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(videoCache);
        }
        #endregion

    }
}
