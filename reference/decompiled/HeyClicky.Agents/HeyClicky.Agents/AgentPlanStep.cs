namespace HeyClicky.Agents;

public sealed record AgentPlanStep(string Text, string Status)
{
	public bool IsDone => Status == "completed";

	public bool IsNow
	{
		get
		{
			string status = Status;
			if (status == "inProgress" || status == "in_progress")
			{
				return true;
			}
			return false;
		}
	}
}
