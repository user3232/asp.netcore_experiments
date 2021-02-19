
# Concurrency

* https://devblogs.microsoft.com/pfxteam/awaiting-socket-operations/
* https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl
* https://stackoverflow.com/questions/12630827/using-net-4-5-async-feature-for-socket-programming/12631467#12631467



# X509Certificate

`System.Security.Cryptography.X509Certificates.X509Certificate`

It is object representing:

* Certificate.

# X509Certificate2

`System.Security.Cryptography.X509Certificates.X509Certificate2`

It is object representing:
* Certificate,
* possibly certificate private key.

# Importing certificates

* https://docs.hidglobal.com/auth-service/Content/pages/BuildingApps/Csharp/Read_different_certificate_key_file_formats_with_C_.htm
* https://stackoverflow.com/questions/50227580/create-x509certificate2-from-pem-file-in-net-core
* https://stackoverflow.com/questions/48905438/digital-signature-in-c-sharp-without-using-bouncycastle
* https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.pemfields?view=net-5.0
* https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.pemencoding.write?view=net-5.0

# BSD Sockets

References:

* An Advanced Socket Communication Tutorial
* http://users.pja.edu.pl/~jms/qnx/help/tcpip_4.25_en/prog_guide/
* https://man7.org/linux/man-pages/man2/bind.2.html
* https://man7.org/linux/man-pages/man7/ip.7.html

## 

Definition: 

* The basic building block for communication is the socket. 
* A socket is an endpoint of communication to which a name may be bound.
* Each socket in use has:
  * a type and
  * one or more associated processes.

Sockets exist within communication domains. A communication domain is an
abstraction introduced to bundle common properties of processes communicating
through sockets.

## Socket types

* Stream socket:
  * bidirectional, 
  * reliable, 
  * sequenced, and 
  * unduplicated 
  
  flow of data without record boundaries. Aside from the bidirectional nature of
  the dataflow, a pair of connected stream sockets provides an interface nearly
  identical to that of pipes.

* Datagram socket:
  * bidirectional flow of data that **isn't** guaranteed to be:
    * sequenced, 
    * reliable, or 
    * unduplicated
  
  An important characteristic of a datagram socket is that record boundaries in
  data are preserved.

* Raw socket:

  Provides users access to the underlying communication protocols that support
  socket abstractions. These sockets are normally datagram-oriented, though
  their exact characteristics depend on the interface provided by the protocol.
  Raw sockets aren't intended for the general user; they've been provided mainly
  for anyone interested in developing new communication protocols or in gaining
  access to some of the more esoteric facilities of an existing protocol.

## Creating sockets

To create a socket, you use the `socket()` function:

```c
#include <sys/types.h>
#include <netinet/in.h>

int domain = AF_INET;   // internet affinity
int type = SOCK_STREAM; // 
int protocol = 0;       // 0 -> derive from domain

sock_fd = socket(domain, type, protocol);
```

This call would result in a stream socket being created with the TCP protocol
providing the underlying communication support.

The default protocol, which is chosen when the protocol argument to the socket
call is 0, should be correct for most situations.


# Working with stream sockets

## Binding local names

**A socket is created without a name. Until a name is bound to a socket,
processes have no way to reference it and consequently no messages may be
received on it.**


**Communicating processes are bound by an ''association.''**

## Association

In the Internet domain, an association is composed of local and remote
addresses, and local and remote ports.  In most domains, associations must be
unique.

In the Internet domain, there may never be duplicate tuples:
```
<local address, local port, remote address, remote port> 
```

## Specifying half of association

* The bind() function allows a process to specify half of an association
  (`<local_address, local_port>`) 
* while the connect() and accept() functions are used to complete a socket's
  association.


The bound name is a variable-length byte string that's interpreted by the
supporting protocols. Its interpretation may vary from communication domain to
communication domain (this is one of the properties that constitute the domain).
As mentioned earlier, names in the Internet domain contain an Internet address
and port number.

In binding an Internet address, you use the following sequence: 


