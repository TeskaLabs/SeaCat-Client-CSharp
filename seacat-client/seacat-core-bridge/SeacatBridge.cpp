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
using namespace std;

ISeacatCoreAPI^ coreAPI = nullptr;
SeacatBridge^ bridge = nullptr;



static void logMsgManaged(char level, const char* message) {
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


SeacatBridge::SeacatBridge()
{
	bridge = this;
}

void SeacatBridge::init(ISeacatCoreAPI^ coreAPI, String^ appId, String^ appIdSuffix, String^ platform, String^ varDirChar) {
	::coreAPI = coreAPI;
	auto appIdCst = ConstCharFromString(appId).c_str();
	auto appIdSuffixCst = ConstCharFromString(appIdSuffix).c_str();
	auto platformCst = ConstCharFromString(platform).c_str();
	auto varDirCharCst = ConstCharFromString(varDirChar).c_str();

	seacatcc_log_setfnct(&logMsgManaged);

	int rc = seacatcc_init(appIdCst, appIdSuffixCst, platformCst, varDirCharCst,
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

int SeacatBridge::run() {
	return seacatcc_run();
}

int SeacatBridge::shutdown() {
	return seacatcc_shutdown();
}

int SeacatBridge::yield(char16 what) {
	if (what > 0xFF) return SEACATCC_RC_E_GENERIC;
	return seacatcc_yield(what);
}

String^ SeacatBridge::state() {
	char state_buf[SEACATCC_STATE_BUF_SIZE];
	seacatcc_state(state_buf);
	return StringFromAscIICharws(state_buf);
}

void SeacatBridge::ppkgen_worker() {
	return seacatcc_ppkgen_worker();
}

int SeacatBridge::csrgen_worker(const Platform::Array<String^>^  params) {
	int i, rc;
	int paramCount = params->Length;
	
	const char** csr_entries = new const char*[paramCount];

	for (i = 0; i<paramCount; i++)
	{
		csr_entries[i] = ConstCharFromString(params[i]).c_str();
	}
	csr_entries[paramCount] = NULL;

	rc = seacatcc_csrgen_worker(csr_entries);

	delete[] csr_entries;
	return rc;
}

int SeacatBridge::set_proxy_server_worker(String^ proxy_host, String^ proxy_port) {
	const char * proxyHostChar = ConstCharFromString(proxy_host).c_str();
	const char * proxyPortChar = ConstCharFromString(proxy_port).c_str();
	int rc = seacatcc_set_proxy_server_worker(proxyHostChar, proxyPortChar);
	return rc;
}

// This is thread-safe (but quite expensive) method to obtain current time in format used by SeaCatCC event loop
double SeacatBridge::time() {
	return seacatcc_time();
}

int SeacatBridge::log_set_mask(int64 bitmask) {
	union seacatcc_log_mask_u cc_mask = {};
	cc_mask.value = bitmask;
	return seacatcc_log_set_mask(cc_mask);
}

int SeacatBridge::socket_configure_worker(int port, char16 domain, char16 type, int protocol, String^ peer_address, String^ peer_port) {
	int domain_int = -1;
	switch (domain)
	{
	case 'u': domain_int = AF_UNIX; break;
	case '4': domain_int = AF_INET; break;
	case '6': domain_int = AF_INET6; break;
	};
	if (domain_int == -1)
	{
		seacatcc_log('E', "Unknown/invalid domain at socket_configure_worker: '%c'", domain);
		return SEACATCC_RC_E_INVALID_ARGS;
	}

	int sock_type_int = -1;
	switch (type)
	{
	case 's': sock_type_int = SOCK_STREAM; break;
	case 'd': sock_type_int = SOCK_DGRAM; break;
	};
	if (sock_type_int == -1)
	{
		seacatcc_log('E', "Unknown/invalid type at socket_configure_worker: '%c'", type);
		return SEACATCC_RC_E_INVALID_ARGS;
	}

	const char * peerAddressChar = ConstCharFromString(peer_address).c_str();
	const char * peerPortChar = ConstCharFromString(peer_port).c_str();
	int rc = seacatcc_socket_configure_worker(port, domain_int, sock_type_int, protocol, peerAddressChar, peerPortChar);

	return rc;
}

String^ SeacatBridge::client_id() {
	String^ result = StringFromAscIIChars(seacatcc_client_id());
	return result;
}

String^  SeacatBridge::client_tag() {
	String^ result = StringFromAscIIChars(seacatcc_client_tag());
	return result;
}

int SeacatBridge::capabilities_store(const Platform::Array<String^>^  capabilities) {
	return 0;
}