using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace AsyncTest
{
    interface IServerWorker
    {
        void Init(TcpClient client);
        void Work();
        void Stop();
    }

    abstract class AbstractWorker : IServerWorker //user
    {
        protected TcpClient Client { private set; get; }
        protected NetworkStream Stream { private set; get; }

        public void Init(TcpClient client)
        {
            Client = client;
            Stream = Client.GetStream();
        }

        public abstract void Work();

        public void Stop()
        {
            Stream.Close();
            Client.Close();
        }
    }

    class EchoWorker : AbstractWorker
    {
        public override void Work()
        {
            while (Stream.DataAvailable)
            {
                Thread.Sleep(10);
            }

            var requestBuffer = new byte[256];
            var stringBuilder = new StringBuilder();

            while (Stream.DataAvailable)
            {
                int bytes = Stream.Read(requestBuffer, 0, requestBuffer.Length);
                stringBuilder.Append(Encoding.UTF8.GetString(requestBuffer, 0, bytes));
            }

            Console.WriteLine(String.Format("Client has sent: {0}", stringBuilder.ToString()));

            stringBuilder.Insert(0, "Server got your message: ");
            var responseBuffer = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            Stream.Write(responseBuffer, 0, responseBuffer.Length);
        }
    }

    sealed class Server<ServerWorker> where ServerWorker : IServerWorker, new()
    {
        private TcpListener Listener { set; get; }
        private IPAddress Address { set; get; }
        private int Port { set; get; }
        private bool Running { set; get; }

        public Server(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            Running = false;

            try
            {
                Listener = new TcpListener(Address, Port);
            }

            catch (Exception e)
            {
                Console.WriteLine(String.Format("Error occured during server initialization: {0}", e.Message));
            }
        }

        private async void process(TcpClient client)
        {
            await Task.Run(() => {
                try
                {
                    var worker = new ServerWorker();

                    worker.Init(client);
                    worker.Work();
                    worker.Stop();
                }

                catch (Exception e)
                {
                    Console.WriteLine(String.Format("Error occured during processing client: {0}", e.Message));
                }
            });
        }

        public async void Start()
        {
            try
            {
                Listener.Start();

                Running = true;
            }

            catch (Exception e)
            {
                Console.WriteLine(String.Format("Error occured during server starting: {0}", e.Message));
            }

            while (Running)
            {
                try
                {
                    var client = await Listener.AcceptTcpClientAsync();

                    process(client);
                }

                catch (Exception e)
                {
                    Console.WriteLine(String.Format("Error occured during client connecting: {0}", e.Message));
                }
            }
        }

        public void Stop()
        {
            Running = false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server<EchoWorker>(IPAddress.Any, 8888);

            server.Start();

            Console.WriteLine("Started");

            Console.ReadLine();

            server.Stop();
        }
    }
}
