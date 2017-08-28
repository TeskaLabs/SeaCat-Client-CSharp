#ifndef _SEACATCC_ALL_WINDOWS_H_
#define _SEACATCC_ALL_WINDOWS_H_

#ifdef SEACATCC_EXPORTS
#define SEACATCC_API __declspec(dllexport)
#elif _WINRT_DLL
#define SEACATCC_API extern
#else
#define SEACATCC_API __declspec(dllimport)
#endif

// MSVC2013 don't understand these
#define inline __inline
#define __attribute__(A) /* do nothing */
#define mode_t int
#define PATH_MAX FILENAME_MAX
#define __func__ __FUNCTION__

//TODO: Windows 10 have a working mmap() implementation, this should apply only for Windows Phone 8 and older windows.
#define NO_MMAP 1

#define strerror_r(errno,buf,len) strerror_s(buf,len,errno)

#define WIN32_LEAN_AND_MEAN

#include <Windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdint.h>
#include <io.h>
#include <malloc.h> 
#include <errno.h>


#if (WINAPI_FAMILY != WINAPI_FAMILY_PHONE_APP)
#include <Wincrypt.h>
#include <iphlpapi.h>
#endif

#include <stdio.h>  
#include <stdarg.h> // for va_list, va_start  
#include <string.h> // for memset  

/* Values for the second argument to access.
   These may be OR'd together.  */
#define R_OK    4       /* Test for read permission.  */
#define W_OK    2       /* Test for write permission.  */
//#define   X_OK    1       /* execute permission - unsupported in windows*/
#define F_OK    0       /* Test for existence.  */

/* Event flag definitions for WSAPoll(). */

#define POLLRDNORM  0x0100
#define POLLRDBAND  0x0200
#define POLLIN      (POLLRDNORM | POLLRDBAND)
#define POLLPRI     0x0400
#define POLLWRNORM  0x0010
#define POLLOUT     (POLLWRNORM)
#define POLLWRBAND  0x0020
#define POLLERR     0x0001
#define POLLHUP     0x0002
#define POLLNVAL    0x0004

//
// Flags for getnameinfo()
//

#define NI_NOFQDN       0x01  /* Only return nodename portion for local hosts */
#define NI_NUMERICHOST  0x02  /* Return numeric form of the host's address */
#define NI_NAMEREQD     0x04  /* Error if the host's name not in DNS */
#define NI_NUMERICSERV  0x08  /* Return numeric form of the service (port #) */
#define NI_DGRAM        0x10  /* Service is a datagram service */

#define NI_MAXHOST      1025  /* Max size of a fully-qualified domain name */
#define NI_MAXSERV      32    /* Max size of a service name */

// definitions from UNIX sockets
#define AI_PASSIVE                  0x00000001  // Socket address will be used in bind() call
#define AI_CANONNAME                0x00000002  // Return canonical name in first ai_canonname
#define AI_NUMERICHOST              0x00000004  // Nodename must be a numeric address string
#define AI_NUMERICSERV              0x00000008  // Servicename must be a numeric port number

#define AI_ALL                      0x00000100  // Query both IP6 and IP4 with AI_V4MAPPED
#define AI_ADDRCONFIG               0x00000400  // Resolution only if global address configured
#define AI_V4MAPPED                 0x00000800  // On v6 failure, query v4 and convert to V4MAPPED format

#define AI_DEFAULT (AI_V4MAPPED | AI_ADDRCONFIG)

#define AI_NON_AUTHORITATIVE        0x00004000  // LUP_NON_AUTHORITATIVE
#define AI_SECURE                   0x00008000  // LUP_SECURE
#define AI_RETURN_PREFERRED_NAMES   0x00010000  // LUP_RETURN_PREFERRED_NAMES

#define AI_FQDN                     0x00020000  // Return the FQDN in ai_canonname
#define AI_FILESERVER               0x00040000  // Resolving fileserver name resolution
#define AI_DISABLE_IDN_ENCODING     0x00080000  // Disable Internationalized Domain Names handling


#define SHUT_RD   0x00
#define SHUT_WR   0x01
#define SHUT_RDWR 0x02


#define EHOSTDOWN 211

#define INET_ADDRSTRLEN  22
#define INET6_ADDRSTRLEN 65

#define ssize_t SSIZE_T

#include "mman.h"

// definitions for file locks
#define   LOCK_SH	      0x01	/* shared file lock */
#define   LOCK_EX	      0x02	/* exclusive file lock */
#define   LOCK_NB	      0x04	/* do not block	when locking */
#define   LOCK_UN	      0x08	/* unlock file */

typedef unsigned int sa_family_t;

struct sockaddr_un {
	sa_family_t sun_family;               /* AF_UNIX */
	char        sun_path[108];            /* pathname */
};

#define MAXPATHLEN MAX_PATH

#include "fcntl.h"
#define F_GETFL		3
#define F_SETFL		6
#define O_NONBLOCK	0x2000


#define	S_IRWXU	0000700			/* RWX mask for owner */
#define	S_IRUSR	0000400			/* R for owner */
#define	S_IWUSR	0000200			/* W for owner */
#define	S_IXUSR	0000100			/* X for owner */

#define	S_IRWXG	0000070			/* RWX mask for group */
#define	S_IRGRP	0000040			/* R for group */
#define	S_IWGRP	0000020			/* W for group */
#define	S_IXGRP	0000010			/* X for group */

#define	S_IRWXO	0000007			/* RWX mask for other */
#define	S_IROTH	0000004			/* R for other */
#define	S_IWOTH	0000002			/* W for other */
#define	S_IXOTH	0000001			/* X for other */

#define S_ISREG(m) (((m) & S_IFMT) == S_IFREG)
#define S_ISDIR(m) (((m) & S_IFMT) == S_IFDIR)

///
#if (WINAPI_FAMILY == WINAPI_FAMILY_PHONE_APP)
const char *inet_ntop(int af, const void *src, char *dst, size_t size);
int inet_pton(int af, const char *src, void *dst);
int inet_pton4(const char *src, u_char *dst, int pton);
int inet_pton6(const char *src, u_char *dst);
#endif
///

__inline int seacatcc_vsnprintf(char *outBuf, size_t size, const char *format, va_list ap)
{
    int count = -1;

    if (size != 0)
        count = _vsnprintf_s(outBuf, size, _TRUNCATE, format, ap);
    if (count == -1)
        count = _vscprintf(format, ap);

    return count;
}

__inline int snprintf(char *outBuf, size_t size, const char *format, ...)
{
    int count;
    va_list ap;

    va_start(ap, format);
    count = seacatcc_vsnprintf(outBuf, size, format, ap);
    va_end(ap);

    return count;
}

///

__inline void milisleep(int milisec)
{
    Sleep(milisec);
}

int flock (int fd, int operation);

#endif //_SEACATCC_ALL_WINDOWS_H_
