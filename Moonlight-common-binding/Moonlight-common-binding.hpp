#pragma once
#include <Limelight.h>
#include <string.h>

typedef unsigned char byte;

namespace Moonlight_common_binding
{
	public ref class MoonlightStreamConfiguration sealed
	{
	public:
		MoonlightStreamConfiguration(int width, int height, int fps, int bitrate, int packetSize,
			const Platform::Array<unsigned char> ^riAesKey, const Platform::Array<unsigned char> ^riAesIv) :
			m_Width(width), m_Height(height), m_Fps(fps), m_Bitrate(bitrate), m_PacketSize(packetSize)
		{
			memcpy(m_riAesKey, riAesKey->Data, sizeof(m_riAesKey));
			memcpy(m_riAesIv, riAesIv->Data, sizeof(m_riAesIv));
		}

		int GetWidth(void) {
			return m_Width;
		}
		int GetHeight(void) {
			return m_Height;
		}
		int GetFps(void) {
			return m_Fps;
		}
		int GetBitrate(void) {
			return m_Bitrate;
		}
		int GetPacketSize(void) {
			return m_PacketSize;
		}
		Platform::Array<unsigned char>^ GetRiAesKey(void) {
			return ref new Platform::Array<byte>(m_riAesKey, sizeof(m_riAesKey));
		}
		Platform::Array<unsigned char>^ GetRiAesIv(void) {
			return ref new Platform::Array<byte>(m_riAesIv, sizeof(m_riAesIv));
		}

	private:
		int m_Width;
		int m_Height;
		int m_Fps;
		int m_Bitrate;
		int m_PacketSize;
		byte m_riAesKey[16];
		byte m_riAesIv[16];
	};

	public delegate void DrSetup(int width, int height, int redrawRate, int drFlags);
	public delegate void DrCleanup(void);
	public delegate int DrSubmitDecodeUnit(const Platform::Array<unsigned char> ^data);

	public ref class MoonlightDecoderRenderer sealed
	{
	public:
		MoonlightDecoderRenderer(DrSetup ^drSetup, DrCleanup ^drCleanup,
			DrSubmitDecodeUnit ^drSubmitDecodeUnit) :
			m_DrSetup(drSetup), m_DrCleanup(drCleanup), m_DrSubmitDecodeUnit(drSubmitDecodeUnit) {}

		void Setup(int width, int height, int redrawRate, int drFlags) {
			m_DrSetup(width, height, redrawRate, drFlags);
		}
		void Cleanup(void) {
			m_DrCleanup();
		}
		int SubmitDecodeUnit(const Platform::Array<byte> ^dataArray) {
			return m_DrSubmitDecodeUnit(dataArray);
		}

	private:
		Moonlight_common_binding::DrSetup ^m_DrSetup;
		Moonlight_common_binding::DrCleanup ^m_DrCleanup;
		Moonlight_common_binding::DrSubmitDecodeUnit ^m_DrSubmitDecodeUnit;
	};

	public delegate void ArInit(void);
	public delegate void ArCleanup(void);
	public delegate void ArPlaySample(const Platform::Array<unsigned char> ^data);

	public ref class MoonlightAudioRenderer sealed
	{
	public:
		MoonlightAudioRenderer(ArInit ^arInit, ArCleanup ^arCleanup, ArPlaySample ^arPlaySample) :
			m_ArInit(arInit), m_ArCleanup(arCleanup),
			m_ArPlaySample(arPlaySample) {}

		void Init(void) {
			m_ArInit();
		}
		void Cleanup(void) {
			m_ArCleanup();
		}
		void PlaySample(const Platform::Array<byte> ^dataArray) {
			m_ArPlaySample(dataArray);
		}

	private:
		Moonlight_common_binding::ArInit ^m_ArInit;
		Moonlight_common_binding::ArCleanup ^m_ArCleanup;
		Moonlight_common_binding::ArPlaySample ^m_ArPlaySample;
	};

	public delegate void ClStageStarting(int stage);
	public delegate void ClStageComplete(int stage);
	public delegate void ClStageFailed(int stage, int errorCode);
	public delegate void ClConnectionStarted(void);
	public delegate void ClConnectionTerminated(int errorCode);
	public delegate void ClDisplayMessage(Platform::String ^message);
	public delegate void ClDisplayTransientMessage(Platform::String ^message);

