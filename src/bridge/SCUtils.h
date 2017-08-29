#pragma once
#include <String>
#include <collection.h>
#include <ppltasks.h>

using namespace Platform;

String^ StringFromAscIIChars(char charBuff[]) {
	std::string s_str = std::string(charBuff);
	std::wstring wid_str = std::wstring(s_str.begin(), s_str.end());
	const wchar_t* w_char = wid_str.c_str();
	Platform::String^ p_string = ref new Platform::String(w_char);
	return p_string;
}

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

std::string* ConstCharFromString(String^ str) {
	std::wstring wstr(str->Begin());
	return new std::string (wstr.begin(), wstr.end());
}
