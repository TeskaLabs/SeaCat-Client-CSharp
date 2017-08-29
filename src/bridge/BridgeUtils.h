#pragma once
#include <collection.h>
#include <ppltasks.h>
#include <String>

using namespace Platform;

/**
* Converts ASCII const char array to String
*/
String^ CharToStr(char charBuff[]) {
	std::string s_str = std::string(charBuff);
	std::wstring wid_str = std::wstring(s_str.begin(), s_str.end());
	const wchar_t* w_char = wid_str.c_str();
	String^ p_string = ref new String(w_char);
	return p_string;
}

/**
* Converts ASCII char to String
*/
String^ CharToStr(const char* chars)
{
	// transform char* to wchar*
	size_t newsize = strlen(chars) + 1;
	wchar_t * wcstring = new wchar_t[newsize];
	size_t convertedChars = 0;
	// transform wchar to string
	mbstowcs_s(&convertedChars, wcstring, newsize, chars, _TRUNCATE);
	String^ str = ref new Platform::String(wcstring);
	// delete temp wchar
	delete[] wcstring;
	return str;
}

/**
* Converts managed string to unmanaged string
*/
std::string* StringToUnmanaged(String^ str) {
	std::wstring wstr(str->Begin());
	return new std::string (wstr.begin(), wstr.end());
}

/**
* Converts string array to unmanaged const char**
*/
const char** StringArrayToUnmanaged(const Platform::Array<String^>^  arr) {
	const char** cCharArr = new const char*[arr->Length + 1];

	for (unsigned int i = 0; i<arr->Length; i++)
	{
		cCharArr[i] = StringToUnmanaged(arr[i])->c_str();
	}

	// last element must be null
	cCharArr[arr->Length] = NULL;
	return cCharArr;
}
