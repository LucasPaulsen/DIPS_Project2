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
    if (!payload) return "";
    //kitchen routes
    console.log(payload);
    if (payload.includes("WAIT")) {
      fontColor = "#FFF";
      document.body.style = "background: #F00;";
      return (
        "Remember to wait before eating. Breakfast is at: " +
        payload.substring(5)
      );
    }
    if (payload.includes("PUT_PILLS_BACK")) {
      fontColor = "#FFF";
      document.body.style = "background: #F00;";
      return "Remember to set your pill box back to where it belongs.";
    }
    if (payload.includes("PILLS_TAKEN_AGAIN")) {
      fontColor = "#FFF";
      document.body.style = "background: #F00;";
      return "DON'T YOU DARE TAKE MORE PILLS!";
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
    if (payload.includes("BREAKFAST_TIME:")) {
      document.body.style = "background: #0F0;";
      fontColor = "#000";
      return (
        "Time has passed " +
        payload.substring(15) +
        ". You can eat breakfast now."
      );
    }
  };

  return (
    <div style={{ textAlign: "center" }}>
      <h3 style={{ color: fontColor }}>Kitchen: {time}</h3>
      <h1 style={{ margin: "auto", color: fontColor }}>
        {handlePayload(payload.message)}
      </h1>
    </div>
  );
}

export default Screen;
