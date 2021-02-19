using Microsoft.Extensions.Caching.Memory;
using QandA.Data.Models;
using System.Linq;
namespace QandA.Data
{
  public class QuestionCache : IQuestionCache
  {
    // TODO - create a memory cache
    // TODO - method to get a cached question
    // TODO - method to add a cached question
    // TODO - method to remove a cached question

    private MemoryCache _cache {get;set;}
    public QuestionCache() 
    {
      _cache = new MemoryCache(
        new MemoryCacheOptions{SizeLimit = 100}
      );
    }

    private string GetCacheKey(int questionId) => $"Question-{questionId}";

    public QuestionGetSingleResponse Get(int questionId)
    {
      _cache.TryGetValue(
        GetCacheKey(questionId), 
        out QuestionGetSingleResponse question
      );
      return question;
    }

    public void Remove(int questionId)
    {
      _cache.Remove(GetCacheKey(questionId));
    }

    public void Set(QuestionGetSingleResponse question)
    {
      _cache.Set(
        key: GetCacheKey(question.QuestionId),
        value: question,
        options: new MemoryCacheEntryOptions().SetSize(1)
      );
      (var min, var max) = FindMinMax(new[]{1,2,3,4,5});


      static (int Min, int Max) FindMinMax(int[] input)
      {
        return (
          Min: input?.Min() ?? int.MaxValue, 
          Max: input?.Max() ?? int.MinValue
        );
      }
    }
  }
}