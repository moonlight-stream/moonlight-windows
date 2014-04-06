#pragma once
#include <Limelight.h>

typedef unsigned char byte;

namespace Limelight_common_binding
{
	public ref class LimelightStreamConfiguration sealed
	{
	public:
		LimelightStreamConfiguration(int width, int height, int fps) :
			m_Width(width), m_Height(height), m_Fps(fps) {}

		int GetWidth(void) {
			return m_Width;
		}
		int GetHeight(void) {
			return m_Height;
		}
		int GetFps(void) {
			return m_Fps;
		}

	private:
		int m_Width;
		int m_Height;
		int m_Fps;
	};

	public delegate void DrSetup(int width, int height, int redrawRate, int drFlags);
	public delegate void DrStart(void);
	public delegate void DrStop(void);
	public delegate void DrRelease(void);
	public delegate void DrSubmitDecodeUnit(const Platform::Array<unsigned char> ^data);

	public ref class LimelightDecoderRenderer sealed
	{
	public:
		LimelightDecoderRenderer(DrSetup ^drSetup, DrStart ^drStart, DrStop ^drStop,
			DrRelease ^drRelease, DrSubmitDecodeUnit ^drSubmitDecodeUnit) :
			m_DrSetup(drSetup), m_DrStart(drStart), m_DrStop(drStop),
			m_DrRelease(drRelease), m_DrSubmitDecodeUnit(drSubmitDecodeUnit) {}

		void Setup(int width, int height, int redrawRate, int drFlags) {
			m_DrSetup(width, height, redrawRate, drFlags);
		}
		void Start(void) {
			m_DrStart();
		}
		void Stop(void) {
			m_DrStop();
		}
		void Destroy(void) {
			m_DrRelease();
		}
		void SubmitDecodeUnit(const Platform::Array<byte> ^dataArray) {
			m_DrSubmitDecodeUnit(dataArray);
		}

	private:
		Limelight_common_binding::DrSetup ^m_DrSetup;
		Limelight_common_binding::DrStart ^m_DrStart;
		Limelight_common_binding::DrStop ^m_DrStop;
		Limelight_common_binding::DrRelease ^m_DrRelease;
		Limelight_common_binding::DrSubmitDecodeUnit ^m_DrSubmitDecodeUnit;
	};

	public delegate void ArInit(void);
	public delegate void ArStart(void);
	public delegate void ArStop(void);
	public delegate void ArRelease(void);
	public delegate void ArPlaySample(const Platform::Array<unsigned char> ^data);

	public ref class LimelightAudioRenderer sealed
	{
	public:
		LimelightAudioRenderer(ArInit ^arInit, ArStart ^arStart, ArStop ^arStop,
			ArRelease ^arRelease, ArPlaySample ^arPlaySample) :
			m_ArInit(arInit), m_ArStart(arStart), m_ArStop(arStop), m_ArRelease(arRelease),
			m_ArPlaySample(arPlaySample) {}

		void Init(void) {
			m_ArInit();
		}
		void Start(void) {
			m_ArStart();
		}
		void Stop(void) {
			m_ArStop();
		}
		void Destroy(void) {
			m_ArRelease();
		}
		void PlaySample(const Platform::Array<byte> ^dataArray) {
			m_ArPlaySample(dataArray);
		}

	private:
		Limelight_common_binding::ArInit ^m_ArInit;
		Limelight_common_binding::ArStart ^m_ArStart;
		Limelight_common_binding::ArStop ^m_ArStop;
		Limelight_common_binding::ArRelease ^m_ArRelease;
		Limelight_common_binding::ArPlaySample ^m_ArPlaySample;
	};

	public delegate void ClStageStarting(int stage);
	public delegate void ClStageComplete(int stage);
	public delegate void ClStageFailed(int stage, int errorCode);
	public delegate void ClConnectionStarted(void);
	public delegate void ClConnectionTerminated(int errorCode);
	public delegate void ClDisplayMessage(Platform::String ^message);
	public delegate void ClDisplayTransientMessage(Platform::String ^message);

	public ref class LimelightConnectionListener sealed
	{
	public:
		LimelightConnectionListener(ClStageStarting ^clStageStarting, ClStageComplete ^clStageComplete,
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
		Limelight_common_binding::ClStageStarting ^m_ClStageStarting;
		Limelight_common_binding::ClStageComplete ^m_ClStageComplete;
		Limelight_common_binding::ClStageFailed ^m_ClStageFailed;
		Limelight_common_binding::ClConnectionStarted ^m_ClConnectionStarted;
		Limelight_common_binding::ClConnectionTerminated ^m_ClConnectionTerminated;
		Limelight_common_binding::ClDisplayMessage ^m_ClDisplayMessage;
		Limelight_common_binding::ClDisplayTransientMessage ^m_ClDisplayTransientMessage;
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

	public ref class LimelightCommonRuntimeComponent sealed
	{
	public:
		static int StartConnection(unsigned int hostAddress, LimelightStreamConfiguration ^streamConfig,
			LimelightConnectionListener ^clCallbacks, LimelightDecoderRenderer ^drCallbacks, LimelightAudioRenderer ^arCallbacks);
		static void StopConnection(void);

		static int SendMouseMoveEvent(short deltaX, short deltaY);

		static int SendMouseButtonEvent(unsigned char action, int button);
		static int SendKeyboardEvent(short keyCode, unsigned char keyAction, unsigned char modifiers);
	};
}