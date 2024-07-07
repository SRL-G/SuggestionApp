using SuggestionAppWeb.Models;

namespace SuggestionAppWeb.Components.Pages;
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
            _suggestion.CategoryId = string.Empty;
            return;
        }

        await suggestionData.CreateSuggestion(s);
        _suggestion = new();
        navManager.NavigateTo("/");
    }
}