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

  HttpServer::server.on("/api/mushrooms", HTTP_GET, []() 
  { 
    int responseCode = 200;
    String response = "";
    String mushroomNumber = "";
    if (HttpServer::server.hasArg(Stairs::MushParam))
    {
      std::string ipAddress = HttpServer::server.client().remoteIP().toString().c_str();

      mushroomNumber = HttpServer::server.arg(Stairs::MushParam);        
      if (clientTracker.find(ipAddress) == clientTracker.end())
      {
        Serial.print("New mushroom Client: ");
        Serial.print(ipAddress.c_str());
        Serial.print(" Mushroom: ");
        Serial.println(mushroomNumber);
        clientTracker.insert(ipAddress);
      }
      
      int mushInt = mushroomNumber.toInt();
      if (mushInt < sizeof(Stairs::mushrooms))
      {
        response = Stairs::mushrooms[mushInt] ? "On" : "Off";
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