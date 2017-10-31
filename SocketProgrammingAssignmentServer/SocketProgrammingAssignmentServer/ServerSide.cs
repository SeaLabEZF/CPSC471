using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace ServerSide
{
    public class FtpServer
    {
        private TcpListener listener;

        public FtpServer()
        { }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Any, 21);
            listener.Start();
            listener.BeginAcceptTcpClient(HandleAcceptTcpClient, listener);
        }

        public void Stop()
        {
            if(listener != null)
            {
                listener.Stop();
            }
        }

        private void HandleAcceptTcpClient(IAsyncResult result)
        {
            TcpClient client = listener.EndAcceptTcpClient(result);
            listener.BeginAcceptTcpClient(HandleAcceptTcpClient, listener);
            //we're connected now we worry about everything else

            NetworkStream stream = client.GetStream();

            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
            using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
            {
                writer.WriteLine("YOU ARE CONNECTED");
                writer.Flush();
                writer.WriteLine("I will repeat after you. Send a blank line to quit.");
                writer.Flush();

                string line = null;

                while(!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    writer.WriteLine("Echoing back: {0}", line);
                    writer.Flush();
                }
            }

        }
    }
}
