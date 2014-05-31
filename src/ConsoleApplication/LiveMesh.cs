using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.LiveFX.Client;
using Microsoft.LiveFX.ResourceModel;

namespace ConsoleApplication
{
    public class LiveMesh
    {
        #region Fields

        private const string serverUri = "https://user-ctp.windows.net";
        private readonly LiveOperatingEnvironment environment = new LiveOperatingEnvironment();
        
        #endregion

        #region Constructor(s)

        public LiveMesh(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        #endregion

        #region Properties

        public string UserName { get; set; }
        public string Password { get; set; }

        #endregion

        #region Methods

        public bool Connect()
        {
            // Using the LiveItemAccessOptions class you can specify if you want to auto-load
            // relations and if you wish to receive notifications.
            LiveItemAccessOptions options = new LiveItemAccessOptions(true);

            // Create a user token. You  need to pass a token based on your Windows Live Id
            // to the LOE object's connect method. The GetWindowsLiveAuthenticationToken() is
            // an extension method which resides in the Microsoft.LiveFX.ResourceModel namespace.
            string credential = new NetworkCredential(UserName, Password, serverUri).GetWindowsLiveAuthenticationToken();

            // Connect and retrieve the Mesh object
            environment.Connect(credential, AuthenticationTokenType.UserToken, new Uri(serverUri), options);

            // When connected the Mesh property of the LOE object will be set.
            return environment.Mesh != null;
        }

        public IEnumerable<MeshObject> GetMeshObjects()
        {
            // If you only want to return Mesh objects which are folders,
            // then add the following where clause:
            // where o.Resource.Type == "LiveMeshFolder"
            return from o in environment.Mesh.CreateQuery<MeshObject>().Execute()
                   select o;

            // OR 
            // CreateQuery<T> Versus Entries collection
            //return from o in environment.Mesh.MeshObjects.Entries select o;
        }

        public IEnumerable<DataEntry> GetFilesOfFolder(MeshObject meshObject)
        {
            // Obtain the data feed. 
            // A Mesh Object representing a folder only has one data feed.
            // It's title is LiveMeshFiles.
            DataFeed feed = (from f in meshObject.CreateQuery<DataFeed>().Execute()
                             where f.Resource.Type == "LiveMeshFiles"
                             select f).First();
            if (feed == null)
            {
                throw new InvalidOperationException("MeshObject does not have any data feeds");
            }
            return from e in feed.CreateQuery<DataEntry>().Execute() select e;
        }

        public void CreateDirectory(string directory)
        {
            // Do not create the directory if it already exists
            if (DirectoryExists(directory))
            {
                return;
            }

            // Create a new Mesh object and set its type
            MeshObject newDirectory = new MeshObject(directory) {Resource = {Type = "LiveMeshFolder"}};

            // Add the new object to your Mesh
            environment.Mesh.MeshObjects.Add(ref newDirectory);

            // Create a data feed, set its type and handler
            DataFeed feed = 
                new DataFeed("LiveMeshFiles") 
                { Resource = { Type = "LiveMeshFiles", HandlerType = "FileSystem" } };

            // Add the data feed to the new Mesh object
            newDirectory.DataFeeds.Add(ref feed);
        }

        public bool DirectoryExists(string directory)
        {
            var q = FindDirectory(directory);
            return q != null;
        }

        public MeshObject FindDirectory(string directory)
        {
            return (from o in environment.Mesh.CreateQuery<MeshObject>().Execute()
                    where o.Resource.Type == "LiveMeshFolder" &&
                          o.Resource.Title == directory
                    select o).FirstOrDefault();
        }

        private static string GetMimeType(string path)
        {
            string mimeType = "application/unknown";
            string ext = Path.GetExtension(path).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
            {
                mimeType = regKey.GetValue("Content Type").ToString();
            }
            return mimeType;
        }

        public void AddFileToDirectory(string directory, string path)
        {
            MeshObject meshObject = FindDirectory(directory);
            if (meshObject == null)
            {
                return;
            }
            DataFeed feed = (from f in meshObject.CreateQuery<DataFeed>().Execute()
                             where f.Resource.Type == "LiveMeshFiles"
                             select f).First();
            Stream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            feed.DataEntries.Add(fileStream, path, GetMimeType(path));
            // Make sure all devices will get synchronized.
            feed.SyncEntries.Synchronize();
            feed.Update();
        }

        public DataEntry GetFileByFilename(string filename)
        {
            IEnumerable<MeshObject> meshObjects = GetMeshObjects();
            foreach(var meshObject in meshObjects)
            {
                DataFeed feed = (from f in meshObject.CreateQuery<DataFeed>().Execute()
                                 where f.Resource.Type == "LiveMeshFiles"
                                 select f).First();
                if (feed != null)
                {
                    DataEntry dataEntry = (from e in feed.CreateQuery<DataEntry>().Execute()
                                           where e.Resource.Title == filename
                                           select e).FirstOrDefault();
                    if (dataEntry != null)
                    {
                        return dataEntry;
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
