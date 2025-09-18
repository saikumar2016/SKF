using Microsoft.AspNetCore.Mvc;
using SKF.Models;
using SKF.Services;



namespace SKF.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly OpenAIService _openAI;
        private readonly DatasheetService _datasheet;
        private readonly CacheService _cache;

        public QueryController(OpenAIService openAI, DatasheetService datasheet, CacheService cache)
        {
            _openAI = openAI;
            _datasheet = datasheet;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] QueryRequest request)
        {
            try
            {
                // Extract product and attribute
                var queryInfo = await _openAI.ExtractQueryInfoAsync(request.UserQuery);

                if (queryInfo == null || string.IsNullOrEmpty(queryInfo.Product) || string.IsNullOrEmpty(queryInfo.Attribute))
                    return BadRequest("Could not extract product or attribute.");

                var cacheKey = $"{queryInfo.Product}:{queryInfo.Attribute}".ToLower();
                var cached = await _cache.GetCachedAnswerAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                    return Ok(cached);

                var value = _datasheet.GetAttribute(queryInfo.Product, queryInfo.Attribute);
                if (value == null)
                    return Ok($"I'm sorry, I can't find that information.");

                var answer = $"The {queryInfo.Attribute.ToLower()} of the {queryInfo.Product} bearing is {value}.";
                await _cache.SetCachedAnswerAsync(cacheKey, answer);

                return Ok(answer);

            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }
    }

}
