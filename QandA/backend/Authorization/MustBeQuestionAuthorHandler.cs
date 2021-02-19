using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using QandA.Data;

namespace QandA.Authorization
{
  public class MustBeQuestionAuthorHandler :
  AuthorizationHandler<MustBeQuestionAuthorRequirement>
  {
    private readonly IDataRepository _dataRepository;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public MustBeQuestionAuthorHandler(
      IDataRepository dataRepository,
      IHttpContextAccessor httpContextAccessor
    )
    {
      _dataRepository = dataRepository;
      _httpContextAccessor = httpContextAccessor;
    }

    protected async override Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      MustBeQuestionAuthorRequirement requirement
    )
    {
      // check that the user is authenticated
      if (context.User.Identity.IsAuthenticated == false)
      {
        context.Fail();
        return;
      }

      // get the question id from the request
      var questionId = _httpContextAccessor
        .HttpContext
          .Request
            .RouteValues["questionId"];
      int questionIdAsInt = Convert.ToInt32(questionId);


      // get the user id from the name identifier claim:
      //   - A claim is information about a user from a trusted source. 
      //   - A claim represents what the subject is, 
      //     not what the subject can do. 
      //   - The ASP.NET Core authentication middleware automatically 
      //     puts userId in a name identifier claim for us.
      var userId = context
        .User                         // The User object is populated with the
                                      // claims by the authentication middleware
          .FindFirst(ClaimTypes.Name)
            .Value;

      // get the question from the data repository
      var question = await _dataRepository.GetQuestion(questionIdAsInt);
      
      // if the question can't be found go to the next piece of middleware
      if (question == null)
      {
        // let it through so the controller can return a 404
        context.Succeed(requirement);
        return;
      }

      // return failure if the user id in the question 
      // from the data repository is different to the 
      // user id in the request

      // return success if we manage to get here
    }
  }
}