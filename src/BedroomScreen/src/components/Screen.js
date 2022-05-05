import React, { useEffect, useState } from "react";
let fontColor = "#000";

function Screen({ payload }) {
  const [time, setTime] = useState(null);

  useEffect(() => {
    setInterval(() => {
      setTime(new Date().toLocaleString("dk-DK"));
    }, 1000);
  }, [setTime]);

  const handlePayload = (payload) => {
    console.log(payload);
    if (!payload) return "";
    if (payload.includes("GOOD_MORNING")) {
      document.body.style = "background: #FFF;";
      fontColor = "#000";
      return "Good Morning. Remember to take your pill.";
    }

    if (payload.includes("BREAKFAST_TIME:")) {
      document.body.style = "background: #0F0;";
      fontColor = "#000";
      return (
        "Time has passed " +
        payload.substring(15) +
        ". You can eat breakfast now."
      );
    }
    if (payload.includes("PUT_PILLS_BACK")) {
      fontColor = "#FFF";
      document.body.style = "background: #F00;";
      return "Please put the pills back!";
    }
    if (payload.includes("PILLS_TAKEN:")) {
      fontColor = "#000";
      document.body.style = "background: #0F0;";
      return (
        "Good job! You took the pills at: " +
        payload.substring(13) +
        "\n" +
        "I will remind you to eat in an hour"
      );
    }
  };

  return (
    <div style={{ textAlign: "center" }}>
      <h3 style={{ color: fontColor }}>Bedroom: {time}</h3>
      <h1 style={{ margin: "auto", color: fontColor }}>
        {handlePayload(payload.message)}
      </h1>
    </div>
  );
}

export default Screen;
