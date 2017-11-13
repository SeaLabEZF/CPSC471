using System;
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
        //create a connection for TCP events
        TcpClient client;

        //for indicating when we should quit
        public Boolean quit;

        //initializer for the FTP Server
        public FtpClient()
        { }

        //prepares to make a TCP request using a single IPAddress
        public void Start(IPAddress address, uint port_number)
        {
            //we shouldnt' quit until we feel like it
            quit = false;

            client = new TcpClient();
            client.BeginConnect(address, (int) port_number, HandleConnection, client);
        }

        //prepares to maek a TCP request using an IPAddress array
        public void Start(IPAddress [] addresses, uint port_number)
        {
            //we shouldnt' quit until we feel like it
            quit = false;

            client = new TcpClient();
            client.BeginConnect(addresses, (int)port_number, HandleConnection, client);
        }

        //called whenever a client connects to the server
        private async void HandleConnection(IAsyncResult result)
        {
            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
                using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                {

                }
            }
        }
    }
}
