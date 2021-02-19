using Base = System;
using Hash = System.Collections;
using Sock = System.Net.Sockets;
using Secur = System.Net.Security;
using Auth = System.Security.Authentication;
using Text = System.Text;
using Cert = System.Security.Cryptography.X509Certificates;


namespace SAuth2.Sockets.Examples.Tls
{
  // https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=net-5.0
  public static class SslTcpClient
  {
    private static Hash.Hashtable certificateErrors = 
      new Hash.Hashtable();

    
    public static bool ValidateServerCertCallback(           // The following method is invoked 
      object sender,                                        // by the
      Cert.X509Certificate certificate,                     // RemoteCertificateValidationDelegate .
      Cert.X509Chain chain,
      Secur.SslPolicyErrors sslPolicyErrors
    )
    {
      if (sslPolicyErrors == Secur.SslPolicyErrors.None)    // No cert errors -> certificate name
        return true;                                        // matches server name when calling
                                                            // sslStream.AuthenticateAsClient(serverName);

      Base.Console.WriteLine(
        "Certificate error: {0}", 
        sslPolicyErrors
      );
      return false;                                         // Do not allow this client to communicate 
                                                            // with unauthenticated servers.
    }

    public static void RunClient(
      string machineName, 
      string serverName
    )
    {
        Sock.TcpClient client = new Sock.TcpClient(         // Create a TCP/IP client socket.
          hostname: machineName,                            // machineName is the host running 
          port: 443                                         // the server application.
        );
        Base.Console.WriteLine("Client connected.");
        
        Sock.NetworkStream underlyingStream = 
          client.GetStream();
        Secur.SslStream sslStream = new Secur.SslStream(    // Create an SSL stream that will 
          innerStream: underlyingStream,                    // close the client's stream.
          leaveInnerStreamOpen: false,
          userCertificateValidationCallback: 
            ValidateServerCertCallback,
          userCertificateSelectionCallback: null
        );
        
        try
        {
                                                            // After creating an SslStream,
                                                            // the server and optionally, 
                                                            // the client must be authenticated.
                                                            // By default The server name must match
                                                            // the name on the server certificate.
                                                            // Both client and server must
                                                            // initiate the authentication.
          sslStream.AuthenticateAsClient(
            targetHost: serverName,
            clientCertificates: null,                       // The client's certificates must be 
                                                            // located in the current user's "My" 
                                                            // certificate store.
            enabledSslProtocols: Auth.SslProtocols.None,    // SslProtocols.None: 
                                                            // Allows the operating system to choose 
                                                            // the best protocol to use, and to block
                                                            // protocols that are not secure. 
            checkCertificateRevocation: true
          );

        }                                                    
        catch (Auth.AuthenticationException e)              // SslStream assumes that a timeout along
        {                                                   // with any other IOException when one 
          Base.Console.WriteLine(                           // is thrown from the inner stream will 
            "Exception: {0}", e.Message                     // be treated as fatal by its caller. 
          );                                                // Reusing a SslStream instance after a 
          if (e.InnerException != null)                     // timeout will return garbage. An 
          {                                                 // application should Close the SslStream 
            Base.Console.WriteLine(                         // and throw an exception in these cases.
              "Inner exception: {0}", 
              e.InnerException.Message
            );
          }
          Base.Console.WriteLine (
            "Authentication failed, closing the connection."
          );
          client.Close();
          return;
        }
        
        
        byte[] messsage = Text.Encoding.UTF8                // Encode a test message into a byte
          .GetBytes("Hello from the client.<EOF>");         // array. Signal the end of the message  
                                                            // using the "<EOF>".
        
        sslStream.Write(messsage);                          // Send hello message to the server.
        sslStream.Flush();                                  // Causes eventual internal bufferring
                                                            // to be invalidated.
        
        string serverMessage = ReadMessage(sslStream);      // Read message from the server.
        Base.Console.WriteLine(
          "Server says: {0}", serverMessage
        );
        
        client.Close();                                     // Close the client connection.
        Base.Console.WriteLine("Client closed.");
    }

    static string ReadMessage(Secur.SslStream sslStream)
    {
      byte [] buffer = new byte[2048];                      // Read the  message sent by the server.
      Text.StringBuilder messageData =                      // The end of the message is signaled 
        new Text.StringBuilder();                           // using the "<EOF>" marker.
      int bytes = -1;                                       
      do
      {
        bytes = sslStream.Read(buffer, 0, buffer.Length);
        
        Text.Decoder decoder = Text.Encoding.UTF8           // Use Decoder class to convert 
          .GetDecoder();                                    // from bytes to UTF8 in case a 
        char[] chars = new char[                            // character spans two buffers.
          decoder.GetCharCount(buffer,0,bytes)
        ];
        decoder.GetChars(buffer, 0, bytes, chars,0);
        messageData.Append (chars);
        if (messageData.ToString().IndexOf("<EOF>") != -1)  // Check for EOF.
        {
          break;
        }
      } while (bytes != 0);

      return messageData.ToString();
    }

    public static int Main(string[] args)
    {
      string serverCertificateName = null;                  // User can specify the machine name
      string machineName = null;                            // and server name.
      if (args == null ||args.Length <1 )                   // Server name must match the name
      {                                                     // on the server's certificate.
        DisplayUsage();
      }
      
      machineName = args[0];
      if (args.Length <2 )
      {
          serverCertificateName = machineName;
      }
      else
      {
          serverCertificateName = args[1];
      }
      SslTcpClient.RunClient(
        machineName: machineName, 
        serverName: serverCertificateName
      );
      return 0;
      
      static void DisplayUsage()
      {
        Base.Console.WriteLine(
          "To start the client specify:"
        );
        Base.Console.WriteLine(
          "clientSync machineName [serverName]"
        );
        Base.Environment.Exit(1);
      }
    }

  }


}