using System.Collections.Concurrent;

namespace CafeBot.TelegramBot.States;

public class UserStateManager : IUserStateManager
{
    private readonly ConcurrentDictionary<long, UserState> _states = new();
    private readonly ConcurrentDictionary<long, UserStateData> _data = new();

    public UserState GetState(long userId)
    {
        return _states.GetOrAdd(userId, UserState.None);
    }

    public void SetState(long userId, UserState state)
    {
        _states[userId] = state;
    }

    public UserStateData GetStateData(long userId) // Переименовано GetData в GetStateData
    {
        return _data.GetOrAdd(userId, _ => new UserStateData());
    }

    public void SetData(long userId, UserStateData data)
    {
        _data[userId] = data;
    }

    public void ClearState(long userId)
    {
        _states.TryRemove(userId, out _);
        _data.TryRemove(userId, out _);
    }

    public void ClearStateData(long userId) // Добавлен новый метод
    {
        _data.TryRemove(userId, out _);
    }
}