using Base = System;
using Net = System.Net;
using Sock = System.Net.Sockets;
using Secur = System.Net.Security;
using Auth = System.Security.Authentication;
using Text = System.Text;
using Cert = System.Security.Cryptography.X509Certificates;
using Io = System.IO;


namespace SAuth2.Sockets.Examples.Tls
{

  // https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=net-5.0
  public static class SslTcpServer
  {
    static Cert.X509Certificate2 serverCertificate = null;  // The certificate parameter specifies the
                                                            // name of the file containing the machine
                                                            // certificate.
    
    public static int Main(string[] args)
    {
      // var onlyCert = new Cert.X509Certificate();
      
      string certificate = null;
      if (args == null ||args.Length < 1 )
      {
          DisplayUsage();
      }
      certificate = args[0];
      SslTcpServer.RunServer(certificate);
      return 0;

      static void DisplayUsage()
      {
          Base.Console.WriteLine("To start the server specify:");
          Base.Console.WriteLine("serverSync certificateFile.cer");
          Base.Environment.Exit(1);
      }
    }
    
    public static void RunServer(string certificate)
    {
      serverCertificate = ImportCert(certificate);

      Sock.TcpListener listener = new Sock.TcpListener(     // Create a TCP/IP (IPv4) socket and 
        localaddr: Net.IPAddress.Any,                       // listen for any incoming connections
        port: 8080                                          // on port 8080
      );
      listener.Start();

      while (true)                                          // Accept and dispatch clients connections.
      {
        Base.Console.WriteLine(
          "Waiting for a client to connect..."
          + "\nType CTRL-C to terminate the server."
        );
        
        Sock.TcpClient client = listener.AcceptTcpClient(); // Application blocks while waiting 
                                                            // for an incoming connection.
                                                            // Type CTRL-C to terminate the server
                                                            // when running this function in 
                                                            // console application.
        ProcessClient(client);
      }
    }

    static void ProcessClient (Sock.TcpClient client)
    {
      Secur.SslStream sslStream = new Secur.SslStream(      // A client has connected. Create the
        innerStream: client.GetStream(),                    // SslStream using the client's
        leaveInnerStreamOpen: false,                        // network stream.
        userCertificateValidationCallback: null,
        userCertificateSelectionCallback: null,
        encryptionPolicy: Secur.EncryptionPolicy.RequireEncryption
      );
      
      try
      {

        sslStream.AuthenticateAsServer(                     // Authenticate the server but don't
          serverCertificate: serverCertificate,             // require the client to authenticate.
          clientCertificateRequired: false, 
          checkCertificateRevocation: true
        );
        
        DisplaySecurityLevel(sslStream);                    // Display the properties and settings 
        DisplaySecurityServices(sslStream);                 // for the authenticated stream.
        DisplayCertificateInformation(sslStream);
        DisplayStreamProperties(sslStream);

        sslStream.ReadTimeout = 5000;                       // Set timeouts for the read and write
        sslStream.WriteTimeout = 5000;                      // to 5 seconds.
        
        Base.Console.WriteLine(                             // Read a message from the client.
          "Waiting for client message..."               
        );
        string messageData = ReadMessage(sslStream);
        Base.Console.WriteLine(
          "Received: {0}", 
          messageData
        );
        
        byte[] message = Text.Encoding.UTF8.GetBytes(       // Write a message to the client.
          "Hello from the server.<EOF>"
        );
        Base.Console.WriteLine("Sending hello message.");
        sslStream.Write(message);
      }
      catch (Auth.AuthenticationException e)                // Catch exeptions and close sslStream
      {                                                     // and TcpClient, don't reuse!!!.
        Base.Console.WriteLine(                               
          "Exception: {0}",                                 // SslStream assumes that a timeout along 
          e.Message                                         // with any other IOException when one  
        );                                                  // is thrown from the inner stream will  
        if (e.InnerException != null)                       // be treated as fatal by its caller.   
        {                                                   // Reusing a SslStream instance after a  
          Base.Console.WriteLine(                           // timeout will return garbage. An  
            "Inner exception: {0}",                         // application should Close the SslStream 
            e.InnerException.Message                        // and throw an exception in these cases.    
          );                                                  
        }                                                       
        Base.Console.WriteLine (
          "Authentication failed - closing the connection."
        );
        sslStream.Close();
        client.Close();
        return;
      }
      finally
      {
        sslStream.Close();                                  // The client stream will be closed 
        client.Close();                                     // with the sslStream because we specified
                                                            // this behavior when creating the sslStream.
      }
    }

    static string ReadMessage(Secur.SslStream sslStream)
    {
      var sr = new Io.StreamReader(
          stream: sslStream,
          encoding: Text.Encoding.UTF8
        );
      return sr.ReadToEnd();
    }

