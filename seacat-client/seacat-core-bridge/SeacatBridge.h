#pragma once

namespace seacat_core_bridge
{
	using namespace Platform;

	[Windows::Foundation::Metadata::WebHostHidden]
	public interface class ISeacatCoreAPI
	{
	public:
		virtual void LogMessage(Platform::String^ message);
	};

	public ref class SeacatBridge sealed
	{
	public:
		SeacatBridge();

		void init(ISeacatCoreAPI^ coreAPI, String^ appId, String^ appIdSuffix, String^ platform, String^ varDirChar);
	};
}