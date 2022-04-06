#include <HttpLightServer.h>
#include <Stairs.h>
#include <WebServer.h>
#include <unordered_set>
#include <HttpServer.h>

std::unordered_set<std::string> clientTracker;

void HttpLightServer::SetupHttpRoutes()
{
  HttpServer::server.on("/api/stairs", HTTP_GET, []() 
  { 
    int responseCode = 200;
    String response = "";
    String stairNumber = "";
    if (HttpServer::server.hasArg(Stairs::StairParam))
    {
      std::string ipAddress = HttpServer::server.client().remoteIP().toString().c_str();

      stairNumber = HttpServer::server.arg(Stairs::StairParam);        
      if (clientTracker.find(ipAddress) == clientTracker.end())
      {
        Serial.print("New Client: ");
        Serial.print(ipAddress.c_str());
        Serial.print(" Stair: ");
        Serial.println(stairNumber);
        clientTracker.insert(ipAddress);
      }
      
      int stairInt = stairNumber.toInt();
      if (stairInt < sizeof(Stairs::stairs))
      {
        response = Stairs::stairs[stairInt] ? "On" : "Off";
      }
      else
      {
        responseCode = 404;
      }
    }

    HttpServer::server.send(responseCode, "text/plain", response);  
  });


  HttpServer::server.begin();
}