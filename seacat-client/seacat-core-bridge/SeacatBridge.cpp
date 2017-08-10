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
	coreAPI->LogMessage(level, StringFromAscIIChars(message));
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

int SeacatBridge::run() {
	return 0;
}

int SeacatBridge::shutdown() {
	return 0;
}

int SeacatBridge::yield(char16 what) {
	return 0;
}

String^ SeacatBridge::state() {
	return "";
}

void SeacatBridge::ppkgen_worker() {

}

int SeacatBridge::csrgen_worker(const Platform::Array<String^>^  params) {
	return 0;
}

int SeacatBridge::set_proxy_server_worker(String^ proxy_host, String^ proxy_port) {
	return 0;
}

// This is thread-safe (but quite expensive) method to obtain current time in format used by SeaCatCC event loop
double SeacatBridge::time() {
	return 0;
}

int SeacatBridge::log_set_mask(int64 bitmask) {
	return 0;
}

int SeacatBridge::socket_configure_worker(int port, char16 domain, char16 type, int protocol, String^ peer_address, String^ peer_port) {
	return 0;
}

String^ SeacatBridge::client_id() {
	return "";
}

String^  SeacatBridge::client_tag() {
	return "";
}

int SeacatBridge::capabilities_store(const Platform::Array<String^>^  capabilities) {
	return 0;
}