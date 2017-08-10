#pragma once
#include "pch.h"
#include <String>
using namespace Platform;

String^ StringFromAscIIChars(const char* chars)
{
	size_t newsize = strlen(chars) + 1;
	wchar_t * wcstring = new wchar_t[newsize];
	size_t convertedChars = 0;
	mbstowcs_s(&convertedChars, wcstring, newsize, chars, _TRUNCATE);
	String^ str = ref new Platform::String(wcstring);
	delete[] wcstring;
	return str;
}

std::string ConstCharFromString(String^ str) {
	std::wstring wstr(str->Begin());
	std::string sstr(wstr.begin(), wstr.end());
	return sstr;
}
