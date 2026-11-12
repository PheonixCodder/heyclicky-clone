using System.Runtime.CompilerServices;

namespace HeyClicky.App.Settings;

public sealed record ConnectorSignInState(ConnectorSignInKind Kind, string? Text = null)
{
	public static readonly ConnectorSignInState Unknown = new ConnectorSignInState(ConnectorSignInKind.Unknown);

	[CompilerGenerated]
	private ConnectorSignInState(ConnectorSignInState original)
	{
		Kind = original.Kind;
		Text = original.Text;
	}
}
