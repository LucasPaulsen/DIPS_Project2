import React, { createContext, useEffect, useState } from "react";
import Publisher from "./Publisher";
import mqtt from "mqtt";
import Screen from "../Screen";

export const QosOption = createContext([]);
const qosOption = [
  {
    label: "0",
    value: 0,
  },
  {
    label: "1",
    value: 1,
  },
  {
    label: "2",
    value: 2,
  },
];

const url = "wss://test.mosquitto.org:8081/mqtt";
const options = {
  keepalive: 30,
  protocol: "wss", //mqtt
  protocolId: "MQTT",
  protocolVersion: 4,
  clean: true,
  reconnectPeriod: 1000,
  connectTimeout: 30 * 1000,
  rejectUnauthorized: false,
};

const HookMqtt = () => {
  const [client, setClient] = useState(null);
  const [isSubed, setIsSub] = useState(false);
  const [payload, setPayload] = useState({});

  const mqttConnect = () => {
    setClient(mqtt.connect(url, options));
    console.log("connecting");
  };

  useEffect(() => {
    if (!client) {
      mqttConnect();
    }
    if (client && !isSubed) {
      client.subscribe("DIPS8/BEDROOM_SCREEN");
      setIsSub(true);
      console.log("subscribed");
    }

    if (client) {
      client.on("message", (topic, message) => {
        const payload = { topic, message: message.toString() };
        setPayload(payload);
      });
    }
  }, [client, isSubed]);

  const mqttPublish = (context) => {
    if (client) {
      const { topic, qos, payload } = context;
      client.publish(topic, payload, { qos }, (error) => {
        if (error) {
          console.log("Publish error: ", error);
        }
      });
    }
  };

  return (
    <>
      <Screen payload={payload} />
      {/* <QosOption.Provider value={qosOption}>
        <Publisher publish={mqttPublish} />
      </QosOption.Provider> */}
    </>
  );
};

export default HookMqtt;
