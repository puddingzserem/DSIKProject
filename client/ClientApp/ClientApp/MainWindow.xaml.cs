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
using System.Text.RegularExpressions;
using ClientApp.Views;

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
        string execPath;


        public MainWindow()
        {
            InitializeComponent();
            execPath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
        }
        private void Connect(IPAddress serverIP, Int32 serverPort)
        {
            LogEvent($"*** Requested connecting to a server ***\nIP: {serverIP.ToString()}\nPort: {serverPort.ToString()}");
            try
            {
                LogEvent("Connecting to server");
                tcpClient.Connect(serverIP, serverPort);
                LogEvent("Initialising stream");
                stream = tcpClient.GetStream();
                GetListOfEntities();
                //DownloadEntity("Dupies");
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
            Byte[] data = System.Text.Encoding.ASCII.GetBytes($"{message}");
            stream.Write(data, 0, data.Length);
            LogEvent($"Sent: {message}");

        }
        private string GetResponse(int i)
        {
            LogEvent("*** Waiting for response ***");

            Byte[] data = new Byte[i];
            String responseData = String.Empty;

            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, i);
            LogEvent($"Received: {responseData}");
            responseData = responseData.Replace("\0", string.Empty);
            return responseData;
        }
        private void GetListOfEntities()
        {
            LogEvent("*** Requested list of available entities from server ***\n");
            SendMessage("l");
            string entities = GetResponse(2048);
            List<Entity> listOfEntities = new List<Entity>();
            string[] entitiesArray = entities.Split('\n');

            foreach (string entity in entitiesArray)
            {
                if (entity.Contains("."))
                {
                    continue;
                }
                listOfEntities.Add(new Entity(entity));
            }
            ItemsListing.Items.Clear();
            List<string> cachedItems = new List<string>();
            string[] stringCachedItems = Directory.GetDirectories("./_CacheDirectory").ToArray();
            foreach(string str in stringCachedItems)
            {
                    cachedItems.Add(str.Substring(str.LastIndexOfAny(new char[] { '\\', '/' }) + 1));
            }

            LogEvent("Checking available entities with cache\n");
            var regex = new Regex(@"^[a-zA-Z]+$");

            foreach (Entity entity in listOfEntities)
            {
                Match match = regex.Match(entity.GetEntityName());
                if (match.Success)
                {
                    if (cachedItems.Contains(entity.GetEntityName()))
                        entity.IsDownloaded();
                    EntityView entityView = new EntityView(entity, this);
                    entityView.RefreshRequest += DoubleClick;
                    ItemsListing.Items.Add(entityView);
                }
            }

        }
        private void DownloadEntity(string whichEntity)
        {
            LogEvent("*** Requested download ***\n");
            SendMessage("d");
            if (GetResponse(2).Contains("OK"))
            {
                LogEvent("Server waiting for specification\n");
                string filename = whichEntity;
                string path = $"./_CacheDirectory/{whichEntity}";

                LogEvent($"Waiting for info about {whichEntity}\n");
                SendMessage(whichEntity);

                int howManyFiles = Int32.Parse(GetResponse(4));
                LogEvent($"Will receive {howManyFiles} files\n");

                Directory.CreateDirectory(path);

                for (int i = 0; i < howManyFiles; i++)
                {
                    switch (i)
                    {
                        case 0:
                            filename = whichEntity + ".txt";
                            break;
                        case 1:
                            filename = whichEntity + ".jpg";
                            break;
                        case 2:
                            filename = whichEntity + ".mp4";
                            break;
                    }
                    SendMessage("OK");

                    long size = Int64.Parse(GetResponse(20));

                    SendMessage("OK");


                    LogEvent($"*** Receiving a file {i + 1} of {howManyFiles} ***");
                    using (FileStream fs = new FileStream((path + "/" + filename), FileMode.OpenOrCreate))
                    {
                        var receivedFileBuffer = new byte[size];
                        byte[] buffer = null;
                        while (size > 0)
                        {
                            buffer = new byte[1024];
                            int partsize = stream.Read(buffer, 0, 1024);
                            fs.Write(buffer, 0, partsize);
                            size -= partsize;
                        }

                        System.Windows.MessageBox.Show("File received\n");
                    }
                    
                    
                }
                SendMessage("OK");
            }
            //Preview(GetEntityDataFromDirectory($".\\_CacheDirectory\\{whichEntity}"));
            try { Preview(GetEntityDataFromDirectory($"./_CacheDirectory/{whichEntity}")); } catch { LogEvent("Couldn't get all animal data\n"); }
            GetListOfEntities();

        }
        private void UploadEntity(Entity entity)
        {
            string[] path = new string[3];
            long[] size = new long[3];
            int i=0;
            SendMessage("s");
            GetResponse(2);
            SendMessage(entity.GetEntityName());
            GetResponse(2);


            if (entity.GetEntityDescription() != null)
            {
                path[0] = entity.GetEntityDescription();
                size[0] = new System.IO.FileInfo(path[0]).Length;
 
                i++;
            }
            if (entity.GetEntityImage() != null)
            {
                path[1] = entity.GetEntityImage();
                size[1] = new System.IO.FileInfo(path[1]).Length;

                i++;
            }
            if (entity.GetEntityVideo() != null)
            {
                path[2] = entity.GetEntityVideo();
                size[2] = new System.IO.FileInfo(path[2]).Length;

                i++;
            }

            SendMessage(i.ToString());
            GetResponse(2);
            for(int j=0; j<i; j++)
            {
                byte[] data = File.ReadAllBytes(path[j]);
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                int bufferSize = 1024;
                SendMessage(size[j].ToString());
                GetResponse(2);

                int bytesSent = 0;
                int bytesLeft = data.Length;

                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(bufferSize, bytesLeft);
                    stream.Write(data, bytesSent, curDataSize);
                    bytesSent += curDataSize;
                    bytesLeft -= curDataSize;
                }

                GetResponse(2);

            }


        }

        private void DisconnectButtonClick(object sender, RoutedEventArgs e)
        {
            LogEvent("Disconnecting...\n");
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
                ServerMessageBox.IsEnabled = true;
                SendMessageButton.IsEnabled = true;
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
            ServerMessageBox.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
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
                ItemDescription.Text = File.ReadAllText(entityObject.GetEntityDescription(), Encoding.Default);
            else
                ItemDescription.Text = null;
            if (!String.IsNullOrEmpty(entityObject.GetEntityImage()))
            {
                string path = execPath+"\\_CacheDirectory\\"+entityObject.GetEntityName()+"\\"+ entityObject.GetEntityImage().Substring(entityObject.GetEntityImage().LastIndexOfAny(new char[] { '\\', '/' }) + 1);
                var uriSource = new Uri(path);
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
        private Entity GetEntityDataFromDirectory(string path)
        {
            String name = path.Substring(path.LastIndexOfAny(new char[] { '\\', '/' }) + 1);
            Entity newObject = new Entity(name);
            List<string> files = Directory.GetFiles(path).ToList();
            //description
            string description = files.Find(p => p.Contains(".txt"));
            if (!String.IsNullOrEmpty(description)) newObject.SetEntityDescription(description);
            //image
            string image = files.Find(p => p.Contains(".jpg"));
            if (!String.IsNullOrEmpty(image)) newObject.SetEntityImage(image);
            //video
            string video = files.Find(p => p.Contains(".mp4"));
            if (!String.IsNullOrEmpty(video)) newObject.SetEntityVideo(video);
            return newObject;
        }
        private void UploadButtonClick(object sender, RoutedEventArgs e)
        {
            LogEvent("Uploading entity data");
            String pathToFiles = UploadFolderPath.Text;
            Entity entity = GetEntityDataFromDirectory(pathToFiles);
            //Preview(entity);
            ColorBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 106, 2));
            UploadEntity(entity);
            GetListOfEntities();
        }

        private void ItemVideoButtonClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(videoCache);
        }

        private void DoubleClick(object sender, Entity entity)
        {
            if (entity.Downloaded())
            {
                try { Preview(GetEntityDataFromDirectory($".\\_CacheDirectory\\{entity.GetEntityName()}")); } catch { LogEvent("Couldn't get all animal data\n"); }
            }
            else
            {
                //DownloadEntity(entity.GetEntityName());
                try { DownloadEntity(entity.GetEntityName()); } catch { LogEvent($"Error downloading {entity.GetEntityName()} from server"); }
            }
        }

        private void SendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(ServerMessageBox.Text))
                SendMessage(ServerMessageBox.Text);
        }
    }
}
