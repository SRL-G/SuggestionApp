namespace SuggestionAppLibrary.DataAccess;
public class MongoSuggestionData(IDbConnection db, IUserData userData, IMemoryCache cache) : ISuggestionData
{
   private readonly IMongoCollection<SuggestionModel> _suggestions = db.SuggestionCollection;
   private const string _cacheName = "SuggestionData";

   public async Task<List<SuggestionModel>> GetAllSuggestions()
   {
      var output = cache.Get<List<SuggestionModel>>(_cacheName);
      if (output is null)
      {
         var results = await _suggestions.FindAsync(s => s.Archived == false);
         output = results.ToList();

         cache.Set(_cacheName, output, TimeSpan.FromMinutes(1));
      }
      return output;
   }

   public async Task<List<SuggestionModel>> GetusersSuggestions(string userId)
   {
      var output = cache.Get<List<SuggestionModel>>(userId);
      if (output is null)
      {
         var results = await _suggestions.FindAsync(s => s.Author.Id == userId);
         output = results.ToList();

         cache.Set(userId, output, TimeSpan.FromMinutes(1));
      }
      return output;
   }

   public async Task<List<SuggestionModel>> GetAllApprovedSuggestions()
   {
      var output = await GetAllSuggestions();
      return output.Where(x => x.ApprovedForRelease).ToList();
   }

   public async Task<SuggestionModel> GetSuggestion(string id)
   {
      var results = await _suggestions.FindAsync(s => s.Id == id);
      return results.FirstOrDefault();
   }

   public async Task<List<SuggestionModel>> GetAllSuggestionsWaitingForApproval()
   {
      var output = await GetAllSuggestions();
      return output.Where(x => x.ApprovedForRelease == false && x.Rejected == false).ToList();
   }

   public async Task UpdateSuggestion(SuggestionModel suggestion)
   {
      await _suggestions.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
      cache.Remove(_cacheName);
   }

   public async Task UpvoteSuggestion(string suggestionId, string userId)
   {
      var client = db.Client;

      using var session = await client.StartSessionAsync();

      session.StartTransaction();

      try
      {
         var _db = client.GetDatabase(db.DbName);
         var suggestionsInTransaction = _db.GetCollection<SuggestionModel>(db.SuggestionCollectionName);
         var suggestion = (await suggestionsInTransaction.FindAsync(s => s.Id == suggestionId)).First();

         bool isUpvote = suggestion.UserVotes.Add(userId);
         if (isUpvote is false)
         {
            suggestion.UserVotes.Remove(userId);
         }

         await suggestionsInTransaction.ReplaceOneAsync(session, s => s.Id == suggestionId, suggestion);

         var usersInTransaction = _db.GetCollection<UserModel>(db.UserCollectionName);
         var user = await userData.GetUser(userId);

         if (isUpvote)
         {
            user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
         }
         else
         {
            var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).First();
            user.VotedOnSuggestions.Remove(suggestionToRemove);
         }

         await usersInTransaction.ReplaceOneAsync(session, u => u.Id == userId, user);

         await session.CommitTransactionAsync();

         cache.Remove(_cacheName);
      }
      catch
      {
         await session.AbortTransactionAsync();
         throw;
      }
   }

   public async Task CreateSuggestion(SuggestionModel suggestion)
   {
      var client = db.Client;

      using var session = await client.StartSessionAsync();

      session.StartTransaction();

      try
      {
         var _db = client.GetDatabase(db.DbName);
         var suggestionsInTransaction = _db.GetCollection<SuggestionModel>(db.SuggestionCollectionName);
         await suggestionsInTransaction.InsertOneAsync(session, suggestion);

         var usersInTransaction = _db.GetCollection<UserModel>(db.UserCollectionName);
         var user = await userData.GetUser(suggestion.Author.Id);
         user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));
         await usersInTransaction.ReplaceOneAsync(session, u => u.Id == user.Id, user);

         await session.CommitTransactionAsync();
      }
      catch
      {
         await session.AbortTransactionAsync();
         throw;
      }
   }
}