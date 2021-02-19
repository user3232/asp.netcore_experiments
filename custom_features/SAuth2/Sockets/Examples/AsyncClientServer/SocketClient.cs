using Base = System;  
using Net = System.Net;  
using Sock = System.Net.Sockets;  
using Text = System.Text;  
using T = System.Threading; 

// This is commented example from:
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/synchronous-client-socket-example
namespace SAuth2.Sockets.Examples.AsyncClientServer
{

  

  public class ClientSocketState                    // State object for receiving data 
  {                                                 // from remote device.
    public Sock.Socket workSocket = null;           // Client socket.
    public const int ReceiveBufferSize = 256;       // Size of receive buffer.
    public byte[] ReceiveBuffer =                   // Receive buffer.
      new byte[ReceiveBufferSize];  
    public Text.StringBuilder sb =                  // Received accumulated data as string.
      new Text.StringBuilder();  
  }  
  
  public static class AsynchronousClient 
  {  

    public static void StartSyncClient() 
    {  
      byte[] bytes = new byte[1024];                // Data buffer for incoming data. 
      try {                                         // Connect to a remote device.
        Net.IPHostEntry ipHostInfo = Net.Dns
          .GetHostEntry(Net.Dns.GetHostName());  
        Net.IPAddress ipAddress = 
          ipHostInfo.AddressList[0];                
        Net.IPEndPoint remoteEP =                   // Establish the remote endpoint 
          new Net.IPEndPoint(ipAddress,11000);      // for the socket. This example uses 
                                                    // port 11000 on the local computer.

        Sock.Socket sender = new Sock.Socket(       // Create a TCP/IP  socket.
          addressFamily: ipAddress.AddressFamily,
          socketType: Sock.SocketType.Stream, 
          protocolType: Sock.ProtocolType.Tcp 
        );  
          
        try 
        {  
          sender.Connect(remoteEP);                 // Connect the socket to the remote 
                                                    // endpoint. Catch any errors.
          Base.Console.WriteLine(
            "Socket connected to {0}",  
            sender.RemoteEndPoint.ToString()
          );  
            
          byte[] msg = Text.Encoding.ASCII          // Encode the data string into a byte array.
            .GetBytes("This is a test<EOF>");  
          int bytesSent = sender.Send(msg);         // Send the data through the socket.  

          int bytesRec = sender.Receive(bytes);     // Receive the response from the remote device.
          Base.Console.WriteLine(
            "Echoed test = {0}",  
            Text.Encoding.ASCII.GetString(
              bytes,0,bytesRec
            )
          );  
            
          sender.Shutdown(Sock.SocketShutdown.Both);// Release the socket.
          sender.Close();  
        } 
        catch (Base.ArgumentNullException ane) 
        {  
          Base.Console.WriteLine(
            "ArgumentNullException : {0}",
            ane.ToString()
          );  
        } 
        catch (Sock.SocketException se) 
        {  
          Base.Console.WriteLine(
            "SocketException : {0}",
            se.ToString()
          );  
        } 
        catch (Base.Exception e) 
        {  
          Base.Console.WriteLine(
            "Unexpected exception : {0}", 
            e.ToString()
          );  
        }  
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine( e.ToString());  
      }  
    }  
      





    private const int port = 11000;                 // The port number for the remote device.
  
    private static T.ManualResetEvent connectDone = // ManualResetEvent instances 
        new T.ManualResetEvent(false);              // signal completion.
    private static T.ManualResetEvent sendDone =
        new T.ManualResetEvent(false);  
    private static T.ManualResetEvent receiveDone =
        new T.ManualResetEvent(false);  
  
    private static string response = string.Empty;  // The response from the remote device.
  
