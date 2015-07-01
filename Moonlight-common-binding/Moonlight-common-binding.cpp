/* Binding between the main Moonlight app and Common */
#if 0
#include "Moonlight-common-binding.hpp"

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <winerror.h> 
#include <Objbase.h> 
#include <string>

// Tell the linker to link using these libraries
#pragma comment(lib, "ws2_32.lib")

#include <opus.h>
#pragma comment(lib, "opus.lib")
#pragma comment(lib, "celt.lib")
#pragma comment(lib, "silk_common.lib")
#pragma comment(lib, "silk_float.lib")

static OpusDecoder *s_OpusDecoder;

using namespace Moonlight_common_binding;
using namespace Platform;

static MoonlightDecoderRenderer ^s_DrCallbacks;
static MoonlightAudioRenderer ^s_ArCallbacks;
static MoonlightConnectionListener ^s_ClCallbacks;
static MoonlightPlatformCallbacks ^s_PlCallbacks;

#define INITIAL_FRAME_BUFFER_SIZE 1048576
static int s_FrameBufferSize;
static char* s_FrameBuffer;

/* Each of these methods call into the appropriate Moonlight Common method */
void DrShimSetup(int width, int height, int redrawRate, void* context, int drFlags) {
	s_DrCallbacks->Setup(width, height, redrawRate, drFlags);
}
void DrShimCleanup(void) {
	free(s_FrameBuffer);
	s_DrCallbacks->Cleanup();
}
int DrShimSubmitDecodeUnit(PDECODE_UNIT decodeUnit) {
	PLENTRY entry;
	int offset = 0;

	/* Resize the frame buffer if the current frame is too big.
	 * This is safe without locking because this function is
	 * called only from a single thread. */
	if (s_FrameBufferSize < decodeUnit->fullLength) {
		s_FrameBufferSize = decodeUnit->fullLength;
		s_FrameBuffer = (char*) malloc(s_FrameBufferSize);
	}

	if (s_FrameBuffer == NULL) {
		return DR_NEED_IDR;
	}

	entry = decodeUnit->bufferList;
	while (entry != NULL)
	{
		memcpy(&s_FrameBuffer[offset], entry->data, entry->length);
		offset += entry->length;
		entry = entry->next;
	}

	s_DrCallbacks->SubmitDecodeUnit(Platform::ArrayReference<byte>((byte*)s_FrameBuffer, decodeUnit->fullLength));
}

#define MAX_OUTPUT_SHORTS_PER_CHANNEL 240
#define CHANNEL_COUNT 2
#define SAMPLE_RATE_HZ 48000

void ArShimInit(void) {
	int err;

	s_OpusDecoder = opus_decoder_create(SAMPLE_RATE_HZ,
		CHANNEL_COUNT,
		&err);

	s_ArCallbacks->Init();
}
void ArShimCleanup(void) {
	if (s_OpusDecoder != NULL) {
		opus_decoder_destroy(s_OpusDecoder);
		s_OpusDecoder = NULL;
	}

	s_ArCallbacks->Cleanup();
}
void ArShimDecodeAndPlaySample(char* sampleData, int sampleLength) {
	opus_int16 decodedBuffer[MAX_OUTPUT_SHORTS_PER_CHANNEL * CHANNEL_COUNT];
	int decodedSamples;
		
	decodedSamples = opus_decode(s_OpusDecoder, (const unsigned char*)sampleData, sampleLength,
		decodedBuffer, MAX_OUTPUT_SHORTS_PER_CHANNEL, 0);
	if (decodedSamples > 0) {
		s_ArCallbacks->PlaySample(Platform::ArrayReference<byte>((byte*)decodedBuffer,
			decodedSamples * CHANNEL_COUNT * sizeof(opus_int16)));
	}
}

void ClShimStageStarting(int stage) {
	s_ClCallbacks->StageStarting(stage);
}
void ClShimStageComplete(int stage) {
	s_ClCallbacks->StageComplete(stage);
}
void ClShimStageFailed(int stage, long errorCode) {
	s_ClCallbacks->StageFailed(stage, errorCode);
}
void ClShimConnectionStarted(void) {
	s_ClCallbacks->ConnectionStarted();
}
void ClShimConnectionTerminated(long errorCode) {
	s_ClCallbacks->ConnectionTerminated(errorCode);
}
void ClShimDisplayMessage(char *message) {
	std::string stdStr = std::string(message);
	std::wstring wStr = std::wstring(stdStr.begin(), stdStr.end());
	const wchar_t* wChar = wStr.c_str();
	Platform::String^ messageString = ref new Platform::String(wChar);

	s_ClCallbacks->DisplayMessage(messageString); 
}
void ClShimDisplayTransientMessage(char *message) {
	std::string stdStr = std::string(message);
	std::wstring wStr = std::wstring(stdStr.begin(), stdStr.end());
	const wchar_t* wChar = wStr.c_str();
	Platform::String^ messageString = ref new Platform::String(wChar);

	s_ClCallbacks->DisplayTransientMessage(messageString);
}

