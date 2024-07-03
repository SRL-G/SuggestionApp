namespace SuggestionAppLibrary.DataAccess;
public class MongoCategoryData(IDbConnection db, IMemoryCache cache) : ICategoryData
{
   private readonly IMongoCollection<CategoryModel> _categories = db.CategoryCollection;
   private const string _cacheName = "CategoryData";

   public async Task<List<CategoryModel>> GetAllCategories()
   {
      var output = cache.Get<List<CategoryModel>>(_cacheName);
      if (output is null)
      {
         var results = await _categories.FindAsync(_ => true);
         output = results.ToList();

         cache.Set(_cacheName, output, TimeSpan.FromDays(1));
      }
      return output;
   }

   public Task CreateCategory(CategoryModel category)
   {
      return _categories.InsertOneAsync(category);
   }
}