﻿#pragma once

namespace seacat_core_bridge
{
	using namespace Platform;

	[Windows::Foundation::Metadata::WebHostHidden]
	public interface class ISeacatCoreAPI
	{
	public:
		virtual void LogMessage(char16 level, Platform::String^ message);

		virtual void CallbackWorkerRequest(char16 worker);
	};

	public ref class SeacatBridge sealed
	{
	public:
		SeacatBridge();

		int init(ISeacatCoreAPI^ coreAPI, String^ appId, String^ appIdSuffix, String^ platform, String^ varDirChar);

		int run();

		int shutdown();

		int yield(char16 what);

		String^ state();

		void ppkgen_worker();

		int csrgen_worker(const Platform::Array<String^>^  params);

		int set_proxy_server_worker(String^ proxy_host, String^ proxy_port);

		// This is thread-safe (but quite expensive) method to obtain current time in format used by SeaCatCC event loop
		double time();

		int log_set_mask(int64 bitmask);

		int socket_configure_worker(int port, char16 domain, char16 type, int protocol, String^ peer_address, String^ peer_port);

		String^ client_id();
		
		String^  client_tag();

		int capabilities_store(const Platform::Array<String^>^  capabilities);
	};
}