using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace stagetwo
{

    class Protocol
    {
        public class ProtocolError : Exception
        {
            public int code;
            public string message;

            public ProtocolError(int code, string message)
            {
                this.code = code;
                this.message = message;
            }
        }

        public class LoopExit : Exception
        {
            public LoopExit() { }
        }

        StreamReader stdin;

        public Protocol(StreamReader stdin)
        {
            this.stdin = stdin;
        }

        public List<object> ReceiveCommand()
        {
            List<object> command;

            try
            {
                // Read base64 encoded data
                var raw_stream = new MemoryStream(Convert.FromBase64String(stdin.ReadLine()));

                // Create a gzip deflation stream from the data
                var gz = new GZipStream(raw_stream, CompressionMode.Decompress);

                // Copy the decrypted data out
                var stream = new MemoryStream();
                gz.CopyTo(stream);

                // Decode the command array
                command = JsonConvert.DeserializeObject<List<object>>(Encoding.UTF8.GetString(stream.ToArray()));

                return command;
            }
            catch (Exception)
            {
                throw new ProtocolError(-1, "failed to decode command");
            }
        }

        public void SendResponse(Dictionary<string, object> response)
        {
            // Serialize the response object
            byte[] serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));

            // Create the raw output stream and a gzip compressor
            var raw_stream = new MemoryStream();
            var gz = new GZipStream(raw_stream, CompressionMode.Compress);

            // Compress the serialized object
            gz.Write(serialized, 0, serialized.Length);

            // Write a base64-encoded string
            Console.WriteLine(Convert.ToBase64String(raw_stream.ToArray()));
        }

        public void Loop()
        {
            bool running = true;

            while (running)
            {
                try
                {
                    // Retrieve the argument list
                    List<object> command = ReceiveCommand();

                    // Extract the type and method name
                    var typeName = (string)command[0];
                    var methodName = (string)command[1];

                    // Resolve the type and method name, then invoke the method with the given arguments
                    var result = (Dictionary<string, object>)GetType().Assembly.GetType("stagetwo." + typeName).GetMethod(
                        methodName
                    ).Invoke(
                        null,
                        command.GetRange(2, command.Count - 2).ToArray()
                    );

                    SendResponse(new Dictionary<string, object>
                    {
                        { "error", 0 },
                        { "result", result }
                    });
                }
                catch (LoopExit)
                {
                    running = false;
                }
                catch (ProtocolError e)
                {
                    SendResponse(new Dictionary<string, object>
                    {
                        { "error", e.code },
                        { "message", e.message }
                    });
                }
                catch (Exception e)
                {
                    SendResponse(new Dictionary<string, object>
                    {
                        { "error", -1 },
                        { "message", e.Message }
                    });
                }
            }
        }

    }
}
