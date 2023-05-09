export interface Flight {
  FlarmId: string; //flarmId
  DisplayName: string; //displayName
  Registration: string; //registration
  Type: number; //type
  Model: string; //model
  Latitude: number; //lat
  Longitude: number; //lng
  HeightMSL: number; //altitude
  HeightAGL: number; //agl
  Timestamp: number; //timestamp
  Speed: number; //speed
  Vario: number //vario
  VarioAverage: number //varioAverage
  Receiver: string; //receiver
  ReceiverPosition: any //receiverPosition
}
