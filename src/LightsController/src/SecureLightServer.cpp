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
#include <Credentials.h>
#include <Stairs.h>

using namespace httpsserver;

#define DIR_PUBLIC "/public"
#define testUser apiUSERID
#define testPwd apiPASSWORD

SSLCert * getCertificate();

HTTPSServer * secureServer;

void BlinkLED(int count)
{
  for (int i = 0; i < count; i++)
  {
    digitalWrite(2, HIGH);
    delay(500);
    digitalWrite(2, LOW);
    delay(500);
  }
  delay(3000);
}

void SecureLightServer::setupSecureRoutes()
{
  SSLCert *cert = getCertificate();
  if (cert == NULL) {
    Serial.println("Could not load certificate. Stop.");
    while(true);
  }

  secureServer = new HTTPSServer(cert);

  ResourceNode * getToggleStairsNode = new ResourceNode("/api/stairs", "POST", &toggleStairs);
  secureServer->registerNode(getToggleStairsNode);

  ResourceNode * getToggleMushroomsNode = new ResourceNode("/api/mushrooms", "POST", &toggleMushrooms);
  secureServer->registerNode(getToggleMushroomsNode);


  Serial.println("Starting server...");
  secureServer->start();
  if (secureServer->isRunning()) {
    Serial.println("Server ready.");
  }
}

/**
 * This function will either read the certificate and private key from SPIFFS or
 * create a self-signed certificate and write it to SPIFFS for next boot
 */
SSLCert * SecureLightServer::getCertificate() {
  // Try to open key and cert file to see if they exist
  File keyFile = SPIFFS.open("/key.der");
  File certFile = SPIFFS.open("/cert.der");

  // If now, create them 
  if (!keyFile || !certFile || keyFile.size()==0 || certFile.size()==0) {
    BlinkLED(3);
    Serial.println("No certificate found in SPIFFS, generating a new one for you.");
    Serial.println("If you face a Guru Meditation, give the script another try (or two...).");
    Serial.println("This may take up to a minute, so please stand by :)");

    SSLCert * newCert = new SSLCert();
    // The part after the CN= is the domain that this certificate will match, in this
    // case, it's esp32.local.
    // However, as the certificate is self-signed, your browser won't trust the server
    // anyway.
    int res = createSelfSignedCert(*newCert, KEYSIZE_512, "CN=test,O=acme,C=DE");
    if (res == 0) {
      // We now have a certificate. We store it on the SPIFFS to restore it on next boot.

      bool failure = false;
      // Private key
      keyFile = SPIFFS.open("/key.der", FILE_WRITE);
      if (!keyFile || !keyFile.write(newCert->getPKData(), newCert->getPKLength())) {
        Serial.println("Could not write /key.der");
        failure = true;
      }
      if (keyFile) keyFile.close();

      // Certificate
      certFile = SPIFFS.open("/cert.der", FILE_WRITE);
      if (!certFile || !certFile.write(newCert->getCertData(), newCert->getCertLength())) {
        Serial.println("Could not write /cert.der");
        failure = true;
      }
      if (certFile) certFile.close();

      if (failure) {
        Serial.println("Certificate could not be stored permanently, generating new certificate on reboot...");
      }

      digitalWrite(2, HIGH);
      return newCert;

    } else {
      // Certificate generation failed. Inform the user.
      Serial.println("An error occured during certificate generation.");
      Serial.print("Error code is 0x");
      Serial.println(res, HEX);
      Serial.println("You may have a look at SSLCert.h to find the reason for this error.");
      return NULL;
    }

	} else {
    BlinkLED(1);

    Serial.println("Reading certificate from SPIFFS.");

    // The files exist, so we can create a certificate based on them
    size_t keySize = keyFile.size();
    size_t certSize = certFile.size();

    uint8_t * keyBuffer = new uint8_t[keySize];
    if (keyBuffer == NULL) {
      BlinkLED(2);
      Serial.println("Not enough memory to load privat key");
      return NULL;
    }
    uint8_t * certBuffer = new uint8_t[certSize];
    if (certBuffer == NULL) {
      delete[] keyBuffer;
      BlinkLED(3);
      Serial.println("Not enough memory to load certificate");
      return NULL;
    }
    keyFile.read(keyBuffer, keySize);
    certFile.read(certBuffer, certSize);

    // Close the files
    keyFile.close();
    certFile.close();
    Serial.printf("Read %u bytes of certificate and %u bytes of key from SPIFFS\n", certSize, keySize);

    digitalWrite(2, HIGH);
    return new SSLCert(certBuffer, certSize, keyBuffer, keySize);
  }
}

