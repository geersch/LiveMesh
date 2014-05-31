using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.LiveFX.Client;

namespace ConsoleApplication
{
    class Program
    {
        private const string userName = "xxxxx@xxxxx.xxxxx";
        private const string password = "xxxxx";

        static void Main(string[] args)
        {
            LiveMesh myMesh = new LiveMesh(userName, password);

            bool connected = myMesh.Connect();
            if (connected)
            {
                Console.WriteLine("Connected");
                Console.WriteLine();

                Console.WriteLine("Your Mesh Objects");
                Console.WriteLine("-----------------");
                Console.WriteLine();

                // List the MeshObjects in your Mesh
                IEnumerable<MeshObject> meshObjects = myMesh.GetMeshObjects();
                foreach(var meshObject in meshObjects)
                {
                    Console.WriteLine(meshObject.Resource.Title);
                    Console.WriteLine();

                    IEnumerable<DataEntry> files = myMesh.GetFilesOfFolder(meshObject);
                    foreach(var file in files)
                    {
                        Console.WriteLine(String.Format("\t{0}", file.Resource.Title));
                    }
                    Console.WriteLine();
                }

                // Add a folder
                myMesh.CreateDirectory("My new folder");
                myMesh.AddFileToDirectory("My new folder", "About.txt");

                // Retrieve a file
                DataEntry dataEntry = myMesh.GetFileByFilename("About.txt");
                if (dataEntry != null)
                {
                    MemoryStream stream = new MemoryStream();
                    dataEntry.ReadMediaResource(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(stream);
                    Console.WriteLine(reader.ReadToEnd());
                }
            }
            else
            {
                Console.WriteLine("Could not establish connection");
            }

            Console.ReadLine();
        }
    }
}
