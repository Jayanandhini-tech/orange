
using Microsoft.Extensions.Logging;
using System.Collections;
using System.IO.Ports;

using VM.Services.Interfaces;

public class Modbus : IModbus
{

    private SerialPort sp = new SerialPort();
    private readonly ILogger<Modbus> logger;
    public string modbusStatus { get; set; } = string.Empty; // For Juice Machine


    public Modbus(ILogger<Modbus> logger)
    {
        this.logger = logger;
    }

    public void Init(string portName, int baudRate = 19200, int databits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.Two)
    {
        //Assign desired settings to the serial port:
        sp.PortName = portName;
        sp.BaudRate = baudRate;
        sp.DataBits = databits;
        sp.Parity = parity;
        sp.StopBits = stopBits;
        //These timeouts are default and cannot be editted through the class at this point:
        sp.ReadTimeout = 5000;
        sp.WriteTimeout = 5000;
    }

    public bool IsOpen()
    {
        return sp.IsOpen;
    }

    public bool RunMotor(int motor, int cabinId = 1)
    {
        try
        {
            var values = new short[] { (short)motor };
            return SendFc16((byte)cabinId, 0, 1, values);
        }
        catch (Exception ex)
        {
            modbusStatus = ex.Message;
            logger.LogError(modbusStatus);
            return false;
        }
    }


    //public int Status(int cabinId = 1)
    //{
    //    int ret = -1;
    //    try
    //    {

    //        short[] values = new short[1];

    //        //bool success = SendFc4((byte)cabinId, 0, 1, ref values);

    //        //if (success)
    //        //    return Convert.ToInt32(values[0]);

    //        //return ret;
    //        bool success = SendFc3((byte)cabinId, 1, 1, ref values);
    //        return success ? Convert.ToInt32(values[0]) : ret;
    //    }
    //    catch (Exception ex)
    //    {
    //        modbusStatus = ex.Message;
    //        logger.LogError(modbusStatus);
    //        return ret;
    //    }
    //}
    public int Status(int cabinId = 1)
    {
        int ret = -1;
        try
        {
            short[] values = new short[1];

            // Make sure we're reading from 40001 → start = 0
            bool success = SendFc3((byte)cabinId, 0, 1, ref values);
            logger.LogInformation($"Modbus status read: success = {success}, value = {values[0]}");
            return success ? Convert.ToInt32(values[0]) : ret;
        }
        catch (Exception ex)
        {
            modbusStatus = ex.Message;
            logger.LogError(modbusStatus);
            return ret;
        }
    }

    public bool GetInstantValue(int RegisterNumber)
    {
        bool[] values = new bool[2];
        bool send = SendFc2(Convert.ToByte(1), (ushort)RegisterNumber, 2, ref values);
        if (!send)
            return false;

        return values[0];
    }
    public int GetResponseValue(int registerNumber)
    {
        short[] values = new short[1];
        bool success = SendFc4(1, (ushort)registerNumber, 1, ref values);
        if (success)
            return values[0];
        else
            return -1; // or some default/failure code
    }
    public bool AdminButtonPressed()
    {
        bool[] value = new bool[1];
        bool cmdSent = SendFc1(1, 564, 1, ref value);

        if (!cmdSent)
            return false;

        return value[0];
    }

    public bool CloseAntiThieftDoor(byte cabinId = 1)
    {
        return SendFc5(cabinId, 565, true);
    }



    #region Open / Close Procedures
    public bool Open()
    {
        //Ensure port isn't already opened:
        if (!sp.IsOpen)
        {
            try
            {
                sp.Open();
            }
            catch (Exception err)
            {
                modbusStatus = "Error opening " + sp.PortName + ": " + err.Message;
                return false;
            }
            modbusStatus = sp.PortName + " opened successfully";
            return true;
        }
        else
        {
            modbusStatus = sp.PortName + " already opened";
            return false;
        }
    }
    public bool Close()
    {
        //Ensure port is opened before attempting to close:
        if (sp.IsOpen)
        {
            try
            {
                sp.Close();
            }
            catch (Exception)
            {

                //  modbusStatus = "Error closing " + sp.PortName + ": " + err.Message;
                return false;
            }
            //  modbusStatus = sp.PortName + " closed successfully";
            return true;
        }
        else
        {
            modbusStatus = sp.PortName + " is not open";
            return false;
        }
    }
    #endregion

