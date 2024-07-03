namespace SuggestionAppUI.Pages;
public partial class Profile
{
   private UserModel? _loggedInUser;
   private List<SuggestionModel>? _submissions;
   private List<SuggestionModel>? _approved;
   private List<SuggestionModel>? _archived;
   private List<SuggestionModel>? _pending;
   private List<SuggestionModel>? _rejected;

   protected override async Task OnInitializedAsync()
   {
      //TODO - Replace with user lookup
      _loggedInUser = await authProvider.GetUserFromAuth(userData);

      var results = await suggestionData.GetusersSuggestions(_loggedInUser.Id);

      if (_loggedInUser is not null && results is not null)
      {
         _submissions = [.. results.OrderByDescending(s => s.DateCreated)];
         _approved = [.. _submissions.Where(s => s.ApprovedForRelease && !s.Archived && !s.Rejected)];
         _archived = [.. _submissions.Where(s => s.Archived && !s.Rejected)];
         _pending = [.. _submissions.Where(s => !s.ApprovedForRelease && !s.Rejected).ToList()];
         _rejected = [.. _submissions.Where(s => s.Rejected)];
      }
   }

   private void ClosePage()
   {
      navManager.NavigateTo("/");
   }
}