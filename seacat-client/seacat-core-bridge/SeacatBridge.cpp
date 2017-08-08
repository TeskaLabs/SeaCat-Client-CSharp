// Class1.cpp
#include "pch.h"
#include "SeacatBridge.h"

extern "C" {
#include "alts/windows/all_windows.h"
#define SEACATCC_API extern
#include "seacatcc.h"
}

using namespace seacat_core_bridge;
using namespace Platform;

ISeacatCoreAPI^ coreAPI = nullptr;

extern "C" {

	void logMsg(char level, const char * message) {
		coreAPI->LogMessage(Platform::IntPtr(&message));
	}

	void initSeacat() {
		seacatcc_log_setfnct(&logMsg);
	}
}


SeacatBridge::SeacatBridge()
{
}

void SeacatBridge::init(ISeacatCoreAPI^ coreAPI) {
	::coreAPI = coreAPI;
	initSeacat();
}