    private static void StartAsyncClient() 
    {  
      try                                           // Connect to a remote device.
      {  
        Net.IPHostEntry ipHostInfo =                // Establish the remote endpoint 
          new Net.IPHostEntry()                     // for the socket. The name of the
          {                                         // remote device is "host.contoso.com".
            AddressList = new[]                     // Normally use:
            {                                       // Net.Dns.GetHostEntry("host.contoso.com");
              Net.IPAddress.Loopback
            }, 
            Aliases = new[]{"contoso.com"}, 
            HostName = "host.contoso.com"
          };
            
        Net.IPAddress ipAddress = 
          ipHostInfo.AddressList[0];  
        Net.IPEndPoint remoteEP = 
          new Net.IPEndPoint(ipAddress, port);  
        
        // Create a TCP/IP socket.  
        Sock.Socket client = new Sock.Socket(
          addressFamily: ipAddress.AddressFamily,  
          socketType: Sock.SocketType.Stream, 
          protocolType: Sock.ProtocolType.Tcp
        );  
        
        client.BeginConnect(                        // Connect to the remote endpoint.  
          remoteEP: remoteEP,
          callback: ConnectCallback, 
          state:    client
        );  
        connectDone.WaitOne();                      // Wait for signal from ConnectCallback
          
        Send(client,"This is a test<EOF>");         // Send test data to the remote device.
                                                    // Adding <EOF> is client-server contract.
        sendDone.WaitOne();                         // Wait for SendCallback signal.

        Receive(client);                            // Receive the response from the remote device.
        receiveDone.WaitOne();                      // Wait for ReceiveCallback signal.

        Base.Console.WriteLine(                     // Write the response to the console.
          $"Response received : {response}"
        );  
          
        client.Shutdown(Sock.SocketShutdown.Both);  // Disallow communication for socket.
        client.Close();                             // Free resources

      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  
    }  
  
    private static void ConnectCallback(
      Base.IAsyncResult ar
    ) 
    {  
      try 
      {  
        Sock.Socket client =                        // Retrieve the socket from the state object.
          (Sock.Socket) ar.AsyncState;  

        client.EndConnect(ar);                      // Complete the connection.  

        Base.Console.WriteLine(
          $"Socket connected to "  
          + client.RemoteEndPoint
        );  
        
        connectDone.Set();                          // Signal that the connection has been made.
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  
    }  
  
    private static void Receive(
      Sock.Socket client
    ) 
    {  
      try 
      {  
        ClientSocketState state =                   // Create the state object.  
          new ClientSocketState();  
        state.workSocket = client;  
          
        client.BeginReceive(                        // Begin receiving the data from 
          buffer:      state.ReceiveBuffer,         // the remote device.
          offset:      0, 
          size:        ClientSocketState.ReceiveBufferSize, 
          socketFlags: 0,  
          callback:    ReceiveCallback, 
          state:       state
        );  
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  
    }  
  
    private static void ReceiveCallback( 
      Base.IAsyncResult ar 
    ) 
    {  
      try 
      {  
        ClientSocketState state =                   // Retrieve the state object and the 
          (ClientSocketState) ar.AsyncState;        // client socket from the asynchronous 
        Sock.Socket client = state.workSocket;      // state object.

        int bytesRead = client.EndReceive(ar);      // Read data from the remote device.

        if (bytesRead > 0) 
        {  
          state.sb.Append(                          // There might be more data, 
            Text.Encoding.ASCII                     // so store the data received so far.
            .GetString(
              bytes: state.ReceiveBuffer,
              index: 0,
              count: bytesRead
            )
          );  
            
          client.BeginReceive(                      // Get the rest of the data.
            buffer:      state.ReceiveBuffer,
            offset:      0,
            size:        ClientSocketState.ReceiveBufferSize,
            socketFlags: 0,  
            callback:    ReceiveCallback, 
            state:       state
          );  
        } 
        else 
        {  
          if (state.sb.Length > 1)                  // All the data has arrived; 
          {                                         // put it in response.
            response = state.sb.ToString();  
          }  
          receiveDone.Set();                        // Signal that all bytes have been received.
        }  
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  
    }  
  
    private static void Send(
      Sock.Socket client, 
      string data
    ) 
    {  
        
      byte[] byteData = Text.Encoding.ASCII         // Convert the string data to byte 
        .GetBytes(data);                            // data using ASCII encoding.

        
      client.BeginSend(                             // Begin sending the data to the remote device.
        buffer:      byteData, 
        offset:      0, 
        size:        byteData.Length, 
        socketFlags: 0,  
        callback:    SendCallback, 
        state:       client
      );  
    }  
  
    private static void SendCallback(
      Base.IAsyncResult ar
    ) 
    {  
      try 
      {  
        Sock.Socket client =                        // Retrieve the socket from 
          (Sock.Socket) ar.AsyncState;              // the state object.
        int bytesSent = client.EndSend(ar);         // Complete sending the data 
                                                    // to the remote device.
        Base.Console.WriteLine(
          $"Sent {bytesSent} bytes to server."
        );  
          
        sendDone.Set();                             // Signal that all bytes have been sent.
      } 
      catch (Base.Exception e) 
      {  
        Base.Console.WriteLine(e.ToString());  
      }  
    }  
  
    public static int Main(string[] args) 
    {  
      StartAsyncClient();  
      return 0;  
    }  
}  

}