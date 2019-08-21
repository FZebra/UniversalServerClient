using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ClientHome
{
    interface IClient
    {
        void Connect(IPAddress address, int port);
        void Work();
        void Stop();
    }

    abstract class AbstractClient : IClient
    {
        private int _port = 8888;

        protected TcpClient Client { set; get; }
        protected NetworkStream Stream { private set; get; }
        protected IPAddress Address { set; get; }
        protected bool Connected { set; get; }
        protected int Port
        {
            set
            {
                if (value < 0 || value > 65535)
                {
                    throw new ArgumentOutOfRangeException("Port number must be in interval between 0 and 65535");
                }

                _port = value;
            }

            get
            {
                return (Connected) ? _port : -1;
            }
        }

        

        public void Connect(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            Connected = false;

            try
            {
                Client = new TcpClient();
                Client.Connect(Address, Port);
                Stream = Client.GetStream();
                Connected = true;
            }

            catch (Exception e)
            {
                Console.WriteLine(String.Format("Error occured during Connection {0}", e.Message));
            }
        }

        public abstract void Work();

        public void Stop()
        {
            try
            {
                Stream.Close();
                Client.Close();
                Connected = false;
            }

            catch (Exception e)
            {
                Console.WriteLine(String.Format("Error occured during Stop {0}", e.Message));
            }
        }

    }


    class EchoClient : AbstractClient
    {
        public async override void Work()
        {
            if (!Connected)
            {
                Console.WriteLine("No connections available");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 10; ++i)
                    {
                        if (Connected)
                        {
                            byte[] data = Encoding.Unicode.GetBytes("Hello");
                            Stream.Write(data, 0, data.Length);
                            Console.WriteLine("Done");
                            Thread.Sleep(300);
                        }
                        else
                        {
                            Console.WriteLine("Suddenly disconection during Work");
                            return;
                        }
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine(String.Format("Error occured during Work {0}", e.Message));
                }
            });
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var iP = IPAddress.Parse("127.0.0.1"); ;
            EchoClient client = new EchoClient();
            client.Connect(iP, 8888);
            client.Work();
            Console.ReadLine();
            client.Stop();
        }
    }
}
