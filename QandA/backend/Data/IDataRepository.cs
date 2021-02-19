using QandA.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QandA.Data {
  public interface IDataRepository {
    IEnumerable<QuestionGetManyResponse> GetQuestions();
    IEnumerable<QuestionGetManyResponse> GetQuestionsWithAnswers();
    IEnumerable<QuestionGetManyResponse> GetQuestionsWithAnswersOpt();
    IEnumerable<QuestionGetManyResponse> GetQuestionsBySearch(string search);
    IEnumerable<QuestionGetManyResponse> GetUnansweredQuestions();

    Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsAsync();
    Task<QuestionGetSingleResponse> GetQuestion(int questionId);
    bool QuestionExists(int questionId);
    AnswerGetResponse GetAnswer(int answerId);

    QuestionGetSingleResponse PostQuestion(QuestionPostFullRequest question);
    QuestionGetSingleResponse PutQuestion(int questionId, QuestionPutRequest question);
    void DeleteQuestion(int questionId);
    AnswerGetResponse PostAnswer(AnswerPostFullRequest answer);
  }
}
