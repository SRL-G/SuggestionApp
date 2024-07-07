namespace SuggestionAppWeb.Components.Pages;
public partial class Index
{
    private UserModel? _loggedInUser;
    private List<SuggestionModel>? _suggestions;
    private List<CategoryModel> _categories = [];
    private List<StatusModel> _statuses = [];
    private SuggestionModel? _archivingSuggestion;

    private string _selectedCategory = "All";
    private string _selectedStatus = "All";
    private string _searchText = string.Empty;
    private bool _isSortedByNew = true;
    private bool _showCategories = false;
    private bool _showStatuses = false;

    protected override async Task OnInitializedAsync()
    {
        _categories = await categoryData.GetAllCategories();
        _statuses = await statusData.GetAllStatuses();
        await LoadAndVerifyUser();
    }

    private async Task ArchiveSuggestion()
    {
        _archivingSuggestion!.Archived = true;
        await suggestionData.UpdateSuggestion(_archivingSuggestion);
        _suggestions!.Remove(_archivingSuggestion);
        _archivingSuggestion = null;
        //await FilterSuggestions();
    }

    private void LoadCreatePage()
    {
        if (_loggedInUser is not null)
        {
            navManager.NavigateTo("/Create");
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private async Task LoadAndVerifyUser()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        string? objectId = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("objectidentifier"))?.Value;

        if (string.IsNullOrWhiteSpace(objectId) == false)
        {
            _loggedInUser = await userData.GetUserFromAuthentication(objectId) ?? new();
            string firstName = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("givenname"))?.Value!;
            string lastName = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("surname"))?.Value!;
            string displayName = authState.User.Claims.FirstOrDefault(c => c.Type.Equals("name"))?.Value!;
            string email = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value!;

            bool isDirty = false;
            if (objectId.Equals(_loggedInUser.ObjectIdentifier) == false)
            {
                isDirty = true;
                _loggedInUser.ObjectIdentifier = objectId;
            }
            if (firstName.Equals(_loggedInUser.FirstName) == false)
            {
                isDirty = true;
                _loggedInUser.FirstName = firstName;
            }
            if (lastName.Equals(_loggedInUser.LastName) == false)
            {
                isDirty = true;
                _loggedInUser.LastName = lastName;
            }
            if (displayName.Equals(_loggedInUser.DisplayName) == false)
            {
                isDirty = true;
                _loggedInUser.DisplayName = displayName;
            }
            if (email.Equals(_loggedInUser.EmailAddress) == false)
            {
                isDirty = true;
                _loggedInUser.EmailAddress = email;
            }

