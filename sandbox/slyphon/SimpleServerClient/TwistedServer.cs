using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace CSReactor {
  /// <summary>
  /// Summary description for SimpleClientServer.
  /// </summary>
  public interface IConnectionLost {
    String reason {get; set;}
    Exception failure {get; set;}
  }

  public interface ITransport {
    void write(String data);
    void writeSequence(ICollection data);
    void loseConnection();
    IPEndPoint getHost();
  }

  public interface IProtocol {
    ITransport Transport { get; set; }
    IFactory Factory { get; set; }
    bool Connected { get; }
    void dataReceived(String data);
    void connectionLost(IConnectionLost reason);
    void makeConnection(ITransport transport);
    void connectionMade();
  }
	
  public interface IFactory {
    IProtocol buildProtocol();
    void doStart();
    void doStop();
  }

  public class BaseProtocol {
    protected bool connected = false;
    protected ITransport transport;
    protected IFactory factory;
    protected IPEndPoint address = null;
		
    public BaseProtocol() : this(null, null) {}

    public BaseProtocol(IPEndPoint address) : this(address, null) {
    }

    public BaseProtocol(IPEndPoint address, IFactory factory) {
      this.address = address;
      this.factory = factory;
    }

    public bool Connected {
      get { return this.connected; }
    }
		
    public ITransport Transport {
      get { return (ITransport)this.transport; }
      set { this.transport = (ITransport)value; }
    }

    public IFactory Factory {
      get { return this.factory; }
      set { this.factory = value; }
    }

    public virtual void makeConnection(ITransport transport) {
      this.connected = true;
      this.transport = transport;
      this.connectionMade();
    }
    public virtual void connectionMade() {}
  }

  public class Protocol : BaseProtocol, IProtocol {
    public virtual void connectionLost(IConnectionLost reason) {}
    public virtual void dataReceived(String data) {}
  }

  public class Factory : IFactory {
    /* System.Type protocolClass = typeof(Foo)
     */
		
    public virtual void doStart() {}
    public virtual void doStop() {}
    public virtual IProtocol buildProtocol() {
      return this.buildProtocol(null, null);
    }
    public virtual IProtocol buildProtocol(IPEndPoint addr) {
      return this.buildProtocol(addr, null);
    }
    public virtual IProtocol buildProtocol(IPEndPoint addr, IFactory factory) {
      // YOU MUST OVERRIDE THIS IN SUBCLASSES!
      return null;
    }
  }
	
  public class Transport : ITransport {
    public virtual void write(String data) {}
    public virtual void writeSequence(ICollection data) {}
    public virtual void loseConnection() {}
    public virtual IPEndPoint getHost() { return null; }
  }

  public class TwistedServer {

    public static IPAddress localhost = IPAddress.Parse("127.0.0.1");
    public static int port = 9999;

    public void Server() {
      try {
        TcpListener myList = new TcpListener(localhost, port);
        myList.Start();

        Console.WriteLine("server listening on " + port);
        Console.WriteLine("local end point is: " + myList.LocalEndpoint);
        Console.WriteLine("waiting for a connection....");

        Socket s = myList.AcceptSocket();
        Console.WriteLine("connection accepted from " + s.RemoteEndPoint);
        byte[] b = new byte[1024];
        int k = s.Receive(b);
        Console.WriteLine("received...");
				
        for (int i=0;i<k;i++)
          Console.Write(Convert.ToChar(b[i]));
        ASCIIEncoding asen = new ASCIIEncoding();
        s.Send(asen.GetBytes("the string was received by the server."));
        Console.WriteLine("\nSent Ack");
        s.Close();
        myList.Stop();
      } catch (Exception e) {
        Console.WriteLine("error\n:" + e.StackTrace);
      }
    }
		

    public void OnConnect(IAsyncResult ar) {
      Console.WriteLine("got connection");
      Socket listener = (Socket)ar.AsyncState;
      Socket theclient = listener.EndAccept(ar);
      ASCIIEncoding asen = new ASCIIEncoding();
      theclient.Send(asen.GetBytes("this is the message"));
      listener.BeginAccept(new AsyncCallback(OnConnect), listener);
    }

    public void AsyncServer() {
      try {
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(localhost, port));
        listener.Listen(10);
        listener.BeginAccept(new AsyncCallback(OnConnect), listener);
        Console.WriteLine("set up async callback");
      } catch (Exception e) {
        Console.WriteLine("error\n:" + e.StackTrace);
      }				
    }

    public static void Main() {
      TwistedServer ss = new TwistedServer();
      ss.AsyncServer();

      Console.WriteLine("Press a key...");
      Console.Read();
    }
  }
}
