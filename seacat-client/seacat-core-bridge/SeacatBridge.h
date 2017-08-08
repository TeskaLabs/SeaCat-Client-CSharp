#pragma once

namespace seacat_core_bridge
{
	public interface class ISeacatCoreAPI
	{
	public:
		virtual void LogMessage(Platform::IntPtr message);
	};

	public ref class SeacatBridge sealed
	{
	public:
		SeacatBridge();

		void init(ISeacatCoreAPI^ coreAPI);
	};
}