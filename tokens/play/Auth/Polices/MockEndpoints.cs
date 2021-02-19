using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable
using System.Text.RegularExpressions; // Regex
using System.Collections.Specialized; // NameValueCollection
using System.Net;                     // WebUtility
using System.Web;                     // HttpUtility
using System.Text;                    // Encoding

namespace play.Auth.Polies
{

  


  public static class Rts
  {
    public static string ShowQuestion = 
      "api/view/question?" + 
        "questionTitle={questionTitle}";
    public static string AskQuestion = 
      "api/create/question?" + 
        "questionTitle={questionTitle}" + 
        "&questionContent={questionContent}";
    public static string AnswerQuestion = 
      "api/create/answer?" + 
        "questionTitle={questionTitle}" + 
        "&answerTitle={answerTitle}" + 
        "&answerContent={answerContent}";
    public static string ModifyQuestion = 
      "api/modify/question?" + 
        "questionTitle={questionTitle}" + 
        "&questionContent={questionContent}";
    public static string ModifyAnswer = 
      "api/modify/answer?" + 
        "questionTitle={questionTitle}" +
        "&answerTitle={answerTitle}" + 
        "&answerContent={answerContent}";
    public static string DeleteQuestion = 
      "api/delete/question?" + 
        "questionTitle={questionTitle}"+ 
        "&questionContent={questionContent}";
    public static string DeleteAnswer = 
      "api/delete/answer?" + 
        "questionTitle={questionTitle}" + 
        "&answerTitle={answerTitle}" + 
        "&answerContent={answerContent}";

    public static string ViewScope    = "api.view";
    public static string CreateScope  = "api.create";
    public static string ModifyScope  = "api.modify";
    public static string DeleteScope  = "api.delete";
  }



  

  public static class Apis
  {
    public static string ShowQuestion(
      string claimedUser,
      string questionTitle
    ) => $"First question matching title \"{questionTitle}\"\nis ...";

    public static string AskQuestion(
      string claimedUser,
      string questionTitle,
      string questionContent
    ) => 
      $"Question with Title: {questionTitle}\n" + 
      $"Was Asked by User: {claimedUser}\n" + 
      $"Question:\n" + 
      questionContent.Replace("\n", "\n  ") + "\n";

    public static string AnswerQuestion(
      string claimedUser,
      string questionTitle,
      string answerTitle,
      string answerContent
    ) => 
      $"Question with Title: {questionTitle}\n" + 
      $"  Was Answered by User: {claimedUser}\n" +
      $"  Answer Title: {answerTitle}\n" +
      $"  Answer:\n" +
      answerContent.Replace("\n", "\n    ") + "\n";

    public static string ModifyQuestion(
      string claimedUser,
      string questionTitle,
      string questionContent
    ) => 
      $"Question with Title: {questionTitle}\n" + 
      $"Was Modified by User: {claimedUser}\n" +
      $"Question (modified):\n" + 
      questionContent.Replace("\n", "\n  ") + "\n";

    public static string ModifyAnswer(
      string claimedUser,
      string questionTitle,
      string answerTitle,
      string answerContent
    ) => 
      $"Question with Title: {questionTitle}\n" +
      $"  With Answer with Title: {answerTitle}\n" + 
      $"  Was modified by user: {claimedUser}\n" +
      $"  Answer (modified):\n" +
      answerContent.Replace("\n", "\n    ") + "\n";

    public static string DeleteQuestion(
      string claimedUser,
      string questionTitle
    ) => 
      $"Question with Title: {questionTitle}\n" + 
      $"Was deleted by user: {claimedUser}\n";

    public static string DeleteAnswer(
      string claimedUser,
      string questionTitle,
      string answerTitle
    ) => 
      $"Answer with Title: {answerTitle}\n" +
      $"Of Question with Title: {questionTitle}\n" +
      $"Was deleted by user: {claimedUser}\n";
  }


}