#define SLEEPING
// #undef SLEEPING


using QandA.Data.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace QandA.Data
{
  public class MemDataRepository : IDataRepository
  {
    private MemoryDb memDb;
    public MemDataRepository(MemoryDb memoryDb)
    {
      memDb = memoryDb;
    }

    public void DeleteQuestion(int questionId)
    {
      memDb.Data.RemoveAll(q => q.QuestionId == questionId);
    }

    public AnswerGetResponse GetAnswer(int answerId)
    {
      foreach (var q in memDb.Data)
      {
          var answer = q.Answers.FirstOrDefault(a => a.AnswerId == answerId);
          if(answer != null) return answer;
      }
      return null;
    }

    public Task<QuestionGetSingleResponse> GetQuestion(int questionId)
    {
      QuestionGetSingleResponse result = memDb.Data.FirstOrDefault(q => q.QuestionId == questionId);
      return Task.FromResult(result);
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestions()
    {
      #if SLEEPING
      Thread.Sleep(50);
      #endif
      return memDb.Data.Select(MemoryDb.Question.ToQuestionGetManyResponse);
    }

  



    public IEnumerable<QuestionGetManyResponse> GetQuestionsBySearch(string search)
    {
      return memDb.Data
        .Where(q => q.Title.Contains(search) || q.Content.Contains(search))
        .Select(MemoryDb.Question.ToQuestionGetManyResponse);
    }

    private IEnumerable<AnswerGetResponse> GetAnswers(int questionId)
    {
      #if SLEEPING
      Thread.Sleep(50);
      #endif

      var allAnswers = memDb.Data
        .SelectMany(q => q.Answers.Select(a => new {id = q.QuestionId, ans = a}) )
        .ToList();

      var answersWithQuestionId = allAnswers.Where(ans_and_qid => ans_and_qid.id == questionId).ToList();
      var castedAnswers = answersWithQuestionId.Select(ans_and_qid => (AnswerGetResponse) ans_and_qid.ans).ToList();

      // this function simulates table join (as .SelectMany)
      // and filtering (.Where)
      // it is inefficient on purpose
      return castedAnswers;
        
        
        // .Cast<AnswerGetResponse>();
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestionsWithAnswers()
    {
      #if SLEEPING
      Thread.Sleep(50);
      #endif
      // simulates inefficient data quering
      var questions = GetQuestions().ToList();
      foreach (var question in questions)
        {
          var answs = GetAnswers(question.QuestionId).ToList();
          question.Answers = answs;
        }
      return questions;
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestionsWithAnswersOpt()
    {
      #if SLEEPING
      Thread.Sleep(50);
      #endif
      // simulates efficient data quering
      var q_and_as = memDb.Data.Select(q => new QuestionGetManyResponse() {
          Content = q.Content,
          Created = q.Created,
          QuestionId = q.QuestionId,
          Title = q.Title,
          UserName = q.UserName,
          Answers = q.Answers.Select(MemoryDb.Answer.ToAnswerGetResponse).ToList()
        }
      ).ToList();
      return q_and_as;
    }

    public IEnumerable<QuestionGetManyResponse> GetUnansweredQuestions()
    {
      return memDb.Data
        .Where(q => q.Answers.Any() == false)
        .Select(MemoryDb.Question.ToQuestionGetManyResponse);
    }

    public AnswerGetResponse PostAnswer(AnswerPostFullRequest answer)
    {
      var question = memDb.Data.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
      var answerResponse = new MemoryDb.Answer(){
        AnswerId = question.Answers.Count == 0 ? 0 : 1 + question.Answers.Max(a => a.AnswerId),
        Content = answer.Content,
        UserName = answer.UserName,
        UserId = answer.UserId,
        Created = answer.Created,
      };
      question.Answers.Add(answerResponse);
      return answerResponse;
    }

    public QuestionGetSingleResponse PostQuestion(QuestionPostFullRequest question)
    {
      var newQuestion = new MemoryDb.Question() {
        QuestionId = 1 + memDb.Data.Max(q => q.QuestionId),
        Title = question.Title,
        Content = question.Content,
        UserId = question.UserId,
        UserName = question.UserName,
        Created = question.Created,
        Answers = new List<MemoryDb.Answer>()
      };
      memDb.Data.Add(newQuestion);
      return newQuestion;
    }

    public QuestionGetSingleResponse PutQuestion(int questionId, QuestionPutRequest question)
    {
      var foundQuestion = memDb.Data.FirstOrDefault(q => q.QuestionId == questionId);
      foundQuestion.Content = question.Content;
      foundQuestion.Title = question.Title;
      
      return foundQuestion;
    }

    public bool QuestionExists(int questionId)
    {
      return memDb.Data.Exists(q => q.QuestionId == questionId);
    }

    public Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsAsync()
    {
      // to use await metod must be signet await
      // await Task.Delay(0);
      // return GetUnansweredQuestions();
      return Task.FromResult(GetUnansweredQuestions());
    }
  }
}