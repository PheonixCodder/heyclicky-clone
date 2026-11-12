using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HeyClicky.App.UI;
using HeyClicky.Core.Browser;
using Windows.UI;

namespace HeyClicky.App.Drive;

public sealed class UploadApprovalCard
{
	private readonly TakeOverConsentCard card = new TakeOverConsentCard();

	private bool isAsking;

	private const int TimeoutSeconds = 120;

	public void Ask(IReadOnlyList<string> fileNames, string? site, string jobTitle, Color accent, Action saveAlways, TaskCompletionSource<UploadApprovalAnswer> answer)
	{
		if (isAsking || DriveNotchOwnership.OwnsNotch)
		{
			answer.TrySetResult(UploadApprovalAnswer.Busy);
			return;
		}
		isAsking = true;
		string text = BrowserUploads.ApprovalQuestion(fileNames, site);
		card.Present(text, text, accent, delegate
		{
			Finish(UploadApprovalAnswer.Allowed);
		}, delegate
		{
			Finish(UploadApprovalAnswer.Declined);
		}, Sounds.Play, delegate
		{
			Finish(UploadApprovalAnswer.Unanswered);
		}, BrowserUploads.ApprovalNote(jobTitle), "Approve this time", asksTheUser: true, 120, "Always approve", delegate
		{
			saveAlways();
			Finish(UploadApprovalAnswer.Allowed);
		});
		void Finish(UploadApprovalAnswer result)
		{
			isAsking = false;
			answer.TrySetResult(result);
		}
	}
}
