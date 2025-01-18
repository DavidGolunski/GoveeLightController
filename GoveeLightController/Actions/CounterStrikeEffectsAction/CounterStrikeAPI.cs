using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace GoveeLightController {


    public class CounterStrikeAPI {

        private static CounterStrikeAPI instance;
        public static CounterStrikeAPI Instance {
            get => instance ?? (instance = new CounterStrikeAPI());
            private set => instance = value;
        }


        private readonly HttpListener listener;

        private GameState previousGameState;
        private GameState currentGameState;

        public CounterStrikeAPI() {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:3000/");
            Reset();
        }

        public void Dispose() {
            StopListening();
        }

        public void Reset() {
            previousGameState = null;
            currentGameState = null;

        }

        public void StartListening() {
            listener?.Start();
        }

        public void StopListening() {
            listener?.Stop();
        }

        public bool WaitForUpdate() {
            if(listener == null || !listener.IsListening)
                return false;

            bool successfull = false;
            try {
                HttpListenerContext context = listener.GetContext();

                // Read the incoming data
                string json;
                using(var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) {
                    json = reader.ReadToEnd();
                }

                successfull = HandleMessage(json);

                
                // Send a response back to CS
                HttpListenerResponse response = context.Response;
                string responseString = "OK";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch(Exception ex) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "An exception occured in Counter Strike GSI:\n" + ex.StackTrace);
            }

            return successfull;
        }

        public bool HandleMessage(string json) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, json);

            JObject csUpdates = JObject.Parse(json);

            if(csUpdates == null)
                return false;

            // Check for auth token, so no messages from other potential GameStateIntegrations are used
            // this will be sent with every message
            if(csUpdates["auth"]?["token"] == null)
                return false;
            if(csUpdates["auth"]["token"].ToString() != "GoveeLightController")
                return false;


            previousGameState = currentGameState;
            currentGameState = new GameState(csUpdates, previousGameState);
       
            Logger.Instance.LogMessage(TracingLevel.INFO, "Message Handling Successfull\n" + currentGameState.ToString());
            return true;
        }

        // function to retrieve information about the latest event.
        // if "popEvent" is true, it will set it to "NO_EVENT" aferwards, ensuring no double usage of the same event
        public CsEventTypes GetEvent() {
            if(currentGameState == null)
                return CsEventTypes.NO_EVENT;

            return currentGameState.GetEvent(previousGameState);
        }
    }
}