void SecureLightServer::toggleStairs(HTTPRequest * req, HTTPResponse * res)
{
  int responseCode = 200;
  Serial.println("ToggleStairs called"); 
 
  std::string userName = req->getBasicAuthUser();
  std::string psw = req->getBasicAuthPassword();
 
  Serial.print("User: ");
  Serial.println(userName.c_str());
  Serial.print("pwd Len: ");
  Serial.println(psw.length());

  if (userName != testUser && psw != testPwd)
  {
    Serial.println("unauthorized caller");
    responseCode = 403;
  }
  else
  { 
    Serial.print("Toggling stair ");
    ResourceParameters * params = req->getParams();
    std::string stairString;
    std::string allStairCmd;
    if (params->getQueryParameter(Stairs::StairParam, stairString))
    {
      Serial.println(stairString.c_str());
      
      String s = stairString.c_str();
      int stairInt = s.toInt();

      if (stairInt < sizeof(Stairs::stairs))
      {
        Stairs::stairs[stairInt] = !Stairs::stairs[stairInt];
      }
      else
      {
        responseCode = 404;
      }
    }
    else if (params->getQueryParameter("All", allStairCmd))
    {
      bool newState = false;
      if (allStairCmd == "1")
      {
        newState = true;
      }

      for (int i = 0; i < sizeof(Stairs::stairs); i++)
      {
        Stairs::stairs[i] = newState;
      }
    }
    else
    {
      Serial.println("all");

      for (int x = 0; x < sizeof(Stairs::stairs); x++)
      {
        Stairs::stairs[x] = !Stairs::stairs[x];
      }
    }
  }

  res->setStatusCode(responseCode);
  res->setHeader("Content-Type", "text/plain");
  res->println("");
}

void SecureLightServer::toggleMushrooms(HTTPRequest * req, HTTPResponse * res)
{
  int responseCode = 200;
  Serial.println("ToggleMushrooms called"); 
 
  std::string userName = req->getBasicAuthUser();
  std::string psw = req->getBasicAuthPassword();
 
  Serial.print("User: ");
  Serial.println(userName.c_str());
  Serial.print("pwd Len: ");
  Serial.println(psw.length());

  if (userName != testUser && psw != testPwd)
  {
    Serial.println("unauthorized caller");
    responseCode = 403;
  }
  else
  { 
    Serial.print("Toggling mushroom ");
    ResourceParameters * params = req->getParams();
    std::string mushString;
    if (params->getQueryParameter(Stairs::MushParam, mushString))
    {
      Serial.println(mushString.c_str());
      
      String s = mushString.c_str();
      int mushInt = s.toInt();

      if (mushInt < sizeof(Stairs::mushrooms))
      {
        Stairs::mushrooms[mushInt] = !Stairs::mushrooms[mushInt];
      }
      else
      {
        responseCode = 404;
      }
    }
    else
    {
      Serial.println("all");

      for (int x = 0; x < sizeof(Stairs::mushrooms); x++)
      {
        Stairs::mushrooms[x] = !Stairs::mushrooms[x];
      }
    }
  }

  res->setStatusCode(responseCode);
  res->setHeader("Content-Type", "text/plain");
  res->println("");
}