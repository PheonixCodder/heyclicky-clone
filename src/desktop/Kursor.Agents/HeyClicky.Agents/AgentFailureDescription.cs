using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;

namespace HeyClicky.Agents;

public static class AgentFailureDescription
{
	public const string SecureConnectionFailedMessage = "HeyClicky couldn't make a secure connection to its server. A firewall, VPN or antivirus on this network may be blocking it. Retry, or try another network.";

	public static string Diagnostic(Exception error)
	{
		List<string> list = new List<string>();
		Exception ex = error;
		while (ex != null && list.Count < 4)
		{
			list.Add(ex.GetType().Name + ": " + ((ex.Message.Length <= 300) ? ex.Message : ex.Message.Substring(0, 300)));
			ex = ex.InnerException;
		}
		list.Reverse();
		return string.Join(" < ", list);
	}

	public static string UserMessage(Exception error)
	{
		if (!IsSecureConnectionFailure(error))
		{
			return error.Message;
		}
		return "HeyClicky couldn't make a secure connection to its server. A firewall, VPN or antivirus on this network may be blocking it. Retry, or try another network.";
	}

	private static bool IsSecureConnectionFailure(Exception error)
	{
		for (Exception ex = error; ex != null; ex = ex.InnerException)
		{
			if ((ex is AuthenticationException || ex is HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError }) ? true : false)
			{
				return true;
			}
		}
		return false;
	}
}
