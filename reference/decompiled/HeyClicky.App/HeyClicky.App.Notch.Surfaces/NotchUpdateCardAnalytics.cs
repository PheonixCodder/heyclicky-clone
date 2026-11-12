using System.Text.Json.Nodes;
using HeyClicky.App.Dock;

namespace HeyClicky.App.Notch.Surfaces;

public static class NotchUpdateCardAnalytics
{
	public static JsonObject ShownProperties(ArcCardContent content, bool isDone)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["state"] = (content.IsFinalizing ? "finalizing" : ArcDockAnalytics.StateName(content.State)) };
		if (isDone)
		{
			CardReadyOutput readyOutput = content.ReadyOutput;
			jsonObject["output_kind"] = (((object)readyOutput != null) ? ArcDockAnalytics.OutputKind(readyOutput.Kind) : "none");
			jsonObject["has_result_card"] = (object)content.Answer != null;
		}
		else
		{
			CardFooterRequest request = content.Request;
			if ((object)request != null)
			{
				jsonObject["request"] = ArcDockAnalytics.RequestKind(request.Kind);
			}
		}
		return jsonObject;
	}
}
