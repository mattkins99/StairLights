#include <HttpLightServer.h>
#include <Stairs.h>
#include <WebServer.h>

WebServer * HttpLightServer::server;

void HttpLightServer::SetupHttpRoutes()
{
  HttpLightServer::server = new WebServer(80);
  HttpLightServer::server->on("/api/stairs", HTTP_GET, GetStairs);

  HttpLightServer::server->begin();
}

void HttpLightServer::GetStairs()
{
  int responseCode = 200;
  String response = "";
  String stairNumber = "";
  if (HttpLightServer::server->hasArg(Stairs::StairParam))
  {
    stairNumber = HttpLightServer::server->arg(Stairs::StairParam);    
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

  HttpLightServer::server->send(responseCode, "text/plain", response);
}