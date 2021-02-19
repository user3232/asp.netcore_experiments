using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using QandA.Data;
using QandA.Data.Models;
using QandA.Hubs;

namespace QandA.Controllers
{
  // [controller] will be substituted with name of controller class
  // without Controller substring at the end
  // this will give in this case: api/questions
  [Route("api/[controller]")]
  [ApiController] // it is controller class indicator
  public class QuestionsController : ControllerBase
  {
    // this will be injected by DI:
    private readonly IDataRepository _dataRepository;

    private readonly IHubContext<QuestionsHub> _questionHubContext;

    private readonly IQuestionCache _cache;

    public QuestionsController (
      IDataRepository dataRepository,
      IHubContext<QuestionsHub> questionHubContext, 
      IQuestionCache questionCache
    )
    {
      _dataRepository = dataRepository;
      _questionHubContext = questionHubContext;
      _cache = questionCache;
    }

    public enum FetchAnswersOptimizations
    {
        none = 0,
        sql = 1
    }

    /* 
      HttpGet identifies method that will handle get request to this resource.
      It causes additional things:

      - Data from query parameters is automatically mapped
        to action method parameters that have the same name.
        localhost:5000/api/Qestions?search=type => search param value = type

      HttpGet parameter adds route to current route from context, giving here:
      url = api/questions/search=search_expression 

      Info:
      - https://docs.microsoft.com/en-us/aspnet/web-api/overview/formats-and-model-binding/parameter-binding-in-aspnet-web-api
    */
    // tests:
    // curl --cacert mkcerts/ca/root-ca-localhost.mk.crt --request GET --url https://localhost:5001/api/questions
    // https://localhost:5001/api/questions
    // https://localhost:5001/api/questions?search=&includeAnswers=
    // https://localhost:5001/api/questions?includeAnswers=true
    // https://localhost:5001/api/questions?search=&includeAnswers=yo
    // https://localhost:5001/api/questions?search=w&includeAnswers=true
    // https://localhost:5001/api/questions?search=w&includeAnswers=x&opts=none
    // https://localhost:5001/api/questions?search=w&includeAnswers=true&opts=x
    // https://localhost:5001/api/questions?search=w&includeAnswers=true&opts=
    [HttpGet]
    public IEnumerable<QuestionGetManyResponse> GetQuestions(
      string search, 
      bool includeAnswers, // not specified or specified empty -> false is bound
                           // specified bad sting e.g. 'no'    -> error 
                           //                                  -> http bad request 
                           //                                     retured by the server
      FetchAnswersOptimizations opts // not specified or similar -> 0 value is bound
                                     // specified 'badString' -> error -> bad http
    )
    {
      if (string.IsNullOrEmpty(search))
      {
        if(includeAnswers) {
          if(opts == FetchAnswersOptimizations.none) {
            return _dataRepository.GetQuestionsWithAnswers();
          }
          else if(opts == FetchAnswersOptimizations.sql) {
            return _dataRepository.GetQuestionsWithAnswersOpt();
          }
          else {
            return _dataRepository.GetQuestionsWithAnswers();
          }
        }
        else {
          /* 
            ASP.NET Core will automatically convert the questions object to
            JSON format and put this in the response body. It will also
            automatically return 200 as the HTTP status code.
          */
          return _dataRepository.GetQuestions();
        }
      }
      else
      {
        return _dataRepository.GetQuestionsBySearch(search);
      }
    }

    /* 
      HttpGet parameter adds route to current route from context, giving here:
      url = api/questions/unanswered 
    */
    [HttpGet("unanswered")]
    public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions()
    {
      return await _dataRepository.GetUnansweredQuestionsAsync();
    }


    /* 
      HttpGet parameter in currly brackets adds any route to current route context,
      and bind its value with method parameter, giving here for example:
      - url = api/questions/1 => questionId = 1
      - url = api/questions/yoyo => questionId = yoyo

      ActionResult<T> is container type for:
      - T
      - or ActionResult (not generic)

      ActionResult (not generic) have:
      - public virtual void ExecuteResult(ActionContext context)

      above method can be called to influence context (server response)

      req: HTTP GET (read)
      url: localhost:5000/api/question/some_identifier
     */
    [HttpGet("{questionId}")]
    public ActionResult<QuestionGetSingleResponse> GetQuestion(int questionId)
    {
      var question = _cache.Get(questionId);
      if(question==null)
      {
        // call the data repository to get the question
        question = _dataRepository.GetQuestion(questionId).Result;

        // return HTTP status code 404 if the question isn't found
        if (question == null)
        {
          return NotFound(); // ActionResult -> StatusCodeResult -> NotFoundResult
        }

        _cache.Set(question);
      }

      // return question in response with status code 200
      // ActionResult<T> have implicit conversion operator from T
      // this way following works:
      return question;
    }



