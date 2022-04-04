#ifndef HTTPLIGHTSERVER_H
#define HTTPLIGHTSERVER_H

#include <WebServer.h>

class HttpLightServer
{
    public:
        static WebServer * server;
        static void SetupHttpRoutes();
        static void GetStairs();
};

#endif