```c

/* 
Socket address. Only number values.
DNS host must be mapped to socket address
using name resolver (gethostbyname)
*/
struct sockaddr_in {
  sa_family_t    sin_family; /* address family: AF_INET */
  in_port_t      sin_port;   /* port in network byte order */
  struct in_addr sin_addr;   /* internet address */
};

/* Internet address. */
struct in_addr {
  uint32_t       s_addr;     /* address in network byte order */
};

/* Usage: */
#include <sys/types.h>
#include <netinet/in.h>

struct sockaddr_in sock_addr;
//...
bind(
  sock, 
  (struct sockaddr *) &sock_addr, 
  sizeof (sock_addr)
);
```

## Connecting Client

Establishing a connection is usually asymmetric; one process is a client and the
other is a server. The server, when willing to offer its advertised services,
binds a socket to a well-known address associated with the service and then
passively listens on its socket. An unrelated process can then rendezvous with
the server.

The client requests services from the server by initiating a connection to the
server's socket. To initiate the connection, the client uses a connect() call.
This might appear as: 

```c
struct sockaddr_in server_addr;
// ...
connect( 
  sock, 
  (struct sockaddr *)&server_addr, 
  sizeof (server_addr)
);
```

**If the client's socket is unbound at the time of the connect call, the system
will automatically select and bind a name to the socket if necessary (this is
usually how local addresses are bound to a socket).**

## Listening Server

For the server to receive a client's connection, it must, after binding its
socket:

* Indicate a willingness to listen for incoming connection requests.
* Actually accept the connection.


To indicate a willingness to listen for connection requests, the server uses a
listen() call: 

```c
int backlog = 5;
listen(sock, backlog);
```

The backlog parameter to the listen() call specifies the maximum number of
outstanding connections that may be queued awaiting acceptance by the server
process. There is a system-defined number of maximum connections on any one
queue. This prevents processes from hogging system resources by setting the
backlog value to be very large and then ignoring all connection requests.

If a connection is requested while the server's queue is full, the connection
won't be refused. Instead, the individual messages that make up the request will
be ignored, forcing the client to retry. While the client retries the connection
request, the server has time to make room in its pending connection queue. 


With a socket marked as listening, a server may accept a connection:

```c
struct sockaddr_in connect_from_addr;
/* ... */
fromlen = sizeof (connect_from_addr);
newsock = accept( 
  sock, 
  (struct sockaddr *)&connect_from_addr, 
  &fromlen
);
```

When a connection is accepted, a new descriptor is returned 
(**along with a new socket**). 

If the server wishes to find out who its client is, it may supply a buffer for
the client socket's name. The value-result parameter fromlen is initialized by
the server to indicate how much space is associated with from, and is then
modified on return to reflect the true size of the name. If the client's name
isn't of interest, the second parameter may be a NULL pointer. 

An accept() call normally blocks. It won't return until a connection is
available or until the call is interrupted by a signal to the process.

Furthermore, a process can't indicate that it will accept connections from only
a specific individual or individuals. The user process takes care of considering
who the connection is from and of closing down the connection if it doesn't wish
to speak to the process. 


## Server listening on multiple sockets

If the server process wants to accept connections on more than one socket or to
avoid blocking on the accept call, it can do so in several ways:

## Data transfer

With a connection established, data may begin to flow. To send and receive data,
you can choose from several calls.

If the peer entity at each end of a connection is anchored, you can send or
receive a message without specifying the peer. In this case, you can use the
normal read() and write() functions:

```c
write(sock, buf, sizeof (buf));
read(sock, buf, sizeof (buf));
```

## Discarding sockets

Once a socket is no longer of interest, it may be discarded by applying a close
to the descriptor:

```c
close(s);
```

If data is associated with a socket that promises reliable delivery (e.g. a
stream socket) when a close() takes place, the system will continue to attempt
to transfer the data. However, if after a fairly long period of time the data is
still undelivered, it will be discarded. If you have no use for any pending
data, you can perform a shutdown() on the socket prior to closing it:

```c
int no_read_after_close = 0;
int no_send_after_close = 1;
int no_read_or_send_after_close = 2;
shutdown(sock, no_read_or_send_after_close);
```


# Working with datagram sockets (connectionless)

Although processes are still likely to be clients and servers, there's no
requirement that connections be established. Instead, each message includes the
destination address.