    /* 
      HttpPost attribute indicates method as handler of http post requests

      questionPostRequest method argument will be binded with post parameters 
      from http request body

      req: HTTP POST (data in body, create)
      url: localhost:5000/api/question
     */
    [HttpPost]
    public ActionResult<QuestionGetSingleResponse> PostQuestion(
      QuestionPostRequest questionPostRequest
    )
    {
      // TODO - call the data repository to save the question
      var savedQuestion = _dataRepository.PostQuestion(
        new QuestionPostFullRequest
        {
          Title = questionPostRequest.Title,
          Content = questionPostRequest.Content,
          UserId = "1",
          UserName = "bob.test@test.com",
          Created = DateTime.UtcNow
        }
      );

      // TODO - return HTTP status code 201
      // response contains success status and 
      // route to posted question as server resource
      // and saved post values
      return CreatedAtAction(
        nameof(GetQuestion), // basic route
        new { questionId = savedQuestion.QuestionId }, // route extension
        savedQuestion // saved post values
      );
    }

    // req: HTTP PUT (update)
    // url: localhost:5000/api/question/blbalba2
    [HttpPut("{questionId}")]
    public ActionResult<QuestionGetSingleResponse> PutQuestion(
      int questionId,
      QuestionPutRequest questionPutRequest
    )
    {
      // get the question from the data repository
      var question = _dataRepository.GetQuestion(questionId).Result;
      if (question == null)
      {
        // return HTTP status code 404 if the question isn't found
        return NotFound();
      }
      // update the question model (if applicable)
      questionPutRequest.Title = string.IsNullOrEmpty(questionPutRequest.Title) ?
        question.Title : questionPutRequest.Title;
      questionPutRequest.Content = string.IsNullOrEmpty(questionPutRequest.Content) ?
        question.Content : questionPutRequest.Content;
      // call the data repository with the updated question model to update the question in the database
      var savedQuestion = _dataRepository.PutQuestion(questionId, questionPutRequest);

      _cache.Remove(savedQuestion.QuestionId);
      
      // return the saved question
      return savedQuestion;
    }

    /* 
      req: HTTP DELETE
      url: localhost:5000/api/question/blbalba2 
      
    */ 
    [HttpDelete("{questionId}")]
    public ActionResult DeleteQuestion(int questionId)
    {
      var question = _dataRepository.GetQuestion(questionId);
      if (question == null)
      {
        return NotFound();
      }
      _dataRepository.DeleteQuestion(questionId);
      _cache.Remove(questionId);
      return NoContent();
    }

    /* 
      This function servs http with:
        req: http post
        url: api/questions/answer
        body: AnswerPostRequest in body as json
     */
    [HttpPost("answer")]
    public ActionResult<AnswerGetResponse> PostAnswer(AnswerPostRequest answerPostRequest)
    {
      var questionExists = _dataRepository
        .QuestionExists(answerPostRequest.QuestionId.Value);

      if (!questionExists)
      {
        return NotFound();
      }

      var savedAnswer =_dataRepository.PostAnswer(
        new AnswerPostFullRequest
        {
          QuestionId = answerPostRequest.QuestionId.Value,
          Content = answerPostRequest.Content,
          UserId = "1",
          UserName = "bob.test@test.com",
          Created = DateTime.UtcNow
        }
      );  

      _cache.Remove(answerPostRequest.QuestionId.Value);

      _questionHubContext
        .Clients
          .Group($"Question-{answerPostRequest.QuestionId.Value}")
            .SendAsync(
              "ReceiveQuestion", // JavaScript client handler name
              _dataRepository
                .GetQuestion(answerPostRequest.QuestionId.Value)
            );


      return savedAnswer;
    }
  }
}