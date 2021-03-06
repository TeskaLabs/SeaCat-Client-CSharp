﻿// Class1.cpp
#include <collection.h>
#include <ppltasks.h>
#include "SeacatBridge.h"
#include <string>
#include "BridgeUtils.h"

// include seacat as C source
extern "C" {
#include "all_windows.h"
#include "seacatcc.h"
}

using namespace SeaCatCSharpBridge;
using namespace Platform;
using namespace std;

// ===================================== C <-> C++ METHODS =====================================

ISeacatCoreAPI^ coreAPI = nullptr;
SeacatBridge^ bridge = nullptr;

// last used write buffer
static ByteBuffWrapper^ writeBuffer = nullptr;
// pointer to last used data of the write buffer
void* writeBufferDataPtr = nullptr;
// last used read buffer
static ByteBuffWrapper^ readBuffer = nullptr;
// pointer to last used data of the read buffer
void* readBufferDataPtr = nullptr;

static void logMsgManaged(char level, const char* message) {
	coreAPI->LogMessage(level, CharToStr(message));
}

static void callback_write_ready(void ** data, uint16_t * data_len) {
	assert(writeBuffer == nullptr);

	// call the client and obtain data
	writeBuffer = coreAPI->CallbackWriteReady();

	// extract pointer to data and pass it to the output parameter
	if (writeBuffer != nullptr) {
		auto length = writeBuffer->limit - writeBuffer->position;

		*data = writeBuffer->data->Data + writeBuffer->position;
		*data_len = writeBuffer->limit - writeBuffer->position;
	}
	else {
		*data = NULL;
		*data_len = 0;
	}

	writeBufferDataPtr = *data;
}

static void callback_read_ready(void ** data, uint16_t * data_len) {
	assert(readBuffer == nullptr);

	// call the client and obtain data
	readBuffer = coreAPI->CallbackReadReady();
	// read buffer must always start at 0
	assert(readBuffer->position == 0);
	*data = readBuffer->data->Data + readBuffer->position;
	*data_len = readBuffer->capacity - readBuffer->position;

	readBufferDataPtr = *data;
}

static void callback_frame_received(void * data, uint16_t data_len) {
	assert(readBuffer != nullptr);
	// pass data to client for reading
	coreAPI->CallbackFrameReceived(readBuffer, data_len);
	readBuffer = nullptr;
	readBufferDataPtr = nullptr;
}

static void callback_frame_return(void * data) {

	// choose between read and write frame
	if (readBuffer != nullptr && readBufferDataPtr != nullptr && data == readBufferDataPtr) {
		coreAPI->CallbackFrameReturn(readBuffer);
		readBuffer = nullptr;
		readBufferDataPtr = nullptr;
	}
	else if (writeBuffer != nullptr && writeBufferDataPtr != nullptr && data == writeBufferDataPtr) {
		coreAPI->CallbackFrameReturn(writeBuffer);
		writeBuffer = nullptr;
		writeBufferDataPtr = nullptr;
	}
	else {
		logMsgManaged('E', "Unknown frame!!!");
	}
}

static void callback_worker_request(char worker) {
	char16 workerChr = worker;
	coreAPI->CallbackWorkerRequest(workerChr);
}

static double callback_evloop_heartbeat(double now) {
	double ret = coreAPI->CallbackEvLoopHeartBeat(now);
	assert(ret > 0);
	return ret;
}

// other hooks
static void callback_evloop_started(void) {
	coreAPI->CallbackEvloopStarted();
}


static void callback_gwconn_reset(void) {
	coreAPI->CallbackGwconnReset();
}

static void callback_gwconn_connected(void) {
	coreAPI->CallbackGwconnConnected();
}

static void callback_state_changed(void) {
	// obtain the state and pass it to the client
	char* buffer = new char[SEACATCC_STATE_BUF_SIZE];
	seacatcc_state(buffer);
	auto str = CharToStr(buffer);
	coreAPI->CallbackStateChanged(str);
	delete[] buffer;
}

static void callback_clientid_changed(void) {
	auto clientId = seacatcc_client_id();
	auto clientTag = seacatcc_client_tag();
	auto clientIdStr = CharToStr(clientId);
	auto clientTagStr = CharToStr(clientTag);

	coreAPI->CallbackClientidChanged(clientIdStr, clientTagStr);
}

// ===================================== C++ <-> C# METHODS =====================================


SeacatBridge::SeacatBridge() {
	bridge = this;
}

