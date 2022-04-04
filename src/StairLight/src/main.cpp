#include <Arduino.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <EEPROM.h>
#include <mdns.h>
#include <Credentials.h>

#define EEPROM_SIZE 256
#define WIFI_SSID wiFiSSID
#define WIFI_PSK  wiFiPASSWORD

const String light = "5";
const int lightPin = 15;

HTTPClient http;
volatile uint8_t lightState;
volatile uint8_t errorCount;
String uri;

void setup() {
  Serial.begin(9600);
  pinMode(lightPin, OUTPUT);
  digitalWrite(lightPin, LOW);
  lightState = LOW;
  EEPROM.begin(EEPROM_SIZE);
  errorCount = 0;

  // build URI
  uri = "/api/stairs?Stair=";
  uri += light;

  Serial.println("Connecting to WiFi."); 
  WiFi.setHostname("Stair5"); 
  WiFi.begin(WIFI_SSID, WIFI_PSK);
  int connectionWaitLimitCounter = 0;
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    connectionWaitLimitCounter++;

    if (connectionWaitLimitCounter > 120)
    {
      ESP.restart();
    }
  }
  Serial.println("");
  Serial.println("Connected");
  Serial.println("Ip Address");
  Serial.println(WiFi.localIP());
  WiFi.dnsIP(WiFi.gatewayIP());

  delay(2000);
}

void loop() {
  try
  {
    http.begin("192.168.0.160", 80, uri);
    int responseCode = http.GET();
    Serial.print("ResponseCode: ");
    Serial.println(responseCode);

    if (responseCode == 200)
    {
      String response = http.getString();
      Serial.println("Reading body");
      Serial.println(response);

      if (response == "On")
      {
        if (lightState == HIGH)
        {
          Serial.println("Keep on keepin on.");
        }
        else
        {
          Serial.println("Turning lights on.");
          digitalWrite(lightPin, HIGH);
          lightState = HIGH;
          Serial.println("Lights are now on");
        }
      }
      else if (response == "Off")
      {      
        if (lightState == LOW)
        {
          Serial.println("The dark is my friend.  I like it this way.");
        }
        else
        {
          Serial.println("Turning lights Off.");
          digitalWrite(lightPin, LOW);
          lightState = LOW;
          Serial.println("Lights are now off");
        }
      }
      else
      {
        // Um... what?
        Serial.print("Unexpected success response?");
        //Serial.println(response);
        String lightMessage = lightState == HIGH ? "On" : "Off";
        Serial.print("Lights are ");
        Serial.println(lightMessage);
      }
      
      delay(100);
      errorCount = 0;
    }
    else
    {
      errorCount++;
      Serial.print("Got here. ErrorCount: ");
      Serial.println(errorCount);
      delay(1000);
    }
  }
  catch(const std::exception& e)
  {
    errorCount++;
  }
 
  if (errorCount > 50)
  {
    Serial.println("Rebooting! That fixes everything");
    ESP.restart();
  }  
}