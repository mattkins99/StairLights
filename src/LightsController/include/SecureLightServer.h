#ifndef SECURELIGHTSERVER_H
#define SECURELIGHTSERVER_H

#include <SecureLightServer.h>
#include <SPIFFS.h>
#include <FS.h>
#include <ArduinoJson.h>
#include <string>
#include <HTTPSServer.hpp>
#include <SSLCert.hpp>
#include <HTTPRequest.hpp>
#include <HTTPResponse.hpp>
#include <util.hpp>


using namespace httpsserver;

class SecureLightServer
{
    public:
        HTTPSServer * secureServer;
        SecureLightServer(){};
        void setupSecureRoutes();
        SSLCert * getCertificate();
        static void toggleStairs(HTTPRequest * req, HTTPResponse * res);
};

#endif