	public ref class MoonlightConnectionListener sealed
	{
	public:
		MoonlightConnectionListener(ClStageStarting ^clStageStarting, ClStageComplete ^clStageComplete,
			ClStageFailed ^clStageFailed, ClConnectionStarted ^clConnectionStarted,
			ClConnectionTerminated ^clConnectionTerminated, ClDisplayMessage ^clDisplayMessage,
			ClDisplayTransientMessage ^clDisplayTransientMessage) : m_ClStageStarting(clStageStarting),
			m_ClStageComplete(clStageComplete), m_ClStageFailed(clStageFailed), m_ClConnectionStarted(clConnectionStarted),
			m_ClConnectionTerminated(clConnectionTerminated), m_ClDisplayMessage(clDisplayMessage),
			m_ClDisplayTransientMessage(clDisplayTransientMessage) {}

		void StageStarting(int stage) {
			m_ClStageStarting(stage);
		}
		void StageComplete(int stage) {
			m_ClStageComplete(stage);
		}
		void StageFailed(int stage, int errorCode) {
			m_ClStageFailed(stage, errorCode);
		}
		void ConnectionStarted(void) {
			m_ClConnectionStarted();
		}
		void ConnectionTerminated(int errorCode) {
			m_ClConnectionTerminated(errorCode);
		}
		void DisplayMessage(Platform::String ^message) {
			m_ClDisplayMessage(message);
		}
		void DisplayTransientMessage(Platform::String ^message) {
			m_ClDisplayTransientMessage(message);
		}

	private:
		Moonlight_common_binding::ClStageStarting ^m_ClStageStarting;
		Moonlight_common_binding::ClStageComplete ^m_ClStageComplete;
		Moonlight_common_binding::ClStageFailed ^m_ClStageFailed;
		Moonlight_common_binding::ClConnectionStarted ^m_ClConnectionStarted;
		Moonlight_common_binding::ClConnectionTerminated ^m_ClConnectionTerminated;
		Moonlight_common_binding::ClDisplayMessage ^m_ClDisplayMessage;
		Moonlight_common_binding::ClDisplayTransientMessage ^m_ClDisplayTransientMessage;
	};

	public enum class MouseButtonAction : int {
		Press = 0x07,
		Release = 0x08
	};

	public enum class MouseButton : int {
		Left = 0x01,
		Middle = 0x02,
		Right = 0x03
	};

	public enum class KeyAction : int {
		Down = 0x03,
		Up = 0x04
	};

	public enum class Modifier : int {
		ModifierShift = 0x01,
		ModifierCtrl = 0x02,
		ModifierAlt = 0x04
	};

	public enum class ButtonFlags : int {
		A = 0x1000,
		B = 0x2000,
		X = 0x4000,
		Y = 0x8000,
		Up = 0x0001,
		Down = 0x0002,
		Left = 0x0004,
		Right = 0x0008,
		LB = 0x0100,
		RB = 0x0200,
		Play = 0x0010,
		Back = 0x0020,
		LS = 0x0040,
		RS = 0x0080,
		Special = 0x0400
	};

	public ref class MoonlightCommonRuntimeComponent sealed
	{
	public:
		static int StartConnection(Platform::String^ host, MoonlightStreamConfiguration ^streamConfig,
			MoonlightConnectionListener ^clCallbacks, MoonlightDecoderRenderer ^drCallbacks, MoonlightAudioRenderer ^arCallbacks,
			int serverMajorVersion);

		static void StopConnection(void);
		static int SendMouseMoveEvent(short deltaX, short deltaY);
		static int SendMouseButtonEvent(unsigned char action, int button);
		static int SendKeyboardEvent(short keyCode, unsigned char keyAction, unsigned char modifiers);
		static int SendControllerInput(short buttonFlags, byte leftTrigger, byte rightTrigger, short leftStickX, 
			short leftStickY, short rightStickX, short rightStickY);
		static int SendMultiControllerInput(short controllerNumber, short buttonFlags, byte leftTrigger, byte rightTrigger, short leftStickX,
			short leftStickY, short rightStickX, short rightStickY);
		static int SendScrollEvent(short scrollClicks);
	};
}