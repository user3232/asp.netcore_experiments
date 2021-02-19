using QandA.Data.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace QandA.Data
{
  public class MemoryDb
  {

    public class Question {
      public int QuestionId;
      public string Title;
      public string Content;
      public string UserId;
      public string UserName;
      public DateTime Created;
      public List<Answer> Answers;


      public static implicit operator QuestionGetManyResponse(Question q) => 
        ToQuestionGetManyResponse(q);
      public static implicit operator QuestionGetSingleResponse(Question q) => 
        ToQuestionGetSingleResponse(q);

      public static QuestionGetManyResponse ToQuestionGetManyResponse(Question q)
      {
        return new QuestionGetManyResponse() {
          Content = q.Content,
          Created = q.Created,
          QuestionId = q.QuestionId,
          Title = q.Title,
          UserName = q.UserName,
        };
      }
      public static QuestionGetSingleResponse ToQuestionGetSingleResponse(Question q)
      {
        return new QuestionGetSingleResponse() {
          Content = q.Content,
          Created = q.Created,
          QuestionId = q.QuestionId,
          Title = q.Title,
          UserName = q.UserName,
          UserId = q.UserId,
          Answers = q.Answers.Select(Answer.ToAnswerGetResponse)
        };
      }

      
    }

    public class Answer {
      public int AnswerId;
      public string Content;
      public string UserId;
      public string UserName;
      public DateTime Created;

      public static implicit operator AnswerGetResponse(Answer a) => ToAnswerGetResponse(a);
      public static AnswerGetResponse ToAnswerGetResponse(Answer a) {
        return new AnswerGetResponse() {
          AnswerId = a.AnswerId,
          Content = a.Content,
          Created = a.Created,
          UserName = a.UserName,
        };
      }
      
    }
    
    public List<Question> Data = Produce();
    public static List<Question> Produce()
    {
      var questions = new List<Question>
      {
        new Question() 
        {
          QuestionId = 1,
          Title = "Why should I learn TypeScript?",
          Content = "TypeScript seems to be getting popular so I wondered whether it is worth my time learning it? What benefits does it give over JavaScript?",
          UserId = "1",
          UserName = "bob.test@test.com",
          Created = DateTime.Parse("2019-05-18 14:32"),
          Answers = new List<Answer>() 
          {
            new Answer() 
            {
              AnswerId = 1,
              Content = "To catch problems earlier speeding up your developments",
              UserId = "2",
              UserName = "jane.test@test.com",
              Created = DateTime.Parse("2019-05-18 14:40"),
            },
            new Answer() 
            {
              AnswerId = 2,
              Content = "So, that you can use the JavaScript features of tomorrow, today",
              UserId = "3",
              UserName = "fred.test@test.com",
              Created = DateTime.Parse("2019-05-18 16:18"),
            }
          },
        },
        new Question()
        {
          QuestionId = 2,
          Title = "Which state management tool should I use?",
          Content = "There seem to be a fair few state management tools around for React - React, Unstated, ... Which one should I use?",
          UserId = "2",
          UserName = "jane.test@test.com",
          Created = DateTime.Parse("2019-05-18 14:48"),
          Answers = new List<Answer>(),
        }
      };

      return questions;
    }
  }
}