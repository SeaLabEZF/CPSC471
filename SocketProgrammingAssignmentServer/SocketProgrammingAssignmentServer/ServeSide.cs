using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServerSide
{
    public class FtpServer
    {
        private TcpListener _listener;

        public FtpServer()
        { }

        public void Start(uint port_number)
        {
            _listener = new TcpListener(IPAddress.Any, (int)port_number);
            _listener.Start();
            _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);
        }

        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Stop();
            }
        }

        private void HandleAcceptTcpClient(IAsyncResult result)
        {

            _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);
            TcpClient client = _listener.EndAcceptTcpClient(result);

            ClientConnection connection = new ClientConnection(client);

            ThreadPool.QueueUserWorkItem(connection.HandleClient, client);
        }
    }

    public class ClientConnection
    {
        //create a listener for TCP events
        private TcpClient _controlClient;
        private TcpClient _dataClient;


        private NetworkStream _controlStream;
        private StreamReader _controlReader;
        private StreamWriter _controlWriter;
        private string _CurrentDirectory = "\\";
        private string _transferType;
        private IPEndPoint _dataEndpoint;
        private TcpListener _passiveListener;
        private Array portArray;
        private StreamReader _dataReader;
        private StreamWriter _dataWriter;

        public ClientConnection(TcpClient client)
        {
            _controlClient = client;

            _controlStream = _controlClient.GetStream();

            _controlReader = new StreamReader(_controlStream);
            _controlWriter = new StreamWriter(_controlStream);
            

        }

        public void HandleClient(object obj)
        {
            _controlWriter.WriteLine("220 Service Ready.");
            _controlWriter.Flush();

            string line;

            try
            {
                while (!string.IsNullOrEmpty(line = _controlReader.ReadLine()))
                {
                    string response = null;

                    string[] command = line.Split(' ');

                    string cmd = command[0].ToUpperInvariant();
                    string arguments = command.Length > 1 ? line.Substring(command[0].Length + 1) : null;
                    if (string.IsNullOrWhiteSpace(arguments))
                        arguments = null;

                    if (response == null)
                    {
                        switch (cmd)
                        {
                            case "TYPE":
                                string[] splitArgs = arguments.Split(' ');
                                response = Type(splitArgs[0], splitArgs.Length > 1 ? splitArgs[1] : null);
                                break;
                            case "PASV":
                                response = Passive();
                                break;
                            case "GET":
                                response = Retrieve(arguments);
                                break;
                            case "PUT":
                                break;
                            case "LS":
                                response = List(arguments);
                                break;
                            case "QUIT":
                                response = "221 Service closing control connection";
                                break;

                            default:
                                response = "502 Command not implemented";
                                break;
                        }
                    }

                    if (_controlClient == null || !_controlClient.Connected)
                    {
                        break;
                    }
                    else
                    {
                        _controlWriter.WriteLine(response);
                        _controlWriter.Flush();

                        if (response.StartsWith("221"))
                        {
                            break;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private string Type(string typeCode, string formatControl)
        {
            string response = "500 ERROR";

            switch (typeCode)
            {
                case "A":
                    response = "200 OK";
                    break;
                case "I":
                    _transferType = typeCode;
                    response = "200 OK";
                    break;
                case "E":
                case "L":
                default:
                    response = "504 Command not implemented for that parameter.";
                    break;        
            }

            if(formatControl != null)
            {
                switch (formatControl)
                {
                    case "N":
                        response = "200 OK";
                        break;
                    case "T":
                    case "C":
                    default:
                        response = "504 Command not implemented for that parameter.";
                        break;
                }
            }

            return response;
        }

        private string Passive()
        {
            IPAddress localAddress = ((IPEndPoint)_controlClient.Client.LocalEndPoint).Address;

            _passiveListener = new TcpListener(localAddress, 0);
            _passiveListener.Start();

            IPEndPoint localEndpoint = ((IPEndPoint)_passiveListener.LocalEndpoint);

            byte[] address = localEndpoint.Address.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(portArray);

            return string.Format("227 Entering Passive Mode.");
        }

        private string List(string pathname)
        {
            if (pathname == null)
                pathname = string.Empty;

            pathname = new DirectoryInfo(Path.Combine(_CurrentDirectory, pathname)).FullName;

            if (Directory.Exists(pathname))
            {
                _dataClient = new TcpClient();
                _dataClient.BeginConnect(_dataEndpoint.Address, _dataEndpoint.Port, DoList, pathname);

                return string.Format("150 LS");
            }
            return "450 Requested file action not taken";
        }

        private void DoList(IAsyncResult result)
        {
            _dataClient.EndConnect(result);

            string pathname = (string)result.AsyncState;

            using (NetworkStream dataStream = _dataClient.GetStream())
            {
                _dataReader = new StreamReader(dataStream, Encoding.ASCII);
                _dataWriter = new StreamWriter(dataStream, Encoding.ASCII);
            }

            IEnumerable<string> directories = Directory.EnumerateDirectories(pathname);

            foreach (string dir in directories)
            {
                DirectoryInfo d = new DirectoryInfo(dir);

                string date = d.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ?
                    d.LastWriteTime.ToString("MMM dd yyyy") :
                    d.LastWriteTime.ToString("MMM dd HH:mm");

                string line = string.Format("drwxr-x-x  2   2003    2003    {0,8} {1} {2}", "4096", date, d.Name);

                _dataWriter.WriteLine(line);
                _dataWriter.Flush();
            }

            IEnumerable<string> files = Directory.EnumerateFiles(pathname);

            foreach (string file in files)
            {
                FileInfo f = new FileInfo(file);

                string date = f.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ?
                    f.LastWriteTime.ToString("MMM dd yyyy") :
                    f.LastWriteTime.ToString("MMM dd HH:mm");

                string line = string.Format("-rw-r--r--     2 2003  2003    {0,8} {1} {2}", f.Length, date, f.Name);

                _dataWriter.WriteLine(line);
                _dataWriter.Flush();
            }

            _dataClient.Close();
            _dataClient = null;

            _controlWriter.WriteLine("226 Transfer complete");
            _controlWriter.Flush();
        }
        private string Retrieve(string pathname)
        {
            if(File.Exists(pathname))
            {
                _dataClient = new TcpClient();
                _dataClient.BeginConnect(_dataEndpoint.Address, _dataEndpoint.Port, DoRetrieve, pathname);
                return string.Format("150 get");
            }
            return "550 File Not Found";
        }
        private void DoRetrieve(IAsyncResult result)
        {
            _dataClient.EndConnect(result);

            string pathname = (string)result.AsyncState;

            using (NetworkStream dataStream = _dataClient.GetStream())
            {
                using (FileStream fs = new FileStream(pathname, FileMode.Open, FileAccess.Read))
                {
                    copyStream(fs, dataStream);

                    _dataClient.Close();
                    _dataClient = null;

                    _controlWriter.WriteLine("226 Closing data connection, file transfer successful");
                    _controlWriter.Flush();
                }
            }
        }

        private static long copyStream(Stream input, Stream output, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int count = 0;
            long total = 0;

            while ((count = input.Read(buffer, 0, buffer.Length))>0)
            {
                output.Write(buffer, 0, count);
                total += count;
            }

            return total;
        }

        private static long copyStreamAscii(Stream input, Stream output, int bufferSize)
        {
            char[] buffer = new char[bufferSize];
            int count = 0;
            long total = 0;

            using (StreamReader rdr = new StreamReader(input))
            {
                using (StreamWriter wtr = new StreamWriter(output, Encoding.ASCII))
                {
                    while((count = rdr.Read(buffer, 0, buffer.Length))>0)
                    {
                        wtr.Write(buffer, 0, count);
                        total += count;
                    }
                }
            }
            return total;
        }

        private long copyStream(Stream input, Stream output)
        {
            if(_transferType == "I")
            {
                return copyStream(input, output, 4096);
            }
            else
            {
                return copyStreamAscii(input, output, 4096);
            }
        }
    }
}