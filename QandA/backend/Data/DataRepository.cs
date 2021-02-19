using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using QandA.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using static Dapper.SqlMapper;
using System.Threading.Tasks;

namespace QandA.Data
{
  public class DataRepository : IDataRepository
  {

    // readonly keyword prevents the variable from being changed
    // outside of the class constructor
    private readonly string _connectionString;

    public DataRepository(IConfiguration configuration)
    {
      _connectionString = configuration["ConnectionStrings:DefaultConnection"];

      // var otherWay1 = configuration.GetSection("ConnectionStrings")["DefaultConnection"];
      // var otherWay2 = configuration.GetConnectionString("DefaultConnection");
      // var otherWay3 = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
    }

    public void DeleteQuestion(int questionId)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        connection.Execute(
          @"EXEC dbo.Question_Delete
          @QuestionId = @QuestionId",
          new { QuestionId = questionId }
        );
      }
    }

    public AnswerGetResponse GetAnswer(int answerId)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.QueryFirstOrDefault<AnswerGetResponse>(
          @"EXEC dbo.Answer_Get_ByAnswerId @AnswerId = @AnswerId",
          new { AnswerId = answerId }
        );
      }
    }

    public async Task<QuestionGetSingleResponse> GetQuestion(int questionId)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        await connection.OpenAsync();
        var question = await connection.QueryFirstOrDefaultAsync<QuestionGetSingleResponse>(
          @"EXEC dbo.Question_GetSingle @QuestionId = @QuestionId",
          new { QuestionId = questionId }
        );
        // TODO - Get the answers for the question
        if (question != null)
        {
          question.Answers = await connection.QueryAsync<AnswerGetResponse>(
            @"EXEC dbo.Answer_Get_ByQuestionId
            @QuestionId = @QuestionId",
            new { QuestionId = questionId }
          );
        }
        return question;
      }
    }

    /* 
      (https://en.wikipedia.org/wiki/Result_set :)
      An SQL result set is:

        - a set of rows from a database, 
        - as well as metadata about the query, such as:
          - the column names, 
          - and the types and sizes of each column. 
          - Depending on the database system, the number of rows in
            the result set may or may not be known. 
            - Usually, this number is not known up front because the
              result set is built on-the-fly.

      A result set is effectively:
        - a table. 
        - The ORDER BY clause can be used in a query to impose a
          certain sort condition on the rows.

      Additionally connection can return multiple result sets!!!
     */
    public QuestionGetSingleResponse GetQuestionOpt(int questionId)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        using (
          GridReader results =
            connection.QueryMultiple(
              @"
                EXEC dbo.Question_GetSingle @QuestionId = @QuestionId;
                EXEC dbo.Answer_Get_ByQuestionId @QuestionId = @QuestionId
              ",
              new { QuestionId = questionId }
            )
        )
        {
          // EXEC ...
          // EXEC ...
          // GO
          // List<HeteroDataSet>
          // List<MapHeteroToHeteroClass>
          // Connect hetero classes
          var question = results
            .Read<QuestionGetSingleResponse>().FirstOrDefault();

          if (question != null)
          {
            question.Answers = results.Read<AnswerGetResponse>().ToList();
          }
          return question;
        }
      }
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestions()
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.Query<QuestionGetManyResponse>(
          @"EXEC dbo.Question_GetMany"
        );
      }
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestionsBySearch(string search)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.Query<QuestionGetManyResponse>(
          @"EXEC dbo.Question_GetMany_BySearch @Search = @Search",
          new { Search = search }
        );
      }
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestionsWithAnswers()
    {
      // simulates inefficient data quering (N+1 problem)
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        var questions = connection.Query<QuestionGetManyResponse>(
          "EXEC dbo.Question_GetMany"
        );
        foreach (var question in questions)
        {
          question.Answers =
          connection.Query<AnswerGetResponse>(
          @"EXEC dbo.Answer_Get_ByQuestionId @QuestionId = @QuestionId",
          new { QuestionId = question.QuestionId })
          .ToList();
        }
        return questions;
      }
    }

    public IEnumerable<QuestionGetManyResponse> GetQuestionsWithAnswersOpt()
    {
      // simulates efficient data quering
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.Query<QuestionGetManyResponse>(
          "EXEC dbo.Question_GetMany_WithAnswers"
        );
      }
    }

    public IEnumerable<QuestionGetManyResponse> GetUnansweredQuestions()
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.Query<QuestionGetManyResponse>(
          "EXEC dbo.Question_GetUnanswered"
        );
      }
    }

    public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsAsync()
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        await connection.OpenAsync();
        return await connection
          .QueryAsync<QuestionGetManyResponse>(
            "EXEC dbo.Question_GetUnanswered"
          );
      }
    }

    public AnswerGetResponse PostAnswer(AnswerPostFullRequest answer)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.QueryFirst<AnswerGetResponse>(
          @"EXEC dbo.Answer_Post
          @QuestionId = @QuestionId, @Content = @Content,
          @UserId = @UserId, @UserName = @UserName,
          @Created = @Created",
          answer
        );
      }
    }

    public QuestionGetSingleResponse PostQuestion(QuestionPostFullRequest question)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        var questionId = connection.QueryFirst<int>(
          @"EXEC dbo.Question_Post
            @Title = @Title, @Content = @Content,
            @UserId = @UserId, @UserName = @UserName,
            @Created = @Created",
          question
        );
        return  GetQuestion(questionId).Result;
      }
    }

    public QuestionGetSingleResponse PutQuestion(int questionId, QuestionPutRequest question)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        connection.Execute(
          @"EXEC dbo.Question_Put
            @QuestionId = @QuestionId, @Title = @Title, @Content = @Content",
          new { QuestionId = questionId, question.Title, question.Content }
        );
        return GetQuestion(questionId).Result;
      }
    }

    public bool QuestionExists(int questionId)
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();
        return connection.QueryFirst<bool>(
          @"EXEC dbo.Question_Exists @QuestionId = @QuestionId",
          new { QuestionId = questionId }
        );
      }
    }
  }
}