int SeacatBridge::init(ISeacatCoreAPI^ coreAPI, String^ appId, String^ appIdSuffix, String^ platform, String^ varDirChar) {
	::coreAPI = coreAPI;

	auto appIdCst = StringToUnmanaged(appId)->c_str();
	auto appIdSuffixCst = appIdSuffix->IsEmpty() ? NULL : StringToUnmanaged(appIdSuffix)->c_str();
	auto platformCst = StringToUnmanaged(platform)->c_str();
	auto varDirCharCst = StringToUnmanaged(varDirChar)->c_str();

	// map log callback
	seacatcc_log_setfnct(&logMsgManaged);

	// initialize seacat
	int rc = seacatcc_init(appIdCst, appIdSuffixCst, platformCst, varDirCharCst,
		callback_write_ready,
		callback_read_ready,
		callback_frame_received,
		callback_frame_return,
		callback_worker_request,
		callback_evloop_heartbeat
	);

	assert(rc == SEACATCC_RC_OK);

	// register hooks
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


	return rc;
}

int SeacatBridge::run() {
	return seacatcc_run();
}

int SeacatBridge::shutdown() {
	return seacatcc_shutdown();
}

int SeacatBridge::yield(char16 what) {
	// 8 bit max
	if (what > 0xFF) return SEACATCC_RC_E_GENERIC;
	char whatCh = (char)what;
	return seacatcc_yield(whatCh);
}

String^ SeacatBridge::state() {
	char state_buf[SEACATCC_STATE_BUF_SIZE];
	seacatcc_state(state_buf);
	return CharToStr(state_buf);
}

void SeacatBridge::ppkgen_worker() {
	return seacatcc_ppkgen_worker();
}

int SeacatBridge::csrgen_worker(const Platform::Array<String^>^  params) {
	auto csr_entries = StringArrayToUnmanaged(params);
	int rc = seacatcc_csrgen_worker(csr_entries);
	delete[] csr_entries;
	return rc;
}

int SeacatBridge::set_proxy_server_worker(String^ proxy_host, String^ proxy_port) {
	const char * proxyHostChar = StringToUnmanaged(proxy_host)->c_str();
	const char * proxyPortChar = StringToUnmanaged(proxy_port)->c_str();
	int rc = seacatcc_set_proxy_server_worker(proxyHostChar, proxyPortChar);
	return rc;
}

double SeacatBridge::time() {
	// thread - safe(but quite expensive) method to obtain current time in format used by SeaCatCC event loop
	return seacatcc_time();
}

int SeacatBridge::log_set_mask(int64 bitmask) {
	union seacatcc_log_mask_u cc_mask = {};
	cc_mask.value = bitmask;
	return seacatcc_log_set_mask(cc_mask);
}

int SeacatBridge::socket_configure_worker(int port, char16 domain, char16 type, int protocol, String^ peer_address, String^ peer_port) {

	// select domain
	int domain_int = -1;
	switch (domain) {
		case 'u': domain_int = AF_UNIX; break;
		case '4': domain_int = AF_INET; break;
		case '6': domain_int = AF_INET6; break;
	};

	if (domain_int == -1) {
		seacatcc_log('E', "Unknown/invalid domain at socket_configure_worker: '%c'", domain);
		return SEACATCC_RC_E_INVALID_ARGS;
	}

	// select socket type
	int sock_type_int = -1;

	switch (type) {
		case 's': sock_type_int = SOCK_STREAM; break;
		case 'd': sock_type_int = SOCK_DGRAM; break;
	};

	if (sock_type_int == -1) {
		seacatcc_log('E', "Unknown/invalid type at socket_configure_worker: '%c'", type);
		return SEACATCC_RC_E_INVALID_ARGS;
	}

	// configure seacat worker
	const char * peerAddressChar = StringToUnmanaged(peer_address)->c_str();
	const char * peerPortChar = StringToUnmanaged(peer_port)->c_str();
	int rc = seacatcc_socket_configure_worker(port, domain_int, sock_type_int, protocol, peerAddressChar, peerPortChar);
	return rc;
}

String^ SeacatBridge::client_id() {
	String^ result = CharToStr(seacatcc_client_id());
	return result;
}

String^  SeacatBridge::client_tag() {
	String^ result = CharToStr(seacatcc_client_tag());
	return result;
}

int SeacatBridge::characteristics_store(const Platform::Array<String^>^  capabilities) {
	auto cStore = StringArrayToUnmanaged(capabilities);
	int rc = seacatcc_characteristics_store(cStore);
	delete[] cStore;
	return rc;
}