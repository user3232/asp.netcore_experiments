using Base = System;  
using Net = System.Net;  
using Sock = System.Net.Sockets;  
using Text = System.Text;  
using T = System.Threading; 

// This is commented example from:
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/synchronous-server-socket-example
namespace SAuth2.Sockets.Examples.AsyncClientServer
{
  public static class SynchronousServer
  {
    public static string data = null;               // Incoming data from the client.
  
    public static void StartSyncListening() 
    {  
      byte[] bytes = new byte[1024];                // Data buffer for incoming data.  
      Net.IPHostEntry ipHostInfo =                  // Establish the local endpoint for the socket.  
        Net.Dns.GetHostEntry(Net.Dns.GetHostName());// Dns.GetHostName returns the name of the  
      Net.IPAddress ipAddress =                     // host running the application.  
        ipHostInfo.AddressList[0];  
      Net.IPEndPoint localEndPoint = 
        new Net.IPEndPoint(ipAddress, 11000);  

      Sock.Socket listener = new Sock.Socket(       // Create a TCP/IP socket.
        addressFamily: ipAddress.AddressFamily,  
        socketType: Sock.SocketType.Stream, 
        protocolType: Sock.ProtocolType.Tcp 
      );  

      try 
      {  
        listener.Bind(localEndPoint);               // Bind the socket to the local endpoint and
        listener.Listen(backlog: 10);               // listen for incoming connections.

        while (true)                                // Start listening for connections.  
        {  
          Base.Console.WriteLine("Waiting for a connection...");  
            
          Sock.Socket handler = listener.Accept();  // Program is suspended while waiting 
          data = null;                              // for an incoming connection.

          while (true)                              // An incoming connection needs to be processed.
          {  
            int bytesRec = handler.Receive(bytes);  
            data += Text.Encoding.ASCII
              .GetString(bytes,0,bytesRec);  
            if (data.IndexOf("<EOF>") > -1) 
            {  
              break;  
            }  
          }  
          
          Base.Console.WriteLine(                   // Show the data on the console.  
            "Text received : {0}", data
          );  
           
          byte[] msg = Text.Encoding.ASCII        
            .GetBytes(data);  
          handler.Send(msg);                        // Echo the data back to the client. 
          
          handler.Shutdown(Sock.SocketShutdown.Both);  
          handler.Close();  
        }  
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  

      Base.Console.WriteLine("\nPress ENTER to continue...");  
      Base.Console.Read();  
    }  
  }




  public class SocketState                          // Data structure used to pass context 
  {                                                 // (state) information between different 
    public const int ReceiveBufferSize = 1024;      // functional calls.     
    public byte[] ReceiveBuffer                     // * ReceiveBufferSize: Size of receive buffer.     
      = new byte[ReceiveBufferSize];                // * ReceiveBuffer: Receive buffer.     
    public Text.StringBuilder ReceivedText          // * ReceivedText: Received accumulated data      
      = new Text.StringBuilder();                   //     as string.       
    public Sock.Socket SocketSocket                 // * SocketSocket: Client socket.    
      = null;                                       
  }                                               
  

  public static class AsynchronousSocketListener
  {
     
    public static T.ManualResetEvent allDone =      // Multi-thread save binary signalling.    
      new T.ManualResetEvent(initialState: false);  // Initial state set to reset.  

