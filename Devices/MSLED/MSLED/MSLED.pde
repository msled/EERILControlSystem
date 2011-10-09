#include <Servo.h>

//Note that if the Buoyancy motor is ever changed from PJ7, changing BUOYANCY_ENABLE may affect all DDRJ and PORTJ entries which will include some serial ports. If it is moved to a standard arduino pin, DDRJ and PORTJ entries may be removed.

const int RIGHT_FIN_PIN = 11,
   BOTTOM_FIN_PIN = 44,
   LEFT_FIN_PIN = 45,
   TOP_FIN_PIN = 46,
   THRUST_PIN = 12,
   BUOYANCY_PIN = 2,
   LED_DIMMER_PIN = 9,
   SURFACE_BAUD = 9600,
   IMU_BAUD = 115200,
   IMU_BUFFER_LENGTH = 25,
   LOGGER_BAUD = 9600,
   VERSION_MAJOR = 4,
   VERSION_MINOR = 0,
   BUFFER_LENGTH = 10,
   LED_POWER = 13,
   BUOYANCY_ENABLE = B01000000,
   V5_FLAG = B11011111,
   FIBER_TRANS_POWER = 10,
   TX_LED = 47,
   RX_LED = 46,
   IMU_READ_FIRMWARE_VERSION = 0xE9;

Servo topFin, rightFin, bottomFin, leftFin, buoyancyPlunger, thruster;

int topFinOffset = 0, rightFinOffset = 0, bottomFinOffset = 0, leftFinOffset = 0, 
    horizontalFinPos = 90, verticalFinPos = 90,
    readLength, current, length, imuLength, terminus = 0x0D;

int buffer[BUFFER_LENGTH];
char imuBuffer[IMU_BUFFER_LENGTH];

boolean imu = false, logger = false;

void setup(){
   //Surface Control
   Serial.begin(SURFACE_BAUD);
   //IMU
   Serial2.begin(IMU_BAUD);
   //Logger
   Serial3.begin(LOGGER_BAUD);
   thruster.attach(THRUST_PIN);
   topFin.attach(TOP_FIN_PIN);
   rightFin.attach(RIGHT_FIN_PIN);
   bottomFin.attach(BOTTOM_FIN_PIN);
   leftFin.attach(LEFT_FIN_PIN);
   pinMode(LED_DIMMER_PIN, OUTPUT);
   //Turn off LEDs
   
   //buoyancyPlunger.attach(BUOYANCY_PIN);
   //DDRJ = DDRJ | BUOYANCY_ENABLE; // sets PJ6 to OUTPUT while leaving all other Port J pinModes unchanged.
   //DDRJ = DDRJ & V5_FLAG; // sets PJ5 to input while leaving all other Port J pinModes unchanged
   pinMode(LED_POWER, OUTPUT);
   pinMode(FIBER_TRANS_POWER, OUTPUT);
   digitalWrite(FIBER_TRANS_POWER, HIGH);
   //pinMode(TX_LED, OUTPUT);
   //pinMode(RX_LED, OUTPUT);
}

void loop(){
 
 if(Serial.available()){
   
   //digitalWrite(RX_LED, HIGH);
   do{
     current = Serial.read();
     buffer[length++] = current;
   }while(current != terminus && length < BUFFER_LENGTH && Serial.available());
   //digitalWrite(RX_LED, LOW);

     
   if(current == terminus){
     switch(buffer[0]){
       case 'v':
         vertical(buffer[1]);
         break;
       case 'h':
         horizontal(buffer[1]);
         break;
       case 't':
         thrust(buffer[1]);
         break;
       case 'p':
         power(buffer[1]);
         break;
       case 'b':
         buoyancy(buffer[1]);
         break;
       case 'a':
         switch(buffer[1]){
           case 't':
             topFinOffset = buffer[2] - 90;
             vertical(verticalFinPos);
             break;
           case 'r':
             rightFinOffset = buffer[2] - 90;
             horizontal(horizontalFinPos);
             break;
           case 'b':
             bottomFinOffset = buffer[2] - 90;
             vertical(verticalFinPos);
             break;
           case 'l':
             leftFinOffset = buffer[2] - 90;
             horizontal(horizontalFinPos);
             break;
         }
         break;
       case 'i':
         imu = buffer[1] > 0;
         if(imu){
           Serial2.write(IMU_READ_FIRMWARE_VERSION);
         }
         break;
       case 'l':
         logger = buffer[1] > 0;
         break;
       case 'o':
         log('v' + String(VERSION_MAJOR, DEC) + "." + String(VERSION_MINOR, DEC));
         break;
     }
     length = 0;
   } else if(length >= BUFFER_LENGTH) {
     log('f0');
     length = 0;
   }
 }
 if(imu){
   while((imuLength = Serial2.available())){
     while(imuLength-- > 0){
       log(Serial2.read());
     }
   }
 }
}

void vertical(int pos){
 if(pos < 64){
   pos = 64;
 } else if (pos > 116){
   pos = 116;
 }
 verticalFinPos = pos;
 topFin.write(pos + topFinOffset);
 bottomFin.write(180 - (pos + bottomFinOffset));
 log('v' + String(pos));
}

void horizontal(int pos){
 if(pos < 64){
   pos = 64;
 } else if (pos > 116){
   pos = 116;
 }
 horizontalFinPos = pos;
 leftFin.write(pos + leftFinOffset);
 rightFin.write(180 - (pos + rightFinOffset));
 log('h' + String(pos));
}

void thrust(int speed){
 thruster.write(speed);
 log('t' + String(speed));
}

void buoyancy(int pos){
 buoyancyPlunger.write(pos);
 log('b' + String(pos));
}

void power(int config){
 switch (config){
   case 0: //All peripherals on including buoyancy motor
      digitalWrite(LED_POWER, HIGH);
      digitalWrite(FIBER_TRANS_POWER, HIGH);
      //PORTJ = PORTJ | BUOYANCY_ENABLE; // sets PJ6 HIGH while leaving all other port J pins unchanged
      break;
   case 1: //All peripherals on except the buoyancy motor
      digitalWrite(LED_POWER, HIGH);
      digitalWrite(FIBER_TRANS_POWER, HIGH);
      //PORTJ = PORTJ & (~BUOYANCY_ENABLE); // sets PJ6 LOW while leaving all other port J pins unchanged
      break;
   case 2:
     digitalWrite(FIBER_TRANS_POWER, LOW);
     digitalWrite(LED_POWER, LOW);
     //PORTJ = PORTJ & (~BUOYANCY_ENABLE); // sets PJ6 LOW while leaving all other port J pins unchanged
 }
   
 log('p' + String(config));
}

void log(char data){
 log(data);
}

void log(String data){
  //digitalWrite(TX_LED, HIGH);
  Serial.print(data);
  Serial.write(terminus);
  if(logger){
    Serial3.print(data);
    Serial3.write(terminus);
  }
  //digitalWrite(TX_LED, LOW);
}

void log(int data){
  //digitalWrite(TX_LED, HIGH);
  Serial.write(data);
  Serial.write(terminus);
  if(logger){
    Serial3.write(data);
    Serial3.write(terminus);
  }
  //digitalWrite(TX_LED, LOW);
}
//  Serial3.write(data);
