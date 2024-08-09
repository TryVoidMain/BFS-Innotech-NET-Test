using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;

namespace Bfs.TestTask.Parser;

public class Parser : IParser
{
    private string[] _messageSeparators = { "\u001c" };
    private string[] _fitnessStateSeparators = { "\u001d" };

    public async IAsyncEnumerable<IMessage> Parse(ChannelReader<ReadOnlyMemory<byte>> source)
    {
        //Перед каждым сообщением первые 2 байта определяют его длину
        //array[0] = (byte)(message.Length / 256);
        //array[1] = (byte)(message.Length % 256);
        var buffer = new byte[0];

        while (await source.WaitToReadAsync())
        {
            if (!source.TryRead(out var memoryBytes))
            {
                continue;
            }

            var bytes = memoryBytes.ToArray();
            AddBytesToBuffer(ref buffer, bytes);
            var length = GetMessageLength(buffer);

            if (length + 2 > buffer.Length)
            {
                continue;
            }

            var message = ExtractMessageFromBuffer(ref buffer, length);
            yield return ProcessMessage(message);
        }
    }

    private int GetMessageLength(byte[] arr)
    {
        var l1 = (int)arr[0] * 256;
        var l2 = (int)arr[1];
        return l1 + l2;
    }

    private void AddBytesToBuffer(ref byte[] buffer, byte[] addBytes)
    {
        buffer = buffer
            .Take(buffer.Length)
            .Concat(addBytes)
            .ToArray();
    }

    private string ExtractMessageFromBuffer(ref byte[] bufferBytes, int messageLength)
    {
        var messageBytes = bufferBytes
            .Skip(2)
            .Take(messageLength)
            .ToArray();

        bufferBytes = bufferBytes
            .Skip(messageLength + 2)
            .Take(bufferBytes.Length - messageLength - 2)
            .ToArray();

        return Encoding.ASCII.GetString(messageBytes, 0, messageBytes.Length);
    }

    private IMessage ProcessMessage(string message)
    {
        var separatedMessage = message.Split(_messageSeparators, StringSplitOptions.RemoveEmptyEntries);
        var messageClassSubClass = separatedMessage[0];
        string LUNO = separatedMessage[1];

        if (messageClassSubClass == "12")
        {
            char DIG = separatedMessage[2][0];
            if (int.TryParse(separatedMessage[2][1].ToString(), out int deviceStatus) &&
                int.TryParse(separatedMessage[2][2].ToString(), out int errorSeverity) &&
                int.TryParse(separatedMessage[2][3].ToString(), out int diagnosticStatus) &&
                int.TryParse(separatedMessage[2][4].ToString(), out int suppliesStatus))
            {
                return new CardReaderState(
                    LUNO, 
                    DIG, 
                    deviceStatus, 
                    errorSeverity, 
                    diagnosticStatus, 
                    suppliesStatus);
            }
        }
        else if (messageClassSubClass == "22")
        {
            char statusDescriptor = separatedMessage[2][0];
            if (statusDescriptor == 'B')
            {
                if (int.TryParse(separatedMessage[3].ToString(), out int transactionNumber))
                {
                    return new SendStatus(
                        LUNO, 
                        statusDescriptor, 
                        transactionNumber);
                }
            }
            else if (statusDescriptor == 'F')
            {
                char messageIdentifier = separatedMessage[3][0];
                char hardwareFitnessIdentifier = separatedMessage[3][1];

                var fitnessStateMessages = separatedMessage[3].Substring(2);
                var fitnessState = ParseFitnessState(fitnessStateMessages);

                return new GetFitnessData(
                    LUNO, 
                    statusDescriptor, 
                    messageIdentifier, 
                    hardwareFitnessIdentifier, 
                    fitnessState);
            }
        }

        return null;
    }

    private FitnessState[] ParseFitnessState(string message)
    {
        var separatedStates = message.Split(_fitnessStateSeparators, StringSplitOptions.RemoveEmptyEntries);
        
        var result = new FitnessState[separatedStates.Length];
        for (int i = 0; i < separatedStates.Length; i++)
        {
            var state = separatedStates[i];
            char DIG = state[0];
            string fitness = state.Substring(1);

            var fitnessState = new FitnessState(DIG, fitness);
            result[i] = fitnessState;
        }

        return result;
    }
}

 /*
       Message with type Card Reader State builded:
       1200100355D1001
       Description:
       (b) 1 = Message class
       (c) 2 = Message sub-class
       (d) 00100355 = LUNO
       CardReaderStateDto { Solicited = False, DeviceIdCode = D, SupplyState = NoOverfillCondition, Status = TimeOutCardHolderTakingCard, Severity = NoError }
       (g1) D = Device Identifier Graphic
       (g2) 1 = Device Status (TimeOutCardHolderTakingCard)
       (g3) 0 = Error Severity (NoError)
       (g4) 0 = Diagnostic Status
       (g5) 1 = Supplies Status (NoOverfillCondition)


       Message with type Send Status builded:
       2200100355B4321
       Description:
       (b) 2 = Message class
       (c) 2 = Message sub-class
       (d) 00100355 = LUNO
       Status data
       (f) B = Status Descriptor (TransactionReplyReady)
       Status Information
       (g1) 4321 = Transaction number


       Message with type Get Fitness Data builded:
       2200100355FJAD01y1A0E00000G0L0w00040003000200010H0
       Description:
       (b) 2 = Message class
       (c) 2 = Message sub-class
       (d) 00100355 = LUNO
       MagneticCardReader RoutineErrorsHaveOccurred,SecondaryCardReader RoutineErrorsHaveOccurred,TimeOfDayClock NoError,CashHandler NoError,ReceiptPrinter NoError,Encryptor NoError,BunchNoteAcceptor NoError,JournalPrinter NoError
       (f) F = Status Descriptor (TerminalState)
       Status Information
       (g1) J = Message Identifier (FitnessData)
       (g2) A = Hardware Fitness Identifier
       (g2) D = Device Identifier Graphic MagneticCardReader
       (g2) 01 = Fitness - RoutineErrorsHaveOccurred
       (g2) y = Device Identifier Graphic SecondaryCardReader
       (g2) 1 = Fitness - RoutineErrorsHaveOccurred
       (g2) A = Device Identifier Graphic TimeOfDayClock
       (g2) 0 = Fitness - NoError
       (g2) E = Device Identifier Graphic CashHandler
       (g2) 00000 = Fitness - NoError
       (g2) G = Device Identifier Graphic ReceiptPrinter
       (g2) 0 = Fitness - NoError
       (g2) L = Device Identifier Graphic Encryptor
       (g2) 0 = Fitness - NoError
       (g2) w = Device Identifier Graphic BunchNoteAcceptor
       (g2) 00040003000200010 = Fitness - NoError
       (g2) H = Device Identifier Graphic JournalPrinter
       (g2) 0 = Fitness - NoError

     */