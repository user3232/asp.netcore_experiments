using S = System.Net.Sockets;
using T = System.Threading.Tasks;
// using System.Threading.Tasks;


namespace SAuth2.Sockets.Examples
{

  public static class CheckConnection
  {

    public static void ReceiveTimoutExample()
    {
      // https://stackoverflow.com/questions/43267197/c-sharp-socket-wait-for-connection-during-x-seconds/43267239

      S.Socket sock = null;

      //socket connection and sending data
      sock.ReceiveTimeout = 5000;
      try {
        var buff = new byte[20];
        var dataReadLength = sock.Receive(buff);
      }
      catch (System.TimeoutException ex)
      {
        System.Console.WriteLine("Caller never answered...");  
        System.Console.WriteLine(ex); 
      }
    }


    // https://stackoverflow.com/questions/18486585/tcpclient-connectasync-get-status
    public static string TryConnectServerTimeoutedExample()
    {
      string serverAddress    = "127.0.0.1";
      int    serverPort       = 8888;
      int    connectTimeoutMs = 1000;


      S.TcpClient tcpClient = new S.TcpClient();
                                      // creates TCP client


      T.Task connectionTask = tcpClient
        .ConnectAsync(                // Connect asynchronousely 
          host: serverAddress,        // to specified server
          port: serverPort            // (dont wait for result)
        );


                                                
      T.Task<S.TcpClient> connectionStatusCheckTask = connectionTask
        .ContinueWith(                // This returns continuation task!!!
          continuationFunction: task => task.IsCompletedSuccessfully ? tcpClient : null, 
                                      // Continuation checks if antecendent task
                                      // (connectionTask) status is not faulted
                                      // and if it is not, returns TcpClient.
          continuationOptions: T.TaskContinuationOptions.ExecuteSynchronously
                                      // Continuation is run synchronously in the 
                                      // same thread as connectionTask after 
                                      // connectionTask finishes. It is not 
                                      // mandatory since running after connectionTask
                                      // is required so also sufficient would
                                      // be: T.TaskContinuationOptions.None
                                      // (default behaviour)
        );


      T.Task timeoutTask = T.Task.Delay(millisecondsDelay: connectTimeoutMs);
                                      // runs asynchronously doing nothing
                                      // function which finishes after
                                      // connectTimeoutMs miliseconds
      
      T.Task<S.TcpClient> timeoutStatusTask = timeoutTask
        .ContinueWith<S.TcpClient>(
          continuationFunction: task => null, 
          continuationOptions: T.TaskContinuationOptions.ExecuteSynchronously
                                      // T.TaskContinuationOptions.None is
                                      // also correct because we use WhenAny
                                      // and awaits.
                                      // Since continuationFunction's are
                                      // very short in this case
                                      // T.TaskContinuationOptions.ExecuteSynchronously
                                      // is also ok.
        );


      T.Task<S.TcpClient> resultTask = 
        T.TaskExtensions.Unwrap(        // Creates task proxying nested tasks
                                        // and flatens result type.

          task: T.Task.WhenAny(         // WhenAny creates task which completes,
                                        // when any of its tasks in parameters completes.
            connectionStatusCheckTask, 
            timeoutStatusTask
          )
        );


      resultTask
        .Wait();                        // Blocks until resultTask completes execution.
      S.TcpClient tcpClientOrNull = 
        resultTask.Result;              // Our resultTask result is TcpClient in case
                                        // of successful connection, or null otherwise.
      return tcpClientOrNull == null
        ? "Connection timed out or faulted or canceled."
        : "ConnectAsync completed successfully.";
    }
  }
}