using System.Collections.Generic;

namespace HeyClicky.Core.Voice;

public sealed record IntegrationAccounts(string Integration, IReadOnlyList<IntegrationAccount> Accounts);
