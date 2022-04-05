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

      if (stairNumber == "0")
      {
        response = Stairs::stair0 ? "On" : "Off";
      }
      else if (stairNumber == "1")
      {
        response = Stairs::stair1 ? "On" : "Off";
      }
      else if (stairNumber == "2")
      {
        response = Stairs::stair2 ? "On" : "Off";
      }
      else if (stairNumber == "3")
      {
        response = Stairs::stair3 ? "On" : "Off";
      }
      else if (stairNumber == "4")
      {
        response = Stairs::stair4 ? "On" : "Off";
      }
      else if (stairNumber == "5")
      {
        response = Stairs::stair5 ? "On" : "Off";
      }
      else if (stairNumber == "6")
      {
        response = Stairs::stair6 ? "On" : "Off";
      }
      else if (stairNumber == "7")
      {
        response = Stairs::stair7 ? "On" : "Off";
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

// void HttpLightServer::GetStairs()
// {
//   int responseCode = 200;
//   String response = "";
//   String stairNumber = "";
//   if (server->hasArg(Stairs::StairParam))
//   {
//     std::string ipAddress = server->client().remoteIP().toString().c_str();

//     stairNumber = server->arg(Stairs::StairParam);        
//     if (clientTracker.find(ipAddress) == clientTracker.end())
//     {
//       Serial.print("New Client: ");
//       Serial.print(ipAddress.c_str());
//       Serial.print(" Stair: ");
//       Serial.println(stairNumber);
//       clientTracker.insert(ipAddress);
//     }

//     if (stairNumber == "0")
//     {
//       response = Stairs::stair0 ? "On" : "Off";
//     }
//     else if (stairNumber == "1")
//     {
//       response = Stairs::stair1 ? "On" : "Off";
//     }
//     else if (stairNumber == "2")
//     {
//       response = Stairs::stair2 ? "On" : "Off";
//     }
//     else if (stairNumber == "3")
//     {
//       response = Stairs::stair3 ? "On" : "Off";
//     }
//     else if (stairNumber == "4")
//     {
//       response = Stairs::stair4 ? "On" : "Off";
//     }
//     else if (stairNumber == "5")
//     {
//       response = Stairs::stair5 ? "On" : "Off";
//     }
//     else if (stairNumber == "6")
//     {
//       response = Stairs::stair6 ? "On" : "Off";
//     }
//     else if (stairNumber == "7")
//     {
//       response = Stairs::stair7 ? "On" : "Off";
//     }
//     else
//     {
//       responseCode = 404;
//     }
//   }

//   server->send(responseCode, "text/plain", response);
// }