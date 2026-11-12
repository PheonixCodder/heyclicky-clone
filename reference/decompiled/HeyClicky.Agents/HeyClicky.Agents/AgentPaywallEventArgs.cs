namespace HeyClicky.Agents;

public sealed record AgentPaywallEventArgs(int StatusCode, string Body, bool Presents);
