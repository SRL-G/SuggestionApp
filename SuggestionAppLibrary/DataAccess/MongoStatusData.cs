namespace SuggestionAppLibrary.DataAccess;
public class MongoStatusData(IDbConnection db, IMemoryCache cache) : IStatusData
{
   private readonly IMongoCollection<StatusModel> _statuses = db.StatusCollection;
   private const string _cacheName = "StatusData";

   public async Task<List<StatusModel>> GetAllStatuses()
   {
      var output = cache.Get<List<StatusModel>>(_cacheName);
      if (output is null)
      {
         var results = await _statuses.FindAsync(_ => true);
         output = results.ToList();

         cache.Set(_cacheName, output, TimeSpan.FromDays(1));
      }
      return output;
   }

   public Task CreateStatus(StatusModel status)
   {
      return _statuses.InsertOneAsync(status);
   }
}