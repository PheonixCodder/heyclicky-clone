namespace HeyClicky.Agents.Jobs;

public static class ClickyJobBrief
{
	private const string WorkFolderBrief = "This is work HeyClicky handed you for the user. Your working directory is its folder: deliverables go in output/, work files in tmp/. When earlier work could help, find_jobs searches it and read_job reads it (../WORK.md lists it too).";

	public static string WorkFolderContext => "This is work HeyClicky handed you for the user. Your working directory is its folder: deliverables go in output/, work files in tmp/. When earlier work could help, find_jobs searches it and read_job reads it (../WORK.md lists it too). When the next step is one only the user can do (sign, pay, log in, approve something), get everything ready first (open the page in their Chrome, fill in what you can), then end your turn telling them in one line what to do, and append <WAIT_FOR>{\"step\": \"Sign the lease in DocuSign\", \"page\": \"https://…\", \"done_when_url_contains\": \"/signing/complete\"}</WAIT_FOR>. Give done_when_url_contains only when you know the address they land on once it's done; HeyClicky then continues this work by itself when their browser gets there. Don't wait or poll for it yourself. (A step you could do yourself is never a WAIT_FOR: on a website you do it in the browser; in a Mac app, its own scripting commands or, for its window, the mac_step tool.)";
}