    public static void ServerStartListening()
    {
      Net.IPHostEntry ipHostInfo =                  // GetHostEntry: gets information about host  
        Net.Dns.GetHostEntry(                       // name or address.       
          hostNameOrAddress:                        // GetHostName: gets host name of computer   
            Net.Dns.GetHostName()                   // running this function.                          
        );                                           
                                  

      Net.IPAddress ipAddress =                     // DNS informations contains IP address or dns 
        ipHostInfo.AddressList[0];                  // or localhost (127.0.0.1) 

      Net.IPEndPoint localEndPoint =                // Defines description of IP address and port 
        new Net.IPEndPoint(                         // of this server.
          address: ipAddress, 
          port:    11000
        );  

      Sock.Socket listenerSocket = new Sock.Socket( // Create a TCP/IP socket.    
        addressFamily: ipAddress.AddressFamily,     // addressFamily: is typically IPv4 or IPv6,      
        socketType:    Sock.SocketType.Stream,      // but see Sock.AddressFamily enum.      
        protocolType:  Sock.ProtocolType.Tcp        // socketType: is typically Tcp over Ip,     
      );                                            // datagrams over IP, etc.    
                                                    // see Sock.SocketType enum.      
                                                    // protocolType: see Sock.ProtocolType.      

      try {  

        listenerSocket.Bind(                        // Socket.Bind: Binds the socket to 
          localEP: localEndPoint                    // the local endpoint
        );  

        Base.Console.WriteLine(
          "listenerSocket.LocalEndPoint: "
          + listenerSocket.LocalEndPoint.ToString()
        );  
        Base.Console.WriteLine(
          "listenerSocket.RemoteEndPoint: "
          + listenerSocket.RemoteEndPoint.ToString()
        );

        listenerSocket.Listen(                      // Socket.Listen: Listen for incoming  
          backlog: 100                              // connections.   
        );                                          // backlog: The maximum length of the pending  
                                                    // connections queue. 

        while (true) 
        {  
          allDone.Reset();                          // Set the event to nonsignaled state.  

            
          Base.Console.WriteLine(
            "Waiting for a connection..."
          );  
          listenerSocket.BeginAccept(               // Socket.BeginAccept: Start an asynchronous    
            callback: AcceptCallback,               // socket to listen for connections.    
            state:    listenerSocket                // callback: Function to be called for       
          );                                        // incoming connection attempt.  
                                                    // state: An object that contains state 
                                                    // information for this function         
                                                    // invocation.            

            
          allDone.WaitOne();                        // Wait until a connection is made 
                                                    // before continuing. This will happen
                                                    // if AcceptCallback signal addDone.
        } // end while
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  

      Base.Console.WriteLine(
        "\nPress ENTER to continue..."
      );  
      Base.Console.Read();                          // Waits for key and reads the next 
                                                    // character from the standard input 
                                                    // stream.
    }

    public static void AcceptCallback(              // Base.IAsyncResult has:    
      Base.IAsyncResult ar                          //   ar.AsyncState     
    )                                               //   ar.AsyncWaitHandle     
    {                                               //   ar.CompletedSynchronously     
                                                    //   ar.IsCompleted      

      allDone.Set();                                // Signal the ServerStartListening.while 
                                                    // loop to continue.  

        
      Sock.Socket listenerSocket =                  // Get the socket that listened to
        (Sock.Socket) ar.AsyncState;                // client request from the passed 
                                                    // state variable in ar.
      
      Sock.Socket socketSocket = 
        listenerSocket.EndAccept(ar);  

      Base.Console.WriteLine(
        "socketSocket.LocalEndPoint: "
        + socketSocket.LocalEndPoint.ToString()
      );  
      Base.Console.WriteLine(
        "socketSocket.RemoteEndPoint: "
        + socketSocket.RemoteEndPoint.ToString()
      );  

        
      SocketState state = new SocketState();        // Create the state to pass.
      state.SocketSocket = socketSocket;            // Add connected client sub-socket 
                                                    // to state.  
      socketSocket.BeginReceive( 
        buffer:      state.ReceiveBuffer, 
        offset:      0, 
        size:        SocketState.ReceiveBufferSize, 
        socketFlags: 0,  
        callback:    ReadCallback, 
        state:       state
      );  
    }


    public static void ReadCallback(
      Base.IAsyncResult ar
    )
    {
      Base.String content = Base.String.Empty;      // Initialize variable to store     
                                                    // content received from client.      
      SocketState state =                           // Retrieve the state object 
        (SocketState) ar.AsyncState;  
      Sock.Socket socketSocket =                    // Retrive the socketSocket Socket  
        state.SocketSocket;  
                                        

      int bytesRead = socketSocket.EndReceive(ar);  // Read data from the client socket.
                                                    // This waits until data will be 
                                                    // available and will fit receive 
                                                    // buffer.

      if (bytesRead > 0) {                          // There  might be more data, 
        state.ReceivedText.Append(                  // so store the data received so far. 
          Text.Encoding.ASCII.GetString(
            bytes: state.ReceiveBuffer, 
            index: 0, 
            count: bytesRead
          )
        );  

        content = state.ReceivedText.ToString();    // Serialize up to now received content. 

        if (content.IndexOf("<EOF>") > -1)          // Check for end-of-file tag. 
        {  
          Base.Console.WriteLine(                   // All the data has been read from the
            $"Read {content.Length}"                // client. Display it on the console.   
            + " bytes from socket."
          );  
          Base.Console.WriteLine( 
            $"Data : {content}"    
          );  
          
          Send(                                     // Echo the data back to the client.  
            handler: socketSocket, 
            data: $"Server received content: "
              + content
          );  

        } 
        else                                        // If no EOF, read more data.
        {  
          socketSocket.BeginReceive(
            buffer:       state.ReceiveBuffer, 
            offset:       0, 
            size:         SocketState.ReceiveBufferSize, 
            socketFlags:  0,  
            callback:     ReadCallback, 
            state:        state
          );  
        }  
      }  
    }

    private static void Send(
      Sock.Socket handler, 
      Base.String data
    )
    {
      byte[] byteData = Text.Encoding.ASCII         // Convert the string data to
        .GetBytes(data);                            // byte data using ASCII encoding.

       
      handler.BeginSend(                            // Begin sending the data to the remote device. 
        buffer:      byteData, 
        offset:      0, 
        size:        byteData.Length, 
        socketFlags: 0,  
        callback:    SendCallback, 
        state:       handler
      );  
    }


    private static void SendCallback(
      Base.IAsyncResult ar
    )
    {
      try
      {
         
        Sock.Socket handler =                       // Retrieve the socket from the state object. 
          (Sock.Socket) ar.AsyncState;  

        int bytesSent = handler.EndSend(ar);        // Complete sending the data to the remote
                                                    // device.
        Base.Console.WriteLine(
          $"Sent {bytesSent} bytes to client."
        );  

        handler.Shutdown(Sock.SocketShutdown.Both); // Disables sends and receives on a Socket  
        handler.Close();                            // Closes the Socket connection and
                                                    // releases all associated resources.

      }
      catch (Base.Exception e)
      {
        Base.Console.WriteLine(e.ToString());  
      }  
    }



    public static int Main(Base.String[] args)
    {
      ServerStartListening();  
      return 0;  
    }
  }
}