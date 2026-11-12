using System;

namespace HeyClicky.Core.Voice;

public delegate void LineAudioChunkHandler(int lineIndex, ReadOnlySpan<byte> pcm16Mono);