Datagram sockets are created as before. If a particular local address is needed,
the bind() operation must precede the first data transmission. Otherwise, the
system will set the local address and/or port when data is first sent.

## Sending data

To send data, you use the sendto() function:

```c
struct sockaddr to_addr;
sendto(
  sock, 
  buf, 
  sizeof (buf), 
  flags, 
  (struct sockaddr *)&to_addr, 
  sizeof (to_addr)
);
```

The `to_addr` value indicate the address of the intended recipient of the
message.

When an unreliable datagram interface is being used, it's unlikely that any
errors will be reported to the sender. When information is present locally to
recognize a message that can't be delivered (e.g. a network is unreachable), the
sendto() call will return -1 and the global variable errno will contain an error
number.

To receive messages on an unconnected datagram socket, you use the recvfrom()
function:

```c
recvfrom( 
  sock, 
  buf, 
  sizeof (buf), 
  flags, 
  (struct sockaddr *)&from_addr, 
  &from_addr_len 
);
```

Once again, `from_addr_len` is a value-result parameter, initially containing
the size of the `from_addr` buffer, and modified on return to indicate the
actual size of the address that the datagram was received from. 

## Using connect() with datagrams

* In addition to the two calls mentioned above (`sendto` and `recvfrom`),
  datagram sockets may also use the connect() call to associate a socket with a
  specific destination address. 
  * In this case, any data sent on the socket will automatically be addressed to
    the connected peer, 
  * and only data received from that peer will be delivered to the user. 
  * Only one connected address is permitted for each socket at one time; 
  * a second connect will change the destination address, and 
  * a connect to a null address (family AF_UNSPEC) will disconnect.
* Connect requests on datagram sockets return immediately, since the peer's
  address is simply recorded. Compare this to stream socket connections, where a
  connect() request would actually initiate the establishment of an end-to-end
  connection. 
* The accept() and listen() functions aren't used with datagram sockets. 


# Network address functions

## Names mapping

* hostnames to network addresses
* network names to network numbers
* protocol names to protocol numbers
* service names to port numbers and appropriate protocol for the server process.

When using any of these functions, you must include the `<netdb.h>` file. 


## Host-names maps

An Internet hostname to address mapping is represented by the `hostent` structure: 

```c
/* 
The official name and the public aliases of the host 
are returned by these functions (gethostbyname, gethostbyaddr), 
along with the address type (family) and a null-terminated 
list of variable-length addresses. This list is required 
because a host may have many addresses, all with the same name. 

The h_addr definition is provided for backward compatibility 
and is defined to be the first address in the list of 
addresses in the hostent structure.
*/
struct hostent {
     char *h_name;      
     char **h_aliases;  
     int  h_addrtype;   
     int  h_length;     
     char **h_addr_list;
};

#define h_addr h_addr_list[0]

```

```c
#include <netdb.h>

/* 
Gets a network host entry by name.
It returns a pointer to a structure of type hostent 
that describes an Internet host. This structure 
contains either the information obtained from the 
name server, named, or broken-out fields from 
a line in /etc/hosts.

When using the name server, gethostbyname() searches
for the named host in the current domain and in the 
domain's parents, unless the name ends in a dot.

Note:   
  If the name contains no dot, and if the 
  environment variable HOSTALIASES contains 
  the name of an alias file, the alias file 
  is first searched for an alias matching the 
  input name. This file has the same form as /etc/hosts.

You can use sethostent() to request the use of a 
connected TCP socket for queries. If the stayopen 
flag is nonzero, all queries to the name server 
will use TCP and the connection will be retained 
after each call to gethostbyname() or gethostbyaddr(). 
If the stayopen flag is zero, queries use UDP datagrams.

*/
char[] name = "my.spec.host.com";
char[] addr = "127.0.0.1";
struct hostent *the_host = gethostbyname( 
  name 
);
struct hostent *the_other_host = gethostbyaddr( 
  addr,
  sizeof (addr),
  AF_INET /* type of address */
);

```

## Hosts databases

The database for these calls is provided either by the:
* `/etc/hosts` file or 
* by use of a name server (as specified in `/etc/resolv.conf`). 

