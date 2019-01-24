using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MCICommon
{
    public static class ProtocolDetector
    {
        public static Task<int> DetectProtocolVersion(string com_port)
        {
            return DetectProtocolVersion(new Func<Task<Stream>>(async () =>
            {
                return await ManagedStream.GetStream(com_port);
            }));
        }

        public static Task<int> DetectProtocolVersion(string ip_or_host, int port)
        {
            return DetectProtocolVersion(new Func<Task<Stream>>(async () =>
            {
                return await ManagedStream.GetStream(ip_or_host, port);
            }));
        }

        public static async Task<int> DetectProtocolVersion(Func<Task<Stream>> stream_retriever)
        {
            int protocol_version = 0;

            //determine if the protocol version is 2
            try
            {
                (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface

                (await ManagedStream.GetStream()).WriteByte((byte)'S');

                if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                    throw new Exception();

                await Task.Delay(10000);

                protocol_version = 2;
            }
            catch (Exception ex)
            {
                //the protocol version isn't 2.1. Check to see if it's 2.0

                await Task.Delay(10000);

                try
                {
                    (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface

                    await Task.Delay(500);

                    (await ManagedStream.GetStream()).WriteByte((byte)'L');

                    await Task.Delay(250);

                    if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                        throw new Exception();

                    await Task.Delay(10000);

                    protocol_version = 1;
                }
                catch (Exception ex1)
                {
                    //the major protocol version isn't 1. test to see if it is 0.

                    await Task.Delay(10000);

                    try
                    {
                        using (StreamWriter conn_writer = new StreamWriter((await ManagedStream.GetStream())))
                        {

                            conn_writer.AutoFlush = true;
                            conn_writer.NewLine = "\r\n";

                            conn_writer.BaseStream.WriteByte((byte)'?');
                            await Task.Delay(1000);
                            conn_writer.WriteLine("write");
                            await Task.Delay(1000);
                            conn_writer.WriteLine("id eeprom");
                            await Task.Delay(1000);
                            conn_writer.WriteLine("raw");
                            await Task.Delay(1000);
                        }

                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception("Unable to determine protocol version.");
                    }
                    catch (Exception ex2)
                    {
                        throw;
                    }
                }
            }

            return protocol_version;
        }
    }
}
