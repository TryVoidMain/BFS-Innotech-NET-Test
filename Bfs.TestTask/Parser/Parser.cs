using System.Text;
using System.Threading.Channels;

namespace Bfs.TestTask.Parser;

public class Parser : IParser
{
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
        buffer = buffer.Take(buffer.Length).Concat(addBytes).ToArray();
    }

    private string ExtractMessageFromBuffer(ref byte[] bufferBytes, int messageLength)
    {
        var messageBytes = bufferBytes.Skip(2).Take(messageLength).ToArray();
        bufferBytes = bufferBytes.Skip(messageLength + 2).Take(bufferBytes.Length - messageLength - 2).ToArray();
        return Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length);
    }

    private IMessage ProcessMessage(string message)
    {
        //Process string message and return IMessage implementation

        return null;
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