using Microsoft.AspNetCore.Components;

namespace SuggestionAppUI.Pages;
public partial class Detalis
{
   [Parameter]
   public string? Id { get; set; }

   private SuggestionModel? _suggestion;
   private UserModel? _loggedInUser;
   private List<StatusModel>? _statuses;
   private string? _settingStatus = string.Empty;
   private string _urlText = string.Empty;

   protected async override Task OnInitializedAsync()
   {
      _suggestion = await suggestionData.GetSuggestion(Id);
      _loggedInUser = await authProvider.GetUserFromAuth(userData);
      _statuses = await statusData.GetAllStatuses();
   }

   private async Task CompleteSetStatus()
   {
      if (_suggestion is null)
      {
         return;
      }
      var status = _statuses?.First(s => s.StatusName.Equals(_settingStatus, StringComparison.OrdinalIgnoreCase));
      switch (_settingStatus)
      {
         case "completed":
            if (string.IsNullOrWhiteSpace(_urlText))
            {
               return;
            }
            _suggestion.SuggestionStatus = status;
            _suggestion.OwnerNotes = $"You are right, this is an important topic for developers. We created a resource about it here: <a href='{_urlText}' target='_blank'>{_urlText}</a>";
            break;
         case "watching":
            _suggestion.SuggestionStatus = status;
            _suggestion.OwnerNotes = $"We noticed the interest this suggestion is getting! If more people are interested we may adress this topic in an upcoming resource.";
            break;
         case "upcoming":
            _suggestion.SuggestionStatus = status;
            _suggestion.OwnerNotes = $"Great suggestion! We have a resource in the pipeline to address this topic.";
            break;
         case "dismissed":
            _suggestion.SuggestionStatus = status;
            _suggestion.OwnerNotes = $"Sometimes a good idea doesn't fit within our scope and vision. This is one of those ideas.";
            break;
         default:
            return;
      }

      _settingStatus = null;
      await suggestionData.UpdateSuggestion(_suggestion);
   }

   private string GetUpvoteTopText()
   {
      if (_suggestion?.UserVotes?.Count > 0)
      {
         return _suggestion.UserVotes.Count.ToString("00");
      }
      else
      {
         if (_suggestion?.Author.Id == _loggedInUser?.Id)
         {
            return "Awaiting";
         }
         else
         {
            return "Click To";
         }
      }
   }

   private string GetUpvoteBottomText()
   {
      return _suggestion?.UserVotes?.Count > 1 ? "Upvotes" : "Upvote";
   }

   private async Task VoteUp()
   {
      if (_loggedInUser is not null)
      {
         if (_suggestion?.Author.Id == _loggedInUser.Id)
         {
            // can't vote on your own suggestion
            return;
         }
         if (_suggestion?.UserVotes.Add(_loggedInUser.Id) == false)
         {
            _suggestion.UserVotes.Remove(_loggedInUser.Id);
         }

         await suggestionData.UpvoteSuggestion(_suggestion?.Id, _loggedInUser.Id);
      }
      else
      {
         navManager.NavigateTo("/MicrosoftIdentit/Account/SignIn", true);
      }
   }

   private string GetVoteClass()
   {
      if (_suggestion?.UserVotes is null || _suggestion.UserVotes.Count == 0)
      {
         return "suggestion-detail-no-votes";
      }
      else if (_suggestion.UserVotes.Contains(_loggedInUser?.Id))
      {
         return "suggestion-detail-voted";
      }
      else
      {
         return "suggestion-detail-not-voted";
      }
   }

   private string GetStatusClass()
   {
      if (_suggestion is null || _suggestion.SuggestionStatus is null)
      {
         return "suggestion-detail-status-none";
      }

      string output = _suggestion.SuggestionStatus.StatusName switch
      {
         "Completed" => "suggestion-detail-status-completed",
         "Watching" => "suggestion-detail-status-watching",
         "Upcoming" => "suggestion-detail-status-upcoming",
         "Dismissed" => "suggestion-detail-status-dismissed",
         _ => "suggestion-detail-status-none"
      };

      return output;
   }
}