using System.Runtime.CompilerServices;

namespace Bfs.TestTask.Driver;

public class CardDriverMock : ICardDriverMock
{
    private CardData? _currentCardData;
    private EjectResult _currentState;

    public async Task<CardData?> ReadCard(CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            // Delay for imitation of read-process
            await Task.Delay(1000);
            return _currentCardData;
        }

        return null;
    }

    public async IAsyncEnumerable<EjectResult> EjectCard([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _currentState = EjectResult.Ejected;
        yield return _currentState;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_currentState == EjectResult.CardTaken)
            {
                yield return EjectResult.CardTaken;
            }
        }

        if (_currentState != EjectResult.CardTaken)
        {
            _currentState = EjectResult.Retracted;
            yield return _currentState;
        }
    }

    public void SetCardData(CardData cardData)
    {
        _currentCardData = cardData;
    }

    public void CantReadCard()
    {
        _currentCardData = null;
    }

    public void TakeCard()
    {
        _currentState = EjectResult.CardTaken;
        _currentCardData = null;
    }
}