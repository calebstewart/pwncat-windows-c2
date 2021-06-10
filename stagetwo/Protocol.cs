using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

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
        JavaScriptSerializer serializer;

        public Protocol(StreamReader stdin)
        {
            this.stdin = stdin;
            this.serializer = new JavaScriptSerializer();
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

                command = serializer.Deserialize<List<object>>(Encoding.UTF8.GetString(stream.ToArray()));

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
            byte[] serialized = Encoding.UTF8.GetBytes(serializer.Serialize(response));

            // Create the raw output stream and a gzip compressor
            var raw_stream = new MemoryStream();

            using (var gz = new GZipStream(raw_stream, CompressionMode.Compress))
            {
                // Compress the serialized object
                gz.Write(serialized, 0, serialized.Length);
            }

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
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                    {
                        if (e.GetBaseException() is LoopExit)
                        {
                            running = false;
                            SendResponse(new Dictionary<string, object>
                            {
                                { "error", 0 },
                                { "result", "exiting" }
                            });
                            continue;
                        }
                        else if (e.GetBaseException() is ProtocolError)
                        {
                            SendResponse(new Dictionary<string, object>
                            {
                                { "error", ((ProtocolError)e.GetBaseException()).code },
                                { "message", ((ProtocolError)e.GetBaseException()).message }
                            });
                            continue;
                        }
                    }

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
