using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerSide;
using System.Threading;
using System.Threading.Tasks;

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
            //if a port number was provided
            if(args.Length > 0)
            {
                //try and read in the port number
                try
                {
                    //try and parse the port number
                    //we need to have an uint to stick the port number in
                    // there is a 'uint' number of avaialable ports
                    //but not all may be available
                    uint port_number = UInt32.Parse(args[0]);

                    //if we can parse the port number
                    //create and start the server
                    FtpServer server = new FtpServer();
                    server.Start(21);

                    //let them know they can press any key to exit, then wait for that event
                    Console.Write("Press any key to exit.");
                    while(!Console.KeyAvailable)
                    {
                        await Task.Delay(100);
                    }

                    //close the server
                    //I need to figure out how to make this work, async is throwing things off
                    //so far closing the proram is stopping it automatically though
                    //server.Stop();

                    //write a new line to make things look nice
                    Console.Write("\n");
                }
                catch
                {
                    //if the 1st paramater isn't a valid uint
                    Console.Write("Invalid port number provided.");
                }
            }
            else
            {
                //if the program is called without a paramater port number
                Console.Write("No port number provided.");
            }
            
            //show that you are closing the app
            //just to look nice, not necessary
            Console.Write("Closing Application.");
            await Task.Delay(700);
        }
    }
}
