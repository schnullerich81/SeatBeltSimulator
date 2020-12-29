byte incomingByte;

int desiredPosition = 0;
int currentPosition = 0;

void setup() {

  // open serial connection
  Serial.begin(57600);

  // pin 2 and 3 to HIGH = enable both directions
  pinMode(2, OUTPUT);
  pinMode(3, OUTPUT);
  digitalWrite(2, HIGH);   
  digitalWrite(3, HIGH); 

  // pin 5 and 6 are the control pins
  pinMode(5, OUTPUT);
  pinMode(6, OUTPUT);

  // 3 sec in to bring the motor to default position
  inMotor();
  currentPosition = 0;
  delay(3000);

}

void loop() {

  while (Serial.available() > 0) {
    // read the incoming byte
    incomingByte = Serial.read();

    // '0'..'9' is sent over the serial connection... '0' = (char)48 
    desiredPosition = incomingByte - 48;

    // just allow 0..9
    if (desiredPosition < 0) {
      desiredPosition = 0;
    }
    if (desiredPosition > 9) {
      desiredPosition = 9;
    }
  }

  // control the motor to reach the desired position
  if (currentPosition < desiredPosition) {
    outMotor();
    delay(130);
    currentPosition ++;
  }

  if (currentPosition > desiredPosition) {
    inMotor();
    delay(130);
    currentPosition --;
  }

  if (desiredPosition == 0) {
    inMotor();
  }

  if (desiredPosition > 0 && currentPosition > 0 && (currentPosition == desiredPosition || currentPosition >= 9)) {
    stopMotor();
  }
  
  delay(1);             
}

void inMotor() {
    analogWrite(5, 0);
    analogWrite(6, 255);
}

void outMotor() {
    analogWrite(6, 0);
    analogWrite(5, 255);
}

void stopMotor() {
    analogWrite(6, 0);
    analogWrite(5, 0);
}
