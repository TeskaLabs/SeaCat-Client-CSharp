#pragma once
#include <collection.h>
#include <ppltasks.h>

namespace SeaCatCSharpBridge
{
	using namespace Platform;

	/**
	* Wrapper for ByteBuffer class, used mainly in the bridge
	*/
	public ref class ByteBuffWrapper sealed {
	public:
		property Platform::Array<byte>^ data;
		property int position;
		property int limit;
		property int capacity;
	};

	/**
	* Interface used for bridge to communicate with the client
	*/
	public interface class ISeacatCoreAPI
	{
	public:
		virtual void LogMessage(char16 level, Platform::String^ message);

		virtual ByteBuffWrapper^ CallbackWriteReady();

		virtual ByteBuffWrapper^ CallbackReadReady();

		virtual void CallbackFrameReceived(ByteBuffWrapper^ frame, int frameLength);

		virtual void CallbackFrameReturn(ByteBuffWrapper^ frame);

		virtual void CallbackWorkerRequest(char16 worker);

		virtual double CallbackEvLoopHeartBeat(double now);

		virtual void CallbackEvloopStarted();

		virtual void CallbackGwconnReset();

		virtual void CallbackGwconnConnected();
		
		virtual void CallbackStateChanged(String^ state);

		virtual void CallbackClientidChanged(String^ clientId, String^ clientTag);

	};

	/**
	* Class for bridge that serves as a communication layer between C# and unmanaged C/C++
	*/
	public ref class SeacatBridge sealed
	{
	public:
		SeacatBridge();

		/**
		* ~ seacatcc_init
		*/
		int init(ISeacatCoreAPI^ coreAPI, String^ appId, String^ appIdSuffix, String^ platform, String^ varDirChar);

		/**
		* ~ seacatcc_run
		*/
		int run();

		/**
		* ~ seacatcc_shutdown
		*/
		int shutdown();

		/**
		* ~ seacatcc_yield
		*/
		int yield(char16 what);	

		/**
		* ~ seacatcc_state
		*/
		String^ state();

		/**
		* ~ seacatcc_ppkgen_worker
		*/
		void ppkgen_worker();

		/**
		* ~ seacatcc_csrgen_worker
		*/
		int csrgen_worker(const Platform::Array<String^>^  params);

		/**
		* ~ seacatcc_set_proxy_server_worker
		*/
		int set_proxy_server_worker(String^ proxy_host, String^ proxy_port);

		/**
		* ~ seacatcc_time
		*/
		double time();

		/**
		* ~ seacatcc_log_set_mask
		*/
		int log_set_mask(int64 bitmask);

		/**
		* ~ socket_configure_worker
		*/
		int socket_configure_worker(int port, char16 domain, char16 type, int protocol, String^ peer_address, String^ peer_port);

		/**
		* ~ seacatcc_client_id
		*/
		String^ client_id();
		
		/**
		* ~ seacatcc_client_tag
		*/
		String^  client_tag();

		/**
		* seacatcc_characteristics_store
		*/
		int characteristics_store(const Platform::Array<String^>^  capabilities);
	};
}