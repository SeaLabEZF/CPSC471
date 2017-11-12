﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace ClientSide
{
    public class FtpClient
    {
        //create a listener for TCP events
        private TcpListener listener;
        public Boolean quit;

        //initializer for the FTP Server
        public FtpClient()
        { }

        //starts the TCP listener and sends the connections to HandleAcceptTcpClient
        public void Start(String ip_address, uint port_number)
        {
            //we shouldnt' quit unti lwe feel like it
            quit = false;

            listener = new TcpListener(IPAddress.Any, (int)port_number);
            listener.Start();
            listener.BeginAcceptTcpClient(HandleAcceptTcpClient, listener);
        }

        //stops the listener from listening to TCP events
        public void Stop()
        {
            if (listener != null)
            {
                listener.Stop();
            }
        }

        public static bool IsLinux
        {
            get
            {
                int r = (int)Environment.OSVersion.Platform;
                return (r == 4) || (r == 6) || (r == 128);
            }
        }

        //called whenever a client connects to the server
        private async void HandleAcceptTcpClient(IAsyncResult result)
        {
            //get a refernce to the client
            TcpClient client = listener.EndAcceptTcpClient(result);

            //further connectsion will also be handled by this method
            listener.BeginAcceptTcpClient(HandleAcceptTcpClient, listener);

            //get the stream from the client and set up a reader and a writer
            NetworkStream stream = client.GetStream();
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
            using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
            {
                //send this to let the client know we accepted the connection
                writer.Write("ftp>");
                writer.Flush();

                //we will keep looping later until we quit
                bool quit = false;

                //read in the inital line
                string line = await reader.ReadLineAsync();

                //while (!quit && !string.IsNullOrEmpty(line))
                //loop until we are told to quit
                while (!quit)
                {
                    //split the line by spaces
                    var args = line.Split(' ');
                    if (args.Length > 0)
                    {
                        if (args[0] == "get")
                        {
                            //set up a new connection to send the file
                            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("something");
                            request.Method = WebRequestMethods.Ftp.UploadFile;

                            StreamReader sourceStream = new StreamReader(args[1]);
                            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                            sourceStream.Close();
                            request.ContentLength = fileContents.Length;

                            Stream requestStream = request.GetRequestStream();
                            requestStream.Write(fileContents, 0, fileContents.Length);
                            requestStream.Close();

                            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                            writer.WriteLine("Download Complete, status {0}", response.StatusCode);

                            response.Close();
                        }
                        else if (args[0] == "put")
                        {
                            //set up a new connection to recieve the file
                            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("something");
                            request.Method = WebRequestMethods.Ftp.DownloadFile;

                            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                            Stream responseStream = response.GetResponseStream();
                            StreamReader dataReader = new StreamReader(responseStream);
                            writer.WriteLine(dataReader.ReadToEnd());

                            writer.WriteLine("Upload Complete, status {0}", response.StatusCode);

                            dataReader.Close();
                            response.Close();
                        }
                        else if (args[0] == "ls") //possibly add 'cd' - or are we supposed to make a 'ls -l' like directory tree?
                        {
                            string temp = null;
                            //list the file
                            Process proc = new Process();
                            if (IsLinux)
                            {
                                proc.StartInfo.FileName = "/bin/bash";
                                temp = "ls";
                            }
                            else
                            {
                                proc.StartInfo.FileName = "cmd.exe";
                                temp = "dir";
                            }
                            proc.StartInfo.CreateNoWindow = true;
                            proc.StartInfo.RedirectStandardInput = true;
                            proc.StartInfo.RedirectStandardOutput = true;
                            proc.StartInfo.UseShellExecute = false;
                            proc.Start();
                            proc.StandardInput.WriteLine(temp);
                            proc.StandardInput.Flush();
                            proc.StandardInput.Close();
                            proc.WaitForExit();
                            writer.Write("ftp>" + proc.StandardOutput.ReadToEnd());
                        }
                        else if (args[0] == "quit")
                        {
                            //set quit to true
                            quit = true;
                        }
                        else
                        {
                            writer.WriteLine("'" + line + "' isn't an accepted command.");
                        }
                    }
                    else
                    {
                        writer.WriteLine("<Empty Line> isn't an accepted command.");
                    }
                    writer.Flush();
                    line = await reader.ReadLineAsync();
                }
            }
        }
    }
}