void PlShimThreadStart(void) {
	s_PlCallbacks->ThreadStart();
}

void PlShimDebugPrint(char *string) {
	std::string stdStr = std::string(string);
	std::wstring wStr = std::wstring(stdStr.begin(), stdStr.end());
	const wchar_t* wChar = wStr.c_str();
	Platform::String^ messageString = ref new Platform::String(wChar);

	s_PlCallbacks->DebugPrint(messageString);
}

int MoonlightCommonRuntimeComponent::StartConnection(unsigned int hostAddress, MoonlightStreamConfiguration ^streamConfig,
	MoonlightConnectionListener ^clCallbacks, MoonlightDecoderRenderer ^drCallbacks, MoonlightAudioRenderer ^arCallbacks,
	MoonlightPlatformCallbacks ^plCallbacks)
{
	STREAM_CONFIGURATION config;
	DECODER_RENDERER_CALLBACKS drShimCallbacks;
	AUDIO_RENDERER_CALLBACKS arShimCallbacks;
	CONNECTION_LISTENER_CALLBACKS clShimCallbacks;
	PLATFORM_CALLBACKS plShimCallbacks;

	config.width = streamConfig->GetWidth();
	config.height = streamConfig->GetHeight();
	config.fps = streamConfig->GetFps();
	config.bitrate = streamConfig->GetBitrate();
	config.packetSize = streamConfig->GetPacketSize();

	memcpy(config.remoteInputAesKey, streamConfig->GetRiAesKey()->Data, sizeof(config.remoteInputAesKey));
	memcpy(config.remoteInputAesIv, streamConfig->GetRiAesIv()->Data, sizeof(config.remoteInputAesIv));
	
	s_ClCallbacks = clCallbacks;
	s_DrCallbacks = drCallbacks;
	s_ArCallbacks = arCallbacks;
	s_PlCallbacks = plCallbacks;

	drShimCallbacks.setup = DrShimSetup;
	drShimCallbacks.cleanup = DrShimCleanup;
	drShimCallbacks.submitDecodeUnit = DrShimSubmitDecodeUnit;

	arShimCallbacks.init = ArShimInit;
	arShimCallbacks.cleanup = ArShimCleanup;
	arShimCallbacks.decodeAndPlaySample = ArShimDecodeAndPlaySample;

	clShimCallbacks.stageStarting = ClShimStageStarting;
	clShimCallbacks.stageComplete = ClShimStageComplete;
	clShimCallbacks.stageFailed = ClShimStageFailed;
	clShimCallbacks.connectionStarted = ClShimConnectionStarted;
	clShimCallbacks.connectionTerminated = ClShimConnectionTerminated;
	clShimCallbacks.displayMessage = ClShimDisplayMessage;
	clShimCallbacks.displayTransientMessage = ClShimDisplayTransientMessage;

	plShimCallbacks.threadStart = PlShimThreadStart;
	plShimCallbacks.debugPrint = PlShimDebugPrint;

	return LiStartConnection(hostAddress, &config, &clShimCallbacks,
		&drShimCallbacks, &arShimCallbacks, &plShimCallbacks, NULL, 0);
}

void MoonlightCommonRuntimeComponent::StopConnection(void) {
	LiStopConnection();
}

int MoonlightCommonRuntimeComponent::SendMouseMoveEvent(short deltaX, short deltaY) {
	return LiSendMouseMoveEvent(deltaX, deltaY);
}

int MoonlightCommonRuntimeComponent::SendMouseButtonEvent(unsigned char action, int button) {
	return LiSendMouseButtonEvent(action, button);
}

int MoonlightCommonRuntimeComponent::SendKeyboardEvent(short keyCode, unsigned char keyAction, unsigned char modifiers) {
	return LiSendKeyboardEvent(keyCode, keyAction, modifiers);
}

int MoonlightCommonRuntimeComponent::SendControllerInput(short buttonFlags, byte leftTrigger, byte rightTrigger, short leftStickX,
	short leftStickY, short rightStickX, short rightStickY) {
	return LiSendControllerEvent(buttonFlags, leftTrigger, rightTrigger, leftStickX, leftStickY, rightStickX, rightStickY);
}

int MoonlightCommonRuntimeComponent::SendMultiControllerInput(short buttonFlags, byte leftTrigger, byte rightTrigger, short leftStickX,
	short leftStickY, short rightStickX, short rightStickY) {
	return LiSendMultiControllerEvent(buttonFlags, leftTrigger, rightTrigger, leftStickX, leftStickY, rightStickX, rightStickY);
}

void MoonlightCommonRuntimeComponent::CompleteThreadStart() {
	LiCompleteThreadStart();
}

#endif