using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Notch;

public static class ChatActionCaption
{
	public sealed record Item(string Symbol, string Text);

	public static List<Item> Items(string caption)
	{
		List<string> list = new List<string>();
		string text = null;
		string[] array = caption.Split("; ");
		for (int j = 0; j < array.Length; j++)
		{
			string text2 = array[j].Trim();
			if (text2.StartsWith("called ", StringComparison.Ordinal))
			{
				string text3 = text2;
				int length = "called ".Length;
				list.AddRange(from t in text3.Substring(length, text3.Length - length).Split(", ")
					select t.Trim() into t
					where t.Length > 0
					select t);
			}
			else if (text2.StartsWith("sources: ", StringComparison.Ordinal))
			{
				string text3 = text2;
				int length = "sources: ".Length;
				text = text3.Substring(length, text3.Length - length);
			}
		}
		List<Item> list2 = new List<Item>();
		bool flag = false;
		foreach (string item2 in list)
		{
			Item item = ItemForTool(item2);
			if (item2 == "web_search" && text != null)
			{
				item = item with
				{
					Text = item.Text + " (sources: " + text + ")"
				};
				flag = true;
			}
			if (!list2.Any((Item i) => i.Text == item.Text))
			{
				list2.Add(item);
			}
		}
		if (text != null && !flag)
		{
			list2.Add(new Item("link", "Sources: " + text));
		}
		return list2;
	}

	private static Item ItemForTool(string tool)
	{
		switch (tool)
		{
		case "web_search":
			return new Item("magnifyingglass", "Searched the web");
		case "recall":
			return new Item("clock.arrow.circlepath", "Looked back through earlier chats");
		case "save_memory":
			return new Item("bookmark.fill", "Saved to memory");
		case "forget_memory":
			return new Item("eraser", "Forgot it");
		case "open_url":
			return new Item("safari", "Opened a page");
		case "play_on_spotify":
			return new Item("play.circle.fill", "Played it on Spotify");
		case "copy_to_clipboard":
			return new Item("doc.on.clipboard", "Copied to your clipboard");
		case "insert_text":
			return new Item("keyboard", "Typed it in");
		case "sessions_spawn":
			return new Item("paperplane.fill", "Started a background job");
		case "sessions_send":
			return new Item("arrowshape.turn.up.right.fill", "Sent it to a background job");
		case "sessions_interrupt":
			return new Item("stop.circle", "Stopped a background job");
		case "sessions_answer_request":
			return new Item("checkmark.bubble", "Answered a background job");
		case "sessions_yield":
			return new Item("hourglass", "Waited on a background job");
		case "routines":
			return new Item("repeat", "Updated your routines");
		case "clicky_memory":
		case "session_status":
			return new Item("person.text.rectangle", "Checked on a background job");
		case "clicky_artifacts":
			return new Item("doc.on.doc", "Looked at a background job's files");
		case "open_artifact":
			return new Item("doc.text", "Opened a file");
		case "reveal_artifact":
			return new Item("folder", "Showed a file in Explorer");
		case "open_agent_dashboard":
			return new Item("house", "Opened Home");
		case "shell_exec":
		case "exec_command":
			return new Item("terminal", "Ran a quick command");
		case "file_list":
			return new Item("folder", "Looked through your files");
		case "file_read":
			return new Item("doc.text.magnifyingglass", "Read a file");
		case "capture_screen":
			return new Item("camera.viewfinder", "Looked at your screen");
		case "google_calendar_find_events":
			return new Item("calendar", "Checked your calendar");
		case "google_calendar_create_event":
			return new Item("calendar.badge.plus", "Added it to your calendar");
		case "save_drawing":
			return new Item("square.and.arrow.down", "Saved the drawing");
		case "send_to_higher_model":
			return new Item("brain", "Thought it through");
		case "view_image":
			return new Item("photo", "Looked at an image");
		case "skip_onboarding_interview":
			return new Item("forward", "Skipped the interview");
		default:
			if (tool == "mcp" || tool.StartsWith("connected_apps", StringComparison.Ordinal) || tool.StartsWith("COMPOSIO", StringComparison.Ordinal))
			{
				return new Item("tray.full", "Read your apps");
			}
			return new Item("wrench.and.screwdriver", "Used " + tool.Replace('_', ' '));
		}
	}
}
