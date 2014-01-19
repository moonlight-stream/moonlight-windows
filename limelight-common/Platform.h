#pragma once

#ifdef _WIN32
#include <Windows.h>
#else
#include <unistd.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#endif

#define Sleep(ms)        { \
	HANDLE sleepEvent = CreateEventEx(NULL, NULL, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS); \
if (!sleepEvent) return; \
	WaitForSingleObjectEx(sleepEvent, ms, FALSE); \
}
