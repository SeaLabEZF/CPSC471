using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerSide;
using ClientSide;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;

namespace SocketProgrammingAssignmentServer
{
    class TestEnvironment
    {
        //our main, which calls an async version of main
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }
        
        //async version of main
        //we want it this way so it stays open until an event
        //this is also much nicer on the processor (0% use woot!)
        public static async Task MainAsync(string[] args)
        {
            //if a first argument was provided
            if(args.Length > 0)
            {
                //try and read in as a unit (as the port number)
                try
                {
                    //try and parse the port number
                    //we need to have an uint to stick the port number in
                    // there is a 'uint' number of avaialable ports
                    //but not all may be available
                    uint port_number = UInt32.Parse(args[0]);

                    //if we can parse the unit, we assume it is a port number
                    //and the program will be running in FTP Server mode
                    FtpServer server = new FtpServer();
                    server.Start(port_number);

                    //let them know they can press any key to exit, then wait for that event
                    Console.Write("Press any key to exit.");
                    while(!Console.KeyAvailable)
                    {
                        await Task.Delay(100);
                    }

                    //close the FTP Server
                    //I need to figure out how to make this work, async is throwing things off
                    //so far closing the proram is stopping it automatically though
                    //so I don't need to call the following:
                    //server.Stop();

                    //write a new line to make things look nice
                    Console.Write("\n");
                }
                catch
                {
                    //if the 1st paramater isn't a valid uint
                    //we need to see if it's running in FTP Client mode

                    //needs to have 2 command line arguments
                    if (args.Length > 1)
                    {
                        try
                        {
                            //see if we can successfully get the IP Address
                            System.Net.IPAddress[] ips = System.Net.Dns.GetHostAddresses(args[0]);

                            //we need to have more than one IPs
                            if (ips.Length > 0)
                            {

                                try
                                {
                                    //try and parse the port number
                                    //we need to have an uint to stick the port number in
                                    // there is a 'uint' number of avaialable ports
                                    //but not all may be available
                                    uint port_number = UInt32.Parse(args[1]);

                                    //if we can parse the unit, we assume it is a port number
                                    //and the program will be running in FTP Client mode
                                    FtpClient client = new FtpClient();
                                    client.Start(ips[0].ToString(), port_number);

                                    //wait until we are required to quit to quit
                                    while (!client.quit)
                                    {
                                        await Task.Delay(100);
                                    }
                                }
                                catch
                                {
                                    Console.Write("Invalid Port Number provided.");
                                }
                            }
                            else
                            {
                                Console.Write("Can't do a DNS lookup on the provided Server Name.  Check your Internet Connection.");
                            }
                        }
                        catch
                        {
                            //not a correct name or IP address so let them know.
                            Console.Write("Invalid Server Name or IP Address.");
                        }
                    }
                    else
                    {
                        Console.Write("If running as server:  Invalid Port Number provided.\nIf running as client:  No port number provided.");
                    }
                }
            }
            else
            {
                //if the program is called without a paramater port number
                Console.Write("If running as server:  No port number provided.\nIf running as client:  No Name or IP address provided.");
            }
            
            //show that you are closing the app
            //just to look nice, not necessary
            Console.Write("Closing Application.");
            await Task.Delay(700);
        }
    }
}
