#include "Limelight-common-binding.h"

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <winerror.h> 
#include <Objbase.h> 


#pragma comment(lib, "limelight-common.lib")
#pragma comment(lib, "ws2_32.lib")

using namespace Limelight_common_binding;
using namespace Platform;

static LimelightDecoderRenderer ^s_DrCallbacks;
static LimelightAudioRenderer ^s_ArCallbacks;
static LimelightConnectionListener ^s_ClCallbacks;

void DrShimSetup(int width, int height, int redrawRate, void* context, int drFlags) {
	s_DrCallbacks->Setup(width, height, redrawRate, drFlags);
}
void DrShimStart(void) {
	s_DrCallbacks->Start();
}
void DrShimStop(void) {
	s_DrCallbacks->Stop();
}
void DrShimRelease(void) {
	s_DrCallbacks->Destroy();
}
void DrShimSubmitDecodeUnit(PDECODE_UNIT decodeUnit) {
	char* fullData;
	const Platform::Array<unsigned char> ^dataArray;

	fullData = (char*)malloc(decodeUnit->fullLength);
	if (fullData != NULL)
	{
		PLENTRY entry;
		int offset = 0;

		entry = decodeUnit->bufferList;
		while (entry != NULL)
		{
			memcpy(&fullData[offset], entry->data, entry->length);
			offset += entry->length;
			entry = entry->next;
		}

		dataArray = ref new Platform::Array<byte>((byte*)fullData, decodeUnit->fullLength);
		s_DrCallbacks->SubmitDecodeUnit(dataArray);

		free(fullData);
	}
}

void ArShimInit(void) {
	s_ArCallbacks->Init();
}
void ArShimStart(void) {
	s_ArCallbacks->Start();
}
void ArShimStop(void) {
	s_ArCallbacks->Stop();
}
void ArShimRelease(void) {
	s_ArCallbacks->Destroy();
}
void ArShimDecodeAndPlaySample(char* sampleData, int sampleLength) {
	const Platform::Array<unsigned char> ^dataArray;
	dataArray = ref new Platform::Array<byte>((byte*)sampleData, sampleLength);
	//s_ArCallbacks->DecodeAndPlaySample(dataArray);
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
	// TODO char* -> Platform::String
}
void ClShimDisplayTransientMessage(char *message) {
	// TODO char* -> Platform::String
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