Because of the differences between these databases and their access protocols,
the information returned may differ:
* When the host-table version of gethostbyname() is used, only one address will
  be returned, but all listed aliases will be included. 
* The name-server version may return alternate addresses, but won't provide any
  aliases other than the one given as an argument. 

## Network-names maps




```c
/* 
As in the case of hostnames, we've provided 
functions for mapping network names to numbers, 
and numbers to names. These functions return 
a pointer to a netent structure:
*/

struct netent { 
     char *n_name;         /* official name of net */
     char **n_aliases;     /* alias list */
     int n_addrtype;       /* net address type */
     unsigned long n_net;  /* network number, host byte order */
};

/* 
The network counterparts to the host functions are 
  getnetbyname(), 
  getnetbyaddr(), 
  and getnetent(); 
these network functions extract their information 
from the /etc/networks file. 
*/
```

[See `etc/networks` usage](https://unix.stackexchange.com/questions/196830/practical-usage-of-etc-networks-file)


Entries from `/etc/networks` are used by tools that try 
to convert numbers to names, e.g. the (deprecated) route 
command. Without a suitable entry it shows:

```
# route
Kernel IP routing table
Destination     Gateway         Genmask         Flags Metric Ref    Use Iface
default         192.168.1.254   0.0.0.0         UG    0      0        0 eth0
192.168.0.0     *               255.255.254.0   U     0      0        0 eth0
```

If I now add a line `mylocalnet 192.168.0.0` to `/etc/networks`:

```
# route
Kernel IP routing table
Destination     Gateway         Genmask         Flags Metric Ref    Use Iface
default         192.168.1.254   0.0.0.0         UG    0      0        0 eth0
mylocalnet      *               255.255.254.0   U     0      0        0 eth0
```

In practice it's never really used.


## Protocol names maps

For protocols, which are defined in the `/etc/protocols` file, 
the protoent structure defines the protocol-name mapping used 
with the functions: 
  * `getprotobyname()`, 
  * `getprotobynumber()`, 
  * and `getprotoent()`:

```c
struct protoent {
     char *p_name;         /* official protocol name */
     char **p_aliases;     /* alias list */
     int  p_proto;         /* protocol number */
};

/* Usage: */

#include <netdb.h>

char[] name = "tcp";
struct protoent *prot_num = getprotobyname( name );
```

## Service names

A well-known service is expected to reside at a specific port and employ a
particular communication protocol. This view is consistent with the Internet
domain, but inconsistent with other network architectures. Furthermore, a
service may reside on multiple ports. If this occurs, the higher-level library
functions will have to be bypassed or extended.

* Services available are contained in the `/etc/services` file.
* A service mapping is described by the servent structure: 

```c
struct servent {
     char *s_name;         /* official service name */
     char **s_aliases;     /* alias list */
     int s_port;           /* port number, network byte order */
     char *s_proto;        /* protocol to use */
};

/* 
The getservbyname() function maps service names to a servent 
structure by specifying a service name and, optionally, 
a qualifying protocol.
*/

/* 
  Line below returns the service specification for 
  a telnet server using any protocol 
*/
struct servent *sp1 = getservbyname("telnet", (char *) 0);

/* 
  Line below returns only the telnet server that uses the 
  TCP protocol.
*/
struct servent *sp2 = getservbyname("telnet", "tcp");
```

# Client-server model

No matter whether the specific protocol used in obtaining a service is symmetric
or asymmetric, when accessing a service there's always a client process and a
server process.


## Server types

* A server process normally listens at a well-known port for service requests.
  That is, the server process remains dormant until a connection is requested by
  a client's connection to the server's address. At such a time, the server
  process "wakes up" and services the client, performing whatever appropriate
  actions the client requests of it.
* Alternative schemes employing a ``service server'' may be used to eliminate a
  flock of server processes clogging the system while remaining dormant most of
  the time. For Internet servers, this scheme has been implemented via `inetd`,
  the so-called "Internet superserver." The `inetd` server reads a configuration
  file at startup and listens to a variety of ports based on the contents of the
  file. When a connection is requested to a port that `inetd` is listening at,
  `inetd` executes the appropriate server program to handle the client. With
  this method, clients are unaware that an intermediary such as `inetd` has
  played any part in the connection. 