    #region CRC Computation
    private void GetCRC(byte[] message, ref byte[] CRC)
    {
        //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
        //return the CRC values:

        ushort CRCFull = 0xFFFF;
        byte CRCHigh = 0xFF, CRCLow = 0xFF;
        char CRCLSB;

        for (int i = 0; i < message.Length - 2; i++)
        {
            CRCFull = (ushort)(CRCFull ^ message[i]);

            for (int j = 0; j < 8; j++)
            {
                CRCLSB = (char)(CRCFull & 0x0001);
                CRCFull = (ushort)(CRCFull >> 1 & 0x7FFF);

                if (CRCLSB == 1)
                    CRCFull = (ushort)(CRCFull ^ 0xA001);
            }
        }
        CRC[1] = CRCHigh = (byte)(CRCFull >> 8 & 0xFF);
        CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
    }
    #endregion

    #region Build Message
    private void BuildMessage(byte address, byte type, ushort start, ushort registers, ref byte[] message)
    {
        //Array to receive CRC bytes:
        byte[] CRC = new byte[2];

        message[0] = address;
        message[1] = type;
        message[2] = (byte)(start >> 8);
        message[3] = (byte)start;
        message[4] = (byte)(registers >> 8);
        message[5] = (byte)registers;

        GetCRC(message, ref CRC);
        message[message.Length - 2] = CRC[0];
        message[message.Length - 1] = CRC[1];
    }
    #endregion

    #region Check Response
    private bool CheckResponse(byte[] response)
    {
        //Perform a basic CRC check:
        byte[] CRC = new byte[2];
        GetCRC(response, ref CRC);
        if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
            return true;
        else
            return false;
    }
    #endregion

    #region Get Response
    private void GetResponse(ref byte[] response)
    {
        //There is a bug in .Net 2.0 DataReceived Event that prevents people from using this
        //event as an interrupt to handle data (it doesn't fire all of the time).  Therefore
        //we have to use the ReadByte command for a fixed length as it's been shown to be reliable.
        for (int i = 0; i < response.Length; i++)
        {
            response[i] = (byte)sp.ReadByte();
        }
    }
    #endregion

    #region Function 16 - Write Multiple Registers
    public bool SendFc16(byte address, ushort start, ushort registers, short[] values)
    {
        //Ensure port is open:
        if (sp.IsOpen)
        {
            //Clear in/out buffers:
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            //Message is 1 addr + 1 fcn + 2 start + 2 reg + 1 count + 2 * reg vals + 2 CRC
            byte[] message = new byte[9 + 2 * registers];
            //Function 16 response is fixed at 8 bytes
            byte[] response = new byte[8];

            //Add bytecount to message:
            message[6] = (byte)(registers * 2);
            //Put write values into message prior to sending:
            for (int i = 0; i < registers; i++)
            {
                message[7 + 2 * i] = (byte)(values[i] >> 8);
                message[8 + 2 * i] = (byte)values[i];
            }
            //Build outgoing message:
            BuildMessage(address, 16, start, registers, ref message);

            //Send Modbus message to Serial Port:
            try
            {
                sp.Write(message, 0, message.Length);
                GetResponse(ref response);
            }
            catch (Exception ex)
            {
                modbusStatus = "Error in write event: " + ex.Message;
                logger.LogError(ex.Message, ex);
                return false;
            }
            //Evaluate message:
            if (CheckResponse(response))
            {
                modbusStatus = "Write successful";
                return true;
            }
            else
            {
                modbusStatus = "CRC error";
                logger.LogInformation("CRC error");
                return false;
            }
        }
        else
        {

            logger.LogInformation("Serial port not open");
            return false;
        }
    }
    #endregion

