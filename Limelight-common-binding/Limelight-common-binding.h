#pragma once

#include <Limelight.h>

namespace Limelight_common_binding
{
	public ref class LimelightStreamConfiguration sealed
	{
	public:
		LimelightStreamConfiguration(int width, int height, int fps);

		int GetWidth(void);
		int GetHeight(void);
		int GetFps(void);

	private:
		int m_Width;
		int m_Height;
		int m_Fps;
	};

	/*public class ILimelightDecoderRenderer
	{
	public:
		virtual ~ILimelightDecoderRenderer() {}

		virtual void Setup(int width, int height, int redrawRate, int drFlags) = 0;
		virtual void Start(void) = 0;
		virtual void Stop(void) = 0;
		virtual void Release(void) = 0;
		virtual void SubmitDecodeUnit(char* data, int size) = 0;
	};*/

	public ref class LimelightCommonRuntimeComponent sealed
	{
	public:
		static int StartConnection(unsigned int hostAddress, LimelightStreamConfiguration ^streamConfig);
		static void StopConnection(void);
	};
}