    static string ReadMessage_Original(Secur.SslStream sslStream)
    {
      byte [] buffer = new byte[2048];                      // Read the  message sent by the client.
      Text.StringBuilder messageData =                      // The client signals the end of the 
        new Text.StringBuilder();                           // message using the "<EOF>" marker.
      int bytes = -1;
      do
      {
        bytes = sslStream.Read(                             // Read the client's test message.
          buffer: buffer, 
          offset: 0, 
          count:  buffer.Length
        );
        Text.Decoder decoder = Text.Encoding.UTF8           // Use Decoder class to convert from 
          .GetDecoder();                                    // bytes to UTF8 in case a character 
        char[] chars = new char[                            // spans two buffers. This is somewhat
          decoder.GetCharCount(                             // messy solution, better use StreamReader
            bytes: buffer,                                  // with Text.Encoding.UTF8
            index: 0,
            count: bytes
          )
        ];
        decoder.GetChars(
          bytes:     buffer, 
          byteIndex: 0, 
          byteCount: bytes, 
          chars:     chars,
          charIndex: 0
        );
        messageData.Append(chars);
        if (messageData.ToString().IndexOf("<EOF>") != -1)  // Check for EOF or an empty message.
        {
            break;
        }
      } while (bytes !=0);
      return messageData.ToString();
    }

    static void DisplaySecurityLevel(Secur.SslStream stream)
    {
      Base.Console.WriteLine(
        "Cipher: {0} strength {1}", 
        stream.CipherAlgorithm, 
        stream.CipherStrength
      );
      Base.Console.WriteLine(
        "Hash: {0} strength {1}", 
        stream.HashAlgorithm, 
        stream.HashStrength
      );
      Base.Console.WriteLine(
        "Key exchange: {0} strength {1}", 
        stream.KeyExchangeAlgorithm, 
        stream.KeyExchangeStrength
      );
      Base.Console.WriteLine(
        "Protocol: {0}", 
        stream.SslProtocol
      );
    }
    static void DisplaySecurityServices(Secur.SslStream stream)
    {
      Base.Console.WriteLine(
        "Is authenticated: {0} as server? {1}", 
        stream.IsAuthenticated, 
        stream.IsServer
      );
      Base.Console.WriteLine(
        "IsSigned: {0}", 
        stream.IsSigned
      );
      Base.Console.WriteLine(
        "Is Encrypted: {0}", 
        stream.IsEncrypted
      );
    }
    static void DisplayStreamProperties(Secur.SslStream stream)
    {
      Base.Console.WriteLine(
        "Can read: {0}, write {1}", 
        stream.CanRead, 
        stream.CanWrite
      );
      Base.Console.WriteLine(
        "Can timeout: {0}", 
        stream.CanTimeout
      );
    }



    static void DisplayCertificateInformation(Secur.SslStream stream)
    {
      Base.Console.WriteLine(
        "Certificate revocation list checked: {0}", 
        stream.CheckCertRevocationStatus
      );

      Cert.X509Certificate localCertificate = 
        stream.LocalCertificate;
      if (stream.LocalCertificate != null)
      {
        Base.Console.WriteLine(
          "Local cert was issued to {0}" 
          + " and is valid from {1} until {2}.",
          localCertificate.Subject,
          localCertificate.GetEffectiveDateString(),
          localCertificate.GetExpirationDateString()
        );
      } 
      else
      {
        Base.Console.WriteLine(
          "Local certificate is null."
        );
      }
      
      Cert.X509Certificate remoteCertificate =              // Display the properties of 
        stream.RemoteCertificate;                           // the client's certificate.
      if (stream.RemoteCertificate != null)
      {
        Base.Console.WriteLine(
          "Remote cert was issued to {0}"
          + " and is valid from {1} until {2}.",
          remoteCertificate.Subject,
          remoteCertificate.GetEffectiveDateString(),
          remoteCertificate.GetExpirationDateString()
        );
      } else
      {
        Base.Console.WriteLine(
          "Remote certificate is null."
        );
      }
    }


    public static Cert.X509Certificate2 ImportCert(
      string certificate
    )
    {
      var serverCertificate = new Cert.X509Certificate2(    // Initializes cert object from file in
        fileName: certificate,                              // DER fromat (.cer files)
        password: "",                                       // Usually certs containing private keys
                                                            // are password protected.
        keyStorageFlags: 
          Cert.X509KeyStorageFlags.UserKeySet               // where to import private key
      );

      // serverCertificate.Import(                             
      //   fileName: certificate,                              
      //   password: "",
      //   keyStorageFlags: 
      //     Cert.X509KeyStorageFlags.DefaultKeySet
      // );

      using Cert.X509Store store = new Cert.X509Store(      // Place to store certificates.
        storeName: "my_testing_store",                      // Store name may be arbitrary or one 
                                                            // of standard (Cert.StoreName enum).
        storeLocation: Cert.StoreLocation.CurrentUser,      // CurrentUser or LocalMachine
        flags: Cert.OpenFlags.MaxAllowed                    // Store operation policy.
      );


      var certInStore = store.Certificates.Find(            // Search store for certificate with
        findType: Cert.X509FindType.FindByThumbprint,       // identical thumbprint.
        findValue: serverCertificate.Thumbprint,
        validOnly: true
      ).Count > 0;
      if(certInStore == false)
      {
        store.Add(serverCertificate);                       // Add certificate, if identical exists
                                                            // it should be ok...
      }

                                                            // In earlier versions of .net (<5)
                                                            // strore didn't implement IDisposible
                                                            // and have to be closed using:
                                                            // store.Close();
      return serverCertificate;
    }


  }


}