    #region Function 3 - Read Registers
    public bool SendFc3(byte address, ushort start, ushort registers, ref short[] values)
    {
        //Ensure port is open:
        if (sp.IsOpen)
        {
            //Clear in/out buffers:
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            //Function 3 request is always 8 bytes:
            byte[] message = new byte[8];
            //Function 3 response buffer:
            byte[] response = new byte[5 + 2 * registers];
            //Build outgoing modbus message:
            BuildMessage(address, 3, start, registers, ref message);
            //Send modbus message to Serial Port:
            try
            {
                sp.Write(message, 0, message.Length);
                GetResponse(ref response);
            }
            catch (Exception ex)
            {
                modbusStatus = "Error in read event: " + ex.Message;
                logger.LogError(ex.Message, ex);
                return false;
            }
            //Evaluate message:
            if (CheckResponse(response))
            {
                //Return requested register values:
                for (int i = 0; i < (response.Length - 5) / 2; i++)
                {
                    values[i] = response[2 * i + 3];
                    values[i] <<= 8;
                    values[i] += response[2 * i + 4];
                }
                modbusStatus = "Read successful";
                return true;
            }
            else
            {
                logger.LogInformation("CRC error");
                return false;
            }
        }
        else
        {
            modbusStatus = "Serial port not open";
            logger.LogInformation("Serial port not open");
            return false;
        }

    }
    #endregion

    #region Function 4 - input Registers
    public bool SendFc4(byte address, ushort start, ushort registers, ref short[] values)
    {
        //Ensure port is open:
        if (sp.IsOpen)
        {
            //Clear in/out buffers:
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            //Function 3 request is always 8 bytes:
            byte[] message = new byte[8];
            //Function 3 response buffer:
            byte[] response = new byte[5 + 2 * registers];
            //Build outgoing modbus message:
            BuildMessage(address, 4, start, registers, ref message);
            //Send modbus message to Serial Port:
            try
            {
                sp.Write(message, 1, message.Length);
                GetResponse(ref response);
            }
            catch (Exception ex)
            {
                modbusStatus = "Error in read event: " + ex.Message;
                logger.LogError(ex.Message, ex);
                return false;
            }
            //Evaluate message:
            if (CheckResponse(response))
            {
                  logger.LogInformation("Modbus response bytes: " + BitConverter.ToString(response));
                //Return requested register values:
                for (int i = 0; i < (response.Length - 5) / 2; i++)
                {
                    //values[i] = response[2 * i + 3];
                    //values[i] <<= 8;
                    //values[i] += response[2 * i + 4];
                    values[i] = (short)((response[2 * i + 4] << 8) | response[2 * i + 3]); // <-- UPDATED LINE
                }
                modbusStatus = "Read successful";
                return true;
            }
            else
            {
                modbusStatus = "CRC error";
                logger.LogInformation("CRC error");
                return false;
            }
        }
        else
        {
            modbusStatus = "Serial port not open";
            logger.LogInformation("Serial port not open");
            return false;
        }

    }
    #endregion


