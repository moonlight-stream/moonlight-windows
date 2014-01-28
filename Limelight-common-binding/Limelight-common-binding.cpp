#include "Limelight-common-binding.h"

#include <stdlib.h>
#include <string.h>
#include <stdio.h>

#pragma comment(lib, "limelight-common.lib")
#pragma comment(lib, "ws2_32.lib")

using namespace Limelight_common_binding;
using namespace Platform;

//ILimelightDecoderRenderer *DecoderRendererCallbacks;

LimelightStreamConfiguration::LimelightStreamConfiguration(int width, int height, int fps) :
m_Width(width), m_Height(height), m_Fps(fps) { }

int LimelightStreamConfiguration::GetWidth(void)
{
	return m_Width;
}

int LimelightStreamConfiguration::GetHeight(void)
{
	return m_Height;
}

int LimelightStreamConfiguration::GetFps(void)
{
	return m_Fps;
}

void DrSetup(int width, int height, int redrawRate, void* context, int drFlags)
{
	//DecoderRendererCallbacks->Setup(width, height, redrawRate, drFlags);
	printf("Setup: %dx%d at %d fps\n", width, height, redrawRate);
}

void DrStart(void)
{
	printf("Start\n");
	//DecoderRendererCallbacks->Start();
}

void DrStop(void)
{
	//DecoderRendererCallbacks->Stop();
}

void DrSubmitDecodeUnit(PDECODE_UNIT decodeUnit)
{
	char* fullData;

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

		printf("Decode unit: %d\n", decodeUnit->fullLength);
		//DecoderRendererCallbacks->SubmitDecodeUnit(fullData, decodeUnit->fullLength);

		free(fullData);
	}
}

void DrRelease(void)
{
	//DecoderRendererCallbacks->Release();
}

int LimelightCommonRuntimeComponent::StartConnection(unsigned int hostAddress, LimelightStreamConfiguration ^streamConfig)
{
	STREAM_CONFIGURATION config;
	DECODER_RENDERER_CALLBACKS drCallbacks;

	config.width = streamConfig->GetWidth();
	config.height = streamConfig->GetHeight();
	config.fps = streamConfig->GetFps();
	
	//DecoderRendererCallbacks = decoderRenderer;

	drCallbacks.setup = DrSetup;
	drCallbacks.start = DrStart;
	drCallbacks.stop = DrStop;
	drCallbacks.release = DrRelease;
	drCallbacks.submitDecodeUnit = DrSubmitDecodeUnit;


	return LiStartConnection(hostAddress, &config, NULL, NULL, 0);
}

void LimelightCommonRuntimeComponent::StopConnection(void)
{
	LiStopConnection();
}

