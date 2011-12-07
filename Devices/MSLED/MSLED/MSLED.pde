#include <Servo.h>

   
const long IMU_BAUD = 115200, SURFACE_BAUD = 115200, LOGGER_BAUD = 115200;

//Note that if the Buoyancy motor is ever changed from PJ7, changing BUOYANCY_ENABLE may affect all DDRJ and PORTJ entries which will include some serial ports. If it is moved to a standard arduino pin, DDRJ and PORTJ entries may be removed.
const int RIGHT_FIN_PIN = 46,
   BOTTOM_FIN_PIN = 11,
   LEFT_FIN_PIN = 44,
   TOP_FIN_PIN = 45,
   THRUST_PIN = 12,
   BUOYANCY_PIN = 2,
   LED_DIMMER_PIN = 9,
   VOLTAGE_PIN = 15,
   CURRENT_PIN = 7,
   HUMIDITY_PIN = 6,
   TEMPERATURE_PIN = 5,
   IMU_BUFFER_LENGTH = 25,
   VERSION_MAJOR = 4,
   VERSION_MINOR = 0,
   BUFFER_LENGTH = 10,
   BUOYANCY_ENABLE = B01000000,
   V5_FLAG = B11011111,
   LED_POWER = 13,
   HORIZONTAL_POWER = 27,
   VERTICAL_POWER = 26,
   FIBER_TRANS_POWER = 10,
   TX_LED = 47,
   RX_LED = 46,
   IMU_READ_FIRMWARE_VERSION_COMMAND_CODE = 0xE9,
   IMU_READ_SENSOR_DATA_COMMAND_CODE = 0xCC,
   IMU_CONTINUOUS_COMMAND_CODE = 0xC4,
   //IMU_MODE_COMMAND_CODE = 0xD4,
   IMU_STOP_CONTINUOUS_MODE_COMMAND_CODE = 0xFA;//,
   //IMU_DEVICE_RESET_COMMAND_CODE = 0xFE;
   
const byte IMU_CONTINUOUS_COMMAND[] = {IMU_CONTINUOUS_COMMAND_CODE, 0xC1, 0x29, IMU_READ_SENSOR_DATA_COMMAND_CODE},
  IMU_STOP_CONTINUOUS_MODE_COMMAND[] = {IMU_STOP_CONTINUOUS_MODE_COMMAND_CODE, 0x75, 0xB4};
  //IMU_CONTINUOUS_MODE_COMMAND[] = {IMU_MODE_COMMAND_CODE, 0xA3, 0x47, 0x2},
  //IMU_DEVICE_RESET_COMMAND[] = {IMU_DEVICE_RESET_COMMAND_CODE, 0x9E, 0x3A};//,

unsigned long lastRamp = 0, lastSensor = 0, time, lastCommand = 0, heartbeatThreshhold = 150;

int topFinOffset = 0, rightFinOffset = 0, bottomFinOffset = 0, leftFinOffset = 0, 
    horizontalFinPos = 90, verticalFinPos = 90,
    readLength, nextByte, length, imuLength, imuRecordLength, imuCommand, terminus = 0x0D,
    buffer[BUFFER_LENGTH], currentThrust, targetThrust, rampDelay = 10, sensorDelay=100;

unsigned short imuChksum,  imuResponseChksum;
    
byte imuBuffer[128], sensorBuffer[17], thrustBuffer[3];

boolean imu = false, logger = false, imuLog = true, sensorDataRead = false, ramping = false;

Servo topFin, rightFin, bottomFin, leftFin, buoyancyPlunger, thruster;

union f2ba{
  byte array[4];
  float val;
} float2ByteArray;

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
   pinMode(LED_POWER, OUTPUT);
   pinMode(VERTICAL_POWER, OUTPUT);
   pinMode(HORIZONTAL_POWER, OUTPUT);
   pinMode(FIBER_TRANS_POWER, OUTPUT);
   pinMode(LED_DIMMER_PIN, OUTPUT);
   
   neutralize();
   
   power(0);
   
   sensorBuffer[0] = 'b';
   thrustBuffer[0] = 't';
   //buoyancyPlunger.attach(BUOYANCY_PIN);
   //DDRJ = DDRJ | BUOYANCY_ENABLE; // sets PJ6 to OUTPUT while leaving all other Port J pinModes unchanged.
   //DDRJ = DDRJ & V5_FLAG; // sets PJ5 to input while leaving all other Port J pinModes unchanged
   //pinMode(TX_LED, OUTPUT);
   //pinMode(RX_LED, OUTPUT);
}