            if (isDirty)
            {
                if (string.IsNullOrWhiteSpace(_loggedInUser.Id))
                {
                    await userData.CreateUser(_loggedInUser);
                }
                else
                {
                    await userData.UpdateUser(_loggedInUser);
                }
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadFilterState();
            await FilterSuggestions();
            StateHasChanged();
        }
    }

    private async Task LoadFilterState()
    {
        var stringResults = await sessionStorage.GetAsync<string>(nameof(_selectedCategory));
        _selectedCategory = stringResults.Success ? stringResults.Value! : "All";

        stringResults = await sessionStorage.GetAsync<string>(nameof(_selectedStatus));
        _selectedStatus = stringResults.Success ? stringResults.Value! : "All";

        stringResults = await sessionStorage.GetAsync<string>(nameof(_searchText));
        _searchText = stringResults.Success ? stringResults.Value! : "";

        var boolResults = await sessionStorage.GetAsync<bool>(nameof(_isSortedByNew));
        _isSortedByNew = !stringResults.Success || boolResults.Value;
    }

    private async Task SaveFilterState()
    {
        await sessionStorage.SetAsync(nameof(_selectedCategory), _selectedCategory);
        await sessionStorage.SetAsync(nameof(_selectedStatus), _selectedStatus);
        await sessionStorage.SetAsync(nameof(_searchText), _searchText);
        await sessionStorage.SetAsync(nameof(_isSortedByNew), _isSortedByNew);
    }

    private async Task FilterSuggestions()
    {
        var output = await suggestionData.GetAllApprovedSuggestions();

        if (_selectedCategory != "All")
        {
            output = output.Where(s => s.Category?.CategoryName == _selectedCategory).ToList();
        }

        if (_selectedStatus != "All")
        {
            output = output.Where(s => s.SuggestionStatus?.StatusName == _selectedStatus).ToList();
        }

        if (string.IsNullOrWhiteSpace(_searchText) == false)
        {
            output = output.Where(
                s => s.Suggestion.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase) ||
                s.Description.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase)
            ).ToList();
        }

        if (_isSortedByNew)
        {
            output = [.. output.OrderByDescending(s => s.DateCreated)];
        }
        else
        {
            output = [.. output.OrderByDescending(s => s.UserVotes.Count).ThenByDescending(s => s.DateCreated)];
        }

        _suggestions = output;

        await SaveFilterState();
    }

    private async Task OrderByNew(bool isNew)
    {
        _isSortedByNew = isNew;
        await FilterSuggestions();
    }

    private async Task OnSearchInput(string? searchInput)
    {
        if (searchInput != null)
        {
            _searchText = searchInput;
            await FilterSuggestions();
        }
    }

    private async Task OnCategoryClick(string category = "All")
    {
        _selectedCategory = category;
        _showCategories = false;
        await FilterSuggestions();
    }

    private async Task OnStatusClick(string status = "All")
    {
        _selectedStatus = status;
        _showStatuses = false;
        await FilterSuggestions();
    }

    private async Task VoteUp(SuggestionModel suggestion)
    {
        if (_loggedInUser is not null)
        {
            if (suggestion.Author.Id == _loggedInUser.Id)
            {
                // can't vote on your own suggestion
                return;
            }
            if (suggestion.UserVotes.Add(_loggedInUser.Id) == false)
            {
                suggestion.UserVotes.Remove(_loggedInUser.Id);
            }

            await suggestionData.UpvoteSuggestion(suggestion.Id, _loggedInUser.Id);

            if (_isSortedByNew == false)
            {
                _suggestions = [.. _suggestions!.OrderByDescending(s => s.UserVotes.Count).ThenByDescending(s => s.DateCreated)];
            }
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private string GetUpvoteTopText(SuggestionModel suggestion)
    {
        if (suggestion.UserVotes?.Count > 0)
        {
            return suggestion.UserVotes.Count.ToString("00");
        }
        else
        {
            return suggestion.Author.Id == _loggedInUser?.Id ? "Awaiting" : "Click To";
        }
    }

    private static string GetUpvoteBottomText(SuggestionModel suggestion)
    {
        return suggestion.UserVotes?.Count > 1 ? "Upvotes" : "Upvote";
    }

    private void OpenDetails(SuggestionModel suggestion)
    {
        navManager.NavigateTo($"/Details/{suggestion.Id}");
    }

    private string SortedByNewClass(bool isNew)
    {
        return isNew == _isSortedByNew ? "sort-selected" : string.Empty;
    }

    private string GetVoteClass(SuggestionModel suggestion)
    {
        if (suggestion.UserVotes is null || suggestion.UserVotes.Count == 0)
        {
            return "suggestion-entry-no-votes";
        }
        else if (suggestion.UserVotes.Contains(_loggedInUser?.Id))
        {
            return "suggestion-entry-voted";
        }
        else
        {
            return "suggestion-entry-not-voted";
        }
    }

    private static string GetSuggestionStatusClass(SuggestionModel suggestion)
    {
        if (suggestion is null || suggestion.SuggestionStatus is null)
        {
            return "suggestion-entry-status-none";
        }

        string output = suggestion.SuggestionStatus.StatusName switch
        {
            "Completed" => "suggestion-entry-status-completed",
            "Watching" => "suggestion-entry-status-watching",
            "Upcoming" => "suggestion-entry-status-upcoming",
            "Dismissed" => "suggestion-entry-status-dismissed",
            _ => "suggestion-entry-status-none"
        };

        return output;
    }

    private string GetSelectedCategory(string category = "All")
    {
        return category == _selectedCategory ? "selected-category" : string.Empty;
    }

    private string GetSelectedStatus(string status = "All")
    {
        return status == _selectedStatus ? "selected-status" : string.Empty;
    }
}