/* Binding between the main Limelight app and Limelight Common */

#include "Limelight-common-binding.hpp"

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <winerror.h> 
#include <Objbase.h> 
#include <string>

// Tell the linker to link using these libraries
// FIXME it builds, but the linker spews warnings. Fix build config.
#pragma comment(lib, "limelight-common.lib")
#pragma comment(lib, "ws2_32.lib")

#include <opus.h>
#pragma comment(lib, "opus.lib")
#pragma comment(lib, "celt.lib")
#pragma comment(lib, "silk_common.lib")
#pragma comment(lib, "silk_float.lib")

static OpusDecoder *s_OpusDecoder;

using namespace Limelight_common_binding;
using namespace Platform;

static LimelightDecoderRenderer ^s_DrCallbacks;
static LimelightAudioRenderer ^s_ArCallbacks;
static LimelightConnectionListener ^s_ClCallbacks;

#define INITIAL_FRAME_BUFFER_SIZE 1048576
static int s_FrameBufferSize;
static char* s_FrameBuffer;

/* Each of these methods call into the appropriate Limelight Common method */
void DrShimSetup(int width, int height, int redrawRate, void* context, int drFlags) {
	s_FrameBufferSize = INITIAL_FRAME_BUFFER_SIZE;
	s_FrameBuffer = (char*) malloc(s_FrameBufferSize);
	if (s_FrameBuffer == NULL) {
		// FIXME: Change DrSetup() to be failable
	}

	s_DrCallbacks->Setup(width, height, redrawRate, drFlags);
}
void DrShimStart(void) {
	s_DrCallbacks->Start();
}
void DrShimStop(void) {
	s_DrCallbacks->Stop();
}
void DrShimRelease(void) {
	free(s_FrameBuffer);
	s_DrCallbacks->Destroy();
}
void DrShimSubmitDecodeUnit(PDECODE_UNIT decodeUnit) {
	PLENTRY entry;
	int offset = 0;

	/* Resize the frame buffer if the current frame is too big.
	 * This is safe without locking because this function is
	 * called only from a single thread. */
	if (s_FrameBufferSize < decodeUnit->fullLength) {
		s_FrameBufferSize = decodeUnit->fullLength;
		s_FrameBuffer = (char*) malloc(s_FrameBufferSize);
		if (s_FrameBuffer == NULL) {
			// FIXME: Change DrSubmitDecodeUnit() to be failable
		}
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
void ArShimStart(void) {
	s_ArCallbacks->Start();
}
void ArShimStop(void) {
	s_ArCallbacks->Stop();
}
void ArShimRelease(void) {
	if (s_OpusDecoder != NULL) {
		opus_decoder_destroy(s_OpusDecoder);
		s_OpusDecoder = NULL;
	}

	s_ArCallbacks->Destroy();
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
void ClShimStageFailed(int stage, int errorCode) {
	s_ClCallbacks->StageFailed(stage, errorCode);
}
void ClShimConnectionStarted(void) {
	s_ClCallbacks->ConnectionStarted();
}
void ClShimConnectionTerminated(int errorCode) {
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

int LimelightCommonRuntimeComponent::StartConnection(unsigned int hostAddress, LimelightStreamConfiguration ^streamConfig,
	LimelightConnectionListener ^clCallbacks, LimelightDecoderRenderer ^drCallbacks, LimelightAudioRenderer ^arCallbacks)
{
	STREAM_CONFIGURATION config;
	DECODER_RENDERER_CALLBACKS drShimCallbacks;
	AUDIO_RENDERER_CALLBACKS arShimCallbacks;
	CONNECTION_LISTENER_CALLBACKS clShimCallbacks;

	config.width = streamConfig->GetWidth();
	config.height = streamConfig->GetHeight();
	config.fps = streamConfig->GetFps();
	config.bitrate = streamConfig->GetBitrate();
	config.packetSize = streamConfig->GetPacketSize();
	
	s_ClCallbacks = clCallbacks;
	s_DrCallbacks = drCallbacks;
	s_ArCallbacks = arCallbacks;

	drShimCallbacks.setup = DrShimSetup;
	drShimCallbacks.start = DrShimStart;
	drShimCallbacks.stop = DrShimStop;
	drShimCallbacks.release = DrShimRelease;
	drShimCallbacks.submitDecodeUnit = DrShimSubmitDecodeUnit;

	arShimCallbacks.init = ArShimInit;
	arShimCallbacks.start = ArShimStart;
	arShimCallbacks.stop = ArShimStop;
	arShimCallbacks.release = ArShimRelease;
	arShimCallbacks.decodeAndPlaySample = ArShimDecodeAndPlaySample;

	clShimCallbacks.stageStarting = ClShimStageStarting;
	clShimCallbacks.stageComplete = ClShimStageComplete;
	clShimCallbacks.stageFailed = ClShimStageFailed;
	clShimCallbacks.connectionStarted = ClShimConnectionStarted;
	clShimCallbacks.connectionTerminated = ClShimConnectionTerminated;
	clShimCallbacks.displayMessage = ClShimDisplayMessage;
	clShimCallbacks.displayTransientMessage = ClShimDisplayTransientMessage;

	return LiStartConnection(hostAddress, &config, &clShimCallbacks,
		&drShimCallbacks, &arShimCallbacks, NULL, 0);
}

void LimelightCommonRuntimeComponent::StopConnection(void) {
	LiStopConnection();
}

int LimelightCommonRuntimeComponent::SendMouseMoveEvent(short deltaX, short deltaY) {
	return LiSendMouseMoveEvent(deltaX, deltaY);
}

int LimelightCommonRuntimeComponent::SendMouseButtonEvent(unsigned char action, int button) {
	return LiSendMouseButtonEvent(action, button);
}

int LimelightCommonRuntimeComponent::SendKeyboardEvent(short keyCode, unsigned char keyAction, unsigned char modifiers) {
	return LiSendKeyboardEvent(keyCode, keyAction, modifiers);
}

int LimelightCommonRuntimeComponent::sendControllerInput(short buttonFlags, byte leftTrigger, byte rightTrigger, short leftStickX,
	short leftStickY, short rightStickX, short rightStickY){
	// TODO waiting on Common code to handle controller input
	// ^^ Is this TODO still true? 
	return(0);
}
