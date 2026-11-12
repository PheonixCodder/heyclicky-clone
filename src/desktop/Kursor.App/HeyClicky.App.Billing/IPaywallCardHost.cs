using System.Threading.Tasks;
using HeyClicky.Core.Billing;

namespace HeyClicky.App.Billing;

public interface IPaywallCardHost
{
	Task<string?> StartCheckoutAsync(string plan, string interval, string surface, PaywallSource source);

	Task<string?> OpenPortalAsync();

	void Close(PaywallCloseKind kind);

	void ContentResized();
}
