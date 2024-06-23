﻿namespace SuggestionAppLibrary.DataAccess;
public class MongoUserData : IUserData
{
   private readonly IMongoCollection<UserModel> _users;

   public MongoUserData(IDbConnection db)
   {
      _users = db.UserCollection;
   }

   public async Task<List<UserModel>> GetUsersAsync()
   {
      var results = await _users.FindAsync(_ => true);
      return results.ToList();
   }

   public async Task<UserModel> GetUser(string id)
   {
      var results = await _users.FindAsync(u => u.Id == id);
      return results.FirstOrDefault();
   }

   public async Task<UserModel> GetUserFromAuthentication(string objectId)
   {
      var results = await _users.FindAsync(u => u.ObjectIdentifier == objectId);
      return results.FirstOrDefault();
   }

   public Task CreateUser(UserModel user)
   {
      return _users.InsertOneAsync(user);
   }

   public Task UpdateUser(UserModel user)
   {
      var filter = Builders<UserModel>.Filter.Eq(field: "Id", user.Id);
      return _users.ReplaceOneAsync(filter, user, options: new ReplaceOptions { IsUpsert = true });
   }
}