    #region Function 2 - Read Registers
    public bool SendFc2(byte address, ushort start, ushort registers, ref bool[] values)
    {
        //Ensure port is open:
        if (sp.IsOpen)
        {
            //Clear in/out buffers:
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            //Function 2 request is always 8 bytes:
            byte[] message = new byte[8];
            //Function 2 response buffer:
            byte[] response = new byte[6];
            //Build outgoing modbus message:
            BuildMessage(address, 2, start, registers, ref message);
            //Send modbus message to Serial Port:
            try
            {
                sp.Write(message, 0, message.Length);
                GetResponse(ref response);
            }
            catch (Exception ex)
            {
                modbusStatus = "Error in read event: " + ex.Message;
                logger.LogError(ex.Message, ex);
                return false;
            }
            //Evaluate message:
            if (CheckResponse(response))
            {
                //Return requested register values:

                BitArray myBA = new BitArray(BitConverter.GetBytes((short)response[3]).ToArray());

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = myBA[i];
                }
                modbusStatus = "Read successful";
                return true;
            }
            else
            {
                modbusStatus = "CRC error";
                logger.LogInformation("CRC error");
                return false;
            }
        }
        else
        {
            modbusStatus = "Serial port not open";
            logger.LogInformation("Serial port not open");
            return false;
        }

    }
    #endregion


    #region Function 1 - Read Coil Status
    public bool SendFc1(byte address, ushort start, ushort registers, ref bool[] values)
    {
        //Ensure port is open:
        if (sp.IsOpen)
        {
            //Clear in/out buffers:
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            //Function 1 request is always 8 bytes:
            byte[] message = new byte[8];
            //Function 1 response buffer:
            byte[] response = new byte[6];
            //Build outgoing modbus message:
            BuildMessage(address, 1, start, registers, ref message);
            //Send modbus message to Serial Port:
            try
            {
                sp.Write(message, 0, message.Length);
                GetResponse(ref response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                return false;
            }
            //Evaluate message:
            if (CheckResponse(response))
            {
                //Return requested register values:              
                BitArray myBA = new BitArray(BitConverter.GetBytes((short)response[3]));

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = myBA[i];
                }
                modbusStatus = "Read successful";
                return true;
            }
            else
            {
                modbusStatus = "CRC error";
                logger.LogInformation("CRC error");
                return false;
            }
        }
        else
        {
            modbusStatus = "Serial port not open";
            logger.LogInformation("Serial port not open");
            return false;
        }

    }
    #endregion


    #region Function 5- Write Single Coil
    public bool SendFc5(byte address, ushort start, bool status)
    {
        //Ensure port is open:
        if (sp.IsOpen)
        {
            //Clear in/out buffers:
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
            //Function 5 request is always 8 bytes:
            byte[] message = new byte[8];
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = 5;
            message[2] = (byte)(start >> 8);
            message[3] = (byte)start;

            if (status)
            {
                message[4] = 255;
                message[5] = 0;
            }
            else
            {
                message[4] = 0;
                message[5] = 0;
            }

            //Function 5 response buffer:
            byte[] response = new byte[6];
            //Build outgoing modbus message:
            //    BuildMessage(address, (byte)5, start, 1, ref message);
            //Send modbus message to Serial Port:


            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];

            try
            {
                sp.Write(message, 0, message.Length);
                GetResponse(ref response);
            }
            catch (Exception ex)
            {
                modbusStatus = "Error in read event: " + ex.Message;
                logger.LogError(ex.Message, ex);
                return false;
            }

            if (CheckResponse(response))
            {
                modbusStatus = "Read successful";
                return true;
            }
            else
            {
                modbusStatus = "CRC error";
                logger.LogInformation("CRC error");
                return false;
            }
        }
        else
        {
            modbusStatus = "Serial port not open";
            logger.LogInformation("Serial port not open");
            return false;
        }

    }

    //public bool SendFc6(byte address, ushort registerAddress, ushort value)
    //{
    //    if (sp.IsOpen)
    //    {
    //        // Clear in/out buffers
    //        sp.DiscardOutBuffer();
    //        sp.DiscardInBuffer();

    //        // Function 6 request is always 8 bytes
    //        byte[] message = new byte[8];
    //        byte[] CRC = new byte[2];

    //        // Build the Modbus RTU message
    //        message[0] = address;                 // Slave address
    //        message[1] = 6;                       // Function code 6
    //        message[2] = (byte)(registerAddress >> 8); // Register address high byte
    //        message[3] = (byte)(registerAddress);      // Register address low byte
    //        message[4] = (byte)(value >> 8);      // Value high byte
    //        message[5] = (byte)(value);           // Value low byte

    //        // Append CRC
    //        GetCRC(message, ref CRC);
    //        message[6] = CRC[0];
    //        message[7] = CRC[1];

    //        // Response will also be 8 bytes (echo of request)
    //        byte[] response = new byte[8];

    //        try
    //        {
    //            sp.Write(message, 0, message.Length);
    //            GetResponse(ref response);
    //        }
    //        catch (Exception ex)
    //        {
    //            modbusStatus = "Error writing register: " + ex.Message;
    //            logger.LogError(ex.Message, ex);
    //            return false;
    //        }

    //        // Validate response
    //        if (CheckResponse(response))
    //        {
    //            modbusStatus = "Write successful";
    //            return true;
    //        }
    //        else
    //        {
    //            modbusStatus = "CRC error";
    //            logger.LogInformation("CRC error");
    //            return false;
    //        }
    //    }
    //    else
    //    {
    //        modbusStatus = "Serial port not open";
    //        logger.LogInformation("Serial port not open");
    //        return false;
    //    }
    //}
    //private const ushort ORANGE_COUNT_REGISTER = 768;

    public bool OrangeCount(byte cabinId, ushort value)
    {
        logger.LogInformation("Updating Orange Count: Writing {Value} to Register 40768");
        return SendFc6(1, 768, value);
    }
    public bool SendFc6(byte address, ushort registerAddress, ushort value)
    {
        if (!sp.IsOpen)
        {
            modbusStatus = "Serial port not open";
            logger.LogError(modbusStatus);
            return false;
        }

        // Build Modbus RTU frame
        byte[] message = new byte[8];
        message[0] = address;                            // Slave address
        message[1] = 6;                                  // Function code 6
        message[2] = (byte)(registerAddress >> 8);       // Register address high byte
        message[3] = (byte)(registerAddress);            // Register address low byte
        message[4] = (byte)(value >> 8);                 // Value high byte
        message[5] = (byte)(value);                      // Value low byte

        byte[] crc = new byte[2];
        GetCRC(message, ref crc);                        // Calculate CRC
        message[6] = crc[0];
        message[7] = crc[1];

        logger.LogInformation("Sending Modbus Write (FC6): {0}", BitConverter.ToString(message));

        byte[] response = new byte[8];

        try
        {
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();

            sp.Write(message, 0, message.Length);
            logger.LogInformation("Modbus message sent. Waiting for response...");

            int bytesRead = sp.Read(response, 0, response.Length);
            logger.LogInformation("Received {0} bytes: {1}", bytesRead, BitConverter.ToString(response));
        }
        catch (Exception ex)
        {
            modbusStatus = "Error writing to register: " + ex.Message;
            logger.LogError("Exception in SendFc6: {0}", ex.Message);
            return false;
        }

        if (!ValidateFc6Response(message, response))
        {
            modbusStatus = "Modbus write failed: Invalid response.";
            logger.LogError(modbusStatus);
            return false;
        }

        modbusStatus = "Write successful";
        return true;
    }
    private bool ValidateFc6Response(byte[] request, byte[] response)
    {
        if (response.Length != 8)
        {
            logger.LogError("Invalid Modbus response length.");
            return false;
        }

        // Check slave address and function code
        if (response[0] != request[0] || response[1] != 6)
        {
            logger.LogError("Modbus response mismatch (Address/Function Code).");
            return false;
        }

        // Check CRC
        byte[] expectedCrc = new byte[2];
        GetCRC(response, ref expectedCrc);
        if (response[6] != expectedCrc[0] || response[7] != expectedCrc[1])
        {
            logger.LogError("CRC check failed. Expected CRC: {0:X2} {1:X2}, Got: {2:X2} {3:X2}",
                expectedCrc[0], expectedCrc[1], response[6], response[7]);
            return false;
        }

        return true;
    }


    #endregion

    public bool ResetJuicer()
    {
        return SendFc5(1, 400, true);
    }


}
