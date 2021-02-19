using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace QandA.Hubs
{
  public class QuestionsHub : Hub
  {
    /* 
      This function is called when client connects to hub
     */
    public override async Task OnConnectedAsync()
    {
      await base.OnConnectedAsync();

      /* 
        1st param ("Message") - JavaScript client handler name
        2nd param (object)    - argument passed to client handler method
       */
      await Clients.Caller.SendAsync("Message", "Successfully connected.");

    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
      await Clients.Caller.SendAsync(
        "Message",
        "Successfully disconnected"
      );
      await base.OnDisconnectedAsync(exception);
    }

    /* 
      Expose method available to clients
     */
    public async Task SubscribeQuestion(int questionId)
    {
      // add the client to a group of clients interested in
      // getting updates on the question
      await Groups.AddToGroupAsync(Context.ConnectionId, $"Question-{questionId}");

      // send a message to the client to indicate that the
      // subscription was successful
      await Clients.Caller.SendAsync(
        "Message",
        $"Successfully subsctibed to question with id = {questionId}"
      );

    }

    public async Task UnsubscribeQuestion(int questionId)
    {
      await Groups.RemoveFromGroupAsync(
        Context.ConnectionId,
        $"Question-{questionId}"
      );
      await Clients.Caller.SendAsync(
        "Message",
        "Successfully unsubscribed"
      );
    }
  }
}