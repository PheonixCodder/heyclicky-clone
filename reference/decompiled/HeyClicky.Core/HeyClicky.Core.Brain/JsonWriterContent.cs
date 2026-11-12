using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Brain;

internal sealed class JsonWriterContent : HttpContent
{
	private readonly Action<Utf8JsonWriter> write;

	public JsonWriterContent(Action<Utf8JsonWriter> write)
	{
		this.write = write;
		base.Headers.ContentType = new MediaTypeHeaderValue("application/json");
	}

	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		await SerializeToStreamAsync(stream, context, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
	}

	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		await using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions
		{
			SkipValidation = true
		});
		write(writer);
		await writer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	protected override bool TryComputeLength(out long length)
	{
		length = 0L;
		return false;
	}
}
