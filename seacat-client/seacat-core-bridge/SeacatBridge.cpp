// Class1.cpp
#include "pch.h"
#include "SeacatBridge.h"
#include <string>
#include "SCUtils.h"

extern "C" {
#include "alts/windows/all_windows.h"
#define SEACATCC_API extern
#include "seacatcc.h"
}

using namespace seacat_core_bridge;
using namespace Platform;

ISeacatCoreAPI^ coreAPI = nullptr;

void logMsgManaged(char level, const char* message) {
	coreAPI->LogMessage((int)level, StringFromAscIIChars(message));
}

static void callback_write_ready(void ** data, uint16_t * data_len) {

}

static void callback_read_ready(void ** data, uint16_t * data_len) {

}

static void callback_frame_received(void * data, uint16_t data_len) {

}

static void callback_frame_return(void * data) {

}

static void callback_worker_request(char worker) {

}

static double callback_evloop_heartbeat(double now) {
	return 0;
}


// other hooks
static void callback_evloop_started(void)
{

}


static void callback_gwconn_reset(void)
{

}

static void callback_gwconn_connected(void)
{

}

static void callback_state_changed(void)
{

}

static void callback_clientid_changed(void)
{

}

extern "C" {

	void logMsg(char level, const char * message) {
		logMsgManaged(level, message);
	}

	void initSeacat(const char* appIdChar, const char* appIdSuffixChar, const char* platform, const char* varDirChar) {
		seacatcc_log_setfnct(&logMsg);
		
		int rc = seacatcc_init(appIdChar, appIdSuffixChar, platform, varDirChar,
			callback_write_ready,
			callback_read_ready,
			callback_frame_received,
			callback_frame_return,
			callback_worker_request,
			callback_evloop_heartbeat
		);

		assert(rc == SEACATCC_RC_OK);
		
		rc = seacatcc_hook_register('E', callback_evloop_started);
		assert(rc == SEACATCC_RC_OK);
		rc = seacatcc_hook_register('R', callback_gwconn_reset);
		assert(rc == SEACATCC_RC_OK);
		rc = seacatcc_hook_register('c', callback_gwconn_connected);
		assert(rc == SEACATCC_RC_OK);
		rc = seacatcc_hook_register('S', callback_state_changed);
		assert(rc == SEACATCC_RC_OK);
		rc = seacatcc_hook_register('i', callback_clientid_changed);
		assert(rc == SEACATCC_RC_OK);

		seacatcc_run();
	}
}


SeacatBridge::SeacatBridge()
{
}

void SeacatBridge::init(ISeacatCoreAPI^ coreAPI, String^ appId, String^ appIdSuffix, String^ platform, String^ varDirChar) {
	::coreAPI = coreAPI;
	auto appIdCst = ConstCharFromString(appId).c_str();
	auto appIdSuffixCst = ConstCharFromString(appIdSuffix).c_str();
	auto platformCst = ConstCharFromString(platform).c_str();
	auto varDirCharCst = ConstCharFromString(varDirChar).c_str();
	initSeacat(appIdCst, appIdSuffixCst, platformCst, varDirCharCst);
}