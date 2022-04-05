#include <WiFi.h>
#include <SPIFFS.h>
#include <FS.h>
#include <ArduinoJson.h>
#include <string>
#include <WebServer.h>
#include <HTTPSServer.hpp>
#include <SSLCert.hpp>
#include <HTTPRequest.hpp>
#include <HTTPResponse.hpp>
#include <util.hpp>
#include <SecureLightServer.h>
#include <HttpLightServer.h>
#include <Stairs.h>
#include <Credentials.h>
#include <ElegantOTA.h>
#include <HttpServer.h>

using namespace httpsserver;

#define DIR_PUBLIC "/public"
#define WIFI_SSID wiFiSSID
#define WIFI_PSK  wiFiPASSWORD
const char *StairParam = "Stair";
const char *defaultStair = "";

WebServer httpServer(80);
SecureLightServer slServer;
HttpLightServer lightServer;

void setup() {
  // For logging
  Serial.begin(115200);

  pinMode(2, OUTPUT);

  // Try to mount SPIFFS without formatting on failure
  if (!SPIFFS.begin(false)) {
    // If SPIFFS does not work, we wait for serial connection...
    while(!Serial);
    delay(1000);

    // Ask to format SPIFFS using serial interface
    Serial.print("Mounting SPIFFS failed. Try formatting? (y/n): ");
    while(!Serial.available());
    Serial.println();

    // If the user did not accept to try formatting SPIFFS or formatting failed:
    if (Serial.read() != 'y' || !SPIFFS.begin(true)) {
      Serial.println("SPIFFS not available. Stop.");
      while(true);
    }
    Serial.println("SPIFFS has been formated.");
  }
  Serial.println("SPIFFS has been mounted.");

  // Connect to WiFi
  Serial.println("Setting up WiFi");
  WiFi.setHostname("LightController");
  WiFi.begin(WIFI_SSID, WIFI_PSK);
  int connectionCounter = 0;
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    connectionCounter++;
    delay(500);

    if (connectionCounter > 300)
    {
      Serial.println("Unable to connect to Wifi.  Reboot and try again.");
      ESP.restart();
    }
  }
  Serial.print("Connected. IP=");
  Serial.println(WiFi.localIP());

  slServer.setupSecureRoutes();
  lightServer.SetupHttpRoutes();  

  // httpServer.on("/", []() {
  //   httpServer.send(200, "text/plain", "Hi! I am ESP32.");
  // });

  ElegantOTA.begin(&HttpServer::server, otaUSERID, otaPASSWORD);
}

void loop() {
  try
  {
    slServer.secureServer->loop();
    HttpServer::server.handleClient();
    delay(1);
  }
  catch(const std::exception& e)
  {
    Serial.println("Uh oh... going down");
    ESP.restart();
  }  
}