void loop(){
 time = millis();
 if(Serial.available()){
   
   //digitalWrite(RX_LED, HIGH);
   do{
     nextByte = Serial.read();
     buffer[length++] = nextByte;
   }while(nextByte != terminus && length < BUFFER_LENGTH && Serial.available());
   //digitalWrite(RX_LED, LOW);

     
   if(nextByte == terminus){
     lastCommand = time;
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
           Serial2.write(IMU_READ_FIRMWARE_VERSION_COMMAND_CODE);
           Serial2.write(IMU_CONTINUOUS_COMMAND, 4);
         } else {
           Serial2.write(IMU_STOP_CONTINUOUS_MODE_COMMAND, 3);
         }
         break;
       case 'l':
         logger = buffer[1] > 0;
         log('l' + buffer[1]);
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
   if(lastSensor + sensorDelay < time || time < lastSensor){
     float2ByteArray.val = analogRead(CURRENT_PIN) * .0049 / 50 / 0.03;
     sensorBuffer[1] = float2ByteArray.array[0]; 
     sensorBuffer[2] = float2ByteArray.array[1]; 
     sensorBuffer[3] = float2ByteArray.array[2]; 
     sensorBuffer[4] = float2ByteArray.array[3]; 
     float2ByteArray.val = analogRead(VOLTAGE_PIN) * .0049 * 302 / 82;
     sensorBuffer[5] = float2ByteArray.array[0]; 
     sensorBuffer[6] = float2ByteArray.array[1]; 
     sensorBuffer[7] = float2ByteArray.array[2]; 
     sensorBuffer[8] = float2ByteArray.array[3]; 
     float2ByteArray.val = (analogRead(HUMIDITY_PIN) / 204.6 - .75) * 33.33;
     sensorBuffer[9] = float2ByteArray.array[0]; 
     sensorBuffer[10] = float2ByteArray.array[1]; 
     sensorBuffer[11] = float2ByteArray.array[2]; 
     sensorBuffer[12] = float2ByteArray.array[3]; 
     float2ByteArray.val = (analogRead(TEMPERATURE_PIN) / 204.6 - .251) / 0.0064 - 40;
     sensorBuffer[13] = float2ByteArray.array[0]; 
     sensorBuffer[14] = float2ByteArray.array[1]; 
     sensorBuffer[15] = float2ByteArray.array[2]; 
     sensorBuffer[16] = float2ByteArray.array[3]; 
     for(int i = 1; i < 17; i++){
       if(sensorBuffer[i] == 0x0D)
         sensorBuffer[i]--;
     }
     log(sensorBuffer, 17);
     lastSensor = time;
   }
   
   if(ramping && (lastRamp + rampDelay < time || time < lastRamp)){
     thrust(targetThrust);
   }
   
   if(imu){
       if((imuLength = Serial2.available()) > 0){
        while(imuLength > 0){
          imuCommand = Serial2.peek();
          switch(imuCommand){
            case IMU_READ_FIRMWARE_VERSION_COMMAND_CODE:
              imuRecordLength = 7;
              break;
            case IMU_READ_SENSOR_DATA_COMMAND_CODE:
              imuRecordLength = 79;
              break;
            case IMU_CONTINUOUS_COMMAND_CODE:
            //case IMU_MODE_COMMAND_CODE:
              imuRecordLength = 4;
              break;
            default:
              imuLog = false;
              imuRecordLength = 1;
              break;
          }
          if(imuRecordLength > imuLength){
            break;
          }
          for(int i = 0; i < imuRecordLength; i++){
            imuBuffer[i] = Serial2.read();
            if(imuBuffer[i] == 0x0D) {
              imuBuffer[i]++;
            }
            imuLength--;
          }
          if(imuLog && checksum(imuBuffer, imuRecordLength)){
            log(imuBuffer, imuRecordLength);
          }
          imuLog = true;
        }
      }
    }
    
    if(time - lastCommand > heartbeatThreshhold){
      neutralize();
    }
}

void neutralize(){
   //Neutral
   thrust(0x34);
   vertical(90);
   horizontal(90);
}

void vertical(int pos){
 verticalFinPos = pos;
 topFin.write(180 - (pos + topFinOffset));
 bottomFin.write(pos + bottomFinOffset);
 log('v' + String(pos));
}

void horizontal(int pos){
 horizontalFinPos = pos;
 rightFin.write(pos + leftFinOffset);
 leftFin.write(180 - (pos + rightFinOffset));
 log('h' + String(pos));
}

void thrust(int speed){
 currentThrust += (speed < currentThrust) ? -1 : 1;
 targetThrust = speed;
 ramping = targetThrust != currentThrust;
 lastRamp = time;
 thruster.write(currentThrust);
 thrustBuffer[1] = currentThrust;
 thrustBuffer[2] = currentThrust >> 8;
 log(thrustBuffer, 3);
}

void buoyancy(int pos){
 buoyancyPlunger.write(pos);
 log('b' + String(pos));
}

void power(int config){
 switch (config){
   case 0: //All peripherals on including buoyancy motor
     digitalWrite(VERTICAL_POWER, HIGH);
     digitalWrite(HORIZONTAL_POWER, HIGH);
     digitalWrite(FIBER_TRANS_POWER, HIGH);
     digitalWrite(LED_POWER, HIGH);
      //PORTJ = PORTJ | BUOYANCY_ENABLE; // sets PJ6 HIGH while leaving all other port J pins unchanged
      break;
   case 1: //All peripherals on except the buoyancy motor
     digitalWrite(VERTICAL_POWER, HIGH);
     digitalWrite(HORIZONTAL_POWER, HIGH);
     digitalWrite(FIBER_TRANS_POWER, HIGH);
     digitalWrite(LED_POWER, HIGH);
      //PORTJ = PORTJ & (~BUOYANCY_ENABLE); // sets PJ6 LOW while leaving all other port J pins unchanged
      break;
   case 2:
     digitalWrite(FIBER_TRANS_POWER, LOW);
     digitalWrite(LED_POWER, LOW);
     digitalWrite(VERTICAL_POWER, LOW);
     digitalWrite(HORIZONTAL_POWER, LOW);
     digitalWrite(FIBER_TRANS_POWER, LOW);
     //PORTJ = PORTJ & (~BUOYANCY_ENABLE); // sets PJ6 LOW while leaving all other port J pins unchanged
 }
   
 log('p' + config);
}

boolean checksum(byte* buffer, int length){
  imuChksum = 0;
  for (int i = 0; i < length - 2; i++)
    imuChksum += buffer[i];
  //-------- Extract the big-endian checksum from reply
  imuResponseChksum = buffer[length - 2] << 8;
  imuResponseChksum += buffer[length - 1];
  return imuChksum == imuResponseChksum;
}

void log(byte* buffer, int length){
  log(buffer, length, true);
}

void log(String data){
  log(data, true);
}

void log(int data){
  log(data, true);
}

void log(int data, boolean terminate){
  //digitalWrite(TX_LED, HIGH);
  Serial.write(data);
  if(terminate){
    Serial.write(terminus);
  }
  if(logger){
    Serial3.write(data);
    if(terminate){
      Serial3.write(terminus);
    }
  }
  //digitalWrite(TX_LED, LOW);
}

void log(byte* buffer, int length, boolean terminate){
  //digitalWrite(TX_LED, HIGH);
  Serial.write(buffer, length);
  if(terminate){
    Serial.write(terminus);
  }
  if(logger){
    Serial3.write(buffer, length);
    if(terminate){
      Serial3.write(terminus);
    }
  }
  //digitalWrite(TX_LED, LOW);
}

void log(String data, boolean terminate){
  //digitalWrite(TX_LED, HIGH);
  Serial.print(data);
  if(terminate){
    Serial.write(terminus);
  }
  if(logger){
    Serial3.print(data);
    if(terminate){
      Serial3.write(terminus);
    }
  }
  //digitalWrite(TX_LED, LOW);
}
