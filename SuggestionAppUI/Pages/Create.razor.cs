using SuggestionAppUI.Models;

namespace SuggestionAppUI.Pages;
public partial class Create
{
   private CreateSuggestionModel _suggestion = new();
   private List<CategoryModel> _categories = [];
   private UserModel _loggedInUser = new();

   protected override async Task OnInitializedAsync()
   {
      _categories = await categoryData.GetAllCategories();
      _loggedInUser = await authProvider.GetUserFromAuth(userData);
   }

   private void ClosePage()
   {
      navManager.NavigateTo("/");
   }

   private async Task CreateSuggestion()
   {
      SuggestionModel s = new()
      {
         Suggestion = _suggestion.Suggestion,
         Description = _suggestion.Description,
         Author = new BasicUserModel(_loggedInUser),
         Category = _categories.Where(c => c.Id == _suggestion.CategoryId).FirstOrDefault()
      };

      if (s.Category is null)
      {
         _suggestion.CategoryId = "";
         return;
      }

      await suggestionData.CreateSuggestion(s);
      _suggestion = new();
      ClosePage();
   }
}