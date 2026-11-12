using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Tools;

public delegate Task<(int Status, string Body)?> WorkerPost(string path, string jsonBody, CancellationToken ct);
