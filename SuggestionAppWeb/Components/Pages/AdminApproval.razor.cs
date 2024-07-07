namespace SuggestionAppWeb.Components.Pages;
public partial class AdminApproval
{
    private List<SuggestionModel> _submissions = [];
    private string _currentEditingTitle = string.Empty;
    private string _editedTitle = string.Empty;
    private string _currentEditingDescription = string.Empty;
    private string _editedDescription = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _submissions = await suggestionData.GetAllSuggestionsWaitingForApproval();
    }

    private async Task ApproveSubmission(SuggestionModel submission)
    {
        submission.ApprovedForRelease = true;
        _submissions.Remove(submission);
        await suggestionData.UpdateSuggestion(submission);
    }

    private async Task RejectSubmission(SuggestionModel submission)
    {
        submission.Rejected = true;
        _submissions.Remove(submission);
        await suggestionData.UpdateSuggestion(submission);
    }

    private void EditTitle(SuggestionModel model)
    {
        _editedTitle = model.Suggestion;
        _currentEditingTitle = model.Id;
        _currentEditingDescription = string.Empty;
    }

    private async Task SaveTitle(SuggestionModel model)
    {
        _currentEditingTitle = string.Empty;
        model.Suggestion = _editedTitle;
        await suggestionData.UpdateSuggestion(model);
    }

    private void EditDescription(SuggestionModel model)
    {
        _editedDescription = model.Description;
        _currentEditingDescription = model.Id;
        _currentEditingTitle = string.Empty;
    }

    private async Task SaveDescription(SuggestionModel model)
    {
        _currentEditingDescription = string.Empty;
        model.Description = _editedDescription;
        await suggestionData.UpdateSuggestion(